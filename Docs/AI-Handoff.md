# Rewild — Dokumen Serah-Terima untuk AI

> **Cara pakai dokumen ini:** kirim file ini saja ke AI lain (atau tempel isinya). Tujuannya supaya
> AI itu langsung paham arsitektur, angka gameplay, jebakan engine, dan aturan kerja proyek ini
> tanpa perlu dibacakan file lain. Kalau AI itu butuh aturan kerja harian yang lebih rinci, arahkan
> ke `AGENTS.md` (ada di akar proyek) — dokumen ini merangkum bagian pentingnya.
>
> **Bahasa:** pemilik proyek berkomunikasi dalam **Indonesia**. Teks yang tampil di game berbahasa
> **Inggris** ("Play", "Moves", "You Died!", "You've Run Out of Moves!"). `[Header]` di Inspector
> berbahasa Inggris, `[Tooltip]` dan komentar kode berbahasa Indonesia.

---

## 1. Ringkasan proyek

| Item | Nilai |
|---|---|
| Nama produk | `Rewild` (company: `Trinova`, versi `1.0.1`, package Android `com.trinova.rewild`) |
| Genre | 2D platformer-puzzle dengan gerak berbasis permukaan (bisa jalan di dinding & langit-langit) dan ayunan jaring |
| Karakter | **Webi**, laba-laba. Membidik dengan jaring, menembak ke platform, lalu ditarik ke platform itu |
| Objek | Mengambil **benih** (`Seed`), menanamnya di **tumpukan tanah** (`TumpTanah`), tanaman tumbuh = level selesai |
| Batasan | Tiap level punya jatah **Moves** (jumlah tembakan jaring). Habis = game over |
| Progresi | 10 level, maksimal **3 bintang per level** (berdasarkan sisa moves), level berikutnya terbuka bila total bintang cukup |
| Platform | Windows (utama) + Android (kontrol sentuh sudah jadi, panduan build di `Docs/Android.md`) |
| Engine | **Unity 6000.2.9f1**, URP 2D (17.2.0), Input System 1.14.2, TextMeshPro, Tilemap + tilemap extras 5.0.2, Timeline |

Kamera ortografis yang bisa digeser (tahan-klik-kanan di Windows, tahan-drag di area kosong pada
Android), zoom otomatis saat menanam. Tidak ada musuh bos, tidak ada sistem nyawa: satu sentuhan
bahaya = mati, lalu popup restart.

---

## 2. Struktur folder & scene

```
Assets/
  Scenes/Menus/      Home Screen, Level Selection, Help, Level 1 Tutorial
  Scenes/Levels/     Level 1 … Level 10
  Scenes/Sandbox/    Experiment, Testing (bukan bagian alur game)
  Scenes/Templates/  URP2DSceneTemplate
  Scripts/Core/      kamera, tilemap builder, progres, audio, alur scene
  Scripts/Gameplay/  Webi, benih/tanaman, musuh, portal, gate/lever, platform bergerak, efek hazard
  Scripts/UI/        HUD, popup, level selection, settings, kontrol sentuh Android
  Scripts/Editor/    jembatan agen, brush slope 26°, snap editor
  Prefabs/Characters Webi
  Prefabs/Enemies    Bug (SurfaceAttached), Fly (Flying)
  Prefabs/Environment Platform, Rotate Grid, Moving Ground, Seed, TumpTanah, Spike_1..3
  Prefabs/Props      BluePortal, GreenPortal, Gate, Lever
  Prefabs/UI         VolumeToggle
  Resources/UI       LockedLevelPopup, Touch Controls  ← dimuat runtime lewat Resources.Load
  Resources/         MusicLibrary.asset, Video/Level1Intro.mp4 (tidak dipakai lagi)
  Art/               sprites, tiles, palettes (lihat §9)
Docs/                Android.md, HowToPlay.md, AI-Handoff.md (file ini)
Tools/agent/         kanal perintah ke Unity yang sedang terbuka (§11)
AGENTS.md            aturan kerja proyek
```

Semua scene level **wajib** terdaftar di Scenes In Build.

---

## 3. Aturan main (loop gameplay)

1. Webi berjalan dengan `A`/`D` (Windows) atau tombol kiri/kanan (Android).
2. Tahan **F** (Windows) atau **analog bidik** (Android) → garis bidik muncul. Putih = target valid
   (collider di **layer 3 / Platform**), merah = bukan.
