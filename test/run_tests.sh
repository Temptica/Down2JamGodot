#!/usr/bin/env bash
# Run the addon's checks in a headless Godot.
#
# The addon is meant to be dropped into someone else's game, so this repository is not itself a
# Godot project. The script assembles a throwaway one in a temp directory, copies the addon and the
# tests in, and runs them there.
#
#   test/run_tests.sh              offline checks against a committed fixture
#   test/run_tests.sh --live       also hit the real API (read-only, no login)
#
# Set GODOT to point at your editor binary if it is not on PATH.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
godot="${GODOT:-$(command -v godot || command -v godot4 || true)}"

if [[ -z "$godot" ]]; then
	echo "error: no Godot binary found. Set GODOT=/path/to/godot" >&2
	exit 1
fi

run_live=false
[[ "${1:-}" == "--live" ]] && run_live=true

workspace="$(mktemp -d)"
trap 'rm -rf "$workspace"' EXIT

mkdir -p "$workspace/test"
cp -r "$root/addons" "$workspace/"
cp -r "$root/test/fixtures" "$workspace/test/"
cp "$root/test/verify.gd" "$root/test/live.gd" "$workspace/test/"

cat > "$workspace/project.godot" <<'PROJECT'
config_version=5

[application]
config/name="d2jam-tests"
config/features=PackedStringArray("4.4")

[rendering]
renderer/rendering_method="gl_compatibility"
PROJECT

# Global class_name lookups only resolve once the project has been imported, so build the class
# cache before running anything.
echo "== importing =="
"$godot" --headless --path "$workspace" --editor --quit >/dev/null 2>&1 || true

echo "== offline checks =="
"$godot" --headless --path "$workspace" --script test/verify.gd

if [[ "$run_live" == true ]]; then
	echo
	echo "== live checks (read-only) =="
	"$godot" --headless --path "$workspace" --script test/live.gd
fi
