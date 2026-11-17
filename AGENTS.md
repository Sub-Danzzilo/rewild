# Rewild — Panduan Proyek (untuk agen AI)

Dokumen ini berisi hal yang **stabil** tentang proyek: aturan kerja, struktur, konvensi, dan jebakan
yang berulang. Jangan taruh di sini catatan kerja sesi (status tugas, TODO, nilai yang sering
berubah) — itu cepat basi dan menyesatkan. Bahasa kerja dengan pemilik proyek: **Indonesia**.

## Aturan kerja dengan pemilik proyek

- **Jangan mengubah posisi, rotasi, atau skala objek di scene tanpa diminta.** Tugas seperti
  "sambungkan script" atau "atur seperti level lain" berarti mengisi referensi dan nilai komponen,
  bukan menata ulang level. Kalau posisi objek menurut saya keliru, **tanyakan dulu**.
- **Jangan memanggil / menekan "Bangun Ulang Tilemap"** pada level yang tile-nya dilukis manual.
- Kalau butuh membuat file baru (dokumen, alat, catatan), tanyakan dulu maksud dan lokasinya.
- Kerjakan hanya yang diminta. Ide tambahan disampaikan sebagai saran di akhir, bukan dikerjakan.
- **Kalau scene level sedang terbuka di Unity dan judulnya bertanda `*`**, isi memori berbeda dari
  file di disk. Beri tahu pemilik bahwa hasil kerja hanya tersimpan kalau ia menekan Ctrl+S, dan
  jangan menulis file scene itu dari luar Unity (lihat *Mengedit scene*).
- **Sebelum meminta pemilik menyimpan scene yang bertanda `*`**, periksa dulu isi memori dengan
  perintah `state` dan bandingkan dengan file di disk. Kalau jumlah tile di memori lebih sedikit
  daripada di disk, jangan minta simpan: kemungkinan ada kerja yang hilang dan Ctrl+S akan
  menimpanya. Laporkan temuannya dan biarkan pemilik memilih (Save / Revert to Saved).
- Sebelum mengedit scene (`.unity`) atau file `.asset` secara langsung: salin ke folder backup
  sementara, lalu setelah selesai pastikan hanya bagian yang dimaksud yang berubah. Untuk mengubah
  isi scene, **utamakan skrip editor sementara** daripada mengedit YAML.
- Verifikasi yang butuh Unity (Console, screenshot) boleh lewat skrip editor sementara + `Editor.log`;
  caranya ada di *Verifikasi tanpa klik UI*.

## Lingkungan

- Unity **6000.2.9f1**, 2D (URP), Input System baru (`Keyboard.current` / `Mouse.current`),
  TextMeshPro, Tilemap.
- Konvensi kode: `[Header]` berbahasa **Inggris**, `[Tooltip]` berbahasa **Indonesia**, komentar
  berbahasa Indonesia, memakai `#region` seperti file yang sudah ada.
- Gizmo bantu memakai `OnDrawGizmos()` (bukan `OnDrawGizmosSelected()`) supaya tetap terlihat di
  Scene view walau objeknya tidak dipilih, dengan dua toggle publik terpisah: arah/jalur dan label.
  Teks label lewat `Handles.Label` di dalam `#if UNITY_EDITOR`, dengan stub kosong untuk build.
- Kompilasi bisa dicek tanpa membuka Unity: `dotnet build Assembly-CSharp.csproj` untuk script
  runtime dan `dotnet build Assembly-CSharp-Editor.csproj` untuk script di `Assets/Scripts/Editor`.
  File `.csproj` bersifat generated: memuat daftar file eksplisit dan memakai **BOM UTF-8 + CRLF**.
  File `.cs` baru harus ditambahkan sementara ke `<Compile Include ...>`, lalu `.csproj`
  dikembalikan persis ke kondisi semula (tanpa BOM/CRLF yang berubah, tanpa sisa baris tambahan).
- Scene berformat YAML Unity. Level 1–3 memakai akhir baris LF, sedangkan sebagian scene menu
  (mis. `Level Selection`) memakai CRLF — jaga konsistensi saat mengedit teks.
