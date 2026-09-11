@tool
extends RefCounted

## Base class for every typed result a [D2JamAPI] method returns.
##
## Jamcore wraps everything in an envelope: [code]{"success": true, "data": ...}[/code] on the way
## out, [code]{"success": false, "error": {"code": ..., "message": ...}}[/code] when something goes
## wrong. This class unwraps that, so callers check [member ok] and read the typed [code]data[/code]
## property the generated subclass adds.
##
## [codeblock]
## var result := await api.get_game("weldroot")
## if not result.ok:
##     push_warning(result.describe())
##     return
## print(result.data.name)
## [/codeblock]
class_name D2JamResult

## True when the request reached the API and the API reported success.
var ok: bool = false

## HTTP status, or 0 when the request never completed.
var status_code: int = 0

## Human readable message the API attached to a successful response, if any.
var message: String = ""

## Machine readable failure code, for example "ERR_VALIDATION" or "ERR_UNAUTHORIZED".
var error_code: String = ""

## Human readable failure reason. Safe to show to a player.
var error_message: String = ""

## Whatever sat in the envelope's "data" field, still untyped. Generated methods parse this into
## their own [code]data[/code] property; reach for it when you want the raw shape.
var payload: Variant = null

## The underlying HTTP response, for headers and diagnostics.
var response: D2JamResponse


## True when the access token was rejected, meaning the player needs to log in again.
func is_unauthenticated() -> bool:
	return status_code == 401 or error_code == "ERR_UNAUTHORIZED"


## True when the API refused because of its rate limit. [method retry_after] says for how long.
func is_rate_limited() -> bool:
	return status_code == 429


## Seconds to wait before retrying a rate limited call, or -1 when the server did not say.
func retry_after() -> int:
	if response == null:
		return -1

	var value: String = response.header("Retry-After")
	if value.is_valid_int():
		return int(value)

	return response.rate_limit_reset()


## A one line description suitable for a log or an error toast.
func describe() -> String:
	if ok:
		return "ok (%d)" % status_code

	var reason: String = error_message if not error_message.is_empty() else "unknown error"
	var code: String = error_code if not error_code.is_empty() else "HTTP %d" % status_code
	return "%s: %s" % [code, reason]


## Populate this result from a raw response. Called by generated methods.
func _apply(source: D2JamResponse) -> void:
	response = source

	if source == null:
		error_code = "ERR_NO_RESPONSE"
		error_message = "The request produced no response."
		return

	status_code = source.status_code

	if not source.transport_error.is_empty():
		error_code = source.client_error_code if not source.client_error_code.is_empty() \
				else "ERR_TRANSPORT"
		error_message = source.transport_error
		return

	var envelope: Variant = source.json()

	if envelope is not Dictionary:
		# Non-JSON bodies happen on gateway errors and on the odd HTML error page.
		if source.is_success():
			ok = true
			payload = source.text()
		else:
			error_code = "ERR_HTTP_%d" % status_code
			error_message = source.text().strip_edges().left(300)
		return

	var body: Dictionary = envelope

	if body.get("success") == true:
		ok = true
		payload = body.get("data")
		message = str(body.get("message", ""))
		return

	# Some routes answer with a bare {"message": ...} and no success flag.
	if not body.has("success") and not body.has("error") and source.is_success():
		ok = true
		payload = body.get("data", body)
		message = str(body.get("message", ""))
		return

	var error: Variant = body.get("error")
	if error is Dictionary:
		error_code = str((error as Dictionary).get("code", ""))
		error_message = str((error as Dictionary).get("message", ""))
	else:
		error_code = "ERR_HTTP_%d" % status_code
		error_message = str(body.get("message", "The request failed."))
