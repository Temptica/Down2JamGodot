extends Node

## A worked example of the whole integration. Attach it to a node, set the exports, and run.
##
## Needs a D2JamService somewhere in the tree -- add one to your main scene and set its game_slug.
## It publishes itself as D2JamService.instance while it is in the tree, which is how this script
## finds it; there is no autoload involved.

## Names as they appear on your game's page on d2jam.com.
@export var leaderboard_name: String = "High Score"
@export var achievement_name: String = "First Blood"

var d2jam: D2JamService


func _ready() -> void:
	d2jam = D2JamService.instance

	if d2jam == null:
		push_error("No D2JamService in the tree. Add one to your main scene.")
		return

	# React to session changes wherever you show the player's name or avatar.
	d2jam.logged_in.connect(_on_logged_in)
	d2jam.logged_out.connect(func() -> void: print("signed out"))
	d2jam.device_link_started.connect(
			func(user_code: String, verification_uri: String) -> void:
				print("go to %s and enter %s (opening it for you now)" % [verification_uri, user_code]))
	d2jam.link_failed.connect(func(message: String) -> void: print("link failed: ", message))
	d2jam.session_expired.connect(func() -> void: print("session expired, link the account again"))

	# The service restores a stored session on its own, so a returning player is often already
	# linked by the time _ready runs here.
	if not d2jam.is_logged_in():
		if not await d2jam.link_device():
			return

	await _show_leaderboard()
	await _show_achievements()


func _on_logged_in(user: D2JamUser) -> void:
	print("signed in as %s (%s)" % [user.name, user.slug])
	# user.profile_picture is a URL; fetch it with an HTTPRequest to show an avatar.


## Read a board and print the top ten, formatted the way the website does.
func _show_leaderboard() -> void:
	var board: D2JamLeaderboard = await d2jam.leaderboards.find(leaderboard_name)
	if board == null:
		print("no leaderboard called '%s' - create it on your game's page" % leaderboard_name)
		return

	print("\n%s (%s)" % [board.name, board.type])

	var ranked: Array[D2JamScore] = d2jam.leaderboards.rank(board)
	for index: int in mini(10, ranked.size()):
		var entry: D2JamScore = ranked[index]
		print("  %2d. %-20s %s" % [
			index + 1,
			entry.user.name,
			D2JamLeaderboards.format(board, entry),
		])

	var position: int = d2jam.leaderboards.position_of(board)
	if position > 0:
		print("  you are #%d" % position)


## Print the player's achievement progress.
func _show_achievements() -> void:
	print("\nachievements: %d%% complete" % roundi(await d2jam.achievements.completion() * 100.0))

	for achievement: D2JamAchievement in await d2jam.achievements.all():
		var held: bool = await d2jam.achievements.is_unlocked(achievement.name)
		print("  [%s] %s - %s" % ["x" if held else " ", achievement.name, achievement.description])


## Call this when the player finishes a run.
##
## The website shows a screenshot next to every leaderboard entry and expects submissions to carry
## one, so this grabs the frame, uploads it, and submits both together. Waiting for
## frame_post_draw matters: the viewport texture is only readable once the frame has been drawn.
func submit_run(score: int) -> void:
	await RenderingServer.frame_post_draw

	if await d2jam.leaderboards.submit_with_screenshot(leaderboard_name, score):
		print("submitted %d" % score)


## A speedrun board stores milliseconds; submit_time takes seconds and converts.
func submit_run_time(seconds: float) -> void:
	await d2jam.leaderboards.submit_time("Fastest Time", seconds)


## Unlocking is safe to call from gameplay code as often as you like: a repeat unlock in the same
## session is dropped before it becomes a request.
func _on_first_kill() -> void:
	await d2jam.achievements.unlock(achievement_name)
