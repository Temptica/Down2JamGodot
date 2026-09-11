extends Control

## Down2Jam plugin demo.
##
## Everything the addon does, on one screen: link an account, load a game, read its leaderboards and
## achievements, submit a score (optionally with a screenshot), and unlock achievements.
##
## Browsing works signed out. Submitting anything needs a Down2Jam account, because every write
## happens as that player.
##
## There is no autoload. The scene holds a D2JamService node -- configured in the inspector, where
## its game_slug is set -- and it publishes itself as D2JamService.instance while it is in the tree,
## which is how this script finds it.

## The service node in this scene. It has already entered the tree by the time _ready runs, so
## instance is set.
@onready var d2jam: D2JamService = D2JamService.instance

var _boards: Array[D2JamLeaderboard] = []
var _achievements: Array[D2JamAchievement] = []
var _avatar_request: HTTPRequest


func _ready() -> void:
	_avatar_request = HTTPRequest.new()
	add_child(_avatar_request)

	# game_slug is set on the node in the scene; the helpers follow it, so setting it is one line.
	%GameSlug.text = d2jam.game_slug

	%LinkButton.pressed.connect(_on_link_pressed)
	%DisconnectButton.pressed.connect(_on_disconnect_pressed)
	%LoadButton.pressed.connect(_on_load_pressed)
	%SubmitButton.pressed.connect(_on_submit_pressed)
	%BoardPicker.item_selected.connect(func(_index: int) -> void: _show_selected_board())

	d2jam.logged_in.connect(_on_logged_in)
	d2jam.logged_out.connect(_on_logged_out)
	d2jam.device_link_started.connect(_on_device_link_started)
	d2jam.link_failed.connect(func(message: String) -> void: _log("link failed: %s" % message, true))
	d2jam.session_expired.connect(func() -> void: _log("session expired, link the account again", true))
	d2jam.request_failed.connect(func(result: D2JamResult) -> void: _log(result.describe(), true))

	d2jam.leaderboards.score_submitted.connect(
			func(board: D2JamLeaderboard, value: float) -> void:
				_log("submitted %s to '%s'" % [value, board.name]))
	d2jam.achievements.unlocked.connect(
			func(achievement: D2JamAchievement) -> void:
				_log("unlocked '%s'" % achievement.name))

	_configure_table()
	_log("ready. browsing works signed out; submitting needs an account.")

	# D2JamService restores a stored session on its own, so a returning player may already be in.
	await get_tree().process_frame
	_refresh_session_ui()

	await _load_game()


#region Session

func _on_link_pressed() -> void:
	%LinkButton.disabled = true
	%SessionStatus.text = "linking..."

	_log("POST /device/code")
	var success: bool = await d2jam.link_device()

	%LinkButton.disabled = d2jam.is_logged_in()
	%DeviceCodeLabel.text = ""

	if success:
		# The achievement list shows who unlocked what, so it changes meaning once signed in.
		await _refresh_achievements()


func _on_disconnect_pressed() -> void:
	_log("DELETE /self/game-tokens/current")
	await d2jam.logout()
	await _refresh_achievements()


func _on_device_link_started(user_code: String, verification_uri: String) -> void:
	_log("go to %s and approve %s (opening it for you now)" % [verification_uri, user_code])
	%DeviceCodeLabel.text = "Approve %s at %s" % [user_code, verification_uri]


func _on_logged_in(user: D2JamUser) -> void:
	_log("linked as %s (%s)" % [user.name, user.slug])
	_refresh_session_ui()
	_load_avatar(user.profile_picture)


func _on_logged_out() -> void:
	_log("disconnected")
	%Avatar.texture = null
	_refresh_session_ui()


func _refresh_session_ui() -> void:
	var signed_in: bool = d2jam.is_logged_in()
	var user: D2JamUser = d2jam.current_user()

	%SessionStatus.text = "linked as %s" % user.name if signed_in and user != null \
			else "not linked"
	%LinkButton.disabled = signed_in
	%DisconnectButton.disabled = not signed_in
	%SubmitButton.disabled = not signed_in

	%SubmitHint.text = "" if signed_in else "link your account to submit"
	%Avatar.visible = %Avatar.texture != null
	_refresh_achievements_ui()


