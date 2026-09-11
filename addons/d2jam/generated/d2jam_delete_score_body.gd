@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Payload for DELETE /score.
class_name D2JamDeleteScoreBody

@export var score_id: int = 0:
	set(value):
		score_id = value
		_track(&"scoreId", value)


## Build a D2JamDeleteScoreBody from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamDeleteScoreBody:
	var result: D2JamDeleteScoreBody = D2JamDeleteScoreBody.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("scoreId") != null:
		result.score_id = int(data["scoreId"])

	return result


## Construct with every required field set.
static func create(score_id: int) -> D2JamDeleteScoreBody:
	var body: D2JamDeleteScoreBody = D2JamDeleteScoreBody.new()
	body.score_id = score_id
	return body
