@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## An achievement defined on a game page. Created by the developer on the Down2Jam website; games
## unlock it by id.
class_name D2JamAchievement

@export var id: int = 0:
	set(value):
		id = value
		_track(&"id", value)

@export var name: String = "":
	set(value):
		name = value
		_track(&"name", value)

@export var description: String = "":
	set(value):
		description = value
		_track(&"description", value)

## Absolute URL to the achievement icon.
@export var image: String = "":
	set(value):
		image = value
		_track(&"image", value)

@export var game_page_id: int = 0:
	set(value):
		game_page_id = value
		_track(&"gamePageId", value)

## Everyone who has unlocked this achievement.
@export var users: Array[D2JamUser] = []:
	set(value):
		users = value
		_track(&"users", value)

@export var created_at: String = "":
	set(value):
		created_at = value
		_track(&"createdAt", value)

@export var updated_at: String = "":
	set(value):
		updated_at = value
		_track(&"updatedAt", value)


## Build a D2JamAchievement from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamAchievement:
	var result: D2JamAchievement = D2JamAchievement.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("id") != null:
		result.id = int(data["id"])
	if data.get("name") != null:
		result.name = str(data["name"])
	if data.get("description") != null:
		result.description = str(data["description"])
	if data.get("image") != null:
		result.image = str(data["image"])
	if data.get("gamePageId") != null:
		result.game_page_id = int(data["gamePageId"])
	if data.get("users") != null:
		result.users = D2JamUser.list_from_json(data["users"])
	if data.get("createdAt") != null:
		result.created_at = str(data["createdAt"])
	if data.get("updatedAt") != null:
		result.updated_at = str(data["updatedAt"])

	return result


## Build an array of D2JamAchievement from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamAchievement]:
	var result: Array[D2JamAchievement] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamAchievement.from_json(entry))

	return result
