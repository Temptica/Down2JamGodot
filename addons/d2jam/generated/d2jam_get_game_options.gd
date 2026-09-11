@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Optional query parameters for [method D2JamAPI.get_game].
##
## Only the properties you assign are sent, so an untouched instance adds nothing to the request.
class_name D2JamGetGameOptions

@export var recap: bool = false:
	set(value):
		recap = value
		_track(&"recap", value)

@export var preview: bool = false:
	set(value):
		preview = value
		_track(&"preview", value)
