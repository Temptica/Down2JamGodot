extends SceneTree

var failures: int = 0


func check(label: String, condition: bool, detail: String = "") -> void:
	if condition:
		print("  ok   %s" % label)
	else:
		failures += 1
		print("  FAIL %s %s" % [label, detail])


func _initialize() -> void:
	print("== parsing a live game response ==")
	var file := FileAccess.open("res://test/fixtures/weldroot.json", FileAccess.READ)
	var envelope: Variant = JSON.parse_string(file.get_as_text())
	var game := D2JamGame.from_json((envelope as Dictionary)["data"])

	check("slug", game.slug == "weldroot", game.slug)
	check("detail responses leave the flattened name empty", game.name.is_empty(), game.name)
	check("the name lives on the jam page",
			game.jam_page != null and game.jam_page.name == "Weldroot")
	check("jam parsed as a nested model", game.jam != null and not game.jam.name.is_empty())
	check("pages parsed", game.pages.size() > 0)
	check("one leaderboard", game.leaderboards.size() == 1, str(game.leaderboards.size()))
	check("three achievements", game.achievements.size() == 3, str(game.achievements.size()))

	var board := game.leaderboards[0]
	check("board id", board.id == 88, str(board.id))
	check("board type", board.type == D2JamLeaderboards.TYPE_SCORE, board.type)
	check("board only_best", board.only_best)
	check("scores parsed", board.scores.size() == 24, str(board.scores.size()))
	check("score user is a model", board.scores[0].user is D2JamUser)
	check("snake_case mapping", board.scores[0].leaderboard_id == 88)

	print("== ranking ==")
	var boards := D2JamLeaderboards.new()
	var ranked := boards.rank(board)
	check("only_best collapses duplicates", ranked.size() < board.scores.size(),
			"%d of %d" % [ranked.size(), board.scores.size()])
	check("sorted high to low", ranked[0].data >= ranked[ranked.size() - 1].data)
	check("one entry per user", _unique_users(ranked) == ranked.size())
	print("  top: %s with %s" % [ranked[0].user.name, D2JamLeaderboards.format(board, ranked[0])])
	check("position_of finds the leader",
			boards.position_of(board, ranked[0].user.slug) == 1)
	check("position_of misses a stranger", boards.position_of(board, "nobody-at-all") == -1)

	print("== value conversion ==")
	check("SCORE board with no decimals sends an int",
			typeof(D2JamLeaderboards.to_payload(board, 16340.0)) == TYPE_INT)
	check("SCORE payload value", D2JamLeaderboards.to_payload(board, 16340.4) == 16340)

	var speedrun := D2JamLeaderboard.new()
	speedrun.type = D2JamLeaderboards.TYPE_SPEEDRUN
	check("SPEEDRUN is time based", D2JamLeaderboards.is_time_based(speedrun))
	check("SPEEDRUN ranks low first", not D2JamLeaderboards.higher_is_better(speedrun))
	check("SPEEDRUN payload is whole milliseconds",
			D2JamLeaderboards.to_payload(speedrun, 82451.0) == 82451)

	var run := D2JamScore.new()
	run.data = 82451
	check("time formatting", D2JamLeaderboards.format(speedrun, run) == "1:22.451",
			D2JamLeaderboards.format(speedrun, run))
	run.data = 3725100
	check("time formatting past an hour",
			D2JamLeaderboards.format(speedrun, run) == "1:02:05.100",
			D2JamLeaderboards.format(speedrun, run))

	var precise := D2JamLeaderboard.new()
	precise.type = D2JamLeaderboards.TYPE_SCORE
	precise.decimal_places = 2
	check("decimal board keeps the fraction",
			is_equal_approx(D2JamLeaderboards.to_payload(precise, 12.345), 12.35),
			str(D2JamLeaderboards.to_payload(precise, 12.345)))

	print("== request bodies ==")
	var score_body := D2JamCreateScoreBody.create(88, 16340)
	score_body.evidence = "https://d2jam.com/api/v1/image/shot.png"
	var payload := score_body.to_dict()
	check("body uses API spelling", payload.has("leaderboardId") and payload.has("score"))
	check("body omits untouched fields", not payload.has("evidenceUrl"), JSON.stringify(payload))
	check("body values", payload["leaderboardId"] == 88 and payload["score"] == 16340)

	var device_code_body := D2JamDeviceCodeBody.create("Test Rig", "weldroot")
	check("device code body", device_code_body.to_dict() ==
			{"clientName": "Test Rig", "gameSlug": "weldroot"},
			device_code_body.to_json())

	var device_token_body := D2JamDeviceTokenBody.create("d2jd_abc")
	check("device token body", device_token_body.to_dict() == {"deviceCode": "d2jd_abc"},
			device_token_body.to_json())

	print("== query encoding ==")
	var options := D2JamListGamesOptions.new()
	options.limit = 5
	options.jam_slug = "d2jam-9"
	var query := D2JamHttpClient.encode_query(options.to_dict())
	check("only assigned options are sent", query == "?limit=5&jamSlug=d2jam-9", query)
	check("empty options make no query", D2JamHttpClient.encode_query({}) == "")
	check("booleans are lowercased",
			D2JamHttpClient.encode_query({"recap": true}) == "?recap=true")
	check("values are escaped",
			D2JamHttpClient.encode_query({"q": "a b&c"}) == "?q=a%20b%26c",
			D2JamHttpClient.encode_query({"q": "a b&c"}))
	check("path segments are escaped",
			D2JamHttpClient.encode_path_segment("a/b") == "a%2Fb",
			D2JamHttpClient.encode_path_segment("a/b"))

	print("== envelope handling ==")
	check("success envelope", _result_of(200, '{"success":true,"data":{"slug":"x"}}').ok)
	var failed := _result_of(401, '{"success":false,"error":{"code":"ERR_UNAUTHORIZED","message":"Unauthorized"}}')
	check("error envelope is not ok", not failed.ok)
	check("error code", failed.error_code == "ERR_UNAUTHORIZED", failed.error_code)
	check("error is recognised as unauthenticated", failed.is_unauthenticated())
	check("error describes itself", failed.describe() == "ERR_UNAUTHORIZED: Unauthorized",
			failed.describe())
	var bare := _result_of(200, '{"message":"Score added"}')
	check("bare message body counts as success", bare.ok and bare.message == "Score added")
	var html := _result_of(502, "<html>bad gateway</html>")
	check("non-JSON failure is not ok", not html.ok)
	check("non-JSON failure keeps the status", html.status_code == 502)

	print("== device link parsing ==")
	var device_code := D2JamDeviceCode.from_json({
		"deviceCode": "d2jd_abc",
		"userCode": "ABCD-1234",
		"verificationUri": "https://d2jam.com/link-device?code=ABCD-1234",
		"expiresIn": 600,
		"interval": 5,
	})
	check("device code", device_code.device_code == "d2jd_abc")
	check("user code", device_code.user_code == "ABCD-1234")
	check("verification uri", device_code.verification_uri.begins_with("https://d2jam.com/link-device"))
	check("expires in", device_code.expires_in == 600)
	check("interval", device_code.interval == 5)

	var device_token := D2JamDeviceToken.from_json({
		"status": "approved",
		"token": "d2j_xyz",
		"user": {"id": 7, "slug": "ategon", "name": "Ategon", "profilePicture": null},
	})
	check("device token status", device_token.status == "approved")
	check("device token value", device_token.token == "d2j_xyz")
	check("device token carries the approving user, since GET /self cannot",
			device_token.user != null and device_token.user.slug == "ategon")

	var user := D2JamUser.from_json({"id": 93, "slug": "temptica", "password": "$2b$hash"})
	check("unexpected fields like a password hash are dropped by the model parser",
			not user.to_dict().has("password"), JSON.stringify(user.to_dict()))

	print("== auth headers ==")
	var auth := D2JamAuth.new()
	auth.persist = false
	auth.adopt("game-token-1")
	check("logged in", auth.is_logged_in())
	var headers := auth.authorization_headers()
	check("single bearer header, no cookie", headers.size() == 1, ", ".join(headers))
	check("bearer header", headers[0] == "Authorization: Bearer game-token-1", headers[0])

	auth.clear()
	check("clear logs out", not auth.is_logged_in())
	auth.free()
	boards.free()

	print("== session persistence ==")
	var persisted_path := "user://d2jam_verify_session.json"
	var writer := D2JamAuth.new()
	writer.persist = true
	writer.storage_path = persisted_path
	var approved_user := D2JamUser.from_json(
			{"id": 7, "slug": "ategon", "name": "Ategon", "profilePicture": null})
	writer.adopt("game-token-2", approved_user)

	var reader := D2JamAuth.new()
	reader.persist = true
	reader.storage_path = persisted_path
	check("a saved session restores without any network call", reader.load_session())
	check("restored token", reader.game_token == "game-token-2", reader.game_token)
	check("restored profile survives the round trip, since nothing can re-fetch it after a restart",
			reader.user != null and reader.user.slug == "ategon" and reader.user.name == "Ategon")

	reader.clear()
	writer.free()
	reader.free()

	print("== url resolution ==")
	var api := D2JamAPIBase.new()
	api.api_host = D2JamAPIBase.DEFAULT_HOST
	check("relative upload path becomes absolute",
			api.resolve_url("/api/v1/image/a.png") == "https://d2jam.com/api/v1/image/a.png",
			api.resolve_url("/api/v1/image/a.png"))
	check("absolute urls pass through",
			api.resolve_url("https://cdn.example/x.png") == "https://cdn.example/x.png")

	api.free()

	print("== singletons ==")
	check("nothing registered before a service exists", D2JamService.instance == null)

	var service := D2JamService.new()
	service.persist_session = false
	service.restore_on_ready = false
	service.game_slug = "weldroot"
	root.add_child(service)

	# In a bare SceneTree script the root window is not inside the tree yet during _initialize, so
	# nodes added here do not get _enter_tree until the first frame. A game never sees this: a node
	# coming from a scene, or added by something already in the tree, registers immediately.
	await process_frame

	check("the service registers itself", D2JamService.instance == service)
	check("the helpers exist by the time instance is set", service.api != null)
	check("the api registers itself",
			service.api != null and D2JamAPI.instance == service.api)
	check("auth registers itself",
			service.auth != null and D2JamAuth.instance == service.auth)
	check("leaderboards register themselves",
			service.leaderboards != null and D2JamLeaderboards.instance == service.leaderboards)
	check("achievements register themselves",
			service.achievements != null and D2JamAchievements.instance == service.achievements)
	check("game_slug reaches the helpers",
			service.leaderboards.game_slug == "weldroot"
			and service.achievements.game_slug == "weldroot")

	service.game_slug = "little-plant-care"
	check("changing game_slug follows through to the helpers",
			service.leaderboards.game_slug == "little-plant-care"
			and service.achievements.game_slug == "little-plant-care")

	var built_api := service.api
	root.remove_child(service)
	check("the service stands down when it leaves the tree", D2JamService.instance == null)
	check("so do its children", D2JamAPI.instance == null and D2JamAuth.instance == null)

	root.add_child(service)
	check("re-entering the tree re-registers", D2JamService.instance == service)
	check("without rebuilding the children", service.api == built_api)
	root.remove_child(service)
	service.free()

	print()
	if failures == 0:
		print("all checks passed")
	else:
		print("%d CHECK(S) FAILED" % failures)

	quit(1 if failures > 0 else 0)


func _unique_users(entries: Array[D2JamScore]) -> int:
	var seen := {}
	for entry in entries:
		seen[entry.user_id] = true
	return seen.size()


func _result_of(status: int, body: String) -> D2JamResult:
	var response := D2JamResponse.new()
	response.status_code = status
	response.body = body.to_utf8_buffer()

	var result := D2JamResult.new()
	result._apply(response)
	return result
