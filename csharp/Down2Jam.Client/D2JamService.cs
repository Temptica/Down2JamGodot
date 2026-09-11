using System.Diagnostics;

namespace Down2Jam.Client;

/// <summary>
/// The one object to construct in your game. Wires up the API client, the session, and the
/// leaderboard and achievement helpers, and gives you a single place to link the player's
/// account.
///
/// Construct one with your game's slug on d2jam.com and hold onto it (a static field on your own
/// game-services singleton is the usual place; there is no autoload magic here, this is a plain
/// object):
///
/// <code>
/// var d2jam = new D2JamService("my-game-slug");
/// d2jam.DeviceLinkStarted += (userCode, verificationUri) => ShowCodeScreen(userCode);
///
/// if (await d2jam.LinkDeviceAsync())
/// {
///     await d2jam.Achievements.UnlockAsync("First Blood");
///     await d2jam.Leaderboards.SubmitAsync("High Score", 16340);
/// }
/// </code>
///
/// Only a linked player can submit scores or unlock achievements, and everything happens as that
/// player. There is nothing to configure per game beyond the slug: no API key, no client id, and
/// no password ever passes through the game.
/// </summary>
public sealed class D2JamService : IDisposable
{
    /// <summary>Raised after a successful device link or a restored session, with the player's profile.</summary>
    public event Action<D2JamUser>? LoggedIn;

    /// <summary>Raised after <see cref="LogoutAsync"/>.</summary>
    public event Action? LoggedOut;

    /// <summary>
    /// Raised once <see cref="LinkDeviceAsync"/> has a code to show, before it starts polling.
    /// Display the user code to the player; the verification URI opens in their browser
    /// automatically unless <see cref="AutoOpenBrowser"/> is off.
    /// </summary>
    public event Action<string, string>? DeviceLinkStarted;

    /// <summary>
    /// Raised when a device link attempt is denied, expires, or cannot be started, with a message
    /// safe to show the player.
    /// </summary>
    public event Action<string>? LinkFailed;

    /// <summary>Raised when a stored session stops being accepted. Prompt for a device link again.</summary>
    public event Action? SessionExpired;

    /// <summary>Raised for every failed API call. Convenient for one central error toast.</summary>
    public event Action<D2JamResult>? RequestFailed;

    private string _gameSlug = "";

    /// <summary>
    /// Your game's slug, the last part of its d2jam.com URL. For
    /// <c>https://d2jam.com/g/weldroot</c> that is <c>weldroot</c>.
    /// </summary>
    public string GameSlug
    {
        get => _gameSlug;
        set
        {
            _gameSlug = value;
            Leaderboards.GameSlug = value;
            Achievements.GameSlug = value;
        }
    }

    /// <summary>Base URL of the API. Point it at a local Jamcore instance to test against one.</summary>
    public string ApiHost
    {
        get => Api.ApiHost;
        set => Api.ApiHost = value;
    }

    /// <summary>Open the verification URI in the player's default browser automatically once <see cref="LinkDeviceAsync"/> gets a code.</summary>
    public bool AutoOpenBrowser { get; set; } = true;

    /// <summary>A name for this device or install, shown to the player on the website's approval page. Defaults to the entry assembly's name.</summary>
    public string ClientName { get; set; } = "";

    /// <summary>The generated REST client. Use it directly for anything the helpers do not cover.</summary>
    public D2JamApi Api { get; }

    /// <summary>The player's session.</summary>
    public D2JamAuth Auth { get; }

    /// <summary>Leaderboard submission and ranking.</summary>
    public D2JamLeaderboards Leaderboards { get; }

    /// <summary>Achievement unlocking and progress.</summary>
    public D2JamAchievements Achievements { get; }

    private D2JamGame? _game;
    private Task<D2JamGame?>? _fetchInFlight;