- Scene dan prefab memakai `fileID`; setelah mengedit YAML, pastikan tidak ada `fileID` ganda dan
  referensi `{fileID: ...}` yang dipakai masih ada.

## Struktur folder

| Path | Isi |
|---|---|
| `Assets/Scenes/Menus` | `Home Screen`, `Level Selection`, `Level 1 Intro` |
| `Assets/Scenes/Levels` | Scene level (`Level 1`, `Level 2`, `Level 3`, …) |
| `Assets/Scenes/Sandbox` | `Experiment`, `Testing` (bukan bagian alur game) |
| `Assets/Scripts/Core` | Kamera, tilemap builder, progres, audio, alur scene |
| `Assets/Scripts/Gameplay` | Player, benih, musuh, portal, lever, efek hazard |
| `Assets/Scripts/UI` | HUD, popup, level selection, settings |
| `Assets/Prefabs/Characters` | `Webi` (pemain) |
| `Assets/Prefabs/Environment` | `Platform`, `Grid Miring`, `Moving Ground`, `Seed`, `TumpTanah`, `Spike_1`, `Spike_2` |
| `Assets/Resources/UI` | `LockedLevelPopup` — prefab popup "level terkunci" yang dimuat saat runtime |

Setiap scene level baru **wajib** didaftarkan di Scenes In Build; tanpa itu tombol restart gagal
dan scene tidak bisa dibuka dari Level Selection.

## Alur game

`Home Screen` → `Level Selection` → tombol level → (scene intro bila ada, mis. `Level 1 Intro`)
→ scene level → menang → popup menang (bintang + `Next Level Scene`) atau kembali ke Level Selection.
Tombol **Next Level** dicek dulu: kalau total bintang belum memenuhi syarat level berikutnya, scene
tidak dimuat dan popup "level masih terkunci" tampil di atas popup menang.

- Tombol level memakai `LevelButtonView` (`levelNumber`, `sceneName`), sedangkan syarat bintangnya
  tidak lagi per tombol: satu tabel di `LevelUnlockRules` (`RequiredStarsByLevel`) yang dipakai
  bersama oleh `LevelSelectionController` dan `LevelCompletePopup`. Mengubah syarat buka level cukup
  di tabel itu.
- `LevelIntroController` memutar video dari `Resources` lalu memuat `targetScene`.
- `LevelHudController` menyimpan `levelNumber` yang dipakai `LevelCompletePopup` untuk menyimpan
  bintang (`LevelProgress`, key `Level.Stars.<n>`).

## Struktur scene level

Root object: `Background`, `HUD`, `Level Tilemap`, `Background (World)`, `Main Camera`, lalu prefab
`Webi`, `Seed`, `TumpTanah`, dan beberapa `Spike_1` / `Spike_2`.

- **Level Tilemap**: `Grid` (cell size 2) dengan child `Tilemap_Ground`, `Tilemap_Surface_Grass`,
  `Tilemap_Surface_Grass_2`, `Tilemap_Surface_Oil`, `Tilemap_Surface_Ice`; layer 3 (Platform).
  Ada komponen `LevelTilemapBuilder` (pengisi tile otomatis) dan `CompositeCollider2D`.
- **Background (World)**: satu `SpriteRenderer` latar; dipakai kamera untuk batas level dan
  perluasan latar (`LevelBackgroundExtension` dibuat otomatis saat runtime).
- **Main Camera**: kamera ortografis + `CameraDragController` + `LevelStartCameraIntro`.
- **HUD**: `LevelHudController` beserta popup pause, game over, dan menang.

## Platform miring (chunk grid diputar)

Tilemap tidak bisa memutar tile per cell, jadi terrain miring dibuat dengan **chunk**: prefab
`Grid Miring` (Grid cell 2 + `Tilemap_Ground` + `Tilemap_Surface_Grass`) yang diputar sebagai satu
objek. Isinya meniru level: `Cell Size` 2, layer 3, `Tilemap_Ground` berisi `TilemapCollider2D`
(Composite Operation = Merge) + `Rigidbody2D` static + `CompositeCollider2D` (Geometry Type
Polygons), sedangkan `Tilemap_Surface_Grass` hanya visual (sorting order 0 dan 1).

