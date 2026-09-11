using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Down2Jam.Client;

/// <summary>
/// Holds the player's Down2Jam game token and keeps it usable.
///
/// Down2Jam issues a single long lived token once a player links their account through the
/// device flow (see <see cref="D2JamService.LinkDeviceAsync"/>). It is sent as a bearer token on
/// every authenticated call and never expires on its own -- the player can revoke it from the
/// website's Settings page, or the game can drop it itself with
/// <see cref="D2JamService.LogoutAsync"/>. There is nothing to refresh and nothing to rotate.
///
/// Drive this through <see cref="D2JamService"/> rather than directly.
///
/// <b>On storage:</b> with <see cref="Persist"/> enabled the token is written to
/// <see cref="StoragePath"/> on the player's machine. Anyone who can read that file can act as
/// the player on Down2Jam until the token is revoked, so set <see cref="EncryptionKey"/> in a
/// released build. Nothing else -- no password, no username -- is ever stored, because the
/// device flow never puts the player's password in the game's hands.
/// </summary>
public sealed class D2JamAuth
{
    /// <summary>Raised whenever the stored session changes: a link, a disconnect, or a restore.</summary>
    public event Action? SessionChanged;

    /// <summary>Raised when the API rejects the stored token. The player has to link their account again.</summary>
    public event Action? SessionExpired;

    /// <summary>Keep the player linked between runs by storing their token.</summary>
    public bool Persist { get; set; } = true;

    /// <summary>Where the session is stored on disk.</summary>
    public string StoragePath { get; set; } = DefaultStoragePath();

    /// <summary>Encrypts the stored session. Leave empty during development; set it for a release build.</summary>
    public string EncryptionKey { get; set; } = "";

    /// <summary>The current game token. Set once by <see cref="Adopt"/> after a successful device link.</summary>
    public string GameToken { get; private set; } = "";

    /// <summary>The logged in user, as returned by fetching the profile after a link. Null when disconnected.</summary>
    public D2JamUser? User { get; private set; }

    /// <summary>True when there is a token to authenticate with.</summary>
    public bool IsLoggedIn => GameToken.Length > 0;

    /// <summary>The slug of the logged in user, or an empty string.</summary>
    public string UserSlug => User?.Slug ?? "";

    /// <summary>
    /// Headers that authenticate a request. A game token is a single bearer credential --
    /// unlike a browser session there is no cookie to carry and nothing that rotates.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> AuthorizationHeaders()
    {
        if (!IsLoggedIn)
        {
            yield break;
        }

        yield return new KeyValuePair<string, string>("Authorization", $"Bearer {GameToken}");
    }

    /// <summary>
    /// Store the token and profile returned by a successful device link.
    ///
    /// <paramref name="user"/> is optional so tests and low level callers can adopt a bare token,
    /// but <see cref="D2JamService"/> always supplies it: <c>GET /self</c> does not accept a game
    /// token, so the approved poll response is the only place a profile is ever handed to the
    /// game, and it has to be kept here to survive a restart. See <see cref="LoadSession"/>.
    /// </summary>
    public void Adopt(string token, D2JamUser? user = null)
    {
        GameToken = token;
        if (user is not null)
        {
            User = user;
        }

        if (Persist)
        {
            SaveSession();
        }

        SessionChanged?.Invoke();
    }

    /// <summary>Drop the session because the API rejected it.</summary>
    public void Invalidate()
    {
        if (!IsLoggedIn)
        {
            return;
        }

        Clear();
        SessionExpired?.Invoke();
    }

    /// <summary>Drop the session and forget anything stored on disk.</summary>
    public void Clear()
    {
        GameToken = "";
        User = null;

        if (Persist && File.Exists(StoragePath))
        {
            File.Delete(StoragePath);
        }

        SessionChanged?.Invoke();
    }

    /// <summary>Write the session to <see cref="StoragePath"/>. Called automatically when <see cref="Persist"/> is on.</summary>
    public void SaveSession()
    {
        if (!IsLoggedIn)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new StoredSession
        {
            Version = 3,
            GameToken = GameToken,
            User = User,
        });

        try
        {
            var directory = Path.GetDirectoryName(StoragePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(StoragePath, Protect(payload));
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine($"[d2jam] Could not write the session to {StoragePath}: {exception.Message}");
        }
    }

    /// <summary>Read a previously saved session. Returns true when one was restored.</summary>
    public bool LoadSession()
    {
        if (!File.Exists(StoragePath))
        {
            return false;
        }

        string raw;
        try
        {
            raw = Unprotect(File.ReadAllBytes(StoragePath));
        }
        catch (CryptographicException)
        {
            // A wrong or missing encryption key lands here. Treat it as "no session" rather than
            // an error the player cannot act on.
            return false;
        }
        catch (IOException)
        {
            return false;
        }

        StoredSession? stored;
        try
        {
            stored = JsonSerializer.Deserialize<StoredSession>(raw, D2JamJson.Options);
        }
        catch (JsonException)
        {
            return false;
        }

        if (stored is null || stored.GameToken.Length == 0)
        {
            return false;
        }

        GameToken = stored.GameToken;
        User = stored.User;

        SessionChanged?.Invoke();
        return true;
    }

    private static string DefaultStoragePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Down2Jam", "session.json");

    private byte[] Protect(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        if (EncryptionKey.Length == 0)
        {
            return plainBytes;
        }

        using var aes = new AesGcm(DeriveKey(EncryptionKey), AesGcm.TagByteSizes.MaxSize);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // nonce || tag || ciphertext, all fixed or self-describing length.
        var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(combined, 0);
        tag.CopyTo(combined, nonce.Length);
        ciphertext.CopyTo(combined, nonce.Length + tag.Length);
        return combined;
    }

    private string Unprotect(byte[] stored)
    {
        if (EncryptionKey.Length == 0)
        {
            return Encoding.UTF8.GetString(stored);
        }

        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        if (stored.Length < nonceSize + tagSize)
        {
            throw new CryptographicException("Stored session is too short to be valid.");
        }

        var nonce = stored[..nonceSize];
        var tag = stored[nonceSize..(nonceSize + tagSize)];
        var ciphertext = stored[(nonceSize + tagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(DeriveKey(EncryptionKey), tagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    // A 32 byte key for AES-256-GCM, derived from a passphrase the same way regardless of length.
    private static byte[] DeriveKey(string passphrase) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(passphrase), "d2jam-session"u8.ToArray(), 100_000, HashAlgorithmName.SHA256, 32);

    private sealed class StoredSession
    {
        public int Version { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("game_token")]
        public string GameToken { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("user")]
        public D2JamUser? User { get; set; }
    }
}
