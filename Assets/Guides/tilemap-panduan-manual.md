# Panduan Lengkap Tilemap & Melukis Platform Manual

Panduan kerja untuk **Rewild** — Unity 6000.2.9f1, paket `com.unity.2d.tilemap` 1.0.0 + `com.unity.2d.tilemap.extras` 5.0.2.

Dokumen ini ditulis supaya kamu bisa melukis platform sendiri dari nol, tanpa skrip, dan tetap nyambung dengan semua prefab yang sudah ada (Webi, Bug, Fly, Duri, Gate, Portal, Lever, Seed, TumpTanah).

---

## 0. Cara pakai panduan ini

- Pertama kali: ikuti **Bagian 4 → 9** berurutan (cek sprite → Tile asset → palette → hierarki → melukis).
- Sudah bisa melukis tapi collider/urutan gambar bermasalah: lompat ke **Bagian 10 (collider)** dan **Bagian 11 (sorting)**.
- Mau pakai skrip generator yang sudah terpasang: baca **Bagian 16** dulu supaya tidak saling menimpa.
- Ada yang aneh di layar: buka **Bagian 18 (troubleshooting)** — gejalanya dibuat spesifik.

---

## 1. Kondisi proyek saat ini

| Hal | Kondisi |
|---|---|
| Sprite tile | 10 PNG di [`Assets/Art/Tiles/Source/`](../Art/Tiles/Source/), 100×100 px, `Pixels Per Unit` 100, `Sprite Mode` Multiple (1 slice), slice **rect penuh 100×100**, pivot **Center**, border 0 |
| Tile asset | 10 file `.asset` di [`Assets/Art/Tiles/Tiles/`](../Art/Tiles/Tiles/); nama aset = nama file (`tile_grass_edge` dst.); collider: fill = **Grid**, edge/corner = **None** |
| Tile Palette | `Assets/Art/Tiles/Palettes/palette_platform.prefab` — sudah berisi 10 tile di `Layer1` (nama internal palette masih "New Tile Palette") |
| Paket tilemap | `2d.tilemap` 1.0.0, `2d.tilemap.extras` 5.0.2 (RuleTile, Prefab Brush) |
| Paket 2D lain | `2d.sprite`, `2d.spriteshape` 12.0.2, `2d.animation` 12.0.3, URP 17.2.0 (Renderer2D ada di [`Assets/Settings/Rendering/Renderer2D.asset`](../Settings/Rendering/Renderer2D.asset)) |
| Layer Unity | 0 `Default`, 1 `TransparentFX`, 2 `Ignore Raycast`, 3 **`Platform`**, 4 `Water`, 5 `UI` |
| Sorting Layer | Hanya `Default` (semua objek memakai sorting **order**) |
| Serialisasi | Force Text (scene & prefab berupa YAML) |
| Kamera Level 1 | Orthographic, **Size 5** (= 10 unit tinggi layar) |
| Batas level | `CameraDragController.levelBounds` (Rect, saat ini 0 = tanpa batas) |

Catatan penting: **semua sprite dunia memakai PPU 100**, jadi 100 piksel = 1 unit. Ini patokan yang membuat semua perhitungan di panduan ini konsisten.

Pengecualian: frame animasi Webi belum seragam (`char_webi_walk_01` 100, `walk_02` 90, `walk_03`–`walk_11` 120, `char_webi_shoot_*` 80), sehingga tinggi Webi di layar berubah antar frame. Untuk hitungan tile di panduan ini tidak berpengaruh; samakan ke 100 kalau animasinya terlihat melompat ukuran.

---

## 2. Konsep dasar (1 menit)

| Istilah | Fungsinya |
|---|---|
| **Grid** | Komponen induk yang menentukan ukuran sel (`Cell Size`) dan jarak antar sel. Semua Tilemap harus jadi anak Grid. |
| **Tilemap** | Komponen yang menyimpan data "sel mana berisi tile apa". Satu Grid boleh punya banyak Tilemap (bertumpuk). |
| **Tile asset** (`.asset`) | ScriptableObject kecil: menunjuk satu Sprite + Collider Type + matrix (rotasi/scale/flip). Ini yang dipilih di palette. |
| **Tile Palette** | Jendela untuk memilih tile lalu melukis ke Scene view. |
| **Tilemap Renderer** | Yang menggambar tile (Sorting Layer/Order, Mode: Chunk/Individual). |
| **Tilemap Collider 2D** | Membuat collider mengikuti sel yang ada tile-nya. Opsional. |
| **Composite Collider 2D** | Menggabungkan collider tile jadi satu bentuk utuh (dipakai platform). Butuh `Rigidbody2D` (Body Type: Static). |

