@tool
extends D2JamAPIBase

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Typed client for the Jamcore API (1.0.0).
##
## Every method is asynchronous; await the call and inspect the returned result. Transport,
## authentication and error handling live in the hand written [D2JamAPIBase], so this file only
## describes endpoints.
##
## For leaderboards and achievements prefer the [D2JamLeaderboards] and [D2JamAchievements] nodes,
## which handle value conversion, evidence uploads and name based lookup on top of these calls.
## The client of the most recently added [D2JamAPI] node, so code that only needs the raw API can
## reach it without a reference. [D2JamService] adds one as a child of itself.
class_name D2JamAPI

## The API client in the current scene, or null when none is in the tree.
static var instance: D2JamAPI


func _enter_tree() -> void:
	if instance == null:
		instance = self


func _exit_tree() -> void:
	if instance == self:
		instance = null


## Submit a score to a leaderboard as the logged in user.
##
## The server responds with a message only, no score object. Prefer D2JamLeaderboards.submit(),
## which converts the value for the board type and can attach a screenshot as evidence.
##
## body - Request payload; see [D2JamCreateScoreBody].
##
## POST /score - Requires user login
func create_score(body: D2JamCreateScoreBody) -> D2JamVoidResult:
	var path: String = "/score"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_POST, {}, body.to_dict(), true)
	var result: D2JamVoidResult = D2JamVoidResult.new()
	result._apply(response)
	return result


## Delete a score. Allowed for the score's owner, the game's team members and moderators.
##
## body - Request payload; see [D2JamDeleteScoreBody].
##
## DELETE /score - Requires user login
func delete_score(body: D2JamDeleteScoreBody) -> D2JamVoidResult:
	var path: String = "/score"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_DELETE, {}, body.to_dict(), true)
	var result: D2JamVoidResult = D2JamVoidResult.new()
	result._apply(response)
	return result


## Return one game with its leaderboards (including every score) and achievements (including who
## unlocked them). This is the read path for both features.
##
## game_slug - Path parameter.
## options - Optional query parameters; see [D2JamGetGameOptions]. May be null.
##
## GET /games/{gameSlug} - Uses login if present
func get_game(game_slug: String, options: D2JamGetGameOptions = null) -> D2JamGameResult:
	var path: String = "/games/%s" % [encode_path_segment(str(game_slug))]
	var query: Dictionary = {}
	if options != null:
		query.merge(options.to_dict(), true)
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_GET, query, null, false)
	var result: D2JamGameResult = D2JamGameResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamGame.from_json(result.payload)
	return result


## Return the logged in user's own game entries for the active jam. Useful for resolving your own
## game slug at runtime instead of hardcoding it.
##
## GET /self/current-game - Requires user login
func get_own_current_games() -> D2JamGameListResult:
	var path: String = "/self/current-game"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_GET, {}, null, true)
	var result: D2JamGameListResult = D2JamGameListResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamGame.list_from_json(result.payload)
	return result


## Return a random published game.
##
## GET /games/random - Uses login if present
func get_random_game() -> D2JamGameResult:
	var path: String = "/games/random"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_GET, {}, null, false)
	var result: D2JamGameResult = D2JamGameResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamGame.from_json(result.payload)
	return result


## Return the profile of the logged in user.
##
## GET /self - Requires user login
func get_self() -> D2JamUserResult:
	var path: String = "/self"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_GET, {}, null, true)
	var result: D2JamUserResult = D2JamUserResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamUser.from_json(result.payload)
	return result


## Return a user's public profile.
##
## user_slug - Path parameter.
##
## GET /users/{userSlug} - Public
func get_user(user_slug: String) -> D2JamUserResult:
	var path: String = "/users/%s" % [encode_path_segment(str(user_slug))]
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_GET, {}, null, false)
	var result: D2JamUserResult = D2JamUserResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamUser.from_json(result.payload)
	return result


## List published games. Leaderboards and achievements are not included here; fetch a single game
## for those.
##
## options - Optional query parameters; see [D2JamListGamesOptions]. May be null.
##
## GET /games - Public
func list_games(options: D2JamListGamesOptions = null) -> D2JamGameListResult:
	var path: String = "/games"
	var query: Dictionary = {}
	if options != null:
		query.merge(options.to_dict(), true)
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_GET, query, null, false)
	var result: D2JamGameListResult = D2JamGameListResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamGame.list_from_json(result.payload)
	return result


