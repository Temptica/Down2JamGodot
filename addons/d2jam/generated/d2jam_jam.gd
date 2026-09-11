@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## A Down2Jam jam event.
class_name D2JamJam

@export var id: int = 0:
	set(value):
		id = value
		_track(&"id", value)

@export var name: String = "":
	set(value):
		name = value
		_track(&"name", value)

@export var slug: String = "":
	set(value):
		slug = value
		_track(&"slug", value)

## ISO 8601 timestamp for the start of the jamming phase.
@export var start_time: String = "":
	set(value):
		start_time = value
		_track(&"startTime", value)

@export var jamming_hours: int = 0:
	set(value):
		jamming_hours = value
		_track(&"jammingHours", value)

@export var submission_hours: int = 0:
	set(value):
		submission_hours = value
		_track(&"submissionHours", value)

@export var rating_hours: int = 0:
	set(value):
		rating_hours = value
		_track(&"ratingHours", value)

@export var is_active: bool = false:
	set(value):
		is_active = value
		_track(&"isActive", value)

@export var icon: String = "":
	set(value):
		icon = value
		_track(&"icon", value)

@export var color: String = "":
	set(value):
		color = value
		_track(&"color", value)


## Build a D2JamJam from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamJam:
	var result: D2JamJam = D2JamJam.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("id") != null:
		result.id = int(data["id"])
	if data.get("name") != null:
		result.name = str(data["name"])
	if data.get("slug") != null:
		result.slug = str(data["slug"])
	if data.get("startTime") != null:
		result.start_time = str(data["startTime"])
	if data.get("jammingHours") != null:
		result.jamming_hours = int(data["jammingHours"])
	if data.get("submissionHours") != null:
		result.submission_hours = int(data["submissionHours"])
	if data.get("ratingHours") != null:
		result.rating_hours = int(data["ratingHours"])
	if data.get("isActive") != null:
		result.is_active = bool(data["isActive"])
	if data.get("icon") != null:
		result.icon = str(data["icon"])
	if data.get("color") != null:
		result.color = str(data["color"])

	return result


## Build an array of D2JamJam from a decoded JSON array.
static func list_from_json(source: Variant) -> Array[D2JamJam]:
	var result: Array[D2JamJam] = []
	if source is not Array:
		return result

	for entry: Variant in source:
		result.append(D2JamJam.from_json(entry))

	return result
