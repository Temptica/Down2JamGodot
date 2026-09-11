@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## A game entry. GET /games/{gameSlug} is the only endpoint that returns leaderboards and
## achievements, so it is also the read path for both.
class_name D2JamGame

@export var id: int = 0:
	set(value):
		id = value
		_track(&"id", value)

@export var slug: String = "":
	set(value):
		slug = value
		_track(&"slug", value)

## Only populated by the list endpoints, which flatten it from the jam page. GET /games/{gameSlug}
## leaves it empty; read jam_page.name there, or call D2JamService.page().
@export var name: String = "":
	set(value):
		name = value
		_track(&"name", value)

## List endpoints only, like name.
@export var description: String = "":
	set(value):
		description = value
		_track(&"description", value)

## List endpoints only, like name.
@export var short: String = "":
	set(value):
		short = value
		_track(&"short", value)

## List endpoints only, like name.
@export var thumbnail: String = "":
	set(value):
		thumbnail = value
		_track(&"thumbnail", value)

## REGULAR, ODA or EXTERNAL.
@export var category: String = "":
	set(value):
		category = value
		_track(&"category", value)

@export var published: bool = false:
	set(value):
		published = value
		_track(&"published", value)

@export var published_at: String = "":
	set(value):
		published_at = value
		_track(&"publishedAt", value)

@export var source_url: String = "":
	set(value):
		source_url = value
		_track(&"sourceUrl", value)

@export var source_platform: String = "":
	set(value):
		source_platform = value
		_track(&"sourcePlatform", value)

@export var team_id: int = 0:
	set(value):
		team_id = value
		_track(&"teamId", value)

@export var jam_id: int = 0:
	set(value):
		jam_id = value
		_track(&"jamId", value)

@export var jam: D2JamJam:
	set(value):
		jam = value
		_track(&"jam", value)

@export var pages: Array[D2JamGamePage] = []:
	set(value):
		pages = value
		_track(&"pages", value)

## The page as submitted to the jam. Only populated by GET /games/{gameSlug}, and the place its
## name, description and artwork actually live.
@export var jam_page: D2JamGamePage:
	set(value):
		jam_page = value
		_track(&"jamPage", value)

## The post-jam page, when the team published one. Only populated by GET /games/{gameSlug}.
@export var post_jam_page: D2JamGamePage:
	set(value):
		post_jam_page = value
		_track(&"postJamPage", value)

@export var download_links: Array[D2JamDownloadLink] = []:
	set(value):
		download_links = value
		_track(&"downloadLinks", value)

## Only populated by GET /games/{gameSlug}.
@export var leaderboards: Array[D2JamLeaderboard] = []:
	set(value):
		leaderboards = value
		_track(&"leaderboards", value)

## Only populated by GET /games/{gameSlug}.
@export var achievements: Array[D2JamAchievement] = []:
	set(value):
		achievements = value
		_track(&"achievements", value)

@export var created_at: String = "":
	set(value):
		created_at = value
		_track(&"createdAt", value)

@export var updated_at: String = "":
	set(value):
		updated_at = value
		_track(&"updatedAt", value)


## Build a D2JamGame from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamGame:
	var result: D2JamGame = D2JamGame.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("id") != null:
		result.id = int(data["id"])
	if data.get("slug") != null:
		result.slug = str(data["slug"])
	if data.get("name") != null:
		result.name = str(data["name"])
	if data.get("description") != null:
		result.description = str(data["description"])
	if data.get("short") != null:
		result.short = str(data["short"])
	if data.get("thumbnail") != null:
		result.thumbnail = str(data["thumbnail"])
	if data.get("category") != null:
		result.category = str(data["category"])
	if data.get("published") != null:
		result.published = bool(data["published"])
	if data.get("publishedAt") != null:
		result.published_at = str(data["publishedAt"])
	if data.get("sourceUrl") != null:
		result.source_url = str(data["sourceUrl"])
	if data.get("sourcePlatform") != null:
		result.source_platform = str(data["sourcePlatform"])
	if data.get("teamId") != null:
		result.team_id = int(data["teamId"])
	if data.get("jamId") != null:
		result.jam_id = int(data["jamId"])
	if data.get("jam") != null:
		result.jam = D2JamJam.from_json(data["jam"])
	if data.get("pages") != null:
		result.pages = D2JamGamePage.list_from_json(data["pages"])
	if data.get("jamPage") != null:
		result.jam_page = D2JamGamePage.from_json(data["jamPage"])
	if data.get("postJamPage") != null:
		result.post_jam_page = D2JamGamePage.from_json(data["postJamPage"])
	if data.get("downloadLinks") != null:
		result.download_links = D2JamDownloadLink.list_from_json(data["downloadLinks"])
	if data.get("leaderboards") != null:
		result.leaderboards = D2JamLeaderboard.list_from_json(data["leaderboards"])
	if data.get("achievements") != null:
		result.achievements = D2JamAchievement.list_from_json(data["achievements"])
	if data.get("createdAt") != null:
		result.created_at = str(data["createdAt"])
	if data.get("updatedAt") != null:
		result.updated_at = str(data["updatedAt"])

	return result


## Build an array of D2JamGame from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamGame]:
	var result: Array[D2JamGame] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamGame.from_json(entry))

	return result
