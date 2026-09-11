@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## A leaderboard attached to a game page. Created by the developer on the Down2Jam website; games
## submit to it by id.
class_name D2JamLeaderboard

@export var id: int = 0:
	set(value):
		id = value
		_track(&"id", value)

@export var name: String = "":
	set(value):
		name = value
		_track(&"name", value)

## One of SCORE, GOLF, SPEEDRUN or ENDURANCE. See the TYPE_* constants on D2JamLeaderboards.
@export var type: String = "":
	set(value):
		type = value
		_track(&"type", value)

## Decimal precision for SCORE and GOLF boards. Ignored for time based boards.
@export var decimal_places: int = 0:
	set(value):
		decimal_places = value
		_track(&"decimalPlaces", value)

## How many rows the website shows per page.
@export var max_users_shown: int = 0:
	set(value):
		max_users_shown = value
		_track(&"maxUsersShown", value)

## When true only each user's best entry is ranked.
@export var only_best: bool = false:
	set(value):
		only_best = value
		_track(&"onlyBest", value)

@export var game_page_id: int = 0:
	set(value):
		game_page_id = value
		_track(&"gamePageId", value)

## Every submitted score, unsorted. Use D2JamLeaderboards.rank() to order them.
@export var scores: Array[D2JamScore] = []:
	set(value):
		scores = value
		_track(&"scores", value)

@export var created_at: String = "":
	set(value):
		created_at = value
		_track(&"createdAt", value)

@export var updated_at: String = "":
	set(value):
		updated_at = value
		_track(&"updatedAt", value)


## Build a D2JamLeaderboard from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamLeaderboard:
	var result: D2JamLeaderboard = D2JamLeaderboard.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("id") != null:
		result.id = int(data["id"])
	if data.get("name") != null:
		result.name = str(data["name"])
	if data.get("type") != null:
		result.type = str(data["type"])
	if data.get("decimalPlaces") != null:
		result.decimal_places = int(data["decimalPlaces"])
	if data.get("maxUsersShown") != null:
		result.max_users_shown = int(data["maxUsersShown"])
	if data.get("onlyBest") != null:
		result.only_best = bool(data["onlyBest"])
	if data.get("gamePageId") != null:
		result.game_page_id = int(data["gamePageId"])
	if data.get("scores") != null:
		result.scores = D2JamScore.list_from_json(data["scores"])
	if data.get("createdAt") != null:
		result.created_at = str(data["createdAt"])
	if data.get("updatedAt") != null:
		result.updated_at = str(data["updatedAt"])

	return result


## Build an array of D2JamLeaderboard from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamLeaderboard]:
	var result: Array[D2JamLeaderboard] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamLeaderboard.from_json(entry))

	return result