Alur data: `Sprite → Tile asset → Tile Palette → dilukis ke Tilemap (di bawah Grid) → digambar Tilemap Renderer + collider`.

**Satu sel hanya bisa berisi satu tile per Tilemap.** Karena itu tepi rumput, tanah, dan dekorasi dipisah ke Tilemap berbeda yang bertumpuk — bukan digabung dalam satu tile.

---

## 3. Skala: berapa unit satu tile?

### 3.1 Ukuran nyata aset yang sudah ada

| Prefab | Sprite (px @PPU) | Ukuran dunia | Collider | Sorting order |
|---|---|---|---|---|
| `Webi.prefab` | 997×708 @100, scale 1,21 | **12,05 × 8,50 unit** | Capsule 7×4 (offset −0,2 / −0,3) | 20 |
| `Platform.prefab` | strip tanah + rumput | **80 × 6 unit** | Box 80×6 (+ anak 7×6) | tanah 0, rumput 1 |
| `Bug.prefab` | 480×315 @100 | 4,80 × 3,15 unit | Capsule 6×4,5 | 10 |
| `Fly.prefab` | 332×422 @100 | 3,32 × 4,22 unit | Capsule ⌀5,17 | 0 |
| `Duri.prefab` | 2172×470 @100 | 21,72 × 4,70 unit | Polygon | 0 |
| `Gate.prefab` | 405×1994 @100 | 4,05 × 19,94 unit | — | −1 |
| `BluePortal.prefab` | 536×1232 @100 | 5,36 × 12,32 unit | Box 10,24×15,35 | 0 |
| `GreenPortal.prefab` | 949×1444 @100 | 9,49 × 14,44 unit | Box 9,49×14,44 | 0 |
| `Lever.prefab` | 1004×898 @100 | 10,04 × 8,98 unit | Box 7×7 (trigger) | 0 |
| `Seed.prefab` | 1872×1951 @100 | 18,72 × 19,51 unit | — | 0 |
| `TumpTanah.prefab` | 1962×1412 @100, scale 1,024 | 19,62 × 14,12 unit | — | 5 |

Kesimpulan: dunia game ini **besar**. Webi setinggi 8,5 unit, gerbangnya 20 unit.

Sebagian PNG sudah diekspor ulang setelah tabel di atas dibuat (duri 2172×724, gate 724×2172, lever 1254×1254, portal 1024×1535, seed 2048×2048; dibagi 100 = unit). Kalau butuh ukuran pasti, hitung `dimensi PNG ÷ 100 × scale prefab`, jangan hanya memakai tabel ini.

### 3.2 Ukuran tile yang dipakai sekarang

Tile **4 × 4 unit** → Webi ≈ **2,1 tile** tinggi, Gate ≈ 5 tile, Duri ≈ 5,4 × 1,2 tile, Bug ≈ 1,2 × 0,8 tile.

Ini pas untuk platformer: karakter 2–3 tile, platform 1–2 tile tebal, celah lompat 3–6 tile.

Kalau kamu ingin tile lebih kecil (misal 2 unit) atau lebih besar (6 unit): boleh, tapi **satu level harus seragam**. Jangan campur 2 unit dan 4 unit dalam satu Grid.

### 3.3 Dua setup yang konsisten (pilih salah satu)

| | Setup A — sprite tetap 1 unit | Setup B — sprite dijadikan 4 unit (ramah melukis manual) |
|---|---|---|
| Pixels Per Unit sprite | 100 (tidak diubah) | **25** (diubah di Texture Importer) |
| Grid `Cell Size` | 4 / 4 / 1 | 4 / 4 / 1 |
| Tile asset `Transform` | perlu scale 4 (matrix) | biarkan identity |
| Dilakukan oleh | `LevelTilemapBuilder` otomatis | kamu sendiri (sekali saja) |
| Cocok untuk | tetap pakai generator | **melukis manual di palette** |

**Rekomendasi untuk kamu: Setup B.** Alasannya, tile yang di-drag dari palette langsung berukuran benar tanpa perlu mengatur matrix per tile. Caranya (sekali saja, 2 menit):

1. Pilih `tile_ground_fill.png`, `tile_grass_edge.png`, `tile_grass_corner_outer.png`, `tile_grass_corner_inner.png` (dan varian `Oil`/`Ice`) di Project window.
2. Di Inspector: **Pixels Per Unit → 25** → **Apply**.
3. Grid di scene: `Cell Size` = **4, 4, 1**.
4. Tile asset lama di `Assets/Art/Tiles/Tiles` akan tetap menunjuk sprite yang sama (ukuran otomatis ikut berubah). Kalau ada yang aneh, buat ulang tile-nya dengan drag ke palette.