3. Lepas/klik kiri → jaring melesat, lalu Webi ditarik ke titik itu. **Satu tembakan = 1 move**.
4. Sampai di platform, Webi bisa berjalan lagi. Benih bisa diambil sambil berjalan maupun berayun,
   tetapi menanam hanya saat berjalan.
5. Menanam benih di `TumpTanah` → animasi tumbuh → event `PlantingCompleted` → popup menang.
6. Sisa moves menentukan bintang:

| Sisa moves saat menang | Bintang |
|---|---|
| ≥ `movesForThreeStars` (default 3) | 3 |
| ≥ `movesForTwoStars` (default 1) | 2 |
| 0 | 1 |

Moves habis → popup "You've Run Out of Moves!" (restart / Level Selection). Mati karena hazard,
musuh, atau terjatuh karena oil → "You Died!" / "You Fell!".

**Budget moves per level (nilai `maxMoves` di scene, harus cocok dengan `LevelHudController`):**

| Level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| maxMoves | 6 | 9 | 9 | 8 | 12 | 9 | 8 | 12 | 17 | 10 |

Total 100 moves untuk 10 level. Tiga bintang berarti menyelesaikan level dengan memakai
`maxMoves − 3` tembakan atau lebih sedikit.

**Syarat buka level** (satu tabel: `LevelUnlockRules.RequiredStarsByLevel`, indeks = nomor level):

| Level | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|
| Total bintang minimal | 2 | 4 | 7 | 9 | 11 | 14 | 16 | 18 | 21 |

Maksimal 30 bintang (10 × 3); untuk membuka Level 10 pemain punya paling banyak 27 bintang dari
9 level sebelumnya. Bintang disimpan sebagai **yang terbaik** per level (`PlayerPrefs`,
key `Level.Stars.<n>`), main ulang dengan hasil lebih buruk tidak menurunkan total.

---

## 4. Alur scene

```
Home Screen → Level Selection → (Level 1 Tutorial, hanya level 1) → Level N
Level N (menang) → popup menang: 3 bintang, tombol Next Level / Level Selection / Restart
                                    Next Level dicek dulu ke LevelUnlockRules:
                                    kalau belum cukup bintang → LockedLevelPopup di atasnya
```

- `GameSceneController` memuat scene menu (Play → Level Selection, Settings popup, How to Play → scene
  `Help`, Exit).
- `LevelIntroController` memutar video dari `Resources` lalu memuat `targetScene`. Scene `Level 1 Intro`
  masih ada di folder tetapi dimatikan di Scenes In Build; alur sekarang memakai `Level 1 Tutorial`.
- `LevelHudController` menyimpan `levelNumber` yang dipakai popup menang untuk menyimpan bintang.

---

## 5. Webi — `PlayerController` (inti permainan, ~2.860 baris)

Gerak **berbasis normal permukaan**: Webi menempel pada permukaan tempat dia berdiri, gravitasi
dimatikan (`gravityScale = 0`), dan orientasi badan mengikuti normal. Karena itu dia bisa berjalan
di lantai, dinding, dan langit-langit. Arah "depan" = `currentForward` (tegak lurus normal).

| State (`State`) | Arti |
|---|---|
| `Falling` | jatuh bebas |
| `Walking` | menempel & berjalan di permukaan |
| `Aiming` | berhenti, menggambar garis bidik |
| `Swinging` | jaring ditembakkan / Webi ditarik |
| `Transitioning` | pindah permukaan di tikungan (mis. kaki tangga/dinding) |
| `OilFalling` | terlempar karena menginjak oil |

| Fase ayunan (`SwingPhase`) | Arti |
|---|---|
| `ShootingWeb` | ujung jaring melesat ke target (juga keadaan diam setelah retract selesai) |
| `PullingPlayer` | Webi ditarik menuju titik target |
| `LandingPlayer` | animasi mendarat di platform tujuan |
| `RetractingWeb` | benang ditarik kembali ke badan Webi (setelah mendarat) |

Angka penting (field publik di prefab `Webi`, nilai default):

