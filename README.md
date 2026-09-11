# Down2Plugin

A GDScript client generator for the [Down2Jam](https://d2jam.com) (Jamcore) API, and the Godot addon
it produces.

- **`Down2Plugin/`** — the generator, a .NET console app.
- **`spec/d2jam.overlay.json`** — the semantic overlay the generator needs on top of the spec.
- **`addons/d2jam/`** — the Godot addon: generated client plus a hand written layer over it.
  See [its README](addons/d2jam/README.md) for how to use it in a game.
- **`demo/`** — a Godot 4.7.2 project that exercises the whole addon on one screen.
  See [its README](demo/README.md).
- **`test/`** — headless Godot checks.

## Running the generator

```bash
dotnet run --project Down2Plugin
```

That downloads `https://d2jam.com/api/v1/openapi`, merges it with the overlay, and writes
`addons/d2jam/generated/`. Generated files that are no longer produced are deleted, so a renamed
endpoint does not leave a stale class behind.

```
--spec <url|path>    OpenAPI document to read
--overlay <path>     semantic overlay (default: spec/d2jam.overlay.json)
-o, --output <dir>   where the .gd files go
--cache-spec <path>  also save the downloaded spec, for offline runs
--dry-run            report what would change without writing
--show-skipped       list the spec operations the overlay leaves out
```

## Why there is an overlay

Jamcore's OpenAPI document describes paths, verbs, query parameters and auth requirements
accurately. It does not describe payloads. Every request body is:

```json
{ "type": "object", "additionalProperties": true }
```

and every response is a generic `SuccessResponse` whose `data` is untyped. That is enough to
generate method signatures and nothing else — no models, no field names, no way to know that
`POST /score` wants a `leaderboardId`.

`spec/d2jam.overlay.json` supplies the missing half. Its field shapes were derived from the public
Jamcore backend — `prisma/schema.prisma`, the Zod schemas in `src/features/*/service.ts`, and the
game detail projection — then checked against live responses.

The two halves stay in their own lanes:

| From the spec | From the overlay |
| --- | --- |
| paths and verbs | request body shapes |
| path and query parameters | response models |
| which endpoints need a login | which endpoints to generate at all |
| rate limit and idempotency headers | method names and documentation |

The overlay doubles as the scope filter: an operation with no entry is not generated. Run with
`--show-skipped` to see what that currently excludes. As Jamcore adds real schemas to its spec,
entries in the overlay can be deleted.

## Adding an endpoint

Add an entry to `operations` keyed by `"<verb> <path>"`, exactly as the spec spells it:

```json
"post /rating": {
  "name": "rate_game",
  "doc": "Rate a game in the jam's rating categories.",
  "body": "CreateRatingBody",
  "returns": "void"
}
```

Declare any new body under `bodies` and any new response model under `models`, then regenerate. A
key that does not match a spec operation is an error rather than a silent no-op, so a typo or an
API change surfaces immediately.

Types in the overlay are `int`, `float`, `bool`, `String`, `Variant`, the name of a declared model,
or `Array[...]` of any of those.

## What gets generated

| File | Contents |
| --- | --- |
| `d2jam_api.gd` | `D2JamAPI`, one `await`-able method per covered endpoint, plus its `instance` singleton |
| `d2jam_<model>.gd` | data classes with `from_json` / `list_from_json` |
| `d2jam_<body>.gd` | request bodies with a `create()` for the required fields |
| `d2jam_<op>_options.gd` | optional query parameters, sent only when assigned |
| `d2jam_<type>_result.gd` | typed result wrappers with `ok`, `data` and error details |

JSON's `camelCase` becomes GDScript's `snake_case` on the way in and back again on the way out, so
`leaderboardId` is `leaderboard_id` in your code and `leaderboardId` on the wire.

The generated client extends `D2JamAPIBase`, which is hand written. Transport, authentication,
error handling and URL resolution live there and survive regeneration.

Every node in the addon registers itself as a `static var instance` on entering the tree and stands
down on leaving, so nothing needs an autoload — `D2JamService.instance` and friends are reachable
from anywhere. The generator emits that block into `D2JamAPI` so the generated class matches the
hand written ones.

## Demo

```bash
godot --path demo
```

A single screen covering the whole integration: link an account, load any game by slug, read its
leaderboards and achievements, submit a score with or without a screenshot, unlock achievements. The
log names the endpoint behind every action. Browsing works signed out; writes need a real account
and land on the live site.

`demo/addons/d2jam` is a symlink to `addons/d2jam`, so the demo always runs the code you just
generated.

## Tests

```bash
test/run_tests.sh          # offline, against a committed fixture
test/run_tests.sh --live   # also hits the real API, read-only, no login
```

Set `GODOT` if the editor binary is not on your `PATH`. The offline run parses a real captured
`GET /games/weldroot` response and checks model mapping, ranking, score conversion and formatting,
request body construction, query encoding, envelope and error handling, and the auth header and
token rotation logic.

## Scope

Currently generated: sessions and the player's profile, games, users, leaderboard scores,
achievements, and image upload — the endpoints a game needs at runtime. The site's posts,
collections, teams, moderation and platform administration are deliberately left out.
