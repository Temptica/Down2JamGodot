@tool
extends Node

## Leaderboard helper: submit scores and read rankings without thinking about the wire format.
##
## Down2Jam has no leaderboard endpoint of its own. Boards and their scores are returned as part of
## the game, so this node fetches the game once and caches it, and submits through POST /score.
## Boards themselves are created by the developer on the game's page; a game can only submit to
## boards that already exist.
##
## Values are stored differently per board type, which is the main thing this node hides:
##
## [codeblock]
## # A SCORE board: pass the number the player sees.
## await d2jam.leaderboards.submit("High Score", 16340)
##
## # A SPEEDRUN board: pass seconds and let the node convert to milliseconds.
## await d2jam.leaderboards.submit_time("Fastest Time", 82.451)
##
## # Read the ranking back.
## for entry in await d2jam.leaderboards.ranking("High Score"):
##     print(entry.user.name, d2jam.leaderboards.format(board, entry))
## [/codeblock]
class_name D2JamLeaderboards

## Higher is better. The value is multiplied by 10 ^ decimal_places before storage.
const TYPE_SCORE: String = "SCORE"

## Lower is better, same scaling as [constant TYPE_SCORE]. Named after golf, where a low score wins.
const TYPE_GOLF: String = "GOLF"

## Lower is better. The value is a duration in milliseconds.
const TYPE_SPEEDRUN: String = "SPEEDRUN"

## Higher is better. The value is a duration in milliseconds.
const TYPE_ENDURANCE: String = "ENDURANCE"

## Emitted after a score is accepted.
signal score_submitted(board: D2JamLeaderboard, value: float)

## Emitted when a submission fails, with a message safe to show the player.
signal submit_failed(message: String)

## The generated client. [D2JamService] assigns this.
@export var api: D2JamAPI

## Slug of the game whose boards these are, as it appears in the d2jam.com URL.
@export var game_slug: String = ""

## Supplies the game and caches it. [D2JamService] assigns this so the leaderboard and achievement
## nodes share a single fetch. When unset this node fetches on its own.
var game_provider: Callable

var _cached_game: D2JamGame


## The leaderboard helper in the current scene, or null when none is in the tree.
##
## [D2JamService] adds one as a child of itself, so this is set for you.
static var instance: D2JamLeaderboards


func _enter_tree() -> void:
	if instance == null:
		instance = self


func _exit_tree() -> void:
	if instance == self:
		instance = null


## Every leaderboard on the game page. Cached after the first call.
func boards(force_refresh: bool = false) -> Array[D2JamLeaderboard]:
	var game: D2JamGame = await _game(force_refresh)
	if game == null:
		var empty: Array[D2JamLeaderboard] = []
		return empty

	return game.leaderboards


## Find a board by name, case-insensitively. Returns null when there is no such board.
func find(board_name: String) -> D2JamLeaderboard:
	var wanted: String = board_name.strip_edges().to_lower()

	for board: D2JamLeaderboard in await boards():
		if board.name.strip_edges().to_lower() == wanted:
			return board

	return null


## Submit a score to the named board.
##
## [param value] is the number the player sees: points for a SCORE or GOLF board, and for a time
## board the milliseconds (use [method submit_time] to pass seconds instead). [param evidence_url]
## is a screenshot URL; the website shows one next to every entry and expects submissions to carry
## one, so pass the result of [method capture_evidence] where you can.
##
## Returns true when the score was accepted.
func submit(board_name: String, value: float, evidence_url: String = "") -> bool:
	var board: D2JamLeaderboard = await find(board_name)

	if board == null:
		var message: String = "No leaderboard named '%s' on %s. Create it on the game's page first." \
				% [board_name, game_slug]
		push_warning("[d2jam] " + message)
		submit_failed.emit(message)
		return false

	return await submit_to(board, value, evidence_url)


## Submit to a board you already hold, skipping the name lookup.
func submit_to(board: D2JamLeaderboard, value: float, evidence_url: String = "") -> bool:
	if not _require_api():
		return false

	var body: D2JamCreateScoreBody = D2JamCreateScoreBody.create(board.id, to_payload(board, value))
	if not evidence_url.is_empty():
		body.evidence = evidence_url

	var result: D2JamVoidResult = await api.create_score(body)

	if not result.ok:
		api.report(result)
		submit_failed.emit(result.error_message)
		return false

	# The submitted score is now stale in the cache.
	_cached_game = null
	score_submitted.emit(board, value)
	return true


## Submit a duration in seconds to a SPEEDRUN or ENDURANCE board.
func submit_time(board_name: String, seconds: float, evidence_url: String = "") -> bool:
	return await submit(board_name, seconds * 1000.0, evidence_url)


## Grab the current frame, upload it, and submit it as the score's evidence in one step.
##
## Must be called after [code]await RenderingServer.frame_post_draw[/code] or from
## [code]_process[/code], because the viewport texture is only readable once the frame is drawn.
##
## Captures [param viewport] verbatim -- the whole main window by default. Pass a [SubViewport] to
## capture just one region (hide the HUD first, crop to the play area, whatever you need); this
## does no compositing of its own. For anything more -- a downscale, a watermark, an image from a
## source that is not a viewport at all -- skip this and call [method capture_evidence] or
## [member D2JamAPI.upload_image] on your own bytes instead, then pass the resulting URL to
## [method submit] as [code]evidence_url[/code].
func submit_with_screenshot(board_name: String, value: float, viewport: Viewport = null) -> bool:
	var evidence: String = await capture_evidence(viewport)
	return await submit(board_name, value, evidence)


