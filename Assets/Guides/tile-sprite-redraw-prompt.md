# TILE REDRAW TASK — instruksi kerja untuk AI

> **Untuk manusia:** kirim **seluruh dokumen ini** ke AI. Bagian 1–10 adalah perintah kerja,
> Bagian 7 berisi prompt siap pakai (English), Bagian 9 adalah kontrak hasil.
> Tidak perlu menambahkan penjelasan lain — semua angka yang dibutuhkan sudah ada di sini.

---

## 0. TUGAS

Hasilkan **4 sprite tile** untuk game Unity 2D "Rewild": tanah dan rumput permukaan platform.
Keempatnya harus terbaca sebagai **satu keluarga material** (rumput tumbuh dari tanah yang sama),
bukan dua gambar yang ditempel.

Semua keputusan sudah ditetapkan di dokumen ini. **Jangan** menambah varian, gaya alternatif,
mockup level, karakter, atau aset lain. Kalau ada yang tidak bisa dipenuhi, laporkan — jangan
diam-diam mengubah spesifikasi.

---

## 1. DELIVERABLE

| # | Nama file (persis) | Ukuran | Isi |
|---|---|---|---|
| 1 | `tile_ground_fill.png` | 200 × 200 px | tanah + batu, seamless 4 sisi, opak penuh |
| 2 | `tile_grass_edge.png` | 200 × 200 px | pita rumput di sisi **atas**, sisanya transparan |
| 3 | `tile_grass_corner_outer.png` | 200 × 200 px | rumput di sisi **atas + kiri** (sudut cembung) |
| 4 | `tile_grass_corner_inner.png` | 200 × 200 px | rumput dengan takik di **pojok kiri-atas** (sudut cekung) |

Format: **PNG RGBA**, alpha transparan sungguhan, tanpa latar putih/kotak-kotak.
Satu file = satu tile. Jangan pernah menggabungkan beberapa tile dalam satu gambar.

---

## 2. ATURAN KERAS (semua harus lolos; ini yang akan diperiksa)

1. Kanvas **tepat 200 × 200 px**. Tidak boleh 199, 201, atau 1024.
2. Latar **transparan** (alpha 0). Hanya piksel gambar yang punya alpha.
3. `tile_ground_fill`: **seamless di keempat sisi** — selisih kolom x=0 vs x=199 dan baris
   y=0 vs y=199 **< 8/255** rata-rata.
4. `tile_grass_edge`: pita rumput **55–70 px** dari atas, dan **baris y=0 harus berisi piksel
   gambar** (rumput menyentuh tepi atas kanvas).
5. Di bawah pita rumput ada **rim gelap 8–12 px** (coklat `#4A2008`) yang memudar ke transparan.
6. Tinggi pita di **x=0 dan x=199 harus sama** (selisih ≤ 2 px) supaya tile bisa diulang.
7. **Tidak ada margin transparan** di luar siluet pada sisi yang menyentuh tepi kanvas.
8. Nama file **persis** seperti tabel Bagian 1 (huruf kecil, garis bawah).
9. Isi `tile_grass_edge` hanya material di **sisi atas** — tidak boleh ada rumput di tepi bawah
   atau di tengah kanvas.

---

## 3. SPESIFIKASI VISUAL PER FILE

**`tile_ground_fill`** — tanah padat dengan batu kecil tertanam. Basis `#803008`, batu
`#A85820`/`#B05828` menutup ±12% area, celah gelap `#702808`. Batu maksimal 40 px, disebar
merata, **tidak ada batu yang menyentuh tepi** (agar seamless). Tanpa rumput, tanpa akar,
tanpa gradasi ke satu sisi, tanpa bingkai.

**`tile_grass_edge`** — hanya tepi atas platform. 30% atas kanvas: helai rumput pendek rapat
(`#58A000`, `#68B020`, celah `#287000`), ujung helai tidak rata di baris 0, condong acak.
Di bawahnya: transisi akar `#4A2008` 8–12 px, batas bawahnya bergerigi halus (3–8 px) lalu
memudar ke transparan. 65% bawah kanvas **sepenuhnya transparan**.

**`tile_grass_corner_outer`** — pita rumput di sisi atas dan kiri, bertemu di sudut membulat
kiri-atas (radius 12–20 px). Tebal pita atas = tebal pita kiri. Area kanan-bawah transparan.
Kedua sisi luar menyentuh tepi kanvas.

**`tile_grass_corner_inner`** — takik di pojok kiri-atas (±40% lebar, ±30% tinggi kanvas),
berisi tanah `#803008` sampai tepi atas sehingga terbentuk dinding tanah vertikal di kiri takik.
Rumput mengikuti tepi atas takik dan berhenti tepat di dinding itu, dengan rim `#4A2008`.
Sisa kanvas transparan.

---

## 4. PALET (pakai ini; jangan bikin ramp baru)

