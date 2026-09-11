@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## A single leaderboard entry.
class_name D2JamScore

@export var id: int = 0:
	set(value):
		id = value
		_track(&"id", value)

## The stored raw value. For SCORE and GOLF boards this is the submitted value multiplied by 10 ^
## decimal_places; for SPEEDRUN and ENDURANCE boards it is a duration in milliseconds. Use
## D2JamLeaderboards.score_to_display() rather than reading this directly.
@export var data: int = 0:
	set(value):
		data = value
		_track(&"data", value)

## URL of the screenshot backing this score. May be empty.
@export var evidence: String = "":
	set(value):
		evidence = value
		_track(&"evidence", value)

@export var user_id: int = 0:
	set(value):
		user_id = value
		_track(&"userId", value)

@export var leaderboard_id: int = 0:
	set(value):
		leaderboard_id = value
		_track(&"leaderboardId", value)

@export var user: D2JamUser:
	set(value):
		user = value
		_track(&"user", value)

@export var created_at: String = "":
	set(value):
		created_at = value
		_track(&"createdAt", value)

@export var updated_at: String = "":
	set(value):
		updated_at = value
		_track(&"updatedAt", value)


## Build a D2JamScore from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamScore:
	var result: D2JamScore = D2JamScore.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("id") != null:
		result.id = int(data["id"])
	if data.get("data") != null:
		result.data = int(data["data"])
	if data.get("evidence") != null:
		result.evidence = str(data["evidence"])
	if data.get("userId") != null:
		result.user_id = int(data["userId"])
	if data.get("leaderboardId") != null:
		result.leaderboard_id = int(data["leaderboardId"])
	if data.get("user") != null:
		result.user = D2JamUser.from_json(data["user"])
	if data.get("createdAt") != null:
		result.created_at = str(data["createdAt"])
	if data.get("updatedAt") != null:
		result.updated_at = str(data["updatedAt"])

	return result


## Build an array of D2JamScore from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamScore]:
	var result: Array[D2JamScore] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamScore.from_json(entry))

	return result