Cara pakai: duplikat instance di scene → lukis ground + rumput dengan palette yang sama → sisipkan
sekitar 1 cell ke area datar supaya tidak terlihat jahitannya → putar root `Grid Miring` (sudut
bebas, tidak hanya 45°). Webi berjalan mengikuti kemiringan karena geraknya berbasis normal.

- Jangan mendaftarkan chunk ke `LevelTilemapBuilder` dan jangan menekan "Bangun Ulang Tilemap".
- Rumput sengaja tanpa collider; collision datang dari `Tilemap_Ground`.
- Untuk strip kecil yang melayang, tetap pakai prefab `Platform` (kotak yang diputar).

## Moving Ground (platform bergerak)

`Assets/Prefabs/Environment/Moving Ground.prefab` adalah salinan `Grid Miring` (Grid cell 2 +
`Tilemap_Ground` + dua overlay rumput) yang ditambah `MovingPlatform`. Prefab-nya kosong; tile
dilukis manual per instance dan disimpan sebagai override di scene.

- **Semua tilemap-nya wajib layer 3 (Platform).** Bidik/ayun Webi, deteksi pijakan, dan portal
  hanya mengenali layer itu. Kalau tertinggal di Default (0), garis bidik jadi merah dan Webi tidak
  bisa berdiri di atasnya walaupun collider-nya ada.
- **Root harus di posisi kelipatan 2.** Grid tidak punya origin sendiri — sel dihitung dari posisi
  objeknya, jadi root di `(44.9189, -4.8371)` membuat garis selnya meleset ±1 unit dari grid level.
- `MovingPlatform` menggerakkan collider statis lewat `transform.position` + `Physics2D.SyncTransforms()`
  dan berjalan lebih dulu (`[DefaultExecutionOrder(-100)]`), sehingga raycast Webi di physics step
  yang sama sudah melihat posisi baru. Secara default lintasannya memakai `Local Travel Offset`;
  jika `Waypoints` diisi, platform mengikuti titik dunia sesuai urutan dengan pola `PingPong`,
  `Loop`, `Once`, atau `Random`. Posisi waypoint dicache saat scene mulai.
- Gizmo moving ground tetap digambar di Scene view. Mode waypoint menampilkan jalur, panah,
  label W1/W2, dan waypoint aktif saat Play; `Show Path Gizmo` dan `Show Gizmo Label` mengaturnya.
  Waypoint yang dipakai sebagai titik tujuan platform adalah posisi root platform.
- `CarryRider` sengaja **melewati** objek yang punya `PlayerController`: Webi mengikuti platform
  lewat `PlayerController.FollowSupportSurfaceMotion()`, jadi jangan digeser lagi di sana.

## Tile slope 26° (set L/R)

Grid level berukuran 2 unit, jadi slope 26,57° (kemiringan 0,5) naik 2 unit tiap 4 unit mendatar —
satu baris per **dua sel**. Karena tilemap tidak bisa memiringkan tile per cell, satu baris dipecah
jadi dua setengah tile: `_L` (naik dari kiri-bawah ke kanan-tengah) dan `_R` (naik dari kiri-tengah
ke kanan-atas). Set 45° (`Slope_*_45_0`) hanya satu tile per sel karena kemiringannya 1,0 — jangan
mencampur keduanya.

- Art **dibakar ke PNG**, bukan lewat transform tile: `m_Transform` di tile asset identity dan
  **tidak** mengubah gambar tile di tilemap (sudah dicoba; gambar tetap di 20,8° walau skala 1,3161).
- Ukuran satu tile = satu sel penuh: `lebar rect ÷ PPU = 2 unit`. PPU boleh berbeda antar set
  (50 · 443,5 · 615,5 · 627 di proyek ini); yang penting hasil pembagiannya 2 unit.
- Pivot tile (0,5; 0,5) dan rect setinggi satu sel, sehingga tengah sprite jatuh tepat di tengah
  sel. Penempatan tile jadi tidak bergantung pada anchor/alignment tilemap.
- Titik `physicsShape` di `.meta` sprite adalah **piksel relatif pivot**, bukan unit dunia (pivot di
  tengah → nilainya berkisar ±PPU·1). Bentuk `_L` = segitiga bawah, `_R` = trapesium sampai tepi
  atas sel.
