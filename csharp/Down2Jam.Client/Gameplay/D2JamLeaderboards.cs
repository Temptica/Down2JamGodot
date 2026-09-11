namespace Down2Jam.Client;

/// <summary>
/// Leaderboard helper: submit scores and read rankings without thinking about the wire format.
///
/// Down2Jam has no leaderboard endpoint of its own. Boards and their scores are returned as part
/// of the game, so this fetches the game once and caches it, and submits through <c>POST /score</c>.
/// Boards themselves are created by the developer on the game's page; a game can only submit to
/// boards that already exist.
///
/// Values are stored differently per board type, which is the main thing this class hides:
///
/// <code>
/// // A SCORE board: pass the number the player sees.
/// await leaderboards.SubmitAsync("High Score", 16340);
///
/// // A SPEEDRUN board: pass seconds and let this convert to milliseconds.
/// await leaderboards.SubmitTimeAsync("Fastest Time", 82.451);
///
/// // Read the ranking back.
/// foreach (var entry in await leaderboards.RankingAsync("High Score"))
///     Console.WriteLine($"{entry.User!.Name} {leaderboards.Format(board, entry)}");
/// </code>
/// </summary>
public sealed class D2JamLeaderboards
{
    /// <summary>Higher is better. The value is multiplied by 10 ^ DecimalPlaces before storage.</summary>
    public const string TypeScore = "SCORE";

    /// <summary>Lower is better, same scaling as <see cref="TypeScore"/>. Named after golf, where a low score wins.</summary>
    public const string TypeGolf = "GOLF";

    /// <summary>Lower is better. The value is a duration in milliseconds.</summary>
    public const string TypeSpeedrun = "SPEEDRUN";

    /// <summary>Higher is better. The value is a duration in milliseconds.</summary>
    public const string TypeEndurance = "ENDURANCE";

    /// <summary>Raised after a score is accepted.</summary>
    public event Action<D2JamLeaderboard, double>? ScoreSubmitted;

    /// <summary>Raised when a submission fails, with a message safe to show the player.</summary>
    public event Action<string>? SubmitFailed;

    /// <summary>Slug of the game whose boards these are, as it appears in the d2jam.com URL.</summary>
    public string GameSlug { get; set; } = "";

    private readonly D2JamApi _api;
    private readonly Func<bool, Task<D2JamGame?>>? _gameProvider;
    private D2JamGame? _cachedGame;

    /// <param name="api">The generated client to call through.</param>
    /// <param name="gameProvider">
    /// Supplies the game and caches it, shared with <see cref="D2JamAchievements"/> so both cost
    /// one fetch. When null this fetches on its own.
    /// </param>
    public D2JamLeaderboards(D2JamApi api, Func<bool, Task<D2JamGame?>>? gameProvider = null)
    {
        _api = api;
        _gameProvider = gameProvider;
    }

    /// <summary>Every leaderboard on the game page. Cached after the first call.</summary>
    public async Task<List<D2JamLeaderboard>> BoardsAsync(bool forceRefresh = false)
    {
        var game = await GameAsync(forceRefresh);
        return game?.Leaderboards ?? [];
    }

    /// <summary>Find a board by name, case-insensitively. Returns null when there is no such board.</summary>
    public async Task<D2JamLeaderboard?> FindAsync(string boardName)
    {
        var wanted = boardName.Trim().ToLowerInvariant();

        foreach (var board in await BoardsAsync())
        {
            if (board.Name.Trim().Equals(wanted, StringComparison.OrdinalIgnoreCase))
            {
                return board;
            }
        }

        return null;
    }

    /// <summary>
    /// Submit a score to the named board.
    ///
    /// <paramref name="value"/> is the number the player sees: points for a SCORE or GOLF board,
    /// and for a time board the milliseconds (use <see cref="SubmitTimeAsync"/> to pass seconds
    /// instead). <paramref name="evidenceUrl"/> is a screenshot URL; the website shows one next
    /// to every entry and expects submissions to carry one.
    ///
    /// Returns true when the score was accepted.
    /// </summary>
    public async Task<bool> SubmitAsync(string boardName, double value, string? evidenceUrl = null)
    {
        var board = await FindAsync(boardName);

        if (board is null)
        {
            var message = $"No leaderboard named '{boardName}' on {GameSlug}. Create it on the game's page first.";
            SubmitFailed?.Invoke(message);
            return false;
        }

        return await SubmitToAsync(board, value, evidenceUrl);
    }

    /// <summary>Submit to a board you already hold, skipping the name lookup.</summary>
    public async Task<bool> SubmitToAsync(D2JamLeaderboard board, double value, string? evidenceUrl = null)
    {
        var body = new D2JamCreateScoreBody
        {
            LeaderboardId = board.Id,
            Score = ToPayload(board, value),
        };

        if (!string.IsNullOrEmpty(evidenceUrl))
        {
            body.Evidence = evidenceUrl;
        }

        var result = await _api.CreateScoreAsync(body);

        if (!result.Ok)
        {
            _api.Report(result);
            SubmitFailed?.Invoke(result.ErrorMessage);
            return false;
        }

        // The submitted score is now stale in the cache.
        _cachedGame = null;
        ScoreSubmitted?.Invoke(board, value);
        return true;
    }