| Field | Nilai | Catatan |
|---|---|---|
| `walkSpeed` | 5 | unit/detik |
| `swingSpeed` | 15 | tarikan menuju target |
| `webShootSpeed` | 25 | ujung web |
| `webRetractSpeed` | 30 | tarik benang setelah mendarat |
| `iceTopSpeedMultiplier` | 1.35 | pengali kecepatan di es |
| `iceAcceleration` / `iceDeceleration` | 7 / 3.5 | licin: percepatan kecil, perlambatan kecil |
| `oilFallImpulse` / `oilFallAcceleration` / `oilFallMaxSpeed` | 2.5 / 25 / 22 | terlempar menjauhi permukaan |
| `surfaceSampleDepth` | 0.3 | kedalaman baca tile es/oil |
| `surfaceProbeDistance` | 0.2 | probe cari permukaan/tikungan |
| `spriteFootPadding` | 0.1926 | cadangan; nilai asli per-frame di `spriteFootPaddings` |
| `aimRayDistance` / `aimLineWidth` | 100 / 0.08 | garis bidik |

Hal-hal yang mudah salah:

- **Padding kaki diukur per frame animasi** (`spriteFootPaddings`), bukan satu angka — kanvas tiap
  frame walk/shoot/aim berbeda.
- `GetSurfaceOffset()` = padding visual + `surfaceOffset`; dipakai untuk menempelkan badan ke
  permukaan dan untuk menghitung titik kaki (dipakai pembacaan tile es/oil).
- Platform bergerak: Webi mengikuti lewat `FollowSupportSurfaceMotion()` (translasi saja). Jangan
  geser Webi lagi di komponen lain.
- `SetMovementLocked(true)` dipakai intro kamera/menanam: membatalkan bidikan dan membuang
  permintaan sentuh (`TouchControlsInput.ClearRequests()`).
- Nama class lama di prefab `Webi` adalah **`SpiderPlatformer`**; jangan bingung, itu `PlayerController`.
- Menekan bidik saat Webi belum bisa membidik (sedang berayun / pindah platform) disimpan di
  `pendingAimResume` dan dilanjutkan otomatis setelah mendarat — sebelumnya event satu-frame itu
  hilang dan garis bidik tidak muncul walau jari masih menahan.

### 5.1 Bahaya permukaan (es & oil)

Jenis permukaan dibaca dari **tilemap overlay** (`Tilemap_Surface_Ice`, `Tilemap_Surface_Oil`) di
bawah kaki. Fungsi `GetSurfaceKindAtFeet()` membaca 3 titik sepanjang permukaan (tengah, ±0,35 unit)
× 3 kedalaman (0, ±0,3 unit). Sampling melebar ini perlu karena **sambungan dua ramp 26° bergeser
satu baris sel**: titik tepat di kaki bisa jatuh ke sel kosong dan efek es mati sesaat (Webi
berhenti). Jangan kembalikan ke satu titik.

- Es: kecepatan tidak mengikuti tombol langsung, memakai `slideSpeed` (percepatan/perlambatan kecil).
- Oil: Webi terlempar menjauhi permukaan dan jatuh. Satu sel oil hanya memicu sekali
  (`oilLossCell`) supaya tidak memantul di tempat. Kalau tidak ada ground di bawah → keluar peta → kalah.

---

## 6. Level & tilemap

Struktur scene level (root): `Background`, `HUD`, `Level Tilemap`, `Background (World)`,
`Main Camera`, lalu prefab `Webi`, `Seed`, `TumpTanah`, `Spike_*`, musuh, portal, gate/lever.

`Level Tilemap` = Grid **cell size 2** dengan anak `Tilemap_Ground`, `Tilemap_Surface_Grass`,
`..._Grass_2`, `..._Oil`, `..._Ice`. Layer **3 (Platform)** untuk semuanya; deteksi pijakan, raycast
bidik, dan portal hanya mengenali layer itu.

`LevelTilemapBuilder` bisa membangun tile dari daftar `Blocks`, **tetapi hanya rebuild kalau
`Tile Size` berbeda dari `lastBuiltTileSize` atau `Tilemap_Ground` masih kosong**. Level di proyek
ini dilukis manual, jadi jangan menekan "Bangun Ulang Tilemap" — lukisan tangan akan tertimpa.

### 6.1 Slope 26° dan 45°

Tilemap tidak bisa memutar tile per sel, jadi:

