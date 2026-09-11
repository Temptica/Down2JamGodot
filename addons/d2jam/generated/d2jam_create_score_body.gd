@tool
extends D2JamData

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Payload for POST /score.
class_name D2JamCreateScoreBody

## Id of the leaderboard on the game page.
@export var leaderboard_id: int = 0:
	set(value):
		leaderboard_id = value
		_track(&"leaderboardId", value)

## A number. For SCORE and GOLF boards, the value as a player would read it; the server multiplies
## it by 10 ^ decimal_places. For SPEEDRUN and ENDURANCE boards, a duration in whole milliseconds.
## Send an int unless the board declares decimal places, because the column it lands in is an
## integer. D2JamLeaderboards.submit() picks the right form for you.
var score: Variant = null:
	set(value):
		score = value
		_track(&"score", value)

## URL of a screenshot backing the score. The website requires one on every submission, so treat it
## as effectively mandatory.
@export var evidence: String = "":
	set(value):
		evidence = value
		_track(&"evidence", value)

## Accepted as an alias for evidence by the server. Prefer evidence.
@export var evidence_url: String = "":
	set(value):
		evidence_url = value
		_track(&"evidenceUrl", value)


## Build a D2JamCreateScoreBody from a decoded JSON dictionary.
##
## Missing and null keys are left at their defaults, so a partial payload is safe to parse.
static func from_json(source: Variant) -> D2JamCreateScoreBody:
	var result: D2JamCreateScoreBody = D2JamCreateScoreBody.new()
	if source is not Dictionary:
		return result
	var data: Dictionary = source

	if data.get("leaderboardId") != null:
		result.leaderboard_id = int(data["leaderboardId"])
	if data.get("score") != null:
		result.score = data["score"]
	if data.get("evidence") != null:
		result.evidence = str(data["evidence"])
	if data.get("evidenceUrl") != null:
		result.evidence_url = str(data["evidenceUrl"])

	return result


## Construct with every required field set.
static func create(leaderboard_id: int, score: Variant) -> D2JamCreateScoreBody:
	var body: D2JamCreateScoreBody = D2JamCreateScoreBody.new()
	body.leaderboard_id = leaderboard_id
	body.score = score
	return body
