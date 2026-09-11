@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## The result of one poll of a pending device authorization request. A denied or expired request
## comes back as a failed D2JamResult instead of a status here.
class_name D2JamDeviceToken

## authorization_pending, slow_down, or approved.
@export var status: String = "":
	set(value):
		status = value
		_track(&"status", value)

## The game's access token. Only present when status is approved; empty otherwise.
@export var token: String = "":
	set(value):
		token = value
		_track(&"token", value)

## The player who approved the request. Only present when status is approved; /self does not accept
## a game token, so this is the only way to learn who just linked their account.
@export var user: D2JamUser:
	set(value):
		user = value
		_track(&"user", value)


## Build a D2JamDeviceToken from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamDeviceToken:
	var result: D2JamDeviceToken = D2JamDeviceToken.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("status") != null:
		result.status = str(data["status"])
	if data.get("token") != null:
		result.token = str(data["token"])
	if data.get("user") != null:
		result.user = D2JamUser.from_json(data["user"])

	return result


## Build an array of D2JamDeviceToken from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamDeviceToken]:
	var result: Array[D2JamDeviceToken] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamDeviceToken.from_json(entry))

	return result
