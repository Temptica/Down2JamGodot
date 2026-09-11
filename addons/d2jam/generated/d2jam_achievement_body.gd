@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Payload for POST and DELETE /achievement.
class_name D2JamAchievementBody

## Id of the achievement on the game page.
@export var achievement_id: int = 0:
	set(value):
		achievement_id = value
		_track(&"achievementId", value)


## Build a D2JamAchievementBody from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamAchievementBody:
	var result: D2JamAchievementBody = D2JamAchievementBody.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("achievementId") != null:
		result.achievement_id = int(data["achievementId"])

	return result


## Construct with every required field set.
static func create(achievement_id: int) -> D2JamAchievementBody:
	var body: D2JamAchievementBody = D2JamAchievementBody.new()
	body.achievement_id = achievement_id
	return body
