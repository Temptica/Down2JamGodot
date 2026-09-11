@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## The result of starting a device authorization request. Show user_code to the player and open
## verification_uri in their browser, then poll poll_device_link() with device_code until it
## resolves. Prefer D2JamAuth.start_device_link(), which drives the whole thing.
class_name D2JamDeviceCode

## Secret. Poll poll_device_link() with this until the player approves or denies the request.
@export var device_code: String = "":
	set(value):
		device_code = value
		_track(&"deviceCode", value)

## Short code to show the player, e.g. ABCD-1234. Already embedded in verification_uri.
@export var user_code: String = "":
	set(value):
		user_code = value
		_track(&"userCode", value)

## Absolute URL to open in the player's browser.
@export var verification_uri: String = "":
	set(value):
		verification_uri = value
		_track(&"verificationUri", value)

## Seconds until the request expires unapproved.
@export var expires_in: int = 0:
	set(value):
		expires_in = value
		_track(&"expiresIn", value)

## Minimum seconds to wait between polls.
@export var interval: int = 0:
	set(value):
		interval = value
		_track(&"interval", value)


## Build a D2JamDeviceCode from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamDeviceCode:
	var result: D2JamDeviceCode = D2JamDeviceCode.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("deviceCode") != null:
		result.device_code = str(data["deviceCode"])
	if data.get("userCode") != null:
		result.user_code = str(data["userCode"])
	if data.get("verificationUri") != null:
		result.verification_uri = str(data["verificationUri"])
	if data.get("expiresIn") != null:
		result.expires_in = int(data["expiresIn"])
	if data.get("interval") != null:
		result.interval = int(data["interval"])

	return result


## Build an array of D2JamDeviceCode from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamDeviceCode]:
	var result: Array[D2JamDeviceCode] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamDeviceCode.from_json(entry))

	return result
