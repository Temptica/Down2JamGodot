@tool
extends Node

## Transport, authentication and error plumbing for [D2JamAPI].
##
## The generated client extends this class, so everything here is hand written and safe to edit;
## regenerating never touches it. Add it to a scene through [D2JamService] rather than directly.
class_name D2JamAPIBase

## The public Down2Jam instance.
const DEFAULT_HOST: String = "https://d2jam.com/api/v1"

## Emitted when a call fails, before the result is returned. Handy for a global error toast.
signal request_failed(result: D2JamResult)

## Emitted when the API rejects the stored tokens. The player has to log in again; [D2JamAuth]
## clears its session when this fires.
signal unauthenticated

## Emitted when the API asks the client to slow down, with the seconds it wants you to wait.
signal rate_limited(retry_after_seconds: int)

## Base URL including the version prefix. Point this at a local Jamcore instance to test.
@export var api_host: String = DEFAULT_HOST

## Supplies the game token as a bearer credential. Optional: public endpoints work without it.
@export var auth: D2JamAuth

## Log failed calls with push_warning. Turn off if you handle every result yourself.
@export var warn_on_failure: bool = true

var _client: D2JamHttpClient


func _ready() -> void:
	_ensure_client()


## Perform a request against the API. Generated methods call this; call it yourself for an endpoint
## the overlay does not cover yet.
##
## [param path] is relative to [member api_host] and must already be percent-encoded.
## [param body] is JSON-encoded when it is not null. [param requires_auth] reflects what the spec
## says about the endpoint; credentials are attached whenever they are available regardless, because
## several endpoints change their answer for a logged in user.
func request(
		path: String,
		method: int,
		query: Dictionary = {},
		body: Variant = null,
		requires_auth: bool = false) -> D2JamResponse:
	var headers: PackedStringArray = ["Accept: application/json"]
	var payload: String = ""

	if body != null:
		headers.append("Content-Type: application/json")
		payload = JSON.stringify(body)

	if not _attach_credentials(headers, path, requires_auth):
		return _missing_credentials_response(path, method)

	var url: String = api_host + path + D2JamHttpClient.encode_query(query)
	var response: D2JamResponse = await _ensure_client().request(url, method, headers, payload)

	_after_response(response)
	return response


## Upload a single file as multipart/form-data.
func request_multipart(
		path: String,
		field_name: String,
		file_bytes: PackedByteArray,
		file_name: String,
		requires_auth: bool = false) -> D2JamResponse:
	var upload: Dictionary = D2JamHttpClient.build_file_upload(field_name, file_name, file_bytes)

	var headers: PackedStringArray = [
		"Accept: application/json",
		"Content-Type: %s" % upload["content_type"],
	]

	if not _attach_credentials(headers, path, requires_auth):
		return _missing_credentials_response(path, HTTPClient.METHOD_POST)

	var url: String = api_host + path
	var response: D2JamResponse = await _ensure_client().request_raw(
			url, HTTPClient.METHOD_POST, headers, upload["body"])

	_after_response(response)
	return response


## Turn a server-relative path such as "/api/v1/image/abc.png" into an absolute URL.
##
## Values that are already absolute are returned untouched.
func resolve_url(path: String) -> String:
	if path.is_empty() or path.begins_with("http://") or path.begins_with("https://"):
		return path

	# api_host carries the version prefix that server-relative paths already include, so strip it
	# back to the origin before joining.
	var origin: String = api_host
	var scheme_end: int = origin.find("://")
	var host_end: int = origin.find("/", scheme_end + 3) if scheme_end >= 0 else origin.find("/")
	if host_end > 0:
		origin = origin.substr(0, host_end)

	return origin + ("" if path.begins_with("/") else "/") + path


## Report a failed result through [signal request_failed] and the log. Generated methods do not call
## this; [D2JamService] wires it up so a single handler can watch every call.
func report(result: D2JamResult) -> void:
	if result.ok:
		return

	if warn_on_failure:
		push_warning("[d2jam] %s" % result.describe())

	request_failed.emit(result)


## Percent-encode one path segment. Generated methods use this for path parameters.
static func encode_path_segment(value: String) -> String:
	return D2JamHttpClient.encode_path_segment(value)


func _ensure_client() -> D2JamHttpClient:
	if _client == null or not is_instance_valid(_client):
		_client = D2JamHttpClient.new()
		_client.name = "D2JamHttpClient"
		add_child(_client)

	return _client


func _attach_credentials(headers: PackedStringArray, path: String, requires_auth: bool) -> bool:
	if auth == null or not auth.is_logged_in():
		if requires_auth:
			# A warning rather than an error: a session can expire mid-session through no fault of
			# the caller, and the returned result already says what happened.
			if warn_on_failure:
				push_warning("[d2jam] %s needs a logged in user; no session is available." % path)
			return false
		return true

	headers.append_array(auth.authorization_headers())
	return true


func _missing_credentials_response(path: String, method: int) -> D2JamResponse:
	var response: D2JamResponse = D2JamResponse.new()
	response.url = api_host + path
	response.method = method
	response.status_code = 401
	response.client_error_code = "ERR_NOT_LOGGED_IN"
	response.transport_error = "Not logged in. Call D2JamService.link_device() first."
	return response


func _after_response(response: D2JamResponse) -> void:
	if response.status_code == 401:
		if auth != null:
			auth.invalidate()
		unauthenticated.emit()
	elif response.status_code == 429:
		var retry: String = response.header("Retry-After")
		rate_limited.emit(int(retry) if retry.is_valid_int() else response.rate_limit_reset())
