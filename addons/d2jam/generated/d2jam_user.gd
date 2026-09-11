@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## A Down2Jam user account.
class_name D2JamUser

@export var id: int = 0:
	set(value):
		id = value
		_track(&"id", value)

@export var slug: String = "":
	set(value):
		slug = value
		_track(&"slug", value)

@export var name: String = "":
	set(value):
		name = value
		_track(&"name", value)

## Absolute URL to the avatar image.
@export var profile_picture: String = "":
	set(value):
		profile_picture = value
		_track(&"profilePicture", value)

@export var banner_picture: String = "":
	set(value):
		banner_picture = value
		_track(&"bannerPicture", value)

## One line profile tagline.
@export var short: String = "":
	set(value):
		short = value
		_track(&"short", value)

## Profile biography, may contain HTML.
@export var bio: String = "":
	set(value):
		bio = value
		_track(&"bio", value)

@export var pronouns: String = "":
	set(value):
		pronouns = value
		_track(&"pronouns", value)

@export var links: Array[String] = []:
	set(value):
		links = value
		_track(&"links", value)

@export var link_labels: Array[String] = []:
	set(value):
		link_labels = value
		_track(&"linkLabels", value)

## Twitch login name, if the user linked one.
@export var twitch: String = "":
	set(value):
		twitch = value
		_track(&"twitch", value)

@export var emote_prefix: String = "":
	set(value):
		emote_prefix = value
		_track(&"emotePrefix", value)

@export var mod: bool = false:
	set(value):
		mod = value
		_track(&"mod", value)

@export var admin: bool = false:
	set(value):
		admin = value
		_track(&"admin", value)

## ISO 8601 timestamp.
@export var created_at: String = "":
	set(value):
		created_at = value
		_track(&"createdAt", value)

## ISO 8601 timestamp.
@export var updated_at: String = "":
	set(value):
		updated_at = value
		_track(&"updatedAt", value)


## Build a D2JamUser from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamUser:
	var result: D2JamUser = D2JamUser.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("id") != null:
		result.id = int(data["id"])
	if data.get("slug") != null:
		result.slug = str(data["slug"])
	if data.get("name") != null:
		result.name = str(data["name"])
	if data.get("profilePicture") != null:
		result.profile_picture = str(data["profilePicture"])
	if data.get("bannerPicture") != null:
		result.banner_picture = str(data["bannerPicture"])
	if data.get("short") != null:
		result.short = str(data["short"])
	if data.get("bio") != null:
		result.bio = str(data["bio"])
	if data.get("pronouns") != null:
		result.pronouns = str(data["pronouns"])
	if data.get("links") != null:
		var entries: Array[String] = []
		for entry: Variant in data["links"]:
			entries.append(str(entry))
		result.links = entries
	if data.get("linkLabels") != null:
		var entries: Array[String] = []
		for entry: Variant in data["linkLabels"]:
			entries.append(str(entry))
		result.link_labels = entries
	if data.get("twitch") != null:
		result.twitch = str(data["twitch"])
	if data.get("emotePrefix") != null:
		result.emote_prefix = str(data["emotePrefix"])
	if data.get("mod") != null:
		result.mod = bool(data["mod"])
	if data.get("admin") != null:
		result.admin = bool(data["admin"])
	if data.get("createdAt") != null:
		result.created_at = str(data["createdAt"])
	if data.get("updatedAt") != null:
		result.updated_at = str(data["updatedAt"])

	return result


## Build an array of D2JamUser from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamUser]:
	var result: Array[D2JamUser] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamUser.from_json(entry))

	return result
