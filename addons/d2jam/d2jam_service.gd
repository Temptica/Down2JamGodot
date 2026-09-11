@tool
extends Node

## The one node to add to your game. Wires up the API client, the session, and the leaderboard and
## achievement helpers, and gives you a single place to link the player's account.
##
## Add a D2JamService to your main scene and set [member game_slug] to your game's slug on
## d2jam.com. The node registers itself as [member instance] while it is in the tree, so the rest of
## your code reaches it without needing a reference or an autoload:
##
## [codeblock]
## var d2jam := D2JamService.instance
##
## d2jam.device_link_started.connect(
##         func(user_code, verification_uri): show_code_screen(user_code))
## if await d2jam.link_device():
##     await d2jam.achievements.unlock("First Blood")
##     await d2jam.leaderboards.submit("High Score", 16340)
## [/codeblock]
##
## Only a linked player can submit scores or unlock achievements, and everything happens as that
## player. There is nothing to configure per game beyond the slug: no API key, no client id, and no
## password ever passes through the game.
class_name D2JamService

## The service in the current scene, or null when none is in the tree.
##
## Set while the node is in the tree and cleared when it leaves, so a service that lives in one
## scene stops being reachable once that scene is freed. If you want one session for the whole game,
## put the D2JamService in a scene that is never unloaded. Two services in the tree at once is a
## mistake -- the first one to enter keeps the slot.
static var instance: D2JamService

## Emitted after a successful device link or a restored session, with the player's profile.
signal logged_in(user: D2JamUser)

## Emitted after [method logout].
signal logged_out

## Emitted once [method link_device] has a code to show, before it starts polling. Display
## [param user_code] to the player; [param verification_uri] is opened in their browser automatically
## unless [member auto_open_browser] is off.
signal device_link_started(user_code: String, verification_uri: String)

## Emitted when a device link attempt is denied, expires, or cannot be started, with a message safe
## to show the player.
signal link_failed(message: String)

## Emitted when a stored session stops being accepted. Prompt for a device link again.
signal session_expired

## Emitted for every failed API call. Convenient for one central error toast.
signal request_failed(result: D2JamResult)

## Your game's slug, the last part of its d2jam.com URL. For
## [code]https://d2jam.com/g/weldroot[/code] that is [code]weldroot[/code].
##
## Safe to change at runtime; the helpers follow it.
@export var game_slug: String = "":
	set(value):
		game_slug = value
		if leaderboards != null:
			leaderboards.game_slug = value
		if achievements != null:
			achievements.game_slug = value

## Base URL of the API. Point it at a local Jamcore instance to test against one.
@export var api_host: String = D2JamAPIBase.DEFAULT_HOST:
	set(value):
		api_host = value
		if api != null:
			api.api_host = value

## Log failed calls with push_warning.
@export var warn_on_failure: bool = true

@export_group("Session")

## Keep the player linked between runs by storing their game token.
@export var persist_session: bool = true

## Where the session is stored.
@export var session_path: String = "user://d2jam_session.json"

## Encrypts the stored session. Leave empty in development; set it for a release build. See
## [D2JamAuth] for what is at stake.
@export var session_encryption_key: String = ""

## On [method Node._ready], restore a stored session and fetch the player's profile.
@export var restore_on_ready: bool = true

## Open [member D2JamDeviceCode.verification_uri] in the player's default browser automatically once
## [method link_device] gets a code. Turn this off if you would rather show a QR code or a "copy
## link" button instead.
@export var auto_open_browser: bool = true

## The generated REST client. Use it directly for anything the helpers do not cover.
var api: D2JamAPI

## The player's session.
var auth: D2JamAuth

## Leaderboard submission and ranking.
var leaderboards: D2JamLeaderboards

## Achievement unlocking and progress.
var achievements: D2JamAchievements

var _game: D2JamGame
var _fetching: bool = false

signal _game_fetched(game: D2JamGame)


func _enter_tree() -> void:
	if instance == null:
		instance = self

	# Built here rather than in _ready so that api, auth and the helpers are already usable by
	# anything that reaches for D2JamService.instance during its own _ready.
	_build()


func _exit_tree() -> void:
	if instance == self:
		instance = null


func _ready() -> void:
	if restore_on_ready and not Engine.is_editor_hint():
		await restore_session()


## True when a player is logged in.
func is_logged_in() -> bool:
	return auth != null and auth.is_logged_in()


## The logged in player, or null.
func current_user() -> D2JamUser:
	return auth.user if auth != null else null


## Link the player's Down2Jam account through the device flow.
##
## Starts a device authorization request, emits [signal device_link_started] with a short code and
## a link to open in the player's browser (opened automatically unless [member auto_open_browser] is
## off), then polls until the player approves or denies it there. No password ever reaches the game;
## the player signs in on the website, in their own browser, with their own session.
##
## The issued game token is scoped to [member game_slug] and only ever works for that one game --
## it cannot submit scores or achievements anywhere else, even for a player who owns several games.
##
## [param client_name] is what the player sees on the website's approval page -- pick something that
## identifies this install, like "Steam Deck" or the platform name. Defaults to the project name.
##
## Returns true once the player has approved the request and the game token is stored.
func link_device(client_name: String = "") -> bool:
	if game_slug.is_empty():
		push_error("[d2jam] D2JamService.game_slug is not set; link_device() needs it to scope the token.")
		link_failed.emit("This game is not configured correctly. Tell the developer.")
		return false

	var name: String = client_name if not client_name.is_empty() else _default_client_name()

	var code_result: D2JamDeviceCodeResult = await api.start_device_link(
			D2JamDeviceCodeBody.create(name, game_slug))

	if not code_result.ok:
		var message: String = code_result.error_message if not code_result.error_message.is_empty() \
				else "Could not reach Down2Jam."
		link_failed.emit(message)
		return false

	var code: D2JamDeviceCode = code_result.data
	device_link_started.emit(code.user_code, code.verification_uri)

	if auto_open_browser:
		OS.shell_open(code.verification_uri)

	return await _poll_device_link(code)


