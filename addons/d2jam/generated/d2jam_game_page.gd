@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## One version of a game's page. A game has a JAM page and optionally a POST_JAM page.
class_name D2JamGamePage

@export var id: int = 0:
	set(value):
		id = value
		_track(&"id", value)

## JAM or POST_JAM.
@export var version: String = "":
	set(value):
		version = value
		_track(&"version", value)

@export var name: String = "":
	set(value):
		name = value
		_track(&"name", value)

@export var description: String = "":
	set(value):
		description = value
		_track(&"description", value)

@export var short: String = "":
	set(value):
		short = value
		_track(&"short", value)

@export var thumbnail: String = "":
	set(value):
		thumbnail = value
		_track(&"thumbnail", value)

@export var banner: String = "":
	set(value):
		banner = value
		_track(&"banner", value)

@export var screenshots: Array[String] = []:
	set(value):
		screenshots = value
		_track(&"screenshots", value)

@export var trailer_url: String = "":
	set(value):
		trailer_url = value
		_track(&"trailerUrl", value)

@export var emote_prefix: String = "":
	set(value):
		emote_prefix = value
		_track(&"emotePrefix", value)

@export var game_id: int = 0:
	set(value):
		game_id = value
		_track(&"gameId", value)

@export var download_links: Array[D2JamDownloadLink] = []:
	set(value):
		download_links = value
		_track(&"downloadLinks", value)


## Build a D2JamGamePage from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamGamePage:
	var result: D2JamGamePage = D2JamGamePage.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("id") != null:
		result.id = int(data["id"])
	if data.get("version") != null:
		result.version = str(data["version"])
	if data.get("name") != null:
		result.name = str(data["name"])
	if data.get("description") != null:
		result.description = str(data["description"])
	if data.get("short") != null:
		result.short = str(data["short"])
	if data.get("thumbnail") != null:
		result.thumbnail = str(data["thumbnail"])
	if data.get("banner") != null:
		result.banner = str(data["banner"])
	if data.get("screenshots") != null:
		var entries: Array[String] = []
		for entry: Variant in data["screenshots"]:
			entries.append(str(entry))
		result.screenshots = entries
	if data.get("trailerUrl") != null:
		result.trailer_url = str(data["trailerUrl"])
	if data.get("emotePrefix") != null:
		result.emote_prefix = str(data["emotePrefix"])
	if data.get("gameId") != null:
		result.game_id = int(data["gameId"])
	if data.get("downloadLinks") != null:
		result.download_links = D2JamDownloadLink.list_from_json(data["downloadLinks"])

	return result


## Build an array of D2JamGamePage from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamGamePage]:
	var result: Array[D2JamGamePage] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamGamePage.from_json(entry))

	return result