Kalau PPU diubah, generator (`LevelTilemapBuilder`) **tidak perlu diubah** — skrip itu sekarang menghitung sendiri scale dari ukuran sprite, jadi `Tile Size = 4` tetap benar untuk PPU 100 maupun 25.

**Kondisi asset sekarang:** PPU sprite tile = 100, tetapi `Transform` di sepuluh Tile asset masih **identity**. Artinya kalau tile di-drag dari palette tanpa mengubah apa pun, tile tergambar **1 unit** di dalam sel **4 unit** (sisa 3 unit kosong). Pilih satu, jangan dua-duanya:
- ubah PPU sprite ke **25** (Setup B), atau
- isi `Transform` tiap Tile asset dengan scale **4, 4, 1** (Setup A).

> Efek samping PPU: hanya dipakai untuk tile, tidak ada prefab lain yang memakai 10 sprite ini.

---

## 4. Sprite tile yang tersedia dan artinya

Semua 100×100 px, material menempel di sisi tertentu:

| Sprite | Isi gambar | Dipakai untuk |
|---|---|---|
| `tile_ground_fill` | tanah cokelat penuh, seamless | badan platform (Tilemap dasar) |
| `tile_grass_edge` | rumput di **sisi atas**, sisa transparan | tepi lurus; rotasi 90° untuk kiri, 180° bawah, 270° kanan |
| `tile_grass_corner_outer` | rumput di **atas + kiri** | sudut cembung (tonjolan) |
| `tile_grass_corner_inner` | cekungan kecil di **pojok kiri-atas** | sudut cekung (pertemuan dua platform/tangga) |
| `tile_oil_*` | sama, material oli ungu gelap | platform licin |
| `tile_ice_*` | sama, material es biru-putih | platform licin/dingin |

Sudut: karena aslinya "atas + kiri", rotasi 0/90/180/270 menghasilkan keempat pojok. Ini sama dengan yang dilakukan generator:

| Pojok | Edge | Corner |
|---|---|---|
| atas | 0° | 0° |
| kiri | 90° | 90° (bawah-kiri) |
| kanan | 270° | 270° (atas-kanan) |
| bawah | 180° | 180° (bawah-kanan) |

Kalau kamu melukis manual, rotasi tile dilakukan di palette dengan **`[`** (berlawanan jarum jam) dan **`]`** (searah jarum jam) sebelum mengecat.

Duri **tidak** termasuk material platform — pakai prefab `Duri.prefab` (lihat Bagian 13).

---

## 5. Langkah 1 — pastikan importer sprite benar

Klik sprite → Inspector → **Texture Type: Sprite (2D and UI)**, lalu:

| Setelan | Nilai wajib |
|---|---|
| Sprite Mode | Multiple (1 slice) atau Single — asal nama slice konsisten |
| **Slice (Sprite rect)** | **100×100 penuh** (x 0, y 0) untuk semua tile |
| Pixels Per Unit | 100 (Setup A) / 25 (Setup B) |
| Mesh Type | Tight |
| Extrude Edges | 1 |
| Pivot | **Center** (= alignment 0) — kalau bukan center, tile akan bergeser setengah sel |
| Border | 0,0,0,0 |
| Wrap Mode | Clamp |
| Filter Mode | Point (no filter) untuk gaya pixel-art, Bilinear kalau ingin lebih halus |
| Compression | **None** atau High Quality (jangan Normal Quality, bisa bikin tile buram) |
| Generate Physics Shape | bebas (collider tile pakai Collider Type, bukan ini) |

Cek juga: sprite fill harus **tanpa margin transparan**; kalau ada margin, antar tile akan terlihat garis renggang.

Kenapa slice harus penuh: Tile merotasi sprite di sekitar **titik tengah sel**. Kalau sprite di-slice ketat (mis. `tile_grass_edge` 100×25 atau `tile_grass_corner_inner` 26×26), hasil rotasi 90°/180°/270° akan **melayang di tengah sel**, bukan menempel di sisi luar. Kalau menemukan sprite seperti itu: Sprite Editor → Slice → **Grid By Cell Size 100×100** (atau reset rect ke 0,0,100,100) → Apply.

---

## 6. Langkah 2 — membuat Tile asset

Ada tiga cara; semuanya menghasilkan file `.asset` yang sama:

1. **Dari palette (paling cepat):** buka Tile Palette, drag sprite dari Project window ke area palette. Unity otomatis membuat Tile asset di folder yang ditentukan saat membuat palette.
2. **Manual:** klik kanan di Project → **Create → 2D → Tiles → Tile**, lalu drag sprite ke field `Sprite`.
3. **Dari sprite:** pilih sprite → menu **Assets → Create → 2D → Tiles → Tile** (kalau tersedia di versimu).

Nilai penting di Tile asset:

| Field | Isi |
|---|---|
| `Sprite` | sprite tile |
| `Color` | putih (1,1,1,1) |
| `Collider Type` | lihat tabel di Bagian 10 |
| `Transform` | identity, kecuali memakai Setup A (scale 4) |

**Tips kuat:** `Collider Type` disimpan di Tile asset, bukan di tiap sel. Kalau kamu ubah Collider Type sebuah Tile asset, **semua sel yang memakainya ikut berubah**. Jadi jangan takut salah pilih saat melukis — perbaiki sekali di asset-nya.

---

## 7. Langkah 3 — membuat Tile Palette

1. **Window → 2D → Tile Palette**.
2. Klik **Create New Palette**, beri nama misal `palette_platform_grass`, simpan di `Assets/Art/Tiles/Palettes` (satu-satunya tempat palette proyek ini).
3. Pilih **Grid settings**: `Cell Size` = 4,4,1 (samakan dengan Grid di scene!) dan `Cell Layout: Rectangle`.
4. Drag semua Tile asset material Grass ke palette; susun urutannya (Fill di kiri, lalu Edge, lalu Corner).
5. Ulangi untuk palette `palette_platform_oil` dan `palette_platform_ice`.
6. Untuk hazard/dekorasi nanti: palette terpisah (`palette_hazard`, `palette_decor`) supaya tidak salah kuas.

Jangan mencampur tile berukuran berbeda dalam satu palette — hasil lukisan akan bergeser.

---

## 8. Langkah 4 — susun hierarki Tilemap

Struktur yang dipakai di level (semua anak dari satu Grid):

```text
Level Tilemap            (komponen Grid, Cell Size 4/4/1, Layer: Platform)
├── Tilemap_Ground       (tanah, collider, Sorting Order 0)
├── Tilemap_Surface_Grass(rumput, tanpa collider, Order 1)
├── Tilemap_Surface_Oil  (opsional)
├── Tilemap_Surface_Ice  (opsional)
├── Tilemap_Decor        (tanpa collider, Order 2)
└── Tilemap_Hazard       (collider terpisah / trigger, Order 2)
```

Aturan:

- **Satu Grid per level** — jangan bikin Grid baru di tengah level.
- Semua Tilemap: **Layer = Platform** (kecuali dekorasi/hazard yang ingin diabaikan Webi).
- Yang punya collider **hanya** `Tilemap_Ground` (dan `Hazard` kalau perlu).
- Nama diawali `Tilemap_` supaya gampang dikenali di Hierarchy.

---

## 9. Langkah 5 — melukis

Buka **Window → 2D → Tile Palette**, pilih palette, pilih tile, lalu cat di **Scene view**.

| Tool | Tombol | Fungsi |
|---|---|---|
| Select | `S` | memilih tile di palette / scene |
| Move | `M` | memindahkan tile yang sudah dipasang |
| **Paint** | `B` | kuas cat (utama) |
| **Box Fill** | `U` | isi area persegi sekaligus |
| **Pick** | `I` | ambil tile dari scene (eyedropper), lalu kembali ke Paint |
| Eraser | `D` | hapus tile |
| **Flood Fill** | `G` | isi area kosong/seragam |
| Rotate CCW / CW | `[` / `]` | putar tile aktif 90° |
| Flip X / Y | `Shift+[` / `Shift+]` | cermin tile |

Cara cepat melukis satu platform persegi:

1. Pilih `tile_ground_fill` → `U` (Box Fill) → drag area persegi di Scene view. Isi seluruh badan platform.
2. Pilih `tile_grass_edge` → `B` → cat sepanjang sisi atas. Untuk sisi kiri tekan `[`/`]` sampai rumput menghadap kiri, begitu juga kanan dan bawah.
3. Pojok: pilih `tile_grass_corner_outer` → putar sesuai pojok (`[`/`]`) → klik satu sel di tiap pojok.
4. Cek: tidak boleh ada garis terang/gelap di sambungan; kalau ada, lihat Bagian 18.

Cara memilih kuas lebih dari satu tile: di palette, **klik-drag** beberapa tile saat tool Paint/Box Fill aktif.