- Hanya `Tilemap_Ground` punya physics shape. Overlay `Slope_Grass_Edge_26`, `Slope_Oil_Edge_26`,
  `Slope_Ice_Edge_26` tanpa shape dan `m_ColliderType: 0`; garis art ketiganya menempel di garis
  yang sama dengan ground.
- `Slope26Brush` mengisi dua sel per klik. Matrix brush dipakai di **kedua** sel, sedangkan letak
  sel kedua dan urutan setengahnya dihitung dari geometri (`ResolvePlacement`: ujung garis kedua
  setengah harus bersambung), bukan dari arah kanan matrix. Jadi flip X/Y dan rotasi 90°/180°
  menghasilkan pasangan yang tetap bersambung — dulu flip X menaruh sel kedua di **kiri** sel yang
  diklik sehingga art-nya pecah. Sel yang sudah berisi tile tidak ditimpa — brush jatuh ke perilaku
  default dan hanya menaruh satu tile, sehingga bisa lahir setengah pasangan.
- Rantai yang benar: tiap pasangan naik satu baris, mis. `(10,7)+(11,7)` → `(12,8)+(13,8)` →
  `(14,9)+(15,9)`. Sel dengan matriks `e00: -1, e11: -1` (pasangan diputar 180°) yang dilukis dengan
  **versi brush lama** bergeser 1 unit dan muncul gumpalan di bawah ramp; hapus sel itu lalu lukis
  ulang. Brush sekarang menempatkan pasangan 180° dengan benar, jadi gejala itu tidak lahir lagi.
- Collider slope mengikuti aturan cache di bagian berikut.
- Tanah datar memakai set `Platform_*` dari art lama yang lebih terang, sedangkan tanah slope
  (26° dan 45°) lebih gelap. Yang menyamakan keduanya adalah `m_Color` pada aset tile (bukan
  menimpa PNG): `Platform_Ground_Fill_0` = `(0.7, 0.85, 1)`. Angkanya dihitung dari rata-rata RGB
  sprite dibagi rata-rata tanah slope 26°; kalau artnya diganti, hitung ulang. Nilai warna tile
  dipakai sebagai pengali langsung pada nilai sRGB sprite (sudah diukur: tint 0,372 pada rata-rata
  132 menghasilkan ±45 di layar), jadi jangan membagi lagi lewat konversi Linear.
  Setelah mengubah `m_Color`, jalankan `run refresh-tilemap` supaya scene ikut memperlihatkannya.

## Jebakan engine: collider tilemap

`TilemapCollider2D` menyimpan bentuk collider **per tile** di memori. Setelah sprite diimpor ulang
atau tile dilukis ulang, bentuk itu tidak ikut berubah, sementara `CompositeCollider2D` menyimpan
hasil gabungannya (`m_ColliderPaths`) di dalam scene. Akibatnya art sudah benar tetapi collider
masih mengikuti art lama.

Yang **tidak** cukup (sudah dicoba dan gagal):

- reimport sprite lalu `tilemap.RefreshAllTiles()`;
- memasang ulang semua tile (kosongkan, lalu `SetTile` lagi beserta warna/flag/matrix);
- `tilemapCollider.ProcessTilemapChanges()` lalu `composite.GenerateGeometry()`;
- mematikan-menyalakan `enabled` collider — inilah yang dikerjakan
  `LevelTilemapBuilder.RebuildColliderGeometry()`, jadi komentar "collider dibuat ulang dari tile
  yang ada" di sana **tidak** berlaku saat bentuk sprite berubah.

Yang berhasil: **hancurkan lalu buat ulang `TilemapCollider2D`** (`Undo.DestroyObjectImmediate` +
`Undo.AddComponent`), salin `compositeOperation` (Merge), `isTrigger`, `sharedMaterial`, `density`,
`offset`, `extrusionFactor`, `maximumTileChangeCount`, `useDelaunayMesh`, dan
`layerOverridePriority`, lalu `composite.GenerateGeometry()`.

