# Down2Jam for Godot

Leaderboards, achievements and player sessions for [Down2Jam](https://d2jam.com), for Godot 4.4+.

The REST client in `generated/` is produced from Down2Jam's OpenAPI spec by the generator in this
repository. Everything else is hand written and safe to edit.

## Install

1. Copy the `addons/d2jam` folder into your project.
2. Enable **Down2Jam** in `Project > Project Settings > Plugins`.
3. Add a **D2JamService** node to your main scene and set its **game_slug** to the last part of
   your game's URL. For `https://d2jam.com/g/weldroot` that is `weldroot`.

`D2JamService` publishes itself as a singleton whileit is in the tree, so the rest of your code 
reaches it without a node path or an exported reference:

```gdscript
@onready var d2jam: D2JamService = D2JamService.instance
```

The other nodes do the same, so `D2JamLeaderboards.instance`, `D2JamAchievements.instance`,
`D2JamAuth.instance` and `D2JamAPI.instance` all work too. They are the same objects as
`d2jam.leaderboards`, `d2jam.achievements`, `d2jam.auth` and `d2jam.api`. Use whichever reads
better where you are. Since your token is valid for only one game. You'll always only have on instance.

`instance` is set when the node enters the tree and cleared when it leaves, so a service that lives
in one scene stops being reachable once that scene is freed. If you want one session for the whole
game, put the D2JamService in a scene that is never unloaded or make it an autoload, at
which point `instance` still works and so does the autoload name.

The examples below assume a `d2jam` reference obtained that way.

## Before it will do anything

Leaderboards and achievements are **created by you on your game's page on d2jam.com**, not from
code. The API can submit to a board and unlock an achievement; it cannot create either. Set them up
on the website first, then refer to them by name:

```gdscript
await d2jam.leaderboards.submit("High Score", 16340)
await d2jam.achievements.unlock("First Blood")
```

Both look their target up by name (case-insensitively) and warn if it does not exist, so you never
have to paste numeric ids into your game.

## Linking an account

Everything happens as a logged in Down2Jam user. There is no game-level API key and no password
ever reaches the game: the player links their account by approving the game in their own browser,
where they are (or log in) on d2jam.com, and your code then acts in their name from that point on.

```gdscript
d2jam.device_link_started.connect(
		func(user_code, verification_uri): show_code_screen(user_code))

if await d2jam.link_device():
	print("hello %s" % d2jam.current_user().name)
```

`link_device()` starts a device authorization request, opens `verification_uri` in the player's
browser for you (turn that off with `auto_open_browser = false` if you would rather show a QR code
or a "copy link" button), and polls until the player approves or denies it there. Show
`user_code` somewhere on screen in the meantime, in case the browser tab does not open or the player
wants to approve it from another device.

The issued token is scoped to `game_slug`, not the player's whole account: it can submit scores and
achievements for this game and no other, even if the player owns several games on Down2Jam. Set
`game_slug` before calling `link_device()`; it is what the approval page shows the player and what
the server ties the token to.

What gets stored is a single long lived game token, plus the profile the player approved with, at
`user://d2jam_session.json`. Nothing else lives there, no passwords gets stored. The token does not expire on its
own; the player can revoke it any time from Settings on the website, or your game can drop it with
`d2jam.logout()`. Anyone who can read that file can act as the player until it is revoked, so set
`session_encryption_key` on the service for a release build.

A returning player is usually already linked by the time your first scene loads:

```gdscript
if not d2jam.is_logged_in():
	show_link_screen()
```

## Leaderboards

Four board types, and the type decides both how a value is stored and which direction wins:

| Type | Direction | Value you pass |
| --- | --- | --- |
| `SCORE` | higher wins | points, as the player reads them |
| `GOLF` | lower wins | points, as the player reads them |
| `SPEEDRUN` | lower wins | milliseconds |
| `ENDURANCE` | higher wins | milliseconds |

`D2JamLeaderboards` handles the conversion, so you pass the number your game already has:

```gdscript
await d2jam.leaderboards.submit("High Score", 16340)      # points
await d2jam.leaderboards.submit_time("Fastest Time", 82.451)  # seconds -> milliseconds
```

Reading a board back gives you the ranking the website shows, with the board's `only_best` setting
already applied so a player who submitted twenty times appears once:

```gdscript
var board := await d2jam.leaderboards.find("High Score")
for entry in d2jam.leaderboards.rank(board):
	print(entry.user.name, D2JamLeaderboards.format(board, entry))

print("you are #%d" % d2jam.leaderboards.position_of(board))
```

### Screenshots

The website shows a screenshot next to every entry and its own submission form requires one, so
submit with evidence where you can. `submit_with_screenshot()` is the laziest option. It grabs the
whole main viewport, screenshots it and sends it. But it is not the only one; pick whichever level of control fits your
game.

**Whole window, no setup.** Fine for most games:

```gdscript
await RenderingServer.frame_post_draw
await d2jam.leaderboards.submit_with_screenshot("High Score", score)
```

The `await RenderingServer.frame_post_draw` matters: the viewport texture is only readable once the
frame has been drawn.

**Just one region of the screen**, e.g. hide the HUD or crop to the play area: render that part into
a `SubViewport` and pass it in. Both `submit_with_screenshot()` and `capture_evidence()` take an
optional `Viewport` and default to the main window when you leave it out:

```gdscript
await RenderingServer.frame_post_draw
await d2jam.leaderboards.submit_with_screenshot("High Score", score, my_sub_viewport)
```

**Full control over the image** -- your own composition, a downscale, a watermark, whatever.
`capture_evidence()` on its own just uploads and hands back the URL, so build the score submission
yourself instead of going through `submit_with_screenshot()`:

```gdscript
var evidence_url: String = await d2jam.leaderboards.capture_evidence(my_sub_viewport)
await d2jam.leaderboards.submit("High Score", score, evidence_url)
```

Or skip the plugin's capture entirely and upload bytes you produced yourself -- any `Image`, from
any source, cropped or composed however you like:

```gdscript
var bytes: PackedByteArray = my_custom_image.save_png_to_buffer()
var result: D2JamStringResult = await d2jam.api.upload_image(bytes, "evidence.png")
if result.ok:
	await d2jam.leaderboards.submit("High Score", score, result.data)
```

`submit()` and `submit_to()` take `evidence_url` as a plain string, so anything that gets you a URL
works. Submitting without evidence works too; the entry just has no screenshot next to it.

## Achievements

```gdscript
await d2jam.achievements.unlock("First Blood")

if await d2jam.achievements.is_unlocked("First Blood"):
	show_veteran_intro()

print("%d%% complete" % roundi(await d2jam.achievements.completion() * 100.0))
```

`unlock()` is safe to call from gameplay code as often as you like: an achievement already unlocked
this session is dropped before it becomes a request, and the API treats a repeat as a no-op.

## How authentication works

Worth understanding, because it explains why there is no username or password anywhere in this
plugin.

`link_device()` calls `POST /device/code`, which returns a short `user_code` to show the player and
a `verification_uri` to open in their browser. The player, who's most likely already logged in, approves it there.
The game doesn't run a server or webscoket usually. Which means to get the token, you'll have to poll at 
`POST /device/token` at the given interval the server asked for, backing off whenever it says
`slow_down`, until the request is approved, denied, or expires.

The approved response carries the **game token** and the player's profile together, in the same
payload. That is deliberate: `GET /self` does not accept a game token (only score, achievement and
a handful of other gameplay endpoints do. See the repository README's *Scope*), so this is the only
place the plugin is ever handed a profile to show, and `D2JamAuth` saves both to disk so a restart
does not need to ask for either again.

A game token does not expire and does not rotate. There is exactly one credential, sent as a bearer
token, for as long as the player has not revoked it. A 401 means: the session is cleared
locally and `session_expired` fires.

```gdscript
d2jam.session_expired.connect(show_link_screen)
```

Endpoints that require a session refuse locally rather than making a doomed request, and come back
with `ERR_NOT_LOGGED_IN`.

## Handling failures

Every call returns a result rather than throwing. Check `ok`:

```gdscript
var result := await d2jam.api.get_game("weldroot")
if not result.ok:
	push_warning(result.describe())   # "ERR_VALIDATION: Validation failed"
	return
print(result.data.jam_page.name)
```

`D2JamResult` also carries `status_code`, `error_code`, `is_rate_limited()` and `retry_after()`. For
one central error handler, connect to the service:

```gdscript
d2jam.request_failed.connect(func(result: D2JamResult) -> void: toast(result.error_message))
```

## Caching

`fetch_game()` fetches the game. Leaderboards, scores and achievements all arrive in that one
response and caches it. Both helpers read through it, so a screen showing leaderboards and
achievements together costs one request, not two. Overlapping calls share the in-flight request.

Submitting a score or unlocking an achievement invalidates the cache. Force a refresh yourself with
`await d2jam.fetch_game(true)`.

## Reaching the rest of the API

`d2jam.api` is the generated client; every covered endpoint is a method on it. For an endpoint the
generator does not cover yet, `D2JamAPIBase.request()` takes a path directly:

```gdscript
var response := await d2jam.api.request("/jams", HTTPClient.METHOD_GET)
```

Better still, add the endpoint to `spec/d2jam.overlay.json` and regenerate.

## A note on names

`GET /games/{gameSlug}` does not flatten a game's name, description and artwork onto the game the
way the list endpoints do; on a detail response they live on `jam_page`. Use `await d2jam.page()`,
which returns the post-jam page when the team published one and the jam page otherwise.

### Note
I made the token based feature for down2jam, as well as this plugin. If you have issues with either. Please do contact me
via an issue on git, via discord (temptica), or on stream when I'm live (twitch.tv/temptic404) and I'll try to help as much as I can. I usually participate in the jam myself
so expect delays during this period.
