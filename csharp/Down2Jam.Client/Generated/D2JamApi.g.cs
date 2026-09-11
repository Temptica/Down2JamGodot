#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// Typed client for the Jamcore API (1.0.0). Every method is asynchronous. Transport,
/// authentication and error handling live in the hand written D2JamApiBase, so this class only
/// describes endpoints.
/// </summary>
public sealed partial class D2JamApi : D2JamApiBase
{
    /// <summary>
    /// Submit a score to a leaderboard as the logged in user.
    ///
    /// The server responds with a message only, no score object. Prefer D2JamLeaderboards.submit(),
    /// which converts the value for the board type and can attach a screenshot as evidence.
    ///
    /// POST /score - Requires user login
    /// </summary>
    public async Task<D2JamResult> CreateScoreAsync(D2JamCreateScoreBody body)
    {
        var path = "/score";
        var response = await RequestAsync(path, HttpMethod.Post, null, body, true);
        return D2JamResult.From(response);
    }

    /// <summary>
    /// Delete a score. Allowed for the score's owner, the game's team members and moderators.
    ///
    /// DELETE /score - Requires user login
    /// </summary>
    public async Task<D2JamResult> DeleteScoreAsync(D2JamDeleteScoreBody body)
    {
        var path = "/score";
        var response = await RequestAsync(path, HttpMethod.Delete, null, body, true);
        return D2JamResult.From(response);
    }

    /// <summary>
    /// Return one game with its leaderboards (including every score) and achievements (including
    /// who unlocked them). This is the read path for both features.
    ///
    /// GET /games/{gameSlug} - Uses login if present
    /// </summary>
    public async Task<D2JamResult<D2JamGame>> GetGameAsync(string gameSlug, D2JamGetGameOptions? options = null)
    {
        var path = $"/games/{Uri.EscapeDataString(gameSlug.ToString()!)}";
        var query = new Dictionary<string, object?>();
        if (options != null)
        {
            if (options.Recap != null) query["recap"] = options.Recap;
            if (options.Preview != null) query["preview"] = options.Preview;
        }
        var response = await RequestAsync(path, HttpMethod.Get, query, null, false);
        return D2JamResult<D2JamGame>.From(response, payload => JsonSerializer.Deserialize<D2JamGame>(payload, D2JamJson.Options)!);
    }

    /// <summary>
    /// Return the logged in user's own game entries for the active jam. Useful for resolving your
    /// own game slug at runtime instead of hardcoding it.
    ///
    /// GET /self/current-game - Requires user login
    /// </summary>
    public async Task<D2JamResult<List<D2JamGame>>> GetOwnCurrentGamesAsync()
    {
        var path = "/self/current-game";
        var response = await RequestAsync(path, HttpMethod.Get, null, null, true);
        return D2JamResult<List<D2JamGame>>.From(response, payload => JsonSerializer.Deserialize<List<D2JamGame>>(payload, D2JamJson.Options) ?? []);
    }

    /// <summary>
    /// Return a random published game.
    ///
    /// GET /games/random - Uses login if present
    /// </summary>
    public async Task<D2JamResult<D2JamGame>> GetRandomGameAsync()
    {
        var path = "/games/random";
        var response = await RequestAsync(path, HttpMethod.Get, null, null, false);
        return D2JamResult<D2JamGame>.From(response, payload => JsonSerializer.Deserialize<D2JamGame>(payload, D2JamJson.Options)!);
    }

    /// <summary>
    /// Return the profile of the logged in user.
    ///
    /// GET /self - Requires user login
    /// </summary>
    public async Task<D2JamResult<D2JamUser>> GetSelfAsync()
    {
        var path = "/self";
        var response = await RequestAsync(path, HttpMethod.Get, null, null, true);
        return D2JamResult<D2JamUser>.From(response, payload => JsonSerializer.Deserialize<D2JamUser>(payload, D2JamJson.Options)!);
    }

    /// <summary>
    /// Return a user's public profile.
    ///
    /// GET /users/{userSlug} - Public
    /// </summary>
    public async Task<D2JamResult<D2JamUser>> GetUserAsync(string userSlug)
    {
        var path = $"/users/{Uri.EscapeDataString(userSlug.ToString()!)}";
        var response = await RequestAsync(path, HttpMethod.Get, null, null, false);
        return D2JamResult<D2JamUser>.From(response, payload => JsonSerializer.Deserialize<D2JamUser>(payload, D2JamJson.Options)!);
    }

