# Rewild

**Rewild** is a 2D puzzle-platformer about a small spider named **Webi** who brings a dead world
back to life. You travel from level to level looking for a dirt mound, plant a seed in it, and
watch the plant grow — but the only way to move is your web line, and every shot costs a Move.

Built with **Unity 6.2 (6000.2.9f1)**, 2D URP, the new Input System and Tilemap.

> **Status:** version **1.0.1**. This is a non-commercial student project — see
> [About this project](#about-this-project). Ready-to-play Windows and Android builds are attached
> to the [Releases](https://github.com/Sub-Danzzilo/rewild/releases) page.

---

## Download

| Platform | File on the Releases page | Requirements |
|---|---|---|
| Windows | the `.zip` archive — unzip it and run `Rewild.exe` | Windows 10 version 21H1 (build 19043) or newer, 64-bit |
| Android | the `.apk` — allow installs from unknown sources, then open it | Android 6.0 (API 23) or newer, ARM64 device, landscape |

The layout is built for 1920×1080; Android builds are locked to landscape and show the touch
overlay automatically. The Windows player is built for Intel 64-bit and the APK contains ARM64 code
only, so 32-bit Windows and x86 Android emulators are not supported. Progress (stars) is stored
locally on the device, so reinstalling the game starts over.

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

The game has **10 levels**. A short tutorial slideshow plays before Level 1, and the same slides are
available any time from **How to Play** on the Home Screen. Each level has a fixed Move budget
(from 6 to 17, 100 in total), and the next level unlocks only after collecting enough stars:

| Level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Stars needed | 0 | 2 | 4 | 7 | 9 | 11 | 14 | 16 | 18 | 21 |

The table lives in one place (`LevelUnlockRules.RequiredStarsByLevel`), so the level buttons, the
locked-level popup and the Next Level button always agree. Level 10 asks for 21 of the 27 stars
available from Levels 1–9, so a mix of perfect and 2-star runs is enough — a perfect run is never
required.

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

- **bottom left** — two round buttons (`Button Left`, `Button Right`): hold one to walk.
- **bottom right** — an aim pad with a web knob: hold and drag to aim, then **release to shoot**.
  There is no separate shoot button; the shot fires along the aim line and only if it ends on a
  platform (layer 3).
- **cancel** — while the pad is held, drag onto the cross at the top right and release to cancel;
  no Move is spent.
- **camera** — drag any empty part of the screen to pan.
- **HUD** — pause and retry stay on top of the overlay, and the overlay fades out while the game is
  paused, over or won.

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
- **Android:** see [`Docs/Android.md`](Docs/Android.md) for the keystore and device testing notes.
  The shipped build uses package `com.trinova.rewild`, IL2CPP with ARM64, minimum API level 23 and a
  landscape-only screen. The touch overlay is loaded from `Resources/UI/Touch Controls.prefab`, so
  no level scene stores its own copy.

---

## Project layout

| Path | Content |
|---|---|
| `Assets/Scenes/Menus` | `Home Screen`, `Level Selection`, `Help` (How to Play slides), `Level 1 Tutorial` (slides shown before Level 1) |
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

`Level 1 Intro` (an old video intro) is still in `Menus` but disabled in Scenes In Build — the
tutorial slideshow replaced it. `Assets/Scenes/Sandbox` is not part of the game flow.

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

## About this project

Rewild is a **college coursework project**. It was built as a student assignment, not as a product,
and it is published here so classmates, friends and anyone curious can play it and read how it was
made.

- **Not commercial.** The game is free, the builds are free, and there are no ads, in-app purchases,
  accounts, subscriptions or analytics. Nothing in this project is sold, and there is no plan to
  sell it.
- **No company behind it.** `Trinova` is the placeholder company name used in the Unity player
  settings; it is not a registered studio.
- **AI-assisted, hand-tuned.** Code and art began as AI-generated drafts, then were wired, redrawn
  and balanced by hand — see [Credits](#credits).
- **Feedback is welcome.** If you find a bug or a level that feels unfair, open an issue.

---

## Credits

- Design, direction and playtesting: **Sub-Danzzilo**
- Code and art in this project were generated with **AI assistance** and then iterated, wired and
  tuned by hand. Sprites, animations, UI art and music/SFX did not start from a store asset pack.
- Third-party editor tool included for convenience: **Asset Usage Detector**
  (`Assets/Plugins/AssetUsageDetector`, MIT).

## License

There is no license file in this repository, so the code, art and audio remain **all rights
reserved** by the author. This is a non-commercial student project: you are welcome to play it, share
the release page and read the source for learning, but please do not sell it, publish it as your own
work, or reuse its art or audio without asking. If you want to use a part of it, open an issue first.
