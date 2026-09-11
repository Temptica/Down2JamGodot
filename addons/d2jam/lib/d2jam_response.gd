@tool
extends RefCounted

## A raw HTTP response from [D2JamHttpClient], before any Jamcore envelope is unwrapped.
##
## You normally read the typed [D2JamResult] a generated method returns instead. This is here for
## the cases the typed layer cannot express: inspecting rate limit headers, reading the rotated
## access token, or debugging a request that failed before it reached the API.
class_name D2JamResponse

## The URL that was requested, query string included.
var url: String = ""

## One of the HTTPClient.METHOD_* constants.
var method: int = HTTPClient.METHOD_GET

## HTTP status, or 0 when the request never completed.
var status_code: int = 0

## Response headers as "Name: value" strings, in the order the server sent them.
var headers: PackedStringArray = []

## Raw response body.
var body: PackedByteArray = []

## One of the HTTPRequest.Result constants. [constant HTTPRequest.RESULT_SUCCESS] means the
## exchange completed, whatever the status code was.
var transport_result: int = HTTPRequest.RESULT_SUCCESS

## Set when the request could not be made at all, for example a DNS failure or a timeout.
var transport_error: String = ""

## Error code to report when the client refused to send the request, rather than the network or the
## API failing. Lets a locally detected problem carry a meaningful code instead of ERR_TRANSPORT.
var client_error_code: String = ""


## True when the exchange completed and the status is in the 2xx range.
func is_success() -> bool:
	return transport_error.is_empty() and status_code >= 200 and status_code < 300


## The body decoded as UTF-8 text.
func text() -> String:
	return body.get_string_from_utf8()


## The body parsed as JSON, or null when it is empty or malformed.
##
## Uses a [JSON] instance rather than [method JSON.parse_string] so a non-JSON body -- a gateway's
## HTML error page, say -- does not push an engine error into the player's log on every retry.
func json() -> Variant:
	var raw: String = text()
	if raw.is_empty():
		return null

	var parser: JSON = JSON.new()
	if parser.parse(raw) != OK:
		return null

	return parser.data


## First header with the given name, or an empty string. Matching is case-insensitive.
func header(name: String) -> String:
	var values: PackedStringArray = header_values(name)
	return values[0] if not values.is_empty() else ""


## Every header with the given name. Set-Cookie in particular can appear more than once.
func header_values(name: String) -> PackedStringArray:
	var prefix: String = name.to_lower() + ":"
	var values: PackedStringArray = []

	for entry: String in headers:
		if entry.to_lower().begins_with(prefix):
			values.append(entry.substr(prefix.length()).strip_edges())

	return values


## Seconds until the rate limit window resets, or -1 when the server did not say.
func rate_limit_reset() -> int:
	var value: String = header("RateLimit-Reset")
	return int(value) if value.is_valid_int() else -1


## Requests left in the current rate limit window, or -1 when the server did not say.
func rate_limit_remaining() -> int:
	var value: String = header("RateLimit-Remaining")
	return int(value) if value.is_valid_int() else -1


## The request id Jamcore assigns. Worth quoting when reporting an API bug.
func request_id() -> String:
	return header("X-Request-Id")


func _to_string() -> String:
	if not transport_error.is_empty():
		return "D2JamResponse(%s, transport error: %s)" % [url, transport_error]

	return "D2JamResponse(%s, %d)" % [url, status_code]