- **Slope 26°** (kemiringan 0,5): satu baris = dua sel, art dibagi dua — `_L` (kiri-bawah →
  kanan-tengah) dan `_R` (kiri-tengah → kanan-atas). Rantai naik satu baris tiap pasangan:
  `(10,7)+(11,7)` → `(12,8)+(13,8)` → dst. Alat: `Slope26Brush` (editor) mengisi dua sel per klik,
  letak sel kedua dihitung dari geometri (`ResolvePlacement`), sehingga flip/rotasi tetap
  bersambung. Sel yang sudah berisi tidak ditimpa.
- **Slope 45°** (`Slope_*_45_0`): satu tile per sel. Jangan mencampur set 26° dan 45°.
- Art **dibakar ke PNG** (bukan lewat `m_Transform` tile — itu tidak mengubah gambar di tilemap).
  Ukuran tile = 1 sel penuh: `lebar rect ÷ PPU = 2`. PPU boleh berbeda antar set (50 · 443,5 ·
  615,5 · 627 di proyek ini).
- Hanya `Tilemap_Ground` punya physics shape; overlay (rumput/es/oil) `m_ColliderType: 0`.
- Warna tanah datar disamakan dengan tanah slope lewat `m_Color` **aset Tile** (bukan menimpa PNG):
  `Platform_Ground_Fill_0` = `(0.7, 0.85, 1)`. Setelah mengubah `m_Color`, jalankan
  `run refresh-tilemap` supaya tilemap ikut memperlihatkan.

### 6.2 Chunk grid miring, moving ground, platform

- **`Rotate Grid`** (`Grid Miring`): prefab grid + tilemap yang diputar sebagai satu objek untuk
  terrain miring bebas sudut. Dilukis manual per instance. Jangan didaftarkan ke `LevelTilemapBuilder`.
- **`Moving Ground`**: salinan `Rotate Grid` + `MovingPlatform`. Aturan keras: semua tilemap-nya
  **layer 3**, dan **root di posisi kelipatan 2** (grid tidak punya origin sendiri; kalau tidak,
  garis sel meleset ±1 unit).
- **`Platform`**: kotak yang diputar, untuk strip kecil melayang.

### 6.3 Jebakan collider tilemap (penting)

`TilemapCollider2D` menyimpan bentuk collider **per tile** di memori; `CompositeCollider2D`
menyimpan hasil gabungan (`m_ColliderPaths`) di scene. Setelah sprite diimpor ulang atau tile
dilukis ulang, keduanya **tidak** ikut berubah. Yang **tidak** cukup: `RefreshAllTiles()`, memasang
ulang tile, `ProcessTilemapChanges()`, mematikan-menyalakan `enabled`.

Yang berhasil: **hancurkan lalu buat ulang `TilemapCollider2D`**, salin `compositeOperation`
(Merge), `isTrigger`, `sharedMaterial`, `density`, `offset`, `extrusionFactor`,
`maximumTileChangeCount`, `useDelaunayMesh`, `layerOverridePriority`, lalu
`composite.GenerateGeometry()` — dan **scene harus disimpan**. Di Unity 6 `usedByComposite` usang;
`TilemapCollider2D` tidak punya `edgeRadius`.

Jembatan perintah sudah menyediakan ini: `run rebuild-collider [namaTilemap]`.

---

## 7. Sistem lain

