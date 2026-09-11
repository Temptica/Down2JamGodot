namespace Down2Jam.Client;

/// <summary>
/// Achievement helper: unlock by name, and ask what the player has already earned.
///
/// Achievements are defined by the developer on the game's page on d2jam.com, each with a name,
/// a description and an icon. A game unlocks them for the logged in player; it cannot create
/// them. As with leaderboards there is no dedicated read endpoint, so the list arrives as part
/// of the game and is cached here.
///
/// <code>
/// void OnBossDefeated() => _ = achievements.UnlockAsync("First Blood");
///
/// async void Ready()
/// {
///     if (await achievements.IsUnlockedAsync("First Blood"))
///         ShowVeteranIntro();
/// }
/// </code>
/// </summary>
public sealed class D2JamAchievements
{
    /// <summary>Raised after an achievement is unlocked for the player. Good place to pop a toast.</summary>
    public event Action<D2JamAchievement>? Unlocked;

    /// <summary>Raised when an unlock fails, with a message safe to show the player.</summary>
    public event Action<string>? UnlockFailed;

    /// <summary>Slug of the game whose achievements these are, as it appears in the d2jam.com URL.</summary>
    public string GameSlug { get; set; } = "";

    private readonly D2JamApi _api;
    private readonly Func<bool, Task<D2JamGame?>>? _gameProvider;
    private D2JamGame? _cachedGame;
    private readonly HashSet<int> _unlockedThisSession = [];

    /// <param name="api">The generated client to call through.</param>
    /// <param name="gameProvider">
    /// Supplies the game and caches it, shared with <see cref="D2JamLeaderboards"/> so both cost
    /// one fetch. When null this fetches on its own.
    /// </param>
    public D2JamAchievements(D2JamApi api, Func<bool, Task<D2JamGame?>>? gameProvider = null)
    {
        _api = api;
        _gameProvider = gameProvider;
    }

    /// <summary>Every achievement on the game page. Cached after the first call.</summary>
    public async Task<List<D2JamAchievement>> AllAsync(bool forceRefresh = false)
    {
        var game = await GameAsync(forceRefresh);
        return game?.Achievements ?? [];
    }

    /// <summary>Find an achievement by name, case-insensitively. Returns null when there is no such achievement.</summary>
    public async Task<D2JamAchievement?> FindAsync(string achievementName)
    {
        var wanted = achievementName.Trim();

        foreach (var achievement in await AllAsync())
        {
            if (achievement.Name.Trim().Equals(wanted, StringComparison.OrdinalIgnoreCase))
            {
                return achievement;
            }
        }

        return null;
    }

    /// <summary>
    /// Unlock an achievement for the logged in player.
    ///
    /// Safe to call repeatedly: an achievement already unlocked in this session is skipped
    /// without a request, and the API itself treats a repeat unlock as a no-op. Returns true when
    /// the player holds the achievement afterwards.
    /// </summary>
    public async Task<bool> UnlockAsync(string achievementName)
    {
        var achievement = await FindAsync(achievementName);

        if (achievement is null)
        {
            var message = $"No achievement named '{achievementName}' on {GameSlug}. Create it on the game's page first.";
            UnlockFailed?.Invoke(message);
            return false;
        }

        return await UnlockAchievementAsync(achievement);
    }

    /// <summary>Unlock an achievement you already hold, skipping the name lookup.</summary>
    public async Task<bool> UnlockAchievementAsync(D2JamAchievement achievement)
    {
        if (_unlockedThisSession.Contains(achievement.Id))
        {
            return true;
        }

        var result = await _api.UnlockAchievementAsync(new D2JamAchievementBody { AchievementId = achievement.Id });

        if (!result.Ok)
        {
            _api.Report(result);
            UnlockFailed?.Invoke(result.ErrorMessage);
            return false;
        }

        _unlockedThisSession.Add(achievement.Id);
        _cachedGame = null;
        Unlocked?.Invoke(achievement);
        return true;
    }

    /// <summary>Take an achievement back off the player. Mostly useful while testing.</summary>
    public async Task<bool> RevokeAsync(string achievementName)
    {
        var achievement = await FindAsync(achievementName);
        if (achievement is null)
        {
            return false;
        }

        var result = await _api.LockAchievementAsync(new D2JamAchievementBody { AchievementId = achievement.Id });

        if (!result.Ok)
        {
            _api.Report(result);
            return false;
        }

        _unlockedThisSession.Remove(achievement.Id);
        _cachedGame = null;
        return true;
    }

    /// <summary>True when the given user holds the achievement. Defaults to the logged in player.</summary>
    public async Task<bool> IsUnlockedAsync(string achievementName, string userSlug = "")
    {
        var achievement = await FindAsync(achievementName);
        if (achievement is null)
        {
            return false;
        }

        if (userSlug.Length == 0 && _unlockedThisSession.Contains(achievement.Id))
        {
            return true;
        }

        return Holds(achievement, ResolveSlug(userSlug));
    }

    /// <summary>Every achievement the given user holds. Defaults to the logged in player.</summary>
    public async Task<List<D2JamAchievement>> UnlockedByAsync(string userSlug = "")
    {
        var slug = ResolveSlug(userSlug);
        if (slug.Length == 0)
        {
            return [];
        }

        var held = new List<D2JamAchievement>();
        foreach (var achievement in await AllAsync())
        {
            if (Holds(achievement, slug))
            {
                held.Add(achievement);
            }
        }

        return held;
    }

    /// <summary>
    /// Fraction of the game's achievements the user holds, from 0.0 to 1.0. Defaults to the
    /// logged in player. Returns 0.0 when the game has no achievements.
    /// </summary>
    public async Task<double> CompletionAsync(string userSlug = "")
    {
        var total = (await AllAsync()).Count;
        if (total == 0)
        {
            return 0.0;
        }

        return (await UnlockedByAsync(userSlug)).Count / (double)total;
    }

    /// <summary>Drop the cached game so the next read hits the API.</summary>
    public void InvalidateCache() => _cachedGame = null;

    private static bool Holds(D2JamAchievement achievement, string slug) =>
        slug.Length > 0 && achievement.Users.Any(holder => holder.Slug == slug);

    private string ResolveSlug(string userSlug) => userSlug.Length > 0 ? userSlug : _api.Auth?.UserSlug ?? "";

    private async Task<D2JamGame?> GameAsync(bool forceRefresh = false)
    {
        if (_cachedGame is not null && !forceRefresh)
        {
            return _cachedGame;
        }

        if (_gameProvider is not null)
        {
            _cachedGame = await _gameProvider(forceRefresh);
            return _cachedGame;
        }

        var result = await _api.GetGameAsync(GameSlug);
        if (!result.Ok)
        {
            _api.Report(result);
            return null;
        }

        _cachedGame = result.Data;
        return _cachedGame;
    }
}
