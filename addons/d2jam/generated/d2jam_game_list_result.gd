@tool
extends D2JamResult

# GENERATED FILE - do not edit by hand; your changes will be overwritten.
#
# Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
# Regenerate with: dotnet run --project Down2Plugin

## Result carrying an array of [D2JamGame].
class_name D2JamGameListResult

## The parsed payload. Empty when the request failed.
var data: Array[D2JamGame] = []
