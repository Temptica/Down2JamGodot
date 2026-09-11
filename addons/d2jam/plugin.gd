@tool
extends EditorPlugin

## Registers the Down2Jam integration.
##
## There is deliberately nothing to set up here. Every class in the addon declares a
## [code]class_name[/code], so they are global as soon as the addon is on disk, and [D2JamService]
## publishes itself as [member D2JamService.instance] when it enters the tree. No autoload is added
## and none is needed: add a D2JamService to a scene and the rest of your code can reach it.
##
## The plugin exists so the addon shows up under Project Settings > Plugins, and as the place to
## hang editor tooling if this ever grows any.