| Sistem | File | Inti |
|---|---|---|
| Benih & menanam | `SeedPlantingController` | ambil `Seed` → tanam di `TumpTanah` → animasi tumbuh → `PlantingCompleted`. `PlayerController` di-disable selama animasi. Pile tanah dimatikan karena frame animasi sudah membawa tanahnya |
| Musuh | `EnemyController` | mode `Flying` (Fly) dan `SurfaceAttached` (Bug); waypoint + `PatrolMode`; `flyThroughPlatforms` khusus Fly. Musuh tanpa waypoint mematikan dirinya saat `Start`. Gizmo jalan di luar Play Mode |
| Portal | `PortalController` | dua sisi (biru masuk, hijau keluar). Saat berayun, jaring bisa melewati portal; segmen setelah portal digambar `portalLineRenderer`. Target ayunan mengikuti platform bergerak |
| Gate & lever | `GateLeverController` | lever membuka gate; mask menutupi gate + jalur gesernya supaya Webi tidak menembus celah |
| Hazard | `Spike_1..3`, `HazardSparkleEffect` | duri mematikan; kilau bintang murni visual |
| Platform bergerak | `MovingPlatform` | `[DefaultExecutionOrder(-100)]`, memindah collider statis + `Physics2D.SyncTransforms()`; lintasan via `Local Travel Offset` atau `Waypoints` (`PingPong`/`Loop`/`Once`/`Random`) |
| Partikel permukaan | `TilemapSurfaceMotes` | dipasang otomatis ke tilemap es/oil; bentuk salju vs bulatan, dibuat dari kode (tanpa PNG) |
| SFX dekat | `ProximitySfx` | volume mengikuti jarak ke Webi |
| Kamera | `CameraDragController` | drag (tombol per level), batas dari isi level, mode scripted untuk intro |
| Intro kamera | `LevelStartCameraIntro` | zoom ke tumpukan tanah → tahan → geser ke Webi → zoom out → hitung mundur 3-2-1 (`LevelHudController.PlayCountdown()`), gerak Webi dikunci sampai selesai |
| Latar | `LevelBackgroundExtension` | memperluas latar tanpa mengubah skala/posisi latar asli |
| Audio | `AudioManager` + `MusicLibrary` + `SceneMusicAutoPlay` | musik per scene dari `Resources/MusicLibrary.asset`; level memulai musik saat intro selesai |
| Debug | `DebugScreenshotTaker` | F12 menyimpan screenshot ke `Screenshots/` |

---

## 8. UI

`LevelHudController` mengatur HUD level: label moves, popup pause (dengan hitung mundur resume),
popup game over (3 judul sesuai penyebab), popup menang, dan **memuat prefab kontrol sentuh**.

| Menu / komponen | File | Inti |
|---|---|---|
| Level Selection | `LevelSelectionController`, `LevelButtonView` | total bintang → buka/kunci tombol; klik level terkunci → popup |
| Popup menang | `LevelCompletePopup` | 3 bintang terisi satu per satu sesuai sisa moves; Next Level / Level Selection / Restart |
| Popup terkunci | `LockedLevelPopup` | dipakai dua wujud: instance di scene Level Selection dan prefab `Resources/UI/LockedLevelPopup` (dipanggil `LevelCompletePopup`) |
| How to Play / Tutorial | `HowToPlayController` | 6 slide, PREV/NEXT, panah kiri/kanan, indikator bintang; versi teks Android otomatis (`Application.isMobilePlatform`). Dipakai scene `Help` (Play Scene kosong, Back hidup) dan `Level 1 Tutorial` (slide terakhir menjadi PLAY → `Level 1`, Back mati) |
| Popup | `UIPopupSlide` | slide dari atas, tanpa Animator |
| Tombol | `UIButtonInteractive` | area klik mengikuti bentuk sprite (Preserve Aspect) |
| Settings | `SettingsController`, `AudioToggle`, prefab `VolumeToggle` | musik & SFX, disimpan di PlayerPrefs |
| Latar menu | `BackgroundSlideshow` | slideshow gambar |

Canvas level: **1920×1080**, Canvas Scaler *Scale With Screen Size*, Match 0.5.

### 8.1 Kontrol sentuh Android (global, satu prefab)

Prefab `Assets/Resources/UI/Touch Controls.prefab` dimuat runtime oleh `LevelHudController`
(pola sama seperti `LockedLevelPopup`), jadi **10 scene level tidak menyimpan salinannya**. Ubah
tata letak cukup di prefab itu.

| Objek | Anchor | Posisi | Ukuran | Catatan |
|---|---|---|---|---|
| `Camera Drag Area` | stretch | — | — | transparan, paling belakang |
| `Button Left` | kiri bawah | (300, 245) | 215×235 | ikon panah 105×105 |
| `Button Right` | kiri bawah | (565, 245) | 215×235 | ikon panah 105×105 |
| `Aim Pad` | kanan bawah | (−345, 285) | 330×330 | jaring besar `Web` 112×112 di pusat pad, `Tail` 78×53,4 (hanya muncul saat ditahan) |
| `Cancel Zone` | kanan atas | (−275, −265) | 250×250 | latar `touch_aim_pad` + ikon X 150×150 di (5, −10) |

- Alpha: diam **0,4**, dipakai **0,9**, fade 0,12 s; seluruh overlay disembunyikan saat
  `Time.timeScale <= 0`.
- Analog: `knobRadius` 64 (tepi jaring berhenti di tepi lingkaran hijau), `deadZone` 20,
  `knobReturnSpeed` 18; knob berputar mengikuti arah bidik.
