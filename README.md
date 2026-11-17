# Rewild

**Rewild** is a 2D puzzle-platformer about a small spider named **Webi** who brings a dead world
back to life. You travel from level to level looking for a dirt mound, plant a seed in it, and
watch the plant grow — but the only way to move is your web line, and every shot costs a Move.

Built with **Unity 6.2 (6000.2.9f1)**, 2D URP, the new Input System and Tilemap.

---

## Gameplay

- **Move with a web line.** You cannot jump. Hold the aim key to slow down and pick a target,
  then fire the web; Webi shoots at the anchor, swings toward it, and lands on the surface.
- **Every web shot costs one Move.** The HUD shows how many you have left. Run out and the level
  ends.
- **Plant the seed.** Pick up the seed, find the dirt mound, and plant it. The plant growth
  animation is what completes the level.
- **Stars.** Finish a level with **3 or more Moves** left to earn 3 stars, with **1–2** left for
  2 stars, and on your last Move for 1 star. Stars are saved and gate the next levels.
- **Surfaces matter.** Webi can walk on floors, walls and ceilings (movement follows the surface
  normal). Ice is slippery, oil makes you slide into the void, spikes and enemies kill you, and
  moving platforms carry you along.

### Levels and unlocks

The game has **10 levels**. Each one has a fixed Move budget (from 6 to 17, 100 in total), and the
next level unlocks only after collecting enough stars:

| Level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Stars needed | 0 | 0 | 2 | 4 | 7 | 9 | 11 | 14 | 16 | 18 |

---

## Controls

### Desktop (keyboard + mouse)

| Action | Input |
|---|---|
| Walk | `A` / `D` or `←` / `→` |
| Aim the web (hold) | `F` |
| Shoot the web | Left mouse button |
| Drag the camera | Right mouse button (per-level setting) |
| Pause | HUD pause button |
| Retry level | HUD retry button (hidden during the opening camera intro and the 3-2-1 countdown) |
| Take a debug screenshot | `F12` (Editor only, saved to `Screenshots/`) |

### Android (touch)

The touch overlay is loaded from `Assets/Resources/UI/Touch Controls.prefab`, so all ten level
scenes share one layout:

- left side: virtual pad for walking,
- right side: aim pad with a web knob, then a shoot button,
- HUD buttons for pause and retry.

---

## Tech stack

| | |
|---|---|
| Engine | Unity 6000.2.9f1, 2D (URP 17.2.0) |
| Input | Input System 1.14.2 |
| Tiles | 2D Tilemap + Tilemap Extras 5.0.2 (custom `Slope26Brush`, 26° and 45° slopes) |
| UI | uGUI + TextMeshPro |
| Audio | one persistent `AudioManager` with per-scene playlists |
| Target | Windows desktop + Android |

There is no asmdef and no automated test suite: all runtime scripts live in the single
`Assembly-CSharp` assembly, and the only automated check is compilation.

---

## Getting started

1. Install **Unity 6000.2.9f1** through Unity Hub (Unity 6.2).
2. Clone the repository and add the folder as a project in Unity Hub.
3. Open the project and let Unity import assets and resolve packages (first import takes a while:
   the art and audio alone are ~110 MB).
4. Open `Assets/Scenes/Menus/Home Screen.unity` and press **Play**.

`Library/`, `Temp/`, `Logs/`, `UserSettings/`, the generated `.csproj`/`.slnx` files and the local
backup folders are ignored by Git — Unity recreates them.

### Building

- **Windows:** *File → Build Profiles → Windows*, then Build.
- **Android:** see [`Docs/Android.md`](Docs/Android.md) for the keystore, package name and device
  checking notes used in this project. The game is landscape-only and uses the touch overlay
  described above.

---

## Project layout

| Path | Content |
|---|---|
| `Assets/Scenes/Menus` | `Home Screen`, `Level Selection`, `Level 1 Intro` (video intro) |
| `Assets/Scenes/Levels` | Ten level scenes, `Level 1` … `Level 10` |
| `Assets/Scenes/Sandbox` | `Experiment`, `Testing` — not part of the game flow |
| `Assets/Scripts/Core` | Camera, level intro, tilemap builder, progress, audio, scene flow |
| `Assets/Scripts/Gameplay` | Webi (`PlayerController`), seeds, enemies, portals, levers, hazards |
| `Assets/Scripts/UI` | HUD, popups, level selection, settings, touch controls |
| `Assets/Scripts/Editor` | Custom brushes, snapping helper, local agent bridge |
| `Assets/Art` | Characters, enemies, tiles, props, backgrounds, UI |
| `Assets/Audio` | Music tracks and SFX |
| `Assets/Prefabs` | Webi, platforms, moving ground, seeds, dirt pile, spikes |
| `Assets/Resources` | Prefabs and libraries loaded at runtime (touch controls, music library) |
| `Docs` | Android build notes, How to Play layout, AI handoff document |

### How the levels are built

Terrain is painted by hand on a `Grid` with **cell size 2**. Slopes are not rotated tiles: a 26°
row is split into two half tiles (`_L` / `_R`) that are placed as pairs, and 45° sets use one tile
per cell. Large slanted terrain uses a rotated `Grid Miring` chunk prefab, and moving platforms use
`Moving Ground` (all of their tilemaps must stay on layer 3, `Platform`).

---

## Documentation in this repository

- [`AGENTS.md`](AGENTS.md) — the working rules and the engine traps of this project (collider
  cache, slopes, tile colours, copy-a-scene checklist). Written for an AI coding agent, but it is
  also the most accurate description of how the project is assembled.
- [`Docs/AI-Handoff.md`](Docs/AI-Handoff.md) — a single self-contained handoff document: gameplay
  loop, all numbers, script map, UI layout, art pipeline, known gaps.
- [`Docs/HowToPlay.md`](Docs/HowToPlay.md) — the How To Play screens and menu layout plan.
- [`Docs/Android.md`](Docs/Android.md) — Android build and testing notes.

---

## Credits

- Design, direction and playtesting: **Sub-Danzzilo**
- Code and art in this project were generated with **AI assistance** and then iterated, wired and
  tuned by hand. Sprites, animations, UI art and music/SFX did not start from a store asset pack.
- Third-party editor tool included for convenience: **Asset Usage Detector**
  (`Assets/Plugins/AssetUsageDetector`, MIT).

## License

No license file is included yet, so all rights are reserved by the author. If you want to reuse
parts of this project, please open an issue first.