    /// <summary>
    /// List published games. Leaderboards and achievements are not included here; fetch a single
    /// game for those.
    ///
    /// GET /games - Public
    /// </summary>
    public async Task<D2JamResult<List<D2JamGame>>> ListGamesAsync(D2JamListGamesOptions? options = null)
    {
        var path = "/games";
        var query = new Dictionary<string, object?>();
        if (options != null)
        {
            if (options.Sort != null) query["sort"] = options.Sort;
            if (options.JamSlug != null) query["jamSlug"] = options.JamSlug;
            if (options.JamId != null) query["jamId"] = options.JamId;
            if (options.PageVersion != null) query["pageVersion"] = options.PageVersion;
            if (options.Cursor != null) query["cursor"] = options.Cursor;
            if (options.Limit != null) query["limit"] = options.Limit;
        }
        var response = await RequestAsync(path, HttpMethod.Get, query, null, false);
        return D2JamResult<List<D2JamGame>>.From(response, payload => JsonSerializer.Deserialize<List<D2JamGame>>(payload, D2JamJson.Options) ?? []);
    }

    /// <summary>
    /// List users.
    ///
    /// GET /users - Public
    /// </summary>
    public async Task<D2JamResult<List<D2JamUser>>> ListUsersAsync(D2JamListUsersOptions? options = null)
    {
        var path = "/users";
        var query = new Dictionary<string, object?>();
        if (options != null)
        {
            if (options.Cursor != null) query["cursor"] = options.Cursor;
            if (options.Limit != null) query["limit"] = options.Limit;
        }
        var response = await RequestAsync(path, HttpMethod.Get, query, null, false);
        return D2JamResult<List<D2JamUser>>.From(response, payload => JsonSerializer.Deserialize<List<D2JamUser>>(payload, D2JamJson.Options) ?? []);
    }

    /// <summary>
    /// Revoke an achievement from the logged in user.
    ///
    /// DELETE /achievement - Requires user login
    /// </summary>
    public async Task<D2JamResult> LockAchievementAsync(D2JamAchievementBody body)
    {
        var path = "/achievement";
        var response = await RequestAsync(path, HttpMethod.Delete, null, body, true);
        return D2JamResult.From(response);
    }

    /// <summary>
    /// Poll a pending device authorization request. Returns authorization_pending or slow_down
    /// while waiting, or the game's access token once approved. A denied or expired request comes
    /// back as a failed result rather than a status field.
    ///
    /// POST /device/token - Public
    /// </summary>
    public async Task<D2JamResult<D2JamDeviceToken>> PollDeviceLinkAsync(D2JamDeviceTokenBody body)
    {
        var path = "/device/token";
        var response = await RequestAsync(path, HttpMethod.Post, null, body, false);
        return D2JamResult<D2JamDeviceToken>.From(response, payload => JsonSerializer.Deserialize<D2JamDeviceToken>(payload, D2JamJson.Options)!);
    }

    /// <summary>
    /// Revoke the game token used to authenticate this request. Prefer D2JamAuth.disconnect(),
    /// which also clears the stored session locally.
    ///
    /// DELETE /self/game-tokens/current - Requires user login or game token
    /// </summary>
    public async Task<D2JamResult> RevokeCurrentGameTokenAsync()
    {
        var path = "/self/game-tokens/current";
        var response = await RequestAsync(path, HttpMethod.Delete, null, null, true);
        return D2JamResult.From(response);
    }

    /// <summary>
    /// Begin a device authorization request. Prefer D2JamAuth.start_device_link(), which drives the
    /// whole flow: showing the user_code, opening verification_uri, and polling poll_device_link()
    /// until the player approves or denies it on the website.
    ///
    /// POST /device/code - Public
    /// </summary>
    public async Task<D2JamResult<D2JamDeviceCode>> StartDeviceLinkAsync(D2JamDeviceCodeBody body)
    {
        var path = "/device/code";
        var response = await RequestAsync(path, HttpMethod.Post, null, body, false);
        return D2JamResult<D2JamDeviceCode>.From(response, payload => JsonSerializer.Deserialize<D2JamDeviceCode>(payload, D2JamJson.Options)!);
    }

    /// <summary>
    /// Unlock an achievement for the logged in user. Unlocking one that is already unlocked is
    /// harmless.
    ///
    /// POST /achievement - Requires user login
    /// </summary>
    public async Task<D2JamResult> UnlockAchievementAsync(D2JamAchievementBody body)
    {
        var path = "/achievement";
        var response = await RequestAsync(path, HttpMethod.Post, null, body, true);
        return D2JamResult.From(response);
    }

    /// <summary>
    /// Upload a PNG, JPEG, GIF or WebP image and return its path on the API.
    ///
    /// The returned value is relative (for example /api/v1/image/&lt;uuid&gt;.png); D2JamAPI resolves it
    /// to an absolute URL for you. Requires login, and the image must be at most 8 MiB.
    ///
    /// POST /image - Requires user login
    /// </summary>
    public async Task<D2JamResult<string>> UploadImageAsync(byte[] fileBytes, string fileName)
    {
        var path = "/image";
        var response = await RequestMultipartAsync(path, "upload", fileBytes, fileName, true);
        return D2JamResult<string>.From(response, payload => ResolveUrl(payload.GetString() ?? ""));
    }
}