| Peran | Hex |
|---|---|
| Daun terang | `#68B020` `#58A000` `#409000` |
| Daun gelap / celah | `#287000` `#205800` |
| Akar / rim | `#4A2008` `#3A1806` |
| Tanah gelap | `#702808` `#783008` `#803008` `#883808` |
| Batu (highlight) | `#A85820` `#B05828` |

Rumput dan tanah **berbagi satu ramp coklat** di area akar. Ujung akar memakai `#4A2008`,
bukan hijau — ini yang membuat keduanya menyatu.

---

## 5. BATASAN ROTASI (mempengaruhi cara menggambar)

Game memutar tile yang sama, tidak membuat 4 versi:

| Sisi platform | Rotasi tile |
|---|---|
| atas | 0° |
| kiri | 90° |
| kanan | 270° |
| bawah | 180° |

Konsekuensi yang wajib dipatuhi:

- Pita rumput harus menempel ke **tepi atas** kanvas, bukan mengambang di tengah.
- Ketebalan siluet di sisi kiri dan kanan **sama**, supaya setelah diputar tinggi pita tetap.
- Jangan menaruh detail asimetris yang jadi janggal setelah diputar (mis. satu batu besar
  di satu pojok saja).

---

## 6. PROSEDUR KERJA (jalankan berurutan)

1. Generate pada **1024 × 1024** (bukan langsung 200 — generator buruk di ukuran kecil).
2. Pakai latar **solid magenta `#FF00FF`** (jangan andalkan permintaan "transparent background";
   hampir semua generator tidak mengeluarkannya).
3. Hapus magenta dengan toleransi ±30, rapikan tepi (1 px) supaya tidak ada sisa ungu.
4. Downscale ke **200 × 200** (Lanczos untuk fill, Nearest untuk siluet rumput bila perlu).
5. **Jangan crop** isi: pastikan rumput tetap menyentuh tepi atas dan batu tidak menyentuh tepi.
6. Jalankan **Bagian 8 (self-check)**. Kalau ada yang gagal, perbaiki penyebabnya secara
   spesifik (mis. "pita 92 px → tipiskan ke 65 px"), jangan regenerate acak.
7. Serahkan hasil sesuai **Bagian 9**.

---

## 7. PROMPT (English — kirim per tile, satu gambar per generate)

### STYLE BLOCK — tempel di awal setiap prompt

```text
2D platformer tile sprite for a hand-painted game, square 1:1 canvas, orthographic side view,
no perspective. Style: soft painterly pixel-friendly illustration, clean readable silhouette,
subtle inner shading, no outline stroke.
Palette: grass greens #68B020 #58A000 #409000, shadow greens #287000 #205800,
soil browns #803008 #883808 #702808, rock highlight #A85820, root brown #4A2008.
Single modular tile only, centered, fills the full canvas, no margin, no drop shadow,
no text, no character, no props, no decorative objects, no vignette, no border frame.
Plain solid magenta background (#FF00FF).
```

### PROMPT 1 — `tile_ground_fill`

```text
A single square ground texture tile: packed soil with small embedded stones, brown dirt
(#803008 base) with slightly lighter rounded stones (#A85820) covering about 12% of the area,
soft dark crevices (#702808). Texture must be seamless on all four edges and read evenly when
repeated in a grid; no stone may touch the canvas edge. No grass, no roots, no top edge
highlight, no gradient toward one side, no border, fully opaque to the edges.
```

### PROMPT 2 — `tile_grass_edge`

```text
A single square tile that shows ONLY the top edge of a grassy platform. Upper 30% of the
canvas: a dense band of short grass blades in #58A000 / #68B020 with darker #287000 gaps,
irregular blade tips along the very top edge, blades touching the top border of the canvas.
Directly under the blades: an 8-12 px irregular transition band of dark root brown (#4A2008)
with a few thin roots hanging down, its bottom boundary jagged and soft, fading to full
transparency. Lower 65% of the canvas is fully transparent. Left and right edges must join
seamlessly with an identical tile: same band height, no blade cut off at the border.
No soil fill, no stones, no frame, no grass anywhere except the top edge.
```

### PROMPT 3 — `tile_grass_corner_outer`

```text
A single square tile for an outer convex corner of a grassy platform: grass band along the
TOP edge and along the LEFT edge, meeting in a rounded corner at the top-left (radius
12-20 px). Same grass palette (#68B020 #58A000 #409000), same dark root transition band
(#4A2008) under the blades along both edges. The inner area (bottom-right) is fully
transparent. Both outer edges touch the canvas border. Band thickness on the top and on the
left must be identical. No soil fill, no stones, no frame.
```

### PROMPT 4 — `tile_grass_corner_inner`

```text
A single square tile for a concave inner corner of a grassy platform: a notch cut into the
top-left of the tile, about 40% of the canvas width and 30% of the height, filled with soil
brown (#803008) up to the top border, so a vertical soil wall sits at the left side of the
notch. A grass band follows the horizontal top edge of that notch and stops exactly at the
wall, with the dark root transition (#4A2008) below it. The rest of the tile is transparent.
No frame, no stones larger than 30 px.
```

