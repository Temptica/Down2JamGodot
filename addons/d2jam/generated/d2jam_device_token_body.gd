@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Payload for POST /device/token.
class_name D2JamDeviceTokenBody

@export var device_code: String = "":
	set(value):
		device_code = value
		_track(&"deviceCode", value)


## Build a D2JamDeviceTokenBody from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamDeviceTokenBody:
	var result: D2JamDeviceTokenBody = D2JamDeviceTokenBody.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("deviceCode") != null:
		result.device_code = str(data["deviceCode"])

	return result


## Construct with every required field set.
static func create(device_code: String) -> D2JamDeviceTokenBody:
	var body: D2JamDeviceTokenBody = D2JamDeviceTokenBody.new()
	body.device_code = device_code
	return body
