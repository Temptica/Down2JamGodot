@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Payload for POST /device/code.
class_name D2JamDeviceCodeBody

## Human readable name for this device or install, shown to the player when they approve it on the
## website.
@export var client_name: String = "":
	set(value):
		client_name = value
		_track(&"clientName", value)

## The game this token will be scoped to. The issued game token only ever works for this one game
## -- see D2JamService.game_slug.
@export var game_slug: String = "":
	set(value):
		game_slug = value
		_track(&"gameSlug", value)


## Build a D2JamDeviceCodeBody from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamDeviceCodeBody:
	var result: D2JamDeviceCodeBody = D2JamDeviceCodeBody.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("clientName") != null:
		result.client_name = str(data["clientName"])
	if data.get("gameSlug") != null:
		result.game_slug = str(data["gameSlug"])

	return result


## Construct with every required field set.
static func create(client_name: String, game_slug: String) -> D2JamDeviceCodeBody:
	var body: D2JamDeviceCodeBody = D2JamDeviceCodeBody.new()
	body.client_name = client_name
	body.game_slug = game_slug
	return body