## Profile pictures are plain URLs, so an ordinary HTTPRequest fetches them.
func _load_avatar(url: String) -> void:
	if url.is_empty():
		return

	_avatar_request.request(url)
	var completed: Array = await _avatar_request.request_completed

	if completed[1] != 200:
		return

	var image: Image = Image.new()
	var bytes: PackedByteArray = completed[3]

	var loaded: Error = image.load_png_from_buffer(bytes)
	if loaded != OK:
		loaded = image.load_jpg_from_buffer(bytes)
	if loaded != OK:
		loaded = image.load_webp_from_buffer(bytes)
	if loaded != OK:
		return

	%Avatar.texture = ImageTexture.create_from_image(image)
	%Avatar.visible = true

#endregion


#region Game

func _on_load_pressed() -> void:
	var slug: String = %GameSlug.text.strip_edges()
	if slug.is_empty():
		return

	d2jam.game_slug = slug
	d2jam.invalidate_cache()

	await _load_game()


## One request brings back the game, its leaderboards with every score, and its achievements.
func _load_game() -> void:
	%LoadButton.disabled = true
	%GameInfo.text = "loading..."

	_log("GET /games/%s" % d2jam.game_slug)
	var game: D2JamGame = await d2jam.fetch_game(true)

	%LoadButton.disabled = false

	if game == null:
		%GameInfo.text = "could not load '%s'" % d2jam.game_slug
		_clear_boards()
		_clear_achievements()
		return

	# Detail responses keep the name and artwork on the page rather than the game itself.
	var page: D2JamGamePage = await d2jam.page()
	var title: String = page.name if page != null else d2jam.game_slug

	%GameInfo.text = "%s\njam: %s\n%d leaderboard(s), %d achievement(s)" % [
		title,
		game.jam.name if game.jam != null else "unknown",
		game.leaderboards.size(),
		game.achievements.size(),
	]

	await _refresh_boards()
	await _refresh_achievements()

#endregion


#region Leaderboards

func _configure_table() -> void:
	var table: Tree = %BoardTable
	table.columns = 3
	table.column_titles_visible = true
	table.set_column_title(0, "#")
	table.set_column_title(1, "player")
	table.set_column_title(2, "score")
	table.set_column_expand(0, false)
	table.set_column_custom_minimum_width(0, 44)
	table.set_column_expand(2, false)
	table.set_column_custom_minimum_width(2, 130)


func _refresh_boards() -> void:
	_boards = await d2jam.leaderboards.boards()

	%BoardPicker.clear()
	for board: D2JamLeaderboard in _boards:
		%BoardPicker.add_item("%s (%s)" % [board.name, board.type])

	if _boards.is_empty():
		_clear_boards()
		return

	%BoardPicker.select(0)
	_show_selected_board()


func _selected_board() -> D2JamLeaderboard:
	var index: int = %BoardPicker.selected
	return _boards[index] if index >= 0 and index < _boards.size() else null


## Rank the board and fill the table. `rank` applies the board's only_best setting, so a player who
## submitted twenty times shows once, exactly as the website lists them.
func _show_selected_board() -> void:
	var board: D2JamLeaderboard = _selected_board()
	var table: Tree = %BoardTable
	table.clear()

	if board == null:
		return

	var is_time: bool = D2JamLeaderboards.is_time_based(board)
	%ScoreValue.step = 0.001 if is_time else 1.0
	%ScoreValue.max_value = 86400.0 if is_time else 100000000.0
	%SubmitHint.text = "" if d2jam.is_logged_in() else "link your account to submit"
	%ScoreUnit.text = "seconds" if is_time else "points"

	%BoardRules.text = "%s - %s wins%s" % [
		board.type,
		"lower" if not D2JamLeaderboards.higher_is_better(board) else "higher",
		", only each player's best is ranked" if board.only_best else "",
	]

	var root: TreeItem = table.create_item()
	var you: String = d2jam.auth.user_slug()

	var ranked: Array[D2JamScore] = d2jam.leaderboards.rank(board)
	for index: int in ranked.size():
		var entry: D2JamScore = ranked[index]
		var row: TreeItem = table.create_item(root)

		row.set_text(0, str(index + 1))
		row.set_text(1, entry.user.name if entry.user != null else "?")
		row.set_text(2, D2JamLeaderboards.format(board, entry))

		if entry.user != null and entry.user.slug == you:
			for column: int in 3:
				row.set_custom_color(column, Color("7ee787"))