Latihan yang disarankan (10 menit): bikin 3 platform — datar 6×2, naik-tangga 2 tingkat, dan satu platform "pulau" melayang — lalu hapus. Setelah itu melukis level asli terasa cepat.

---

## 10. Langkah 6 — collider & layer

### 10.1 Collider Type per tile (di Tile asset)

| Jenis tile | Collider Type | Alasan |
|---|---|---|
| Fill / tanah | **Grid** | collider persegi penuh per sel, paling rapi untuk platform |
| Edge rumput/oli/es | **None** | hanya gambar; collider sudah diwakili sel fill |
| Dekorasi (bunga, batu kecil) | **None** | jangan mengganggu gerak |

`Sprite` hanya dipakai kalau kamu butuh bentuk collider mengikuti siluet (misal tangga miring) — lebih mahal dan gampang bocor.

Tile asset di `Assets/Art/Tiles/Tiles/` sudah disetel seperti tabel ini (`tile_ground_fill` = **Grid**, sembilan tile edge/corner = **None**), jadi lukisan dari palette langsung punya collider yang benar tanpa diubah lagi.

### 10.2 Menyiapkan collider platform

Di `Tilemap_Ground`:

1. Tambah **Tilemap Collider 2D**.
2. Tambah **Rigidbody 2D** → `Body Type: Static`.
3. Tambah **Composite Collider 2D** → `Geometry Type: Polygons`.
4. Di Tilemap Collider 2D centang **Used By Composite**.
5. **Layer GameObject = Platform**.
6. Kalau collider belum mengikuti lukisan terbaru: Composite Collider 2D → tombol **Regenerate Geometry** (atau set `Generation Type: Synchronous`).

Kenapa Composite: satu platform 20 sel jadi satu collider utuh, sehingga Webi tidak tersangkut di garis sambungan antar sel.

### 10.3 Layer dan siapa yang peduli

| Script | Field | Isi yang benar |
|---|---|---|
| `PlayerController` | `platformLayer` | `Platform` — dipakai untuk mendeteksi pijakan & target web |
| `EnemyController` | `platformLayer`, `obstacleLayer` | `Platform` (dan layer obstacle lain kalau ada) |
| `GateLeverController` | `platformLayer` (default `1<<3`) | `Platform` |

Kalau tilemap tidak ada di layer `Platform`, Webi akan **jatuh menembus** permukaan walau collider-nya ada. Sebaliknya, jangan menaruh dekorasi ber-collider di `Platform` — Webi akan menempel/mengayun ke bunga.

---

## 11. Langkah 7 — sorting (urutan gambar)

Semua objek memakai Sorting Layer `Default`, dibedakan lewat **Order in Layer**:

| Objek | Order | Sumber |
|---|---|---|
| Gate | −1 | `Gate.prefab` |
| Tanah tilemap | 0 | `Tilemap_Ground` |
| Rumput/oli/es tilemap | 1 | `Tilemap_Surface_*` |
| TumpTanah | 5 | `TumpTanah.prefab` |
| Bug | 10 | `Bug.prefab` |
| Webi | 20 | `Webi.prefab` |

Aturan praktis: tanah 0 → permukaan 1 → dekorasi 2 → properti 5 → musuh 10 → Webi 20. Selama tilemap ada di bawah 5, Webi selalu di depan.

Catatan `Tilemap Renderer`:

- `Mode: Chunk` (default) — efisien, tapi **sorting per tile tidak bisa beda-beda** di dalam satu chunk.
- `Mode: Individual` — tiap tile digambar sendiri; perlu kalau kamu ingin tile punya urutan berbeda (misal dekorasi yang harus menutupi Webi).
- `Detect Chunk Culling Bounds` → `Auto` cukup.

Kalau nanti butuh efek "Webi lewat di belakang rumput depan", siapkan Tilemap terpisah dengan order 25 (di atas Webi), bukan mengandalkan satu tilemap.

---

## 12. Langkah 8 — kamera, batas level, dan UI

- **Kamera Level 1 sekarang `Size = 5`** → hanya 10 unit tinggi, sedangkan Webi 8,5 unit. Set **Size ≈ 12** supaya karakter + platform nyaman dilihat.
- `CameraDragController.levelBounds` (Rect: x, y, width, height) — isi dengan area level supaya kamera berhenti di tepi. Contoh untuk tanah 16 tile × 4 unit mulai di x = −32: `x = -32, y = -12, width = 64, height = 24`.
- HUD/popup memakai Canvas **Screen Space – Overlay**, jadi selalu di atas dunia. Konsekuensinya: **Image `Background` yang full-screen akan menutupi tilemap** — jangan pasang background level sebagai UI Overlay kalau ada tilemap; pakai SpriteRenderer di belakang (order −10) atau matikan sementara.
- PPU 100 + kamera ortho menghasilkan 100 px per unit pada 1080p. Kalau ingin tajam sempurna, pertimbangkan paket Pixel Perfect Camera nanti (tidak wajib).