- `TouchControlsInput` adalah facade statis (state + permintaan satu-frame) supaya script gameplay
  tidak bergantung pada tombol mana yang dipakai.
- Sprite knob dipecah dua: `touch_aim_knob_web.png` (jaring besar) + `touch_aim_knob_tail.png`
  (tali + jaring kecil). Rect sprite `touch_aim_pad.png` **digeser** di `.meta` (`x 55, y 57`)
  supaya lingkaran tombol tepat di tengah rect — jangan dikembalikan ke `36, 71`, jaring akan
  terlihat tidak tengah.

---

## 9. Pipeline art & aset

- Sprite UI di `Assets/Art/UI/Controls/` (sebelumnya bernama Indonesia, sudah di-rename).
- Tile di `Assets/Art/Tiles/`; palette `Assets/Art/Tiles/Palettes/palette_platform.prefab` memakai
  **Grid cell size 1**, sedangkan level cell size 2 — tile dari palette bisa mendarat di grid salah.
- Tidak ada asmdef dan tidak ada folder test: semua script runtime ada di satu assembly
  `Assembly-CSharp`; satu-satunya verifikasi otomatis adalah kompilasi.

---

## 10. Jebakan yang berulang (baca sebelum mengedit apa pun)

1. **Scene bertanda `*` di Unity** punya isi memori berbeda dari disk. Jangan menulis file `.unity`
   yang sedang terbuka; minta pemilik menyimpan (Ctrl+S) atau ubah lewat skrip editor sementara.
2. **Jangan menggeser/merotasi/menskalakan objek scene** kecuali diminta. Tugas "sambungkan script"
   berarti mengisi referensi, bukan menata ulang level. Kalau posisi menurut Anda salah, tanyakan.
3. **Jangan menekan "Bangun Ulang Tilemap"** pada level yang dilukis manual.
4. **Jebakan collider tilemap** (§6.3): bentuk collider tidak ikut berubah saat art berubah.
5. **`fileID` sprite**: sprite **Sprite Mode = Single** direferensikan dengan
   `{fileID: 21300000, guid: ..., type: 3}`. Sprite **Multiple** memakai `internalID` dari `.meta`
   (angka besar, bisa negatif). Salah pakai = Image digambar sebagai **kotak putih**.
6. **Format YAML Unity**: vektor/kuaternion wajib bernama, `{x: -33, y: -0.25}` — bukan
   `{-33.0, -0.25}`. Kesalahan ini membuat prefab gagal di-parse (`Expected closing '}'`).
7. Setelah mengedit prefab/`.meta` dari luar Unity, Unity perlu **impor ulang** (klik jendela Unity /
   Ctrl+R). Prefab YAML yang salah parse tidak akan muncul sebagai error di `dotnet build`.
8. **Scene YAML punya akhir baris berbeda**: `Level 1`–`Level 3` LF, sebagian scene menu (mis.
   `Level Selection`) CRLF. Periksa per file.
9. **`.csproj` bersifat generated**: BOM UTF-8 + CRLF, daftar file eksplisit. Kalau menambah file
   `.cs` sementara, tambahkan ke `<Compile Include="...">` lalu **kembalikan persis** ke kondisi
   semula.
10. **Mengedit `.cs` dari shell Windows**: perintah dikirim dengan CRLF sehingga heredoc bisa rusak.
    Tulis skrip Python sementara di `%TEMP%`/`Temp/`, normalisasi ke `\n`, tulis kembali sebagai
    CRLF + BOM sesuai aslinya, jalankan, lalu hapus skripnya.
11. **Root `Grid Miring`/`Moving Ground` harus di posisi kelipatan 2**, dan semua tilemap platform
    wajib **layer 3**.
12. **Nama file dan objek memakai spasi** (`Level 4.unity`, `Grid Miring`, `Touch Controls.prefab`)
    — selalu kutip di perintah shell. Jembatan perintah memecah argumen per spasi, jadi perintah
    `run reimport` tidak bisa dipakai untuk path ber-spasi.
13. `CameraDragController → playerController` boleh kosong; `dragButton` berbeda antar level
    (klik kiri bisa bentrok dengan tembak).
