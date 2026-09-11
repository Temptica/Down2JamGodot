# Down2Jam plugin demo

A Godot 4.7.2 project that exercises the whole addon on one screen: link an account, load a game,
read its leaderboards and achievements, submit a score, unlock an achievement.

## Running it

Open `demo/` in Godot 4.7.2 and press play. Nothing to configure — it loads a real game from
d2jam.com on startup.

`demo/addons/d2jam` is a symlink to the addon at the repository root, so the demo always runs
whatever you last generated. If symlinks are awkward on your platform, delete it and copy
`addons/d2jam` in instead.

There is no autoload. `main.tscn` holds a **D2Jam** node — a `D2JamService` with its `game_slug` set
in the inspector — and `main.gd` picks it up through `D2JamService.instance`. That is the same setup
you would use in a real game, so the demo doubles as the wiring reference.

## What you can do

**Browsing works signed out.** On startup it loads `weldroot`, a Down2Jam 3 entry that has both a
leaderboard and achievements. Type any other game's slug into the Game box and press Load — try
`little-plant-care`, `the-acres` (a SPEEDRUN board) or `scarecrows-gambit` (a GOLF board) to see the
board types behave differently.

**Linking needs a real d2jam.com account** -- click Link Account and approve it in the browser tab
that opens. On another device, go to the URL the demo prints and enter the code shown there instead.
Every write from then on happens as that player. Once linked:

- Your own row in the leaderboard is highlighted, and the achievement panel shows your progress.
- **Submit score** posts to the selected board. The spinbox switches between points and seconds
  depending on the board type, because time boards store milliseconds and point boards do not.
- **With screenshot** captures the frame, uploads it via `POST /image`, and attaches it as the
  score's evidence — the same thing the website's own submission form requires.
- **unlock** / **revoke** on each achievement calls `POST` / `DELETE /achievement`.

Careful: these are real writes against the live site. Scores and unlocks show up on the game's
public page. Use a game of your own to experiment, or delete what you submit afterwards.

The log at the bottom names the endpoint behind every action, so you can follow what the plugin is
doing.

## Where to look in the code

All of it is in [`main.gd`](main.gd). The parts worth reading:

| Function | Shows |
| --- | --- |
| `_ready` (top) | finding the service through `D2JamService.instance`, with no autoload |
| `_on_link_pressed` | the device link flow and what `link_device()` returns |
| `_load_game` | one request bringing back the game, its boards and its achievements |
| `_show_selected_board` | `rank()` and `format()` — ranking direction and value display per board type |
| `_on_submit_pressed` | seconds-to-milliseconds conversion and the screenshot path |
| `_on_achievement_pressed` | unlock and revoke |
| `_ready` | every signal the service emits and what it is good for |

## Disconnecting

The session is stored in the demo's `user://` folder, so it survives restarts. Disconnect in the
app, or delete `~/.local/share/godot/app_userdata/Down2Jam Plugin Demo/d2jam_session.json`.