- Setelah benar, **scene harus disimpan** supaya path hasil generate ikut tersimpan.
- `CompositeCollider2D`: `m_GeometryType: 1` = Polygons, `m_GenerationType: 0` = Synchronous.
- Unity 6: `usedByComposite` sudah usang (pakai `compositeOperation`, `Merge` = 1);
  `maximumTileChangeCount` bertipe `uint`; `TilemapCollider2D` **tidak** punya `edgeRadius`.
- Cara memastikan collider sudah mengikuti art: kemiringan tepi atas composite harus sama dengan
  kemiringan art (mis. 0,5 untuk 26°) dan kotak collider harus sama dengan kotak sel yang dilukis.
  Bentuk lama akan memperlihatkan kemiringan art sebelumnya (mis. 0,381 ≈ 20,9° pada set 26°).

## Sistem & script

**Core**
- `PlayerController` (nama class lama di prefab: `SpiderPlatformer`) — gerak berbasis normal
  permukaan sehingga Webi bisa berjalan di lantai, dinding, dan langit-langit; ayun web (F untuk
  membidik, klik kiri untuk menembak); `Spawn Surface` untuk memilih menempel di atas atau di bawah
  platform saat level dimulai; `SetMovementLocked()` untuk mengunci gerak dari script lain.
  Gravitasi dimatikan selama menempel (`gravityScale = 0`). `CanPickUpItems` (berjalan atau
  berayun) dipakai untuk mengambil benih, sedangkan `CanInteract` (hanya berjalan) tetap dipakai
  untuk menanam. `FollowSupportSurfaceMotion()` menerapkan delta posisi collider pijakan sebelum
  logika langkah, jadi Webi ikut terbawa platform bergerak saat `Walking` dan `Aiming` (translasi
  saja, rotasi platform belum diikuti). Target ayunan juga mengikuti platform bergerak
  (`TrackMovingSwingTarget`, jangkar disimpan di ruang lokal collider; berlaku juga untuk ayunan
  lewat portal).
- `CameraDragController` — geser kamera dengan drag mouse (`dragButton` diatur per level), zoom
  fokus saat menanam benih, batas dari isi level, serta mode scripted (`BeginScriptedControl`,
  `EndScriptedControl`, `DefaultViewPosition`, `GetClampedViewPosition`) untuk intro kamera.
- `LevelStartCameraIntro` — intro kamera awal level: zoom ke tumpukkan tanah → tahan → geser ke
  Webi → zoom out ke tampilan default → hitung mundur 3-2-1 lewat `LevelHudController.PlayCountdown()`,
  sambil mengunci gerak Webi sampai hitung mundur habis. Parameter waktunya publik.
- `LevelTilemapBuilder` — membangun tilemap dari daftar `Blocks`; **hanya rebuild kalau
  `Tile Size` berbeda dari `lastBuiltTileSize` atau `Tilemap_Ground` masih kosong**. Jadi level yang
  dilukis manual aman saat Play, tetapi berisiko kalau tile di-rebuild. `RebuildColliderGeometry()`
  hanya mengulang generate composite dari bentuk tile yang tersimpan; itu **tidak** cukup kalau
  bentuk sprite berubah (lihat *Jebakan engine: collider tilemap*).
- `LevelProgress` (bintang via PlayerPrefs), `GameSceneController`, `LevelBackgroundExtension`,
  `AudioManager`.

**Gameplay** — `SeedPlantingController` (ambil benih → menanam di `TumpTanah` → animasi tumbuh →
event `PlantingCompleted`), `EnemyController`, `PortalController`, `GateLeverController`,
`HazardSparkleEffect`. Saat benih ditanam, `TumpTanah` (`env_dirt`) **dimatikan** dan visual
tumbuh memakai sorting order yang sama dengan pile — tiap frame animasi sudah membawa gambar
tanahnya sendiri, jadi tanah di layar hanya satu lapis. `RestoreRenderers()` menyalakan lagi pile
kalau fase tumbuh terputus di tengah jalan; setelah animasi selesai pile tetap mati karena frame
terakhir memuat tanahnya sendiri. Ukuran dan posisinya diatur `FitGrowthToSoil()` di koordinat
**lokal** pile (kotak gambar tanah frame animasi ←→ kotak gambar tanah pile, skala seragam); visual
adalah anak pile, jadi rotasinya mengikuti pile (pile 90° → tanaman ikut miring 90°). Tidak ada
field penyesuaian di Inspector. Clip tumbuh hanya menukar sprite (tanpa track rotasi/posisi), jadi
seluruh transform visual berasal dari script ini; acuan fraksi gambar ada di konstanta
`GrowthDirt*Fraction` dan `SoilArt*Fraction` — perbarui kalau file art-nya diganti.