func _clear_boards() -> void:
	_boards.clear()
	%BoardPicker.clear()
	%BoardTable.clear()
	%BoardRules.text = "no leaderboards on this game"


func _on_submit_pressed() -> void:
	var board: D2JamLeaderboard = _selected_board()
	if board == null:
		return

	%SubmitButton.disabled = true
	var value: float = %ScoreValue.value

	# Time boards store milliseconds; the spinbox collects seconds.
	if D2JamLeaderboards.is_time_based(board):
		value *= 1000.0

	if %UseScreenshot.button_pressed:
		# The viewport texture is only readable once the frame has been drawn.
		_log("capturing the frame, POST /image, then POST /score")
		await RenderingServer.frame_post_draw
		await d2jam.leaderboards.submit_with_screenshot(board.name, value)
	else:
		_log("POST /score")
		await d2jam.leaderboards.submit_to(board, value)

	%SubmitButton.disabled = not d2jam.is_logged_in()

	# Submitting invalidates the cache, so this re-reads the board with the new entry in it.
	await _load_game()

#endregion


#region Achievements

func _refresh_achievements() -> void:
	_achievements = await d2jam.achievements.all()
	_refresh_achievements_ui()


func _refresh_achievements_ui() -> void:
	var list: VBoxContainer = %AchievementList
	for child: Node in list.get_children():
		child.queue_free()

	if _achievements.is_empty():
		%CompletionLabel.text = "no achievements on this game"
		%CompletionBar.value = 0
		return

	var you: String = d2jam.auth.user_slug()
	var held: int = 0

	for achievement: D2JamAchievement in _achievements:
		var unlocked: bool = _holds(achievement, you)
		if unlocked:
			held += 1

		list.add_child(_build_achievement_row(achievement, unlocked))

	%CompletionBar.value = float(held) / float(_achievements.size()) * 100.0
	%CompletionLabel.text = "%d of %d unlocked" % [held, _achievements.size()] \
			if not you.is_empty() else "%d achievements - link your account to see your progress" \
			% _achievements.size()


func _clear_achievements() -> void:
	_achievements.clear()
	_refresh_achievements_ui()


func _holds(achievement: D2JamAchievement, slug: String) -> bool:
	if slug.is_empty():
		return false

	for user: D2JamUser in achievement.users:
		if user.slug == slug:
			return true

	return false


func _build_achievement_row(achievement: D2JamAchievement, unlocked: bool) -> Control:
	var row: HBoxContainer = HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)

	var mark: Label = Label.new()
	mark.text = "*" if unlocked else "-"
	mark.custom_minimum_size.x = 14
	mark.add_theme_color_override("font_color",
			Color("7ee787") if unlocked else Color(1, 1, 1, 0.35))
	row.add_child(mark)

	var text: Label = Label.new()
	text.text = "%s - %s" % [achievement.name, achievement.description]
	text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	if not unlocked:
		text.modulate = Color(1, 1, 1, 0.6)
	row.add_child(text)

	var button: Button = Button.new()
	button.text = "revoke" if unlocked else "unlock"
	button.disabled = not d2jam.is_logged_in()
	button.pressed.connect(_on_achievement_pressed.bind(achievement, unlocked))
	row.add_child(button)

	return row


func _on_achievement_pressed(achievement: D2JamAchievement, unlocked: bool) -> void:
	if unlocked:
		_log("DELETE /achievement (%s)" % achievement.name)
		await d2jam.achievements.revoke(achievement.name)
	else:
		_log("POST /achievement (%s)" % achievement.name)
		await d2jam.achievements.unlock(achievement.name)

	await _load_game()

#endregion


func _log(message: String, is_error: bool = false) -> void:
	var stamp: String = Time.get_time_string_from_system()
	var colour: String = "#f0806a" if is_error else "#8b949e"
	%Log.append_text("[color=%s]%s[/color]  %s\n" % [colour, stamp, message])