## List users.
##
## options - Optional query parameters; see [D2JamListUsersOptions]. May be null.
##
## GET /users - Public
func list_users(options: D2JamListUsersOptions = null) -> D2JamUserListResult:
	var path: String = "/users"
	var query: Dictionary = {}
	if options != null:
		query.merge(options.to_dict(), true)
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_GET, query, null, false)
	var result: D2JamUserListResult = D2JamUserListResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamUser.list_from_json(result.payload)
	return result


## Revoke an achievement from the logged in user.
##
## body - Request payload; see [D2JamAchievementBody].
##
## DELETE /achievement - Requires user login
func lock_achievement(body: D2JamAchievementBody) -> D2JamVoidResult:
	var path: String = "/achievement"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_DELETE, {}, body.to_dict(), true)
	var result: D2JamVoidResult = D2JamVoidResult.new()
	result._apply(response)
	return result


## Poll a pending device authorization request. Returns authorization_pending or slow_down while
## waiting, or the game's access token once approved. A denied or expired request comes back as a
## failed result rather than a status field.
##
## body - Request payload; see [D2JamDeviceTokenBody].
##
## POST /device/token - Public
func poll_device_link(body: D2JamDeviceTokenBody) -> D2JamDeviceTokenResult:
	var path: String = "/device/token"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_POST, {}, body.to_dict(), false)
	var result: D2JamDeviceTokenResult = D2JamDeviceTokenResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamDeviceToken.from_json(result.payload)
	return result


## Revoke the game token used to authenticate this request. Prefer D2JamAuth.disconnect(), which
## also clears the stored session locally.
##
## DELETE /self/game-tokens/current - Requires user login or game token
func revoke_current_game_token() -> D2JamVoidResult:
	var path: String = "/self/game-tokens/current"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_DELETE, {}, null, true)
	var result: D2JamVoidResult = D2JamVoidResult.new()
	result._apply(response)
	return result


## Begin a device authorization request. Prefer D2JamAuth.start_device_link(), which drives the
## whole flow: showing the user_code, opening verification_uri, and polling poll_device_link()
## until the player approves or denies it on the website.
##
## body - Request payload; see [D2JamDeviceCodeBody].
##
## POST /device/code - Public
func start_device_link(body: D2JamDeviceCodeBody) -> D2JamDeviceCodeResult:
	var path: String = "/device/code"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_POST, {}, body.to_dict(), false)
	var result: D2JamDeviceCodeResult = D2JamDeviceCodeResult.new()
	result._apply(response)
	if result.ok:
		result.data = D2JamDeviceCode.from_json(result.payload)
	return result


## Unlock an achievement for the logged in user. Unlocking one that is already unlocked is
## harmless.
##
## body - Request payload; see [D2JamAchievementBody].
##
## POST /achievement - Requires user login
func unlock_achievement(body: D2JamAchievementBody) -> D2JamVoidResult:
	var path: String = "/achievement"
	var response: D2JamResponse = await request(path, HTTPClient.METHOD_POST, {}, body.to_dict(), true)
	var result: D2JamVoidResult = D2JamVoidResult.new()
	result._apply(response)
	return result


## Upload a PNG, JPEG, GIF or WebP image and return its path on the API.
##
## The returned value is relative (for example /api/v1/image/<uuid>.png); D2JamAPI resolves it to
## an absolute URL for you. Requires login, and the image must be at most 8 MiB.
##
## file_bytes - Raw image data, for example from Image.save_png_to_buffer().
## file_name - File name to send, including an extension the server accepts.
##
## POST /image - Requires user login
func upload_image(file_bytes: PackedByteArray, file_name: String) -> D2JamStringResult:
	var path: String = "/image"
	var response: D2JamResponse = await request_multipart(path, "upload", file_bytes, file_name, true)
	var result: D2JamStringResult = D2JamStringResult.new()
	result._apply(response)
	if result.ok:
		result.data = resolve_url(str(result.payload))
	return result