---

## 13. Integrasi dengan prefab yang sudah ada

| Prefab | Peran | Cara menaruh di level bertilemap |
|---|---|---|
| `Webi.prefab` | pemain | Taruh dengan **kaki di atas permukaan tanah**; Capsule collider 7×4 sudah agak ke bawah, jadi posisi Y = tinggi permukaan + ±2,4 unit. Set `platformLayer` = Platform. |
| `Bug.prefab` | musuh darat | Butuh permukaan rata minimal 2–3 tile; `platformLayer` wajib diisi, kalau tidak dia jatuh. |
| `Fly.prefab` | musuh terbang | Bebas posisi, tidak butuh pijakan. |
| `Duri.prefab` | hazard | **Tetap prefab**, jangan dijadikan tile. Lebar 21,7 unit (≈5,4 tile); tempel di atas permukaan. |
| `Gate.prefab` | gerbang | Tinggi 19,94 unit (≈5 tile). Beri ruang vertikal 5–6 tile di atas lantai. |
| `BluePortal` / `GreenPortal` | portal | Tinggi 12–14 unit (3–3,5 tile). Boleh menempel di dinding/tepi platform. |
| `Lever.prefab` | tuas gate | Collider trigger 7×7; letakkan di lantai, jangan tertimbun tile (taruh di Tilemap_Decor atau di luar tilemap). |
| `Seed.prefab` | benih | 18,7×19,5 unit; butuh tanah lapang. |
| `TumpTanah.prefab` | tanah tanam | Order 5, jadi tetap di depan tilemap (order 0–1). |

Semua sprite prefab ini **pivot-nya Center** (0,5; 0,5) — sama seperti sprite tile — jadi posisi Transform = **titik tengah** gambar, bukan pojok kiri-bawah. Menaruh prop di atas tanah berarti `Y = tinggi permukaan + setengah tinggi sprite`; `offset` collider bawaan prefab sudah dihitung untuk pivot center, jadi jangan diubah kecuali kamu memang mengganti pivot sprite-nya.

---

## 14. Pindah dari `Platform.prefab` ke tilemap

`Platform.prefab` adalah satu lempeng 80×6 unit dengan 1 BoxCollider + 4 visual rumput. Dengan tile 4 unit, padanannya = **20 tile × 1,5 tile**; agak canggung karena 6 unit bukan kelipatan 4.

Langkah migrasi yang aman:

1. Cat platform versi tilemap di sebelah platform prefab lama.
2. Bandingkan tinggi permukaan (Y atas). Sesuaikan tinggi tilemap supaya Webi bisa lompat/ayun dengan jarak yang sama.
3. Setelah yakin, nonaktifkan (jangan hapus) platform prefab lama.
4. Uji Play Mode: pijakan, ayunan web, dan `GateLeverController` (dia meraycast dari gate ke `Platform`).
5. Kalau sudah stabil, hapus prefab lama.

Kalau kamu lebih suka tinggi platform 6 unit (sama seperti prefab lama), pakai tile **2 unit** dan buat semua platform kelipatan 2 — konsisten itu yang penting, bukan angka 4-nya.

---

## 15. Otomatisasi lanjutan (kalau nanti butuh)

### 15.1 RuleTile (rumput otomatis)

Paket `2d.tilemap.extras` sudah terpasang. RuleTile = tile pintar yang memilih sprite sendiri berdasarkan 8 tetangganya, jadi kamu tidak perlu menempel edge/corner manual.

1. Klik kanan → **Create → 2D → Tiles → Rule Tile**.
2. `Output: Single` (atau `Random` kalau punya varian).
3. Isi daftar aturan:
   - `This` = fill, tanpa syarat → default (tengah platform).
   - `This` + atas kosong → edge rotasi 0.
   - `This` + atas kosong + kiri kosong → corner outer 0.
   - dan seterusnya untuk 4 arah + 4 pojok (total ±12 aturan per material).
4. Drag RuleTile ke palette seperti tile biasa.

Keuntungan: mengubah bentuk platform tidak perlu memperbaiki sudut manual. Kekurangan: menyiapkan aturannya ±30 menit per material, dan rotasi tidak otomatis (aturan harus ditulis per arah).

### 15.2 Prefab Brush (untuk Gate, Lever, Duri)

