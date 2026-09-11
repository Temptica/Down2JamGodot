@tool
extends Node

## Thin async wrapper around [HTTPRequest].
##
## One [HTTPRequest] child is created per call and freed afterwards, so several requests can be in
## flight at once without them stepping on each other. Every call resolves to a [D2JamResponse];
## transport failures come back as a response with [member D2JamResponse.transport_error] set rather
## than as an error you have to catch.
class_name D2JamHttpClient

## Seconds before an in-flight request is abandoned.
@export var timeout_seconds: float = 20.0

## Print every request and its status. Useful while wiring a game up.
@export var verbose: bool = false


## Perform a request with a text body (or no body at all).
func request(
		url: String,
		method: int,
		headers: PackedStringArray = PackedStringArray(),
		body: String = "") -> D2JamResponse:
	return await _perform(url, method, headers, body.to_utf8_buffer())


## Perform a request with a raw binary body, for uploads.
func request_raw(
		url: String,
		method: int,
		headers: PackedStringArray,
		body: PackedByteArray) -> D2JamResponse:
	return await _perform(url, method, headers, body)


func _perform(
		url: String,
		method: int,
		headers: PackedStringArray,
		body: PackedByteArray) -> D2JamResponse:
	var response: D2JamResponse = D2JamResponse.new()
	response.url = url
	response.method = method

	var http: HTTPRequest = HTTPRequest.new()
	http.timeout = timeout_seconds
	http.accept_gzip = true
	add_child(http)

	var error: Error = http.request_raw(url, headers, method, body)

	if error != OK:
		response.transport_result = HTTPRequest.RESULT_CANT_CONNECT
		response.transport_error = "Could not start the request: %s" % error_string(error)
		http.queue_free()
		_log(response)
		return response

	var completed: Array = await http.request_completed
	http.queue_free()

	response.transport_result = completed[0]
	response.status_code = completed[1]
	response.headers = completed[2]
	response.body = completed[3]

	if response.transport_result != HTTPRequest.RESULT_SUCCESS:
		response.transport_error = _describe_transport_result(response.transport_result)

	_log(response)
	return response


func _log(response: D2JamResponse) -> void:
	if verbose:
		print("[d2jam] ", response)


func _describe_transport_result(result: int) -> String:
	match result:
		HTTPRequest.RESULT_CHUNKED_BODY_SIZE_MISMATCH:
			return "The response body was truncated."
		HTTPRequest.RESULT_CANT_CONNECT:
			return "Could not connect to the server."
		HTTPRequest.RESULT_CANT_RESOLVE:
			return "Could not resolve the host name."
		HTTPRequest.RESULT_CONNECTION_ERROR:
			return "The connection was interrupted."
		HTTPRequest.RESULT_TLS_HANDSHAKE_ERROR:
			return "The TLS handshake failed."
		HTTPRequest.RESULT_NO_RESPONSE:
			return "The server sent no response."
		HTTPRequest.RESULT_BODY_SIZE_LIMIT_EXCEEDED:
			return "The response was larger than the configured limit."
		HTTPRequest.RESULT_REQUEST_FAILED:
			return "The request failed."
		HTTPRequest.RESULT_TIMEOUT:
			return "The request timed out."
		_:
			return "The request failed (HTTPRequest result %d)." % result


## Percent-encode one path segment so slugs with unusual characters cannot break out of the path.
static func encode_path_segment(value: String) -> String:
	return value.uri_encode()


## Build a query string, leading "?" included, from a dictionary. Returns "" for an empty
## dictionary. Null values are skipped; arrays are repeated as "key=a&key=b", which is how
## Jamcore reads multi-valued parameters.
static func encode_query(query: Dictionary) -> String:
	var parts: PackedStringArray = []

	for key: Variant in query:
		var value: Variant = query[key]
		if value == null:
			continue

		var encoded_key: String = str(key).uri_encode()

		if value is Array:
			for entry: Variant in value:
				parts.append("%s=%s" % [encoded_key, _encode_value(entry)])
		else:
			parts.append("%s=%s" % [encoded_key, _encode_value(value)])

	return "" if parts.is_empty() else "?" + "&".join(parts)


static func _encode_value(value: Variant) -> String:
	# GDScript prints booleans as "True"/"False"; the API expects lowercase.
	if value is bool:
		return "true" if value else "false"

	return str(value).uri_encode()


## Assemble a multipart/form-data body holding one file.
##
## Returns a dictionary with "body" ([PackedByteArray]) and "content_type" ([String]) so the caller
## can set a matching Content-Type header with the generated boundary.
static func build_file_upload(
		field_name: String,
		file_name: String,
		file_bytes: PackedByteArray,
		mime_type: String = "") -> Dictionary:
	var boundary: String = "D2JamBoundary%d%d" % [Time.get_ticks_usec(), randi()]
	var resolved_mime: String = mime_type if not mime_type.is_empty() else guess_mime_type(file_name)

	var head: String = "--%s\r\n" % boundary
	head += 'Content-Disposition: form-data; name="%s"; filename="%s"\r\n' % [field_name, file_name]
	head += "Content-Type: %s\r\n\r\n" % resolved_mime

	var body: PackedByteArray = head.to_utf8_buffer()
	body.append_array(file_bytes)
	body.append_array(("\r\n--%s--\r\n" % boundary).to_utf8_buffer())

	return {
		"body": body,
		"content_type": "multipart/form-data; boundary=%s" % boundary,
	}


## Best-effort MIME type from a file name. Only the formats the API accepts are recognised.
static func guess_mime_type(file_name: String) -> String:
	match file_name.get_extension().to_lower():
		"png":
			return "image/png"
		"jpg", "jpeg":
			return "image/jpeg"
		"gif":
			return "image/gif"
		"webp":
			return "image/webp"
		_:
			return "application/octet-stream"
