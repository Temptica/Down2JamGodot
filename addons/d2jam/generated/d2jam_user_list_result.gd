@tool
extends D2JamResult

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Result carrying an array of [D2JamUser].
class_name D2JamUserListResult

## The parsed payload. Empty when the request failed.
var data: Array[D2JamUser] = []