### NEGATIVE PROMPT — tempel di semua generate

```text
watermark, text, letters, signature, logo, frame, border, vignette, drop shadow, character,
animal, bug, spider, mushroom, flower, tree, clouds, background scenery, long platform strip,
multiple tiles in one image, tile grid, perspective, isometric, 3D render, photo texture,
blurry, noisy gradients, white background, checkerboard background, half-transparent grass,
grass on the bottom edge of the edge tile
```

---

## 8. SELF-CHECK (wajib dijalankan sebelum menyerahkan hasil)

Jalankan skrip ini pada tiap PNG; semua baris harus `OK`.

```python
from PIL import Image

def cek(path, tinggi_pita_min=55, tinggi_pita_max=70):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    assert (w, h) == (200, 200), f"{path}: ukuran {w}x{h} bukan 200x200"

    def kolom(a, b):
        d = sum(sum(abs(px[a, y][i] - px[b, y][i]) for i in range(3)) for y in range(h))
        return d / h / 3

    def baris(a, b):
        d = sum(sum(abs(px[x, a][i] - px[x, b][i]) for i in range(3)) for x in range(w))
        return d / w / 3

    # tinggi pita: piksel opak pertama dari atas pada x=0 dan x=199
    def pita(x):
        for y in range(h):
            if px[x, y][3] > 16:
                continue
            return y
        return h

    print(f"{path}: seam kiri-kanan {kolom(0, w-1):.1f}, atas-bawah {baris(0, h-1):.1f}, "
          f"pita x0={pita(0)} x199={pita(199)}, alpha y=0: {px[0, 0][3]}")
```

Ambang yang harus dipenuhi:

| Pemeriksaan | Ambang |
|---|---|
| Ukuran | tepat 200 × 200 |
| `tile_ground_fill` seam | selisih tepi < 8/255 di keempat sisi |
| Pita rumput (`edge`, kedua corner) | 55–70 px dari atas |
| Pita di x=0 vs x=199 | selisih ≤ 2 px |
| Baris y=0 | ada piksel dengan alpha > 16 (rumput menyentuh tepi atas) |
| Area transparan `edge` | ≥ 55% kanvas alpha 0 |

---

## 9. KONTRAK OUTPUT

- Serahkan **tepat 4 file PNG** dengan nama dari Bagian 1, masing-masing 200 × 200 px RGBA.
- Jika kamu punya akses ke proyek: tulis ke `Assets/Art/Tiles/Source/` dengan nama tersebut
  dan **jangan menyentuh file `.meta`**.
- Jika kamu hanya bisa menghasilkan gambar, serahkan 4 PNG (bukan sprite sheet) + 4 prompt
  final yang kamu pakai.
- Sertakan laporan singkat: tinggi pita rumput tiap file, selisih seam fill, cakupan alpha.
- Jangan mengirim mockup level, variasi tambahan, versi "alternatif", atau file ZIP berisi
  puluhan percobaan. Kalau ada yang gagal aturan Bagian 2, sebutkan yang mana.

---

## 10. LARANGAN

- Jangan membuat strip panjang selebar platform — tidak bisa dipakai ulang per sel.
- Jangan membuat `tile_grass_top`, `tile_grass_allsides`, atau varian kombinasi lain;
  kombinasi dibentuk dari susunan tile + rotasi.
- Jangan menaruh bayangan jatuh di dalam tile.
- Jangan mengubah nama file: game memuat sprite **berdasarkan nama**
  (`tile_<material>_<bagian>`).
- Jangan membuat tile lebih besar/kecil dari 200 × 200 untuk "kualitas lebih baik" —
  ukuran itu terikat ke Unity PPU 100 dan `tileSize` 2 unit.

---

## 11. FAKTA PROYEK (konteks yang mengikat keputusan di atas)

| Hal | Nilai |
|---|---|
| Engine | Unity 6000.2.9f1, URP 2D, Tilemap + Tilemap Extras |
| PPU aset dunia | 100 (100 px = 1 unit) |
| Ukuran tile di level | `tileSize = 2` unit → sprite 200 px @ PPU 100 = 1 sel |
| Tinggi Webi | 4,24 unit → pita rumput ±14% tinggi karakter |
| Generator tilemap | `Assets/Scripts/Core/LevelTilemapBuilder.cs` (memuat sprite dari nama file) |
| Lokasi sprite | `Assets/Art/Tiles/Source/` |
| Scene target | `Assets/Scenes/Levels/Level 1.unity` (rebuild otomatis saat dibuka) |
| Tile asset | `Assets/Art/Tiles/Tiles/` — fill `ColliderType: Grid`, edge/corner `None` |
| Panduan Unity terkait | `Assets/Guides/tilemap-panduan-manual.md` |
| Brief sprite umum | `Assets/Guides/platform-tilemap-sprite-brief.md` |