**Musuh** — `EnemyController` punya dua mode yang dipakai sekarang: `Flying` (Fly) dan
`SurfaceAttached` (Bug). Gizmo-nya (`showGizmoDirection` dan `showGizmoLabels`, keduanya publik)
menggambar jalur waypoint, panah arah sesuai `PatrolMode`, label `W1`, `W2`, … dan ringkasan pola —
juga di luar Play Mode, jadi tidak perlu Play untuk membaca arah patroli. Musuh yang belum diisi
waypoint ditandai silang merah; itu penting karena tanpa waypoint script mematikan dirinya di
`Start` dan musuh hanya diam. `flyThroughPlatforms` (khusus `Flying`) membuang layer platform dari
pengecekan penghalang dan dari `excludeLayers` collider/Rigidbody, sehingga Fly menembus platform
tanpa mengubah Layer Collision Matrix global.

**UI** — `LevelHudController`, `LevelCompletePopup`, `LevelSelectionController` + `LevelButtonView`,
`LevelIntroController`, `UIPopupSlide`, `LockedLevelPopup`, `SettingsController`, `AudioToggle`,
`BackgroundSlideshow`, `UIButtonInteractive`.

- `LockedLevelPopup` ada dua wujud: instance di scene `Level Selection` (untuk tombol level yang
  terkunci) dan prefab `Assets/Resources/UI/LockedLevelPopup.prefab` yang dimuat `LevelCompletePopup`
  saat tombol **Next Level** ditekan padahal level berikutnya belum terbuka. Prefab dipakai supaya
  sepuluh scene level tidak perlu menyimpan salinan UI yang sama; kalau tampilan popup diubah,
  ubah keduanya (atau ganti instance di Level Selection dengan prefab itu).

## Konvensi koordinat & penempatan

- Grid cell size 2: cell `(x, y)` menempati dunia `x·2 … x·2+2`, `y·2 … y·2+2` (tile anchor 0.5).
- **Webi**: origin ±1,4 unit di atas permukaan saat berdiri (offset sprite kaki; `surfaceOffset`
  di prefab = −0,1). Saat `Spawn Surface = Platform Above`, script yang memutar Webi 180° dan
  menempelkannya ke sisi bawah platform — tidak perlu rotasi manual di scene.
- **TumpTanah**: di atas platform → root sedikit di atas permukaan; digantung di bawah platform →
  rotasi 180° dan root beberapa unit di bawah permukaan atas. Tanaman yang tumbuh mengikuti rotasi
  pile, jadi arahnya otomatis sesuai penempatan tanah.
- **Seed**: sprite 5×5 dengan scale 0,3 (≈1,5 unit); root ±0,75 unit di atas permukaan, plus bob.
- **Spike_1 / Spike_2**: scale prefab 0,3 (≈6×1,3 unit); di scene sering dioverride 0,18–0,45.
  Tempatkan root sedikit di atas permukaan agar durinya tidak tenggelam.
- Layer platform (3) dipakai untuk deteksi pijakan, raycast bidik, dan portal.

## Checklist: membuat level baru dari duplikat scene

Cara yang dipakai pemilik proyek: **Ctrl+D** scene level terbaru supaya popup, HUD, kamera, dan
efeknya konsisten. Isian berikut **tidak** ikut benar sendiri dan harus disesuaikan:

1. Ganti nama scene, taruh di `Assets/Scenes/Levels`, dan tambahkan ke **Scenes In Build**.
2. `LevelHudController → Level Number` = nomor level baru.
3. Scene level sebelumnya → Win Popup → `Next Level Scene` = nama scene baru.
4. `Level Selection` → tombol level baru → `Scene Name` (+ `Required Stars`).
5. Geser `Webi`, `Seed`, `TumpTanah`, dan spike sesuai desain — posisinya masih posisi scene sumber.
6. `Level Tilemap Builder` → kosongkan `Blocks` atau matikan `Build On Enable` (isinya masih data
   scene sumber, jangan sampai menimpa lukisan manual).
