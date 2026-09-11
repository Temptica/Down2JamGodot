@tool
extends Resource

## Base class for every generated data object: response models, request bodies and query options.
##
## Properties are stored twice. The typed GDScript property is what you read and write; a parallel
## dictionary keyed by the API's own spelling records which properties were actually assigned. That
## is what makes [method to_dict] able to send only the fields you touched, which matters for query
## options and for request bodies where an empty string is not the same as an absent key.
class_name D2JamData

var _tracked: Dictionary = {}


## Called by generated setters. You should not need to call this yourself.
func _track(key: StringName, value: Variant) -> void:
	_tracked[String(key)] = value


## True when the property behind [param key] has been assigned. [param key] is the API spelling,
## for example "leaderboardId".
func is_set(key: StringName) -> bool:
	return _tracked.has(String(key))


## Remove a previously assigned property so it is left out of [method to_dict].
func unset(key: StringName) -> void:
	_tracked.erase(String(key))


## The assigned properties, keyed the way the API spells them, ready for JSON or a query string.
##
## Nested [D2JamData] values and arrays of them are converted too.
func to_dict() -> Dictionary:
	var result: Dictionary = {}

	for key: String in _tracked:
		result[key] = _unwrap(_tracked[key])

	return result


## The assigned properties as a JSON string.
func to_json() -> String:
	return JSON.stringify(to_dict())


func _unwrap(value: Variant) -> Variant:
	if value is D2JamData:
		return (value as D2JamData).to_dict()

	if value is Array:
		var entries: Array = []
		for entry: Variant in value:
			entries.append(_unwrap(entry))
		return entries

	return value


func _to_string() -> String:
	var script_name: String = "D2JamData"
	var attached: Script = get_script()
	if attached != null and attached.get_global_name() != &"":
		script_name = String(attached.get_global_name())

	return "%s%s" % [script_name, to_json()]
