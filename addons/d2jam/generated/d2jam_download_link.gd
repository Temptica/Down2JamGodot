@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## A platform download or play link on a game page.
class_name D2JamDownloadLink

@export var id: int = 0:
	set(value):
		id = value
		_track(&"id", value)

@export var url: String = "":
	set(value):
		url = value
		_track(&"url", value)

@export var platform: String = "":
	set(value):
		platform = value
		_track(&"platform", value)

@export var game_page_id: int = 0:
	set(value):
		game_page_id = value
		_track(&"gamePageId", value)


## Build a D2JamDownloadLink from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamDownloadLink:
	var result: D2JamDownloadLink = D2JamDownloadLink.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("id") != null:
		result.id = int(data["id"])
	if data.get("url") != null:
		result.url = str(data["url"])
	if data.get("platform") != null:
		result.platform = str(data["platform"])
	if data.get("gamePageId") != null:
		result.game_page_id = int(data["gamePageId"])

	return result


## Build an array of D2JamDownloadLink from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamDownloadLink]:
	var result: Array[D2JamDownloadLink] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamDownloadLink.from_json(entry))

	return result