7. Pastikan `Main Camera` punya `LevelStartCameraIntro` (ikut otomatis dari duplikat).
8. Play test: intro kamera → gerak → ambil benih → tanam → popup menang → restart → next level.

## Mengedit scene (`.unity`) dan `.asset`

Urutan yang dipakai:

1. Salin file ke `%TEMP%` (atau `Tools\backup`) sebelum menyentuh apa pun.
2. Pilih cara: **skrip editor sementara** untuk mengubah isi scene (tile, komponen, collider);
   edit teks YAML hanya kalau memang tidak ada jalan lain.
3. Jangan menulis file `.unity` yang sedang terbuka di Unity. Perubahan di memori akan menimpanya
   saat pemilik menyimpan, atau sebaliknya — salah satu sisi hilang. Tutup/simpan dulu, atau ubah
   lewat skrip editor.
4. Jaga jenis akhir baris file itu sendiri: `Level 1`–`Level 3` memakai LF, sebagian scene menu
   (mis. `Level Selection`) memakai CRLF. Periksa per file, jangan mengasumsikan.
5. Setelah selesai: tidak ada `fileID` ganda, semua `{fileID: ...}` yang dipakai masih ada, dan
   dibandingkan dengan backup hanya bagian yang dimaksud yang berbeda.

## Verifikasi tanpa klik UI

Agen tidak bisa mengklik Play atau Inspector, jadi verifikasi dilakukan begini:

- **Kompilasi**: `dotnet build Assembly-CSharp.csproj` dan/atau `Assembly-CSharp-Editor.csproj`
  (lihat *Lingkungan*).
- **Mengubah state scene / aset di editor**: tulis skrip sementara di `Assets/Scripts/Editor/`
  dengan `[InitializeOnLoad]` + `EditorApplication.delayCall`, kunci sekali-jalan lewat
  `SessionState` (kunci unik per percobaan, mis. `...Done3`, supaya tidak berulang), dan cetak
  hasilnya dengan `Debug.Log("[Rewild...] ...")`.
- Unity hanya mengompilasi saat jendelanya fokus. Aktifkan lewat Win32: `ShowWindow` 6 lalu 9, plus
  `AttachThreadInput` sebelum `SetForegroundWindow` (tanpa itu Windows menolak), lalu tunggu
  35–45 detik.
- Baca `%LOCALAPPDATA%\Unity\Editor\Editor.log` dan saring dengan prefix tadi. Screenshot jendela
  (`CopyFromScreen` lewat System.Drawing) hanya untuk cek visual.
- Untuk geometri, **hitung dulu secara offline** (Python + PIL): render tile dan collider dari PNG
  dan `.meta`, lalu bandingkan angkanya. Ini lebih kuat daripada menilai screenshot.
- **Hapus skrip sementara setelah selesai**, termasuk `.meta`-nya, dan kembalikan `.csproj`.
- Tidak ada test otomatis di proyek ini; verifikasi perilaku (gerak, ayun web, popup, benih) tetap
  harus play test manual oleh pemilik. Sampaikan itu, jangan mengklaim "sudah dites".

### Jembatan perintah (`RewildAgentBridge`)

`Assets/Scripts/Editor/RewildAgentBridge.cs` adalah jalur cepat ke editor yang sedang terbuka.
Perintah ditulis sebagai file teks, editor menjalankannya, hasilnya JSON — jadi tidak perlu menulis
skrip baru dan menunggu recompile setiap percobaan. Tidak ada port/jaringan dan tidak ada perintah
yang menyimpan scene.

- Aktif hanya kalau `Tools/agent/enabled.flag` ada; hapus file itu untuk mematikan jembatan.
- Menulis perintah: `Tools/agent/queue/<nama>.cmd` berisi teks biasa, mis. `state Tilemap_Ground`.
  Hasilnya ditulis ke `Tools/agent/out/<nama>.result` (JSON) dengan nama file yang sama.
