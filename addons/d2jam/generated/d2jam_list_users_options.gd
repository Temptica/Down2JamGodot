@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Optional query parameters for [method D2JamAPI.list_users].
##
## Only the properties you assign are sent, so an untouched instance adds nothing to the request.
class_name D2JamListUsersOptions

@export var cursor: String = "":
	set(value):
		cursor = value
		_track(&"cursor", value)

@export var limit: int = 0:
	set(value):
		limit = value
		_track(&"limit", value)