`2d.tilemap.extras` juga punya **Prefab Brush**: satu tile yang men-spawn prefab saat dilukis. Cocok kalau level punya banyak gate/tuas dan kamu ingin menaruhnya lewat grid, bukan drag manual.

### 15.3 Animated Tile

Untuk air/oli yang bergerak: `Create → 2D → Tiles → Animated Tile`, isi daftar sprite + kecepatan. Collider Type tetap diatur di sana.

---

## 16. Hubungannya dengan `LevelTilemapBuilder` (skrip generator)

[`LevelTilemapBuilder.cs`](../Scripts/Core/LevelTilemapBuilder.cs) terpasang di objek `Level Tilemap` di scene Level 1. Skrip ini **menggambar seluruh level dari daftar blok** di Inspector (tanah, tangga, platform melayang, material grass/oil/ice) dan membuat Grid + collider otomatis.

Yang perlu kamu tahu:

| Hal | Perilaku |
|---|---|
| `Build On Enable` | Kalau dicentang, level digambar ulang saat scene dibuka **hanya jika** tilemap masih kosong atau `Tile Size` berubah. |
| Menimpa lukisan | **Ya**, kalau build terjadi. Sebelum mulai melukis manual: **matikan `Build On Enable`**. |
| Context menu komponen | Klik kanan judul komponen → **Bangun Ulang Tilemap** / **Hapus Semua Tile**. |
| `Tile Size` | Ukuran tile dalam unit (sekarang 4). Mengubahnya memicu rebuild. |
| `Surfaces` / `Blocks` | Kalau kosong, skrip mengisi default sendiri (material Grass/Oil/Ice + 6 blok contoh). |

**Alur yang disarankan:** matikan `Build On Enable` → hapus semua tile (`Hapus Semua Tile`) → melukis sendiri. Skrip tetap berguna sebagai referensi bentuk/ukuran dan bisa dihidupkan lagi kapan saja.

---

## 17. Checklist alur kerja

Sebelum mulai:

- [ ] PPU sprite tile sesuai setup yang dipilih (100 atau 25) dan seragam.
- [ ] Grid `Cell Size` = ukuran tile (4,4,1).
- [ ] Satu Grid per level, semua Tilemap jadi anaknya.
- [ ] `Build On Enable` dimatikan kalau akan melukis manual.

Saat melukis:

- [ ] Tanah di `Tilemap_Ground`, permukaan di `Tilemap_Surface_*`.
- [ ] Edge/corner memakai rotasi yang benar (rumput menghadap keluar).
- [ ] Tidak ada celah 1 sel di tempat yang harusnya padat (cek dengan `U` = Box Fill).
- [ ] Collider Type: fill = Grid, edge/dekorasi = None.

Sebelum simpan/selesai:

- [ ] Layer `Platform` untuk Tilemap yang dipijak.
- [ ] Composite Collider → Regenerate Geometry.
- [ ] Sorting order: tanah 0, permukaan 1, dekorasi 2.
- [ ] Test Play Mode: Webi bisa berdiri, lompat, dan menembak web ke permukaan baru.
- [ ] `levelBounds` kamera diperbarui sesuai luas level.

---

## 18. Troubleshooting