    /// <summary>Submit a duration in seconds to a SPEEDRUN or ENDURANCE board.</summary>
    public Task<bool> SubmitTimeAsync(string boardName, double seconds, string? evidenceUrl = null) =>
        SubmitAsync(boardName, seconds * 1000.0, evidenceUrl);

    /// <summary>
    /// The board's scores in ranked order, best first. Applies the board's <see cref="D2JamLeaderboard.OnlyBest"/>
    /// setting, so a player who submitted five times appears once with their best run, exactly as
    /// the website shows it.
    /// </summary>
    public async Task<List<D2JamScore>> RankingAsync(string boardName)
    {
        var board = await FindAsync(boardName);
        return board is null ? [] : Rank(board);
    }

    /// <summary>Rank the scores of a board you already hold.</summary>
    public List<D2JamScore> Rank(D2JamLeaderboard board)
    {
        var higherWins = HigherIsBetter(board);
        var entries = new List<D2JamScore>(board.Scores);
        entries.Sort((a, b) => higherWins ? b.Data.CompareTo(a.Data) : a.Data.CompareTo(b.Data));

        if (!board.OnlyBest)
        {
            return entries;
        }

        var seen = new HashSet<int>();
        var best = new List<D2JamScore>();

        foreach (var entry in entries)
        {
            if (!seen.Add(entry.UserId))
            {
                continue;
            }

            best.Add(entry);
        }

        return best;
    }

    /// <summary>The best entry a user holds on a board, or null. Defaults to the logged in player.</summary>
    public D2JamScore? BestFor(D2JamLeaderboard board, string userSlug = "")
    {
        var wanted = userSlug.Length > 0 ? userSlug : CurrentUserSlug();
        if (wanted.Length == 0)
        {
            return null;
        }

        return Rank(board).FirstOrDefault(entry => entry.User?.Slug == wanted);
    }

    /// <summary>One-based position of a user on a board, or -1 when they have no entry.</summary>
    public int PositionOf(D2JamLeaderboard board, string userSlug = "")
    {
        var wanted = userSlug.Length > 0 ? userSlug : CurrentUserSlug();
        if (wanted.Length == 0)
        {
            return -1;
        }

        var ranked = Rank(board);
        for (var index = 0; index < ranked.Count; index++)
        {
            if (ranked[index].User?.Slug == wanted)
            {
                return index + 1;
            }
        }

        return -1;
    }

    /// <summary>True when a larger stored value ranks higher on this board.</summary>
    public static bool HigherIsBetter(D2JamLeaderboard board) =>
        board.Type == TypeScore || board.Type == TypeEndurance;

    /// <summary>True when the board stores durations rather than points.</summary>
    public static bool IsTimeBased(D2JamLeaderboard board) =>
        board.Type == TypeSpeedrun || board.Type == TypeEndurance;

    /// <summary>
    /// Convert a player-facing value into what <c>POST /score</c> expects for this board.
    ///
    /// Time boards take whole milliseconds. Point boards take the value as the player reads it
    /// and are scaled server-side by 10 ^ DecimalPlaces, so a board with no decimal places gets a
    /// plain integer. Boards that do declare decimal places get a rounded value; the server
    /// multiplies it and stores an integer, so a value that does not land exactly on one can be
    /// rejected.
    /// </summary>
    public static object ToPayload(D2JamLeaderboard board, double value)
    {
        if (IsTimeBased(board) || board.DecimalPlaces <= 0)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        var scale = Math.Pow(10.0, -board.DecimalPlaces);
        return Math.Round(value / scale) * scale;
    }

    /// <summary>Turn a stored score back into the number a player reads.</summary>
    public static double ToDisplay(D2JamLeaderboard board, D2JamScore score) =>
        IsTimeBased(board) ? score.Data / 1000.0 : score.Data / Math.Pow(10.0, board.DecimalPlaces);

    /// <summary>
    /// Format a stored score the way the website does: points with the board's precision, or a
    /// duration as <c>m:ss.mmm</c>, gaining an hours field once it runs past an hour.
    /// </summary>
    public static string Format(D2JamLeaderboard board, D2JamScore score)
    {
        if (!IsTimeBased(board))
        {
            return ToDisplay(board, score).ToString("F" + Math.Max(board.DecimalPlaces, 0));
        }

        var total = score.Data;
        var hours = total / 3_600_000;
        var minutes = total % 3_600_000 / 60_000;
        var seconds = total % 60_000 / 1000;
        var milliseconds = total % 1000;

        return hours > 0
            ? $"{hours}:{minutes:D2}:{seconds:D2}.{milliseconds:D3}"
            : $"{minutes}:{seconds:D2}.{milliseconds:D3}";
    }

    /// <summary>Drop the cached game so the next read hits the API.</summary>
    public void InvalidateCache() => _cachedGame = null;

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

    private string CurrentUserSlug() => _api.Auth?.UserSlug ?? "";
}
