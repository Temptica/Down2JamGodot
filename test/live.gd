extends SceneTree

func _initialize() -> void:
	var service := D2JamService.new()
	service.game_slug = "weldroot"
	service.restore_on_ready = false
	service.persist_session = false
	root.add_child(service)
	await process_frame

	print("-- GET /games/weldroot through D2JamAPI --")
	var game := await service.fetch_game()
	if game == null:
		print("FAILED: no game returned")
		quit(1)
		return

	print("game: %s (id %d, jam '%s')" % [await service.game_name(), game.id, game.jam.name])
	print("leaderboards: %d, achievements: %d"
			% [game.leaderboards.size(), game.achievements.size()])

	for board in await service.leaderboards.boards():
		var ranked := service.leaderboards.rank(board)
		print("  board '%s' [%s] - %d ranked entries" % [board.name, board.type, ranked.size()])
		for i in mini(3, ranked.size()):
			print("    %d. %-16s %s" % [
				i + 1,
				ranked[i].user.name,
				D2JamLeaderboards.format(board, ranked[i]),
			])

	for achievement in await service.achievements.all():
		print("  achievement '%s' - %d players: %s"
				% [achievement.name, achievement.users.size(), achievement.description])

	print("-- caching: a second fetch must not hit the network --")
	var again := await service.fetch_game()
	print("same instance reused: %s" % str(again == game))

	print("-- GET /games?limit=2 with options --")
	var options := D2JamListGamesOptions.new()
	options.limit = 2
	var listed := await service.api.list_games(options)
	print("ok=%s, %d games: %s" % [
		listed.ok,
		listed.data.size(),
		", ".join(listed.data.map(func(g: D2JamGame) -> String: return g.name)),
	])

	print("-- an authenticated call with no session must fail cleanly --")
	var denied := await service.api.get_self()
	print("ok=%s -> %s" % [denied.ok, denied.describe()])

	print("-- starting a device link must return a code --")
	var code_result := await service.api.start_device_link(
			D2JamDeviceCodeBody.create("live-test-probe", service.game_slug))
	if code_result.ok:
		print("ok=true -> user_code=%s" % code_result.data.user_code)
	else:
		print("ok=false -> %s" % code_result.describe())

	print("-- polling a bogus device code must fail cleanly --")
	var poll_result := await service.api.poll_device_link(
			D2JamDeviceTokenBody.create("d2jd_not_a_real_code"))
	print("ok=%s -> %s" % [poll_result.ok, poll_result.describe()])

	quit(0)
