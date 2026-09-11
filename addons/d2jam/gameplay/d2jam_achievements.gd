@tool
extends Node

## Achievement helper: unlock by name, and ask what the player has already earned.
##
## Achievements are defined by the developer on the game's page on d2jam.com, each with a name, a
## description and an icon. A game unlocks them for the logged in player; it cannot create them.
## As with leaderboards there is no dedicated read endpoint, so the list arrives as part of the
## game and is cached here.
##
## [codeblock]
## func _on_boss_defeated() -> void:
##     await d2jam.achievements.unlock("First Blood")
##
## func _ready() -> void:
##     if await d2jam.achievements.is_unlocked("First Blood"):
##         show_veteran_intro()
## [/codeblock]
class_name D2JamAchievements

## Emitted after an achievement is unlocked for the player. Good place to pop a toast.
signal unlocked(achievement: D2JamAchievement)

## Emitted when an unlock fails, with a message safe to show the player.
signal unlock_failed(message: String)

## The generated client. [D2JamService] assigns this.
@export var api: D2JamAPI

## Slug of the game whose achievements these are, as it appears in the d2jam.com URL.
@export var game_slug: String = ""

## Supplies the game and caches it. [D2JamService] assigns this so the leaderboard and achievement
## nodes share a single fetch. When unset this node fetches on its own.
var game_provider: Callable

var _cached_game: D2JamGame
var _unlocked_this_session: Dictionary = {}


## The achievement helper in the current scene, or null when none is in the tree.
##
## [D2JamService] adds one as a child of itself, so this is set for you.
static var instance: D2JamAchievements


func _enter_tree() -> void:
	if instance == null:
		instance = self


func _exit_tree() -> void:
	if instance == self:
		instance = null


## Every achievement on the game page. Cached after the first call.
func all(force_refresh: bool = false) -> Array[D2JamAchievement]:
	var game: D2JamGame = await _game(force_refresh)
	if game == null:
		var empty: Array[D2JamAchievement] = []
		return empty

	return game.achievements


## Find an achievement by name, case-insensitively. Returns null when there is no such achievement.
func find(achievement_name: String) -> D2JamAchievement:
	var wanted: String = achievement_name.strip_edges().to_lower()

	for achievement: D2JamAchievement in await all():
		if achievement.name.strip_edges().to_lower() == wanted:
			return achievement

	return null


## Unlock an achievement for the logged in player.
##
## Safe to call repeatedly: an achievement already unlocked in this session is skipped without a
## request, and the API itself treats a repeat unlock as a no-op. Returns true when the player holds
## the achievement afterwards.
func unlock(achievement_name: String) -> bool:
	var achievement: D2JamAchievement = await find(achievement_name)

	if achievement == null:
		var message: String = "No achievement named '%s' on %s. Create it on the game's page first." \
				% [achievement_name, game_slug]
		push_warning("[d2jam] " + message)
		unlock_failed.emit(message)
		return false

	return await unlock_achievement(achievement)


## Unlock an achievement you already hold, skipping the name lookup.
func unlock_achievement(achievement: D2JamAchievement) -> bool:
	if not _require_api():
		return false

	if _unlocked_this_session.has(achievement.id):
		return true

	var result: D2JamVoidResult = await api.unlock_achievement(
			D2JamAchievementBody.create(achievement.id))

	if not result.ok:
		api.report(result)
		unlock_failed.emit(result.error_message)
		return false

	_unlocked_this_session[achievement.id] = true
	_cached_game = null
	unlocked.emit(achievement)
	return true


## Take an achievement back off the player. Mostly useful while testing.
func revoke(achievement_name: String) -> bool:
	if not _require_api():
		return false

	var achievement: D2JamAchievement = await find(achievement_name)
	if achievement == null:
		return false

	var result: D2JamVoidResult = await api.lock_achievement(
			D2JamAchievementBody.create(achievement.id))

	if not result.ok:
		api.report(result)
		return false

	_unlocked_this_session.erase(achievement.id)
	_cached_game = null
	return true


## True when the given user holds the achievement. Defaults to the logged in player.
func is_unlocked(achievement_name: String, user_slug: String = "") -> bool:
	var achievement: D2JamAchievement = await find(achievement_name)
	if achievement == null:
		return false

	if _unlocked_this_session.has(achievement.id) and user_slug.is_empty():
		return true

	return _holds(achievement, _resolve_slug(user_slug))


## Every achievement the given user holds. Defaults to the logged in player.
func unlocked_by(user_slug: String = "") -> Array[D2JamAchievement]:
	var slug: String = _resolve_slug(user_slug)
	var held: Array[D2JamAchievement] = []

	if slug.is_empty():
		return held

	for achievement: D2JamAchievement in await all():
		if _holds(achievement, slug):
			held.append(achievement)

	return held


## Fraction of the game's achievements the user holds, from 0.0 to 1.0. Defaults to the logged in
## player. Returns 0.0 when the game has no achievements.
func completion(user_slug: String = "") -> float:
	var total: int = (await all()).size()
	if total == 0:
		return 0.0

	return float((await unlocked_by(user_slug)).size()) / float(total)


## Drop the cached game so the next read hits the API.
func invalidate_cache() -> void:
	_cached_game = null


func _holds(achievement: D2JamAchievement, slug: String) -> bool:
	if slug.is_empty():
		return false

	for holder: D2JamUser in achievement.users:
		if holder.slug == slug:
			return true

	return false


func _resolve_slug(user_slug: String) -> String:
	if not user_slug.is_empty():
		return user_slug

	if api != null and api.auth != null:
		return api.auth.user_slug()

	return ""


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

	push_error("[d2jam] D2JamAchievements has no API assigned. Add it through D2JamService.")
	return false