    public D2JamService(string gameSlug = "", string apiHost = D2JamApiBase.DefaultHost)
    {
        Auth = new D2JamAuth();
        Auth.SessionExpired += OnSessionExpired;

        Api = new D2JamApi { ApiHost = apiHost, Auth = Auth };
        Api.RequestFailed += result => RequestFailed?.Invoke(result);

        Leaderboards = new D2JamLeaderboards(Api, FetchGameAsync) { GameSlug = gameSlug };
        Achievements = new D2JamAchievements(Api, FetchGameAsync) { GameSlug = gameSlug };

        GameSlug = gameSlug;
    }

    /// <summary>True when a player is logged in.</summary>
    public bool IsLoggedIn => Auth.IsLoggedIn;

    /// <summary>The logged in player, or null.</summary>
    public D2JamUser? CurrentUser => Auth.User;

    /// <summary>
    /// Restore a session stored by a previous run, and load it from disk first if it has not
    /// been loaded yet. Call this once at startup before deciding whether to show the link-device
    /// screen.
    ///
    /// The token and profile were both saved at link time (see <see cref="D2JamAuth"/>), so this
    /// needs no network call. If the token was revoked from the website while the game was
    /// closed, that surfaces the normal way, on the first call that actually needs it: a 401
    /// clears the session and raises <see cref="SessionExpired"/>.
    ///
    /// Returns true when the player is logged in afterwards. A false means "show the link-device
    /// screen".
    /// </summary>
    public bool RestoreSession()
    {
        if (!Auth.IsLoggedIn)
        {
            Auth.LoadSession();
        }

        if (!Auth.IsLoggedIn)
        {
            return false;
        }

        LoggedIn?.Invoke(Auth.User!);
        return true;
    }

    /// <summary>
    /// Link the player's Down2Jam account through the device flow.
    ///
    /// Starts a device authorization request, raises <see cref="DeviceLinkStarted"/> with a short
    /// code and a link to open in the player's browser (opened automatically unless
    /// <see cref="AutoOpenBrowser"/> is off), then polls until the player approves or denies it
    /// there. No password ever reaches the game; the player signs in on the website, in their own
    /// browser, with their own session.
    ///
    /// The issued game token is scoped to <see cref="GameSlug"/> and only ever works for that one
    /// game -- it cannot submit scores or achievements anywhere else, even for a player who owns
    /// several games.
    ///
    /// Returns true once the player has approved the request and the game token is stored.
    /// </summary>
    public async Task<bool> LinkDeviceAsync(CancellationToken cancellationToken = default)
    {
        if (GameSlug.Length == 0)
        {
            LinkFailed?.Invoke("This game is not configured correctly. Tell the developer.");
            return false;
        }

        var name = ClientName.Length > 0 ? ClientName : DefaultClientName();

        var codeResult = await Api.StartDeviceLinkAsync(new D2JamDeviceCodeBody
        {
            ClientName = name,
            GameSlug = GameSlug,
        });

        if (!codeResult.Ok)
        {
            LinkFailed?.Invoke(codeResult.ErrorMessage.Length > 0 ? codeResult.ErrorMessage : "Could not reach Down2Jam.");
            return false;
        }

        var code = codeResult.Data!;
        DeviceLinkStarted?.Invoke(code.UserCode, code.VerificationUri);

        if (AutoOpenBrowser)
        {
            OpenBrowser(code.VerificationUri);
        }

        return await PollDeviceLinkAsync(code, cancellationToken);
    }

    /// <summary>
    /// Disconnect the account: revoke the game token on the server, then forget it locally.
    ///
    /// The token stops working immediately either way, so it is safe to call even if the server
    /// request fails -- for example because it was already revoked from the website.
    /// </summary>
    public async Task LogoutAsync()
    {
        if (Auth.IsLoggedIn)
        {
            await Api.RevokeCurrentGameTokenAsync();
        }

        Auth.Clear();
        _game = null;
        Leaderboards.InvalidateCache();
        Achievements.InvalidateCache();
        LoggedOut?.Invoke();
    }