- Perintah yang tersedia:
  - `ping` — versi Unity, scene aktif, `isPlaying`, `isCompiling`.
  - `state [namaTilemap]` — sel yang berisi tile (koordinat, sprite, matrix `e00…e11`), properti
    `TilemapCollider2D`, dan `path` milik `CompositeCollider2D`. Dibaca dari **memori editor**,
    jadi scene bertanda `*` pun terbaca apa adanya.
  - `run rebuild-collider [namaTilemap]` — membuat ulang `TilemapCollider2D` lalu
    `GenerateGeometry()` (lihat *Jebakan engine: collider tilemap*).
  - `run refresh-tilemap [namaTilemap]` — `Tilemap.RefreshAllTiles()`; perlu dipanggil setelah
    warna/isi aset `Tile` diubah di luar editor, karena tilemap menyimpan cache data tile per sel
    sehingga perubahan aset tidak langsung terlihat. Tidak mengubah isi sel dan tidak menandai
    scene kotor.
  - `run reimport Assets/...` — impor ulang satu aset secara paksa.
- `run` ditolak saat play mode atau saat editor sedang kompilasi/import.
- Tidak ada perintah `save`: menyimpan scene tetap di tangan pemilik.
- Setelah file bridge diubah, Unity masih memakai assembly lama sampai recompile selesai (memicu
  dengan memfokuskan jendela Unity). Cek `ping` dulu; kalau `isCompiling` masih `true`, tunggu.
- Screenshot jendela bisa diambil lewat `PrintWindow` + System.Drawing tanpa merebut fokus, jadi
  tidak perlu perintah `shot` di dalam jembatan.

## Jebakan yang berulang

- Duplikat scene membawa nilai komponen level sumber (level number, next level, nama scene di tombol
  Level Selection, daftar `Blocks`), bukan hanya tampilan level. Selalu lewati checklist di atas.
- Daftar `Blocks` di `LevelTilemapBuilder` tidak otomatis mengikuti tile yang dilukis; setelah
  melukis manual, data itu basi. Hanya berbahaya kalau tile di-rebuild.
- `CameraDragController → playerController` boleh kosong (hanya dipakai untuk memblokir drag saat
  membidik). `dragButton` berbeda antar level — klik kiri bisa bentrok dengan tombol tembak.
- Prefab Webi masih memakai nama class lama `SpiderPlatformer` untuk `PlayerController`.
- `SeedPlantingController` men-`disable` `PlayerController` selama animasi menanam.
- `LevelHudController` memaksa HUD aktif setiap scene level dimuat.
- Padding kaki sprite Webi diukur per frame (`spriteFootPaddings`), bukan satu angka.
- Saat Webi menempel di langit-langit, arah A/D mengikuti orientasi tubuhnya (terasa terbalik).
- Scene level memakai latar yang diperluas otomatis; zoom/geser kamera tetap dibatasi isi level.
- `Assets/Art/Tiles/Palettes/palette_platform.prefab` ber-`Grid` **cell size 1**, sedangkan level
  memakai cell size 2. Tile yang diambil dari palette itu bisa mendarat di grid yang berbeda —
  pastikan target tilemap-nya benar.
- Nama file dan objek memakai spasi (`Level 4.unity`, `Grid Miring`); selalu kutip di perintah shell.
- Mengedit `.cs` dari shell: perintah terminal dikirim dengan akhir baris CRLF sehingga heredoc
  (`<<'EOF'`) gagal dan skrip terpotong. Tulis skrip sementara di `%TEMP%` (mis. Python) yang
  menormalkan isi file ke `\n` saat mengolah lalu menuliskannya kembali sebagai CRLF + BOM seperti
  aslinya, jalankan, lalu hapus skripnya.
- Tidak ada asmdef untuk script game dan tidak ada folder test: semua script runtime berada di satu
  assembly `Assembly-CSharp`, dan satu-satunya verifikasi otomatis adalah kompilasi.
- Folder `.agents/skills/` berisi skill studio (GDD, ADR, sprint) yang tidak dipakai proyek ini.
  Abaikan; jangan dianggap instruksi aktif.
