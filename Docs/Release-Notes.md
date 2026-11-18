# Release notes

Teks final untuk halaman **GitHub Releases**. Salin blok tiap versi apa adanya ke kolom release
notes, dan tulis `Release title` di kolom judul. Isi catatan tetap bahasa Inggris karena dibaca
pemain; metadata di bawah ini bahasa Indonesia untuk pemilik proyek.

Yang diunggah hanya dua aset per versi: `.zip` Windows dan `.apk` Android. Folder
`..._BackUpThisFolder_ButDontShipItWithYourGame`, `Rewild_BurstDebugInformation_DoNotShip`,
`Rewild_Data`, `MonoBleedingEdge`, `D3D12`, `UnityCrashHandler64.exe` dan `DirectML.dll` tidak ikut.

---

## v1.0.1 — Windows & Android

- **Release title:** `Rewild v1.0.1 — Windows & Android`
- **Assets:** `Rewild_v1.0.1_20261118.zip`, `Rewild_v1.0.1_20261118.apk`
- **Windows build:** 18/11/2026 01:49–01:50, versi player `1.0.1`.
- **Android build:** 18/11/2026 02:04 — package `com.trinova.rewild`, `versionName 1.0.1`,
  `versionCode 1`, `minSdk 23`, `targetSdk 36`, ABI `arm64-v8a`.
- **Scene ikut build (14):** Home Screen, Level Selection, Help, Level 1 Tutorial, Level 1–10.
  `Level 1 Intro` dan `Testing` sudah dimatikan.

### Rewild

Rewild is a small 2D puzzle-platformer about a spider named **Webi** who brings a dead world back to
life. Webi cannot jump: you aim a web line, shoot it at a platform, and get pulled toward it. Every
shot costs one **Move**, so reaching the dirt mound with a seed still costs planning.

**v1.0.1** is the second public build, for Windows and Android.

### Downloads

| Platform | Asset | Requirements |
|---|---|---|
| Windows | `Rewild_v1.0.1_20261118.zip` — unzip it anywhere and run `Rewild.exe` | Windows 10 (21H1 / build 19043) or newer, 64-bit, keyboard + mouse |
| Android | `Rewild_v1.0.1_20261118.apk` — allow installs from unknown sources, then open it | Android 6.0 (API 23) or newer, ARM64 phone; play in landscape |

### What's new in 1.0.1

- **A tutorial now plays before Level 1.** Five short slides explain walking, aiming, shooting and
  the Move counter, then the last button turns into PLAY. The old video intro is no longer part of
  the build, so the level loads right after the slides.
- **How to Play is its own `Help` scene**, reachable from the Home Screen. PREV and NEXT disappear
  at the ends of the slides instead of leaving dead buttons, and the Back button now matches the
  Level Selection button.
- **The camera is easier to drag**: the draggable area is wider in every level, so panning no longer
  requires grabbing the exact middle of the screen.
- **Audio fixes**: gameplay SFX (portal hum, footsteps, enemies) stay quiet while the world is
  frozen by pause, death or the win popup, and the star chime on the win popup no longer gets cut
  off.
- **Enemies pass through each other** instead of bumping and blocking a patrol route.
- **Level 9 slopes** are nudged so the landings line up with the painted ground again.
- **Android build is now a proper release build**: package `com.trinova.rewild`, version 1.0.1,
  IL2CPP with ARM64 only, and the app icon installed.

### What's in the game

- **10 hand-painted levels** with 100 Moves in total, from a 6-Move opener to a 17-Move Level 9.
- **Web-swinging movement**: hold to aim, release to shoot. Webi walks on floors, walls and
  ceilings; ice is slippery, oil throws him off the level, spikes and enemies end the run.
- **Stars and unlocks**: 3 stars for finishing with 3+ Moves left, 2 stars with 1–2 left, 1 star on
  the last Move. Your best result per level is saved, so a bad replay never lowers your total.
- **Moving platforms, portals, lever gates and two enemy types** in the later levels.
- **Android touch controls**: walk buttons, an aim pad you release to shoot, a cancel zone, and
  drag-to-pan camera.

### Controls

**Windows** — `A` / `D` to walk, hold `F` to aim, left-click to shoot, hold the right mouse button to
drag the camera, HUD buttons for pause and retry.

