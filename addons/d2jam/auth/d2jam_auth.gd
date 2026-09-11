@tool
extends Node

## Holds the player's Down2Jam game token and keeps it usable.
##
## Down2Jam issues a single long lived token once a player links their account through the device
## flow (see [method D2JamService.link_device]). It is sent as a bearer token on every authenticated
## call and never expires on its own -- the player can revoke it from the website's Settings page, or
## the game can drop it itself with [method D2JamService.disconnect]. There is nothing to refresh and
## nothing to rotate.
##
## Drive this through [D2JamService] rather than directly.
##
## [b]On storage:[/b] with [member persist] enabled the token is written to [member storage_path] on
## the player's machine. Anyone who can read that file can act as the player on Down2Jam until the
## token is revoked, so set [member encryption_key] in a released build. Nothing else -- no password,
## no username -- is ever stored, because the device flow never puts the player's password in the
## game's hands.
class_name D2JamAuth

## Emitted whenever the stored session changes: a link, a disconnect, or a restore.
signal session_changed

## Emitted when the API rejects the stored token. The player has to link their account again.
signal session_expired

## Keep the player linked between runs by storing their token.
@export var persist: bool = true

## Where the session is stored. Keep it under user:// so it works on every platform.
@export var storage_path: String = "user://d2jam_session.json"

## Encrypts the stored session. Leave empty during development; set it for a release build.
@export var encryption_key: String = ""

## The current game token. Set once by [method adopt] after a successful device link.
var game_token: String = ""

## The logged in user, as returned by fetching the profile after a link. Null when disconnected.
var user: D2JamUser


## The session in the current scene, or null when none is in the tree.
##
## [D2JamService] adds one as a child of itself, so this is set for you.
static var instance: D2JamAuth


func _enter_tree() -> void:
	if instance == null:
		instance = self


func _exit_tree() -> void:
	if instance == self:
		instance = null


func _ready() -> void:
	if persist and not Engine.is_editor_hint():
		load_session()


## True when there is a token to authenticate with.
func is_logged_in() -> bool:
	return not game_token.is_empty()


## The slug of the logged in user, or an empty string.
func user_slug() -> String:
	return user.slug if user != null else ""


## Headers that authenticate a request.
##
## A game token is a single bearer credential -- unlike a browser session there is no cookie to
## carry and nothing that rotates.
func authorization_headers() -> PackedStringArray:
	if not is_logged_in():
		return PackedStringArray()

	return ["Authorization: Bearer %s" % game_token]


## Store the token and profile returned by a successful device link.
##
## [param user] is optional so tests and low level callers can adopt a bare token, but
## [D2JamService] always supplies it: [code]GET /self[/code] does not accept a game token, so the
## approved poll response is the only place a profile is ever handed to the game, and it has to be
## kept here to survive a restart. See [method D2JamService.restore_session].
func adopt(token: String, user_profile: D2JamUser = null) -> void:
	game_token = token
	if user_profile != null:
		user = user_profile

	if persist:
		save_session()

	session_changed.emit()


## Drop the session because the API rejected it.
func invalidate() -> void:
	if not is_logged_in():
		return

	clear()
	session_expired.emit()


## Drop the session and forget anything stored on disk.
func clear() -> void:
	game_token = ""
	user = null

	if persist and FileAccess.file_exists(storage_path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(storage_path))

	session_changed.emit()


## Write the session to [member storage_path]. Called automatically when [member persist] is on.
##
## The profile is saved alongside the token, not just its slug, because nothing can be fetched again
## after a restart to fill it back in -- see [method adopt].
func save_session() -> void:
	if not is_logged_in():
		return

	var payload: Dictionary = {
		"version": 3,
		"game_token": game_token,
		"user": user.to_dict() if user != null else null,
	}

	var file: FileAccess = _open(FileAccess.WRITE)
	if file == null:
		push_warning("[d2jam] Could not write the session to %s: %s"
				% [storage_path, error_string(FileAccess.get_open_error())])
		return

	file.store_string(JSON.stringify(payload))
	file.close()


## Read a previously saved session. Returns true when one was restored.
func load_session() -> bool:
	if not FileAccess.file_exists(storage_path):
		return false

	var file: FileAccess = _open(FileAccess.READ)
	if file == null:
		# A wrong or missing encryption key lands here. Treat it as "no session" rather than an
		# error the player cannot act on.
		return false

	var raw: String = file.get_as_text()
	file.close()

	var parsed: Variant = JSON.parse_string(raw)
	if parsed is not Dictionary:
		return false

	var payload: Dictionary = parsed
	game_token = str(payload.get("game_token", ""))

	if not is_logged_in():
		game_token = ""
		return false

	user = D2JamUser.from_json(payload["user"]) if payload.get("user") is Dictionary else null

	session_changed.emit()
	return true


func _open(mode: FileAccess.ModeFlags) -> FileAccess:
	if encryption_key.is_empty():
		return FileAccess.open(storage_path, mode)

	return FileAccess.open_encrypted_with_pass(storage_path, mode, encryption_key)