    /// <summary>
    /// Fetch the game, with its leaderboards and achievements, and cache it.
    ///
    /// Both helpers read through this, so a screen showing leaderboards and achievements together
    /// costs one request rather than two. Overlapping calls share the in-flight request.
    /// </summary>
    public async Task<D2JamGame?> FetchGameAsync(bool forceRefresh = false)
    {
        if (_game is not null && !forceRefresh)
        {
            return _game;
        }

        if (_fetchInFlight is not null)
        {
            return await _fetchInFlight;
        }

        if (GameSlug.Length == 0)
        {
            Console.Error.WriteLine("[d2jam] D2JamService.GameSlug is not set; nothing to fetch.");
            return null;
        }

        var fetch = FetchGameCoreAsync();
        _fetchInFlight = fetch;

        try
        {
            return await fetch;
        }
        finally
        {
            _fetchInFlight = null;
        }
    }

    private async Task<D2JamGame?> FetchGameCoreAsync()
    {
        var result = await Api.GetGameAsync(GameSlug);

        if (result.Ok)
        {
            _game = result.Data;
        }
        else
        {
            Api.Report(result);
        }

        return _game;
    }

    /// <summary>
    /// The page a player should be shown for the cached game: the post-jam page when the team
    /// published one, otherwise the jam page.
    ///
    /// Worth going through, because <c>GET /games/{gameSlug}</c> does not flatten the name,
    /// description and artwork onto the game the way the list endpoints do -- they live on the
    /// page.
    /// </summary>
    public async Task<D2JamGamePage?> PageAsync(bool forceRefresh = false)
    {
        var game = await FetchGameAsync(forceRefresh);
        if (game is null)
        {
            return null;
        }

        return game.PostJamPage ?? game.JamPage ?? game.Pages.FirstOrDefault();
    }

    /// <summary>The game's display name, or an empty string when it cannot be fetched.</summary>
    public async Task<string> GameNameAsync() => (await PageAsync())?.Name ?? "";

    /// <summary>Forget the cached game so the next read hits the API.</summary>
    public void InvalidateCache()
    {
        _game = null;
        Leaderboards.InvalidateCache();
        Achievements.InvalidateCache();
    }

    private static string DefaultClientName()
    {
        var name = AppDomain.CurrentDomain.FriendlyName;
        return string.IsNullOrWhiteSpace(name) ? "Godot Game" : name;
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine($"[d2jam] Could not open the browser: {exception.Message}");
        }
    }

    /// <summary>
    /// Poll a pending device authorization request until it resolves, backing off on slow_down
    /// per RFC 8628 (increase the interval and keep it there, rather than resetting after each
    /// backoff).
    /// </summary>
    private async Task<bool> PollDeviceLinkAsync(D2JamDeviceCode code, CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(code.Interval, 1));
        var deadline = DateTime.UtcNow.AddSeconds(code.ExpiresIn);

        while (true)
        {
            if (DateTime.UtcNow >= deadline)
            {
                LinkFailed?.Invoke("The device link request expired before it was approved.");
                return false;
            }

            await Task.Delay(interval, cancellationToken);

            var pollResult = await Api.PollDeviceLinkAsync(new D2JamDeviceTokenBody { DeviceCode = code.DeviceCode });

            if (!pollResult.Ok)
            {
                LinkFailed?.Invoke(pollResult.ErrorMessage.Length > 0
                    ? pollResult.ErrorMessage
                    : "The device link request was not approved.");
                return false;
            }

            switch (pollResult.Data!.Status)
            {
                case "approved":
                    // GET /self does not accept a game token, so the approved poll response
                    // carries the player's profile directly rather than needing a second,
                    // doomed request for it.
                    Auth.Adopt(pollResult.Data.Token, pollResult.Data.User);
                    LoggedIn?.Invoke(Auth.User!);
                    return true;

                case "slow_down":
                    interval += TimeSpan.FromSeconds(Math.Max(code.Interval, 1));
                    break;
            }
        }
    }

    private void OnSessionExpired()
    {
        _game = null;
        SessionExpired?.Invoke();
    }

    public void Dispose() => Api.Dispose();
}