## Upload a screenshot of [param viewport] (the main window by default) and return its URL, without
## submitting a score. Pass a [SubViewport] to capture just one region instead of the whole window.
##
## Useful on its own when you want to build the score submission yourself rather than going through
## [method submit_with_screenshot] -- or skip this entirely and call
## [member D2JamAPI.upload_image] with bytes from anywhere (a crop, a composite, a watermark) if you
## need more control than a straight viewport capture gives you.
##
## Returns an empty string when the upload fails; submitting without evidence still works, it just
## leaves the entry without a screenshot on the website.
func capture_evidence(viewport: Viewport = null) -> String:
	if not _require_api():
		return ""

	var source: Viewport = viewport if viewport != null else get_viewport()
	if source == null:
		push_warning("[d2jam] No viewport to capture a screenshot from.")
		return ""

	var image: Image = source.get_texture().get_image()
	if image == null:
		push_warning("[d2jam] The viewport texture could not be read this frame.")
		return ""

	var file_name: String = "%s-score-%d.png" % [game_slug, Time.get_unix_time_from_system()]
	var result: D2JamStringResult = await api.upload_image(image.save_png_to_buffer(), file_name)

	if not result.ok:
		api.report(result)
		return ""

	return result.data


## The board's scores in ranked order, best first.
##
## Applies the board's [code]only_best[/code] setting, so a player who submitted five times appears
## once with their best run, exactly as the website shows it.
func ranking(board_name: String) -> Array[D2JamScore]:
	var board: D2JamLeaderboard = await find(board_name)
	return rank(board)


## Rank the scores of a board you already hold.
func rank(board: D2JamLeaderboard) -> Array[D2JamScore]:
	var entries: Array[D2JamScore] = []
	if board == null:
		return entries

	entries.assign(board.scores)
	var higher_wins: bool = higher_is_better(board)

	entries.sort_custom(func(a: D2JamScore, b: D2JamScore) -> bool:
		return a.data > b.data if higher_wins else a.data < b.data)

	if not board.only_best:
		return entries

	var seen: Dictionary = {}
	var best: Array[D2JamScore] = []

	for entry: D2JamScore in entries:
		if seen.has(entry.user_id):
			continue
		seen[entry.user_id] = true
		best.append(entry)

	return best


## The best entry a user holds on a board, or null. Defaults to the logged in player.
func best_for(board: D2JamLeaderboard, user_slug: String = "") -> D2JamScore:
	var wanted: String = user_slug if not user_slug.is_empty() else _current_user_slug()
	if wanted.is_empty():
		return null

	for entry: D2JamScore in rank(board):
		if entry.user != null and entry.user.slug == wanted:
			return entry

	return null


## One-based position of a user on a board, or -1 when they have no entry.
func position_of(board: D2JamLeaderboard, user_slug: String = "") -> int:
	var wanted: String = user_slug if not user_slug.is_empty() else _current_user_slug()
	if wanted.is_empty():
		return -1

	var ranked: Array[D2JamScore] = rank(board)
	for index: int in ranked.size():
		var entry: D2JamScore = ranked[index]
		if entry.user != null and entry.user.slug == wanted:
			return index + 1

	return -1


## True when a larger stored value ranks higher on this board.
static func higher_is_better(board: D2JamLeaderboard) -> bool:
	return board.type == TYPE_SCORE or board.type == TYPE_ENDURANCE


## True when the board stores durations rather than points.
static func is_time_based(board: D2JamLeaderboard) -> bool:
	return board.type == TYPE_SPEEDRUN or board.type == TYPE_ENDURANCE


## Convert a player-facing value into what POST /score expects for this board.
##
## Time boards take whole milliseconds. Point boards take the value as the player reads it and are
## scaled server-side by 10 ^ decimal_places, so a board with no decimal places gets a plain
## integer. Boards that do declare decimal places get a rounded float; the server multiplies it and
## stores an integer, so a value that does not land exactly on one can be rejected.
static func to_payload(board: D2JamLeaderboard, value: float) -> Variant:
	if is_time_based(board) or board.decimal_places <= 0:
		return int(roundf(value))

	return snappedf(value, pow(10.0, -board.decimal_places))


## Turn a stored score back into the number a player reads.
static func to_display(board: D2JamLeaderboard, score: D2JamScore) -> float:
	if is_time_based(board):
		return score.data / 1000.0

	return score.data / pow(10.0, board.decimal_places)


## Format a stored score the way the website does: points with the board's precision, or a duration
## as [code]m:ss.mmm[/code], gaining an hours field once it runs past an hour.
static func format(board: D2JamLeaderboard, score: D2JamScore) -> String:
	if not is_time_based(board):
		return String.num(to_display(board, score), board.decimal_places)

	var total: int = score.data
	var hours: int = total / 3600000
	var minutes: int = (total % 3600000) / 60000
	var seconds: int = (total % 60000) / 1000
	var milliseconds: int = total % 1000

	if hours > 0:
		return "%d:%02d:%02d.%03d" % [hours, minutes, seconds, milliseconds]

	return "%d:%02d.%03d" % [minutes, seconds, milliseconds]


## Drop the cached game so the next read hits the API.
func invalidate_cache() -> void:
	_cached_game = null


func _game(force_refresh: bool = false) -> D2JamGame:
	if _cached_game != null and not force_refresh:
		return _cached_game

	if game_provider.is_valid():
		_cached_game = await game_provider.call(force_refresh)
		return _cached_game

	if not _require_api():
		return null

	var result: D2JamGameResult = await api.get_game(game_slug)
	if not result.ok:
		api.report(result)
		return null

	_cached_game = result.data
	return _cached_game


func _require_api() -> bool:
	if api != null:
		return true

	push_error("[d2jam] D2JamLeaderboards has no API assigned. Add it through D2JamService.")
	return false


func _current_user_slug() -> String:
	if api != null and api.auth != null:
		return api.auth.user_slug()

	return ""