## Restore a session stored by a previous run.
##
## The token and profile were both saved at link time -- see [D2JamAuth] -- so this needs no network
## call. If the token was revoked from the website while the game was closed, that surfaces the
## normal way, on the first call that actually needs it: a 401 clears the session and fires
## [signal session_expired].
##
## Returns true when the player is logged in afterwards. A false means "show the link-device
## screen".
func restore_session() -> bool:
	if not is_logged_in():
		return false

	logged_in.emit(auth.user)
	return true


## Disconnect the account: revoke the game token on the server, then forget it locally.
##
## The token stops working immediately either way, so it is safe to call even if the server request
## fails -- for example because it was already revoked from the website.
func logout() -> void:
	if auth.is_logged_in():
		await api.revoke_current_game_token()

	auth.clear()
	_game = null
	leaderboards.invalidate_cache()
	achievements.invalidate_cache()
	logged_out.emit()


func _default_client_name() -> String:
	var configured: String = ProjectSettings.get_setting("application/config/name", "")
	return configured if not configured.is_empty() else "Godot Game"


## Poll a pending device authorization request until it resolves, backing off on slow_down per
## RFC 8628 (increase the interval and keep it there, rather than resetting after each backoff).
func _poll_device_link(code: D2JamDeviceCode) -> bool:
	var interval: float = float(maxi(code.interval, 1))
	var deadline_msec: int = Time.get_ticks_msec() + code.expires_in * 1000

	while true:
		if Time.get_ticks_msec() >= deadline_msec:
			link_failed.emit("The device link request expired before it was approved.")
			return false

		await get_tree().create_timer(interval).timeout

		var poll_result: D2JamDeviceTokenResult = await api.poll_device_link(
				D2JamDeviceTokenBody.create(code.device_code))

		if not poll_result.ok:
			var message: String = poll_result.error_message if not poll_result.error_message.is_empty() \
					else "The device link request was not approved."
			link_failed.emit(message)
			return false

		match poll_result.data.status:
			"approved":
				# GET /self does not accept a game token, so the approved poll response carries the
				# player's profile directly rather than needing a second, doomed request for it.
				auth.adopt(poll_result.data.token, poll_result.data.user)

				logged_in.emit(auth.user)
				return true
			"slow_down":
				interval += maxi(code.interval, 1)

	return false


## Fetch the game, with its leaderboards and achievements, and cache it.
##
## Both helpers read through this, so a screen showing leaderboards and achievements together costs
## one request rather than two. Overlapping calls share the in-flight request.
func fetch_game(force_refresh: bool = false) -> D2JamGame:
	if _game != null and not force_refresh:
		return _game

	if _fetching:
		return await _game_fetched

	if game_slug.is_empty():
		push_error("[d2jam] D2JamService.game_slug is not set; nothing to fetch.")
		return null

	_fetching = true
	var result: D2JamGameResult = await api.get_game(game_slug)
	_fetching = false

	if result.ok:
		_game = result.data
	else:
		api.report(result)

	_game_fetched.emit(_game)
	return _game


## The page a player should be shown for the cached game: the post-jam page when the team published
## one, otherwise the jam page.
##
## Worth going through, because GET /games/{gameSlug} does not flatten the name, description and
## artwork onto the game the way the list endpoints do -- they live on the page.
func page(force_refresh: bool = false) -> D2JamGamePage:
	var game: D2JamGame = await fetch_game(force_refresh)
	if game == null:
		return null

	if game.post_jam_page != null:
		return game.post_jam_page

	if game.jam_page != null:
		return game.jam_page

	return game.pages[0] if not game.pages.is_empty() else null


## The game's display name, or an empty string when it cannot be fetched.
func game_name() -> String:
	var current: D2JamGamePage = await page()
	return current.name if current != null else ""


## Forget the cached game so the next read hits the API.
func invalidate_cache() -> void:
	_game = null
	leaderboards.invalidate_cache()
	achievements.invalidate_cache()


func _build() -> void:
	# _enter_tree runs again whenever the node is re-parented; the children survive that.
	if api != null:
		return

	auth = D2JamAuth.new()
	auth.name = "D2JamAuth"
	auth.persist = persist_session
	auth.storage_path = session_path
	auth.encryption_key = session_encryption_key
	auth.session_expired.connect(_on_session_expired)
	add_child(auth)

	api = D2JamAPI.new()
	api.name = "D2JamAPI"
	api.api_host = api_host
	api.auth = auth
	api.warn_on_failure = warn_on_failure
	api.request_failed.connect(request_failed.emit)
	add_child(api)

	leaderboards = D2JamLeaderboards.new()
	leaderboards.name = "D2JamLeaderboards"
	leaderboards.api = api
	leaderboards.game_slug = game_slug
	leaderboards.game_provider = fetch_game
	add_child(leaderboards)

	achievements = D2JamAchievements.new()
	achievements.name = "D2JamAchievements"
	achievements.api = api
	achievements.game_slug = game_slug
	achievements.game_provider = fetch_game
	add_child(achievements)


func _on_session_expired() -> void:
	_game = null
	session_expired.emit()