**Android** — hold the left/right buttons to walk. Hold and drag the aim pad to aim, then **release
to shoot**; the shot only fires if the aim line ends on a platform. Drag onto the cross at the top
right and release to cancel without spending a Move. Drag any empty part of the screen to pan.

### Known limitations

- Stars are stored locally on the device, so reinstalling the game resets your progress.
- No controller support; input is keyboard + mouse on Windows and touch on Android.
- The Android hardware Back button does not return to the menu — use the on-screen Back button.
- The Windows build is 64-bit only and the APK is ARM64-only.
- There are no automated tests; the build was play-tested by hand.

### About this project

Rewild is a college coursework project, released for free. It is not a commercial product: no ads,
no in-app purchases, no accounts and no analytics, and there is no plan to sell it. Code and art
started as AI-generated drafts and were then wired, redrawn and balanced by hand.

No license file is included, so all rights are reserved by the author. Feel free to play the builds
and read the source; open an issue before reusing any of the art or audio.

---

## v1.0.0 — Windows & Android

- **Release title:** `Rewild v1.0.0 — Windows & Android`
- **Assets:** `Rewild_v1.0.0_20261117.zip`, `Rewild_v1.0.0_20261117.apk`
- **Windows build:** 17/11/2026 18:28, versi player `1.0.0` (dibaca dari `globalgamemanagers`).
- **Android build:** `Rewild_v1.0.0_20261117.apk`; package-nya belum terverifikasi, jadi bisa
  berbeda dari 1.0.1 (`com.trinova.rewild`).
- **Scene ikut build (14):** Home Screen, Level Selection, How To Play, Level 1 Intro, Level 1–10.
  `Testing` sudah tidak ikut; menu bantuan masih bernama `How To Play` dan video intro masih ada.
- **Belum ada di versi ini:** tutorial slideshow, scene `Help`, perbaikan audio, musuh saling tembus,
  penyesuaian Level 9, perluasan area drag kamera, dan ikon aplikasi baru.

### Rewild

Rewild is a small 2D puzzle-platformer about a spider named **Webi** who brings a dead world back to
life. Webi cannot jump: you aim a web line, shoot it at a platform, and get pulled toward it. Every
shot costs one **Move**, so reaching the dirt mound with a seed still costs planning.

**v1.0.0** is the first public build of the game, for Windows and Android.

### Downloads

| Platform | Asset | Requirements |
|---|---|---|
| Windows | `Rewild_v1.0.0_20261117.zip` — unzip it anywhere and run `Rewild.exe` | Windows 10 (21H1 / build 19043) or newer, 64-bit, keyboard + mouse |
| Android | `Rewild_v1.0.0_20261117.apk` — allow installs from unknown sources, then open it | Android 6.0 (API 23) or newer, ARM64 phone; play in landscape |

### What's in the game

- **10 hand-painted levels** with 100 Moves in total, from a 6-Move opener to a 17-Move Level 9.
- **Web-swinging movement**: hold to aim, release to shoot, and Webi gets pulled to the anchor. He
  walks on floors, walls and ceilings; ice is slippery, oil throws him off the level, spikes and
  enemies end the run.
- **Stars and unlocks**: 3 stars for finishing with 3+ Moves left, 2 stars with 1–2 left, 1 star on
  the last Move. Your best result per level is saved, so a bad replay never lowers your total.
- **A video intro before Level 1**, plus a How to Play menu from the Home Screen.
- **Moving platforms, portals, lever gates and two enemy types** in the later levels.
- **Android touch controls**: walk buttons, an aim pad you release to shoot, and drag-to-pan camera.

### Known limitations

- Stars are stored locally on the device, so reinstalling the game resets your progress.
- No controller support; input is keyboard + mouse on Windows and touch on Android.
- The Android hardware Back button does not return to the menu — use the on-screen Back button.
- The Windows build is 64-bit only.
- There are no automated tests; the build was play-tested by hand.

### About this project

Rewild is a college coursework project, released for free. It is not a commercial product: no ads,
no in-app purchases, no accounts and no analytics, and there is no plan to sell it. Code and art
started as AI-generated drafts and were then wired, redrawn and balanced by hand.

No license file is included, so all rights are reserved by the author. Feel free to play the builds
and read the source; open an issue before reusing any of the art or audio.