| Gejala | Penyebab paling mungkin | Solusi |
|---|---|---|
| Melukis tapi tidak terlihat apa pun | Ada Image UI full-screen (mis. `Background`) di Canvas Overlay | Matikan Image itu atau pindahkan background ke SpriteRenderer order −10 |
| Tile bergeser setengah sel | Pivot sprite bukan Center | Texture Importer → Pivot: Center, Apply |
| Tepi rumput/oli/es mengambang di tengah sel, tidak menempel di sisi | Sprite di-slice ketat (rect ≠ 100×100), jadi rotasi berputar di tengah sel | Sprite Editor → rect tile 0,0 **100×100** (penuh), pivot Center, Apply |
| Tile tetap meleset walau Alignment sudah Center | Nilai `pivot` di dalam slice bukan 0,5;0,5 | Set pivot sprite ke 0.5 / 0.5 lalu Apply |
| Ada garis tipis di antara tile | Sprite punya margin transparan, atau PPU/scale tidak bulat | Crop sprite ke 100×100 penuh, PPU bulat, jangan skala non-integer |
| Tile tampak "kotak warna" / gepeng | `Collider Type: Sprite` + sprite tanpa physics shape, atau tile matrix kena scale dua kali | Set Collider Type `Grid`, pastikan Grid Cell Size = ukuran tile |
| Ada jarak antar tile | Grid `Cell Size` ≠ ukuran tile di dunia | Samakan: PPU 100 → 1 unit → Cell Size 1; PPU 25 → 4 unit → Cell Size 4 |
| Webi jatuh menembus platform | Tilemap bukan layer `Platform`, atau `PlayerController.platformLayer` kosong | Set layer GameObject + field LayerMask |
| Webi tersangkut di sambungan | Tilemap Collider tanpa Composite | Tambah Composite Collider 2D + Rigidbody Static + Used By Composite |
| Collider tidak mengikuti tile baru | Composite `Generation Type: Manual` | Klik **Regenerate Geometry** atau ubah ke Synchronous |
| Tile menutupi Webi | Sorting Order tilemap > order Webi (20) | Turunkan order tilemap (0/1) |
| Webi tidak bisa mengayun ke platform | `platformLayer` tidak termasuk layer tilemap | Samakan LayerMask di Inspector Webi |
| Palette kosong saat dibuka | Belum ada tile yang di-drag / palette rusak | Drag ulang Tile asset ke palette |
| Menambah tile di palette mengubah level lama | Tile asset yang sama dipakai di level | Buat **Tile asset baru** (duplikat) untuk varian berbeda |
| Lua level terlihat terpotong saat kamera digeser | `levelBounds` masih 0 | Isi Rect sesuai area level |
| Lampu/warna tile beda dari sprite aslinya | Sprite di-set ke format compression Normal, atau material 2D Lit | Compression: None/High Quality; pakai material Sprite-Lit default |

---

## 19. Kebutuhan mendatang (yang belum ada di proyek)

Supaya kamu tahu apa yang masih perlu disiapkan saat level makin besar:

1. **Tile dekorasi** — belum ada sprite dekorasi (bunga, batu kecil, akar). Dibutuhkan kalau ingin permukaan tidak monoton. Buat palette `Decor` terpisah, Collider Type `None`.
2. **Tile hazard** — duri masih prefab. Kalau ingin deretan duri panjang lewat tilemap, siapkan `tile_thorn_straight / End_Left / End_Right` (lihat [`platform-tilemap-sprite-brief.md`](platform-tilemap-sprite-brief.md)) + `Tilemap_Hazard` dengan collider trigger.
3. **Sorting Layer khusus** — sekarang hanya `Default`. Kalau nanti butuh "belakang karakter / karakter / depan karakter" yang lebih rapi, tambahkan Sorting Layer di Project Settings → Tags and Layers, lalu ganti semua order jadi berbasis layer.
4. **RuleTile untuk Grass/Oil/Ice** — menghemat waktu saat bentuk platform sering berubah.
5. **Prefab Brush untuk Gate/Lever/Duri** — menaruh properti lewat grid, lebih rapi daripada drag manual.
6. **levelBounds per level** — belum ada satu level pun yang mengisinya; tanpa ini kamera bisa keluar area.
7. **Layer untuk musuh/interactable** — `Bug`/`Fly` masih di `Default`. Kalau nanti butuh collision matrix (misal Webi tidak bertabrakan dengan Fly), tambahkan layer `Enemy`.
8. **Konsistensi PPU** — kalau nanti ada sprite baru, wajib PPU 100 (atau 25 untuk tile) agar skala tidak melompat.

---

## 20. Referensi cepat

**Path penting**

| Isi | Path |
|---|---|
| Sprite tile | `Assets/Art/Tiles/Source/tile_*.png` |
| Tile asset / palette | `Assets/Art/Tiles/Tiles/` dan `Assets/Art/Tiles/Palettes/` |
| Skrip generator | `Assets/Scripts/Core/LevelTilemapBuilder.cs` |
| Prefab | `Assets/Prefabs/{Characters,Enemies,Environment,Props,UI}/*.prefab` |
| Level 1 | `Assets/Scenes/Levels/Level 1.unity` |
| Renderer 2D (URP) | `Assets/Settings/Rendering/Renderer2D.asset` |
| Panduan sprite tile | `Assets/Guides/platform-tilemap-sprite-brief.md` |

**Angka kunci**

| Hal | Nilai |
|---|---|
| PPU semua sprite dunia | 100 (kecuali frame Webi 80/90/120 — perlu diseragamkan) |
| Ukuran tile sekarang | 4 × 4 unit (Cell Size 4) |
| Tinggi Webi | 8,5 unit (≈2,1 tile) |
| Kamera Level 1 | Size 5 → sebaiknya 12 |
| Layer pijakan | 3 = `Platform` |
| Sorting order | tanah 0, rumput 1, dekorasi 2, Webi 20 |