14. `LevelHudController` memaksa HUD aktif setiap scene level dimuat.
15. Saat Webi menempel di langit-langit, arah A/D mengikuti orientasi tubuhnya (terasa terbalik).
16. Duplikat scene membawa nilai level sumber (nomor level, next level, `Blocks`) — lihat checklist
    "membuat level baru" di `AGENTS.md`.

---

## 11. Cara kerja & verifikasi (tanpa mengklik UI)

Agen tidak bisa menekan Play atau Inspector. Pola yang dipakai:

1. **Kompilasi** (satu-satunya cek otomatis):
   ```bash
   dotnet build Assembly-CSharp.csproj          # script runtime
   dotnet build Assembly-CSharp-Editor.csproj   # script di Assets/Scripts/Editor
   ```
   Target: 0 error, 0 warning.
2. **Mengubah state scene/aset**: tulis skrip editor sementara di `Assets/Scripts/Editor/` dengan
   `[InitializeOnLoad]` + `EditorApplication.delayCall`, kunci sekali-jalan lewat `SessionState`, dan
   cetak hasil dengan `Debug.Log("[Rewild...] ...")`. Setelah selesai **hapus skrip + `.meta`-nya**.
   Catatan: Unity hanya mengompilasi saat jendelanya fokus — kalau tidak ada log, berarti skrip belum
   dikompilasi.
3. **Jembatan perintah** (`Assets/Scripts/Editor/RewildAgentBridge.cs`, aktif bila
   `Tools/agent/enabled.flag` ada): tulis teks ke `Tools/agent/queue/<nama>.cmd`, hasil JSON muncul
   di `Tools/agent/out/<nama>.result`.
   - `ping` — versi Unity, scene aktif, `isPlaying`, `isCompiling`.
   - `state [namaTilemap]` — sel yang berisi tile (koordinat, sprite, matrix, flags, warna) +
     properti collider; dibaca dari **memori editor**, jadi scene bertanda `*` pun terbaca.
   - `run rebuild-collider [namaTilemap]`, `run refresh-tilemap [namaTilemap]`,
     `run reimport Assets/...` (tanpa spasi).
   - Tidak ada perintah simpan scene: menyimpan tetap di tangan pemilik.
4. **Geometri diverifikasi offline** dengan Python + PIL (render tile/collider dari PNG dan `.meta`,
   bandingkan angkanya) — lebih kuat daripada menilai screenshot.
5. Setelah edit, cek integritas YAML: tidak ada `fileID` ganda, semua `{fileID: ...}` yang dipakai
   masih ada, dan diff terhadap backup hanya berisi bagian yang dimaksud.

Tidak ada test otomatis. Verifikasi perilaku (gerak, ayun, popup, benih) tetap **play test manual**
oleh pemilik — jangan mengklaim "sudah dites".

---

## 12. Status & hal yang belum ada

- Kontrol Android sudah berfungsi (tombol, analog bidik, area batal, drag kamera); panduan build,
  pengaturan Project Settings, dan checklist uji ada di `Docs/Android.md`. `applicationIdentifier`
  Android belum diisi, orientasi sebaiknya dikunci Landscape Left.
- Belum ada: sistem save selain PlayerPrefs (bintang & audio), localization (teks Inggris hardcoded),
  controller gamepad, analytics, menu pause → settings langsung (settings hanya di Home Screen).
- Ide/pekerjaan lanjutan sebaiknya disampaikan sebagai saran, bukan dikerjakan tanpa diminta.

---

## 13. Ringkasan pesan untuk AI penerima

> Ini proyek Unity 2D "Rewild" (laba-laba Webi, ayun jaring, tanam benih, 10 level, 3 bintang per
> level berdasarkan sisa moves). Semua gameplay ada di `PlayerController` (gerak berbasis normal,
> state machine 6 state, ayunan 4 fase). Level dibangun dari tilemap grid 2 unit dengan terrain
> miring berupa chunk grid yang diputar; collider tilemap punya cache yang harus di-rebuild dengan
> cara khusus. Angka gameplay, aturan bintang, tabel unlock, tata letak kontrol Android, jebakan
> YAML/sprite/csproj, dan cara verifikasi tanpa Unity sudah ada di dokumen ini. Sebelum mengubah
> apa pun: jangan menggeser objek scene, jangan menulis scene yang sedang terbuka, jangan menekan
> "Bangun Ulang Tilemap", dan konfirmasi dulu kalau butuh membuat file baru.
