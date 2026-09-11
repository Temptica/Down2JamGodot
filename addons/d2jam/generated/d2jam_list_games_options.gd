@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Optional query parameters for [method D2JamAPI.list_games].
##
## Only the properties you assign are sent, so an untouched instance adds nothing to the request.
class_name D2JamListGamesOptions

@export var sort: String = "":
	set(value):
		sort = value
		_track(&"sort", value)

@export var jam_slug: String = "":
	set(value):
		jam_slug = value
		_track(&"jamSlug", value)

@export var jam_id: int = 0:
	set(value):
		jam_id = value
		_track(&"jamId", value)

@export var page_version: String = "":
	set(value):
		page_version = value
		_track(&"pageVersion", value)

@export var cursor: String = "":
	set(value):
		cursor = value
		_track(&"cursor", value)

@export var limit: int = 0:
	set(value):
		limit = value
		_track(&"limit", value)
