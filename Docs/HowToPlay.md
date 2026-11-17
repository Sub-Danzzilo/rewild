# How to Play — Rencana Implementasi

Dokumen kerja untuk menu **How to Play** di Rewild. Isinya keputusan desain, angka tata letak yang
siap dipakai, isi tiap slide, dan urutan pengerjaan. Scene-nya sudah dibuat kosong di
`Assets/Scenes/Menus/How To Play.unity`; isinya belum ada apa-apa.

Ditulis dengan bahasa kerja Indonesia; **teks yang tampil di game berbahasa Inggris** supaya
konsisten dengan menu yang sudah ada ("Play", "Settings", "Moves", "You Died!").

---

## 1. Ringkasan

- Menu How to Play adalah **scene terpisah**, bukan popup.
- Bentuknya **slide**: satu topik per halaman, pindah dengan tombol **PREV / NEXT**, ada indikator
  halaman (bintang), dan tombol **Back** kembali ke Home Screen.
- 6 slide, teks Inggris, satu ilustrasi per slide.
- Navigasi juga bisa lewat keyboard (`←`/`→`, `Esc` untuk kembali) dan memakai **unscaled time**
  supaya tetap jalan walau `Time.timeScale = 0`.

---

## 2. Penempatan tombol di Home Screen

### 2.1 Kondisi sekarang (`Assets/Scenes/Menus/Home Screen.unity`, CRLF)

Canvas `UI` memakai **Canvas Scaler: Scale With Screen Size, Reference Resolution 1920×1080,
Match 0.5**. Semua tombol berukuran **600×450** dengan sprite `board_button.png` (1227×395,
Preserve Aspect), jadi gambar plank yang terlihat tingginya hanya **±193 px** di tengah kotak 450.

| Objek | Anchor | Pivot | Anched Position | Size Delta |
|---|---|---|---|---|
| `Logo` | (0.5, 1) | (0.5, 1) | (0, −25) | 700×300 |
| `PlayButton` | (0.5, 0.5) | (0.5, 0.5) | (0, 25) | 600×450 |
| `SettingsButton` | (0.5, 0.5) | (0.5, 0.5) | (0, −125) | 600×450 |
| `ExitButton` | (0.5, 0.5) | (0.5, 0.5) | (0, −275) | 600×450 |

Isi tiap tombol: `Icon` 125×125 di (−100, 5) dan label TMP dengan `font_lilitaone_regular_sdf`
(bold): `PlayText` fontSize 65 di (30, 5), `SettingsText` fontSize 50 di (50, 5),
`ExitText` fontSize 70 di (30, 0).

`SettingsButton` membuka `SettingsPopup` (Image hitam alpha 0.882 + `UIPopupSlide`), bukan scene.

### 2.2 Opsi A — tambah tombol keempat (rekomendasi)

Urutan terbaca: **Play → How to Play → Settings → Exit**. Keempat tombol tetap 600×450 dan jarak
vertikalnya tetap 150. Play naik 35 px, Settings & Exit turun 115 px, dan How to Play mengisi celah
di antaranya:

| Objek | Anched Position baru | Catatan |
|---|---|---|
| `Logo` | (0, −25) | tidak berubah |
| `PlayButton` | (0, 60) | naik 35 |
| `HowToPlayButton` | (0, −90) | **baru** |
| `SettingsButton` | (0, −240) | turun 115 |
| `ExitButton` | (0, −390) | turun 115 |

Pemeriksaan ruang (layar tinggi 1080, setengah 540):

- plank paling atas: 60 + 96 = **156** (logo berakhir di 215 → sisa 59 px),
- plank paling bawah: −390 − 96 = **−486** (masih di dalam −540).

Cara membuat: **duplikat `SettingsButton`** (Ctrl+D) di dalam `UI`, lalu:

1. `m_Name` → `HowToPlayButton`.
2. Anched Position → `(0, -90)`.
3. Hapus anak `Icon` (atau ganti dengan ikon baru, lihat §7), ubah `SettingsText` →
   `HowToPlayText`, anchedPosition `(0, 5)`, teks **`How to Play`**, fontSize **50**.
4. `Button → On Click ()`: **+** → seret objek `UI` (yang punya `GameSceneController`) →
   `GameSceneController → LoadHowToPlay()`.
5. Geser `PlayButton`, `SettingsButton`, `ExitButton` sesuai tabel di atas.

### 2.3 Opsi B — tanpa menggeser objek lama

Kalau posisi tiga tombol lama mau dibiarkan persis (aturan proyek: jangan geser objek tanpa diminta),
How to Play bisa jadi tombol ikon **260×260** di kiri atas, meniru `BackButton` di Level Selection:

- Anchor (0, 1), pivot (0.5, 0.5), Anched Position **(170, −150)**, size 260×260,
  anak `Icon` 100×100 di (0, 5) memakai `icon_help.png` (aset baru, §7).

Kekurangannya: kurang terlihat dibanding tombol teks. Pilih salah satu sebelum mulai.

### 2.4 Opsi C — mengganti Settings (ide awal)

Hanya kalau pengaturan audio dipindah dulu ke popup pause di level. Tanpa itu, pemain kehilangan
Music/SFX. Tidak direkomendasikan.

---

## 3. Scene `How To Play`

- Path: `Assets/Scenes/Menus/How To Play.unity` (**sudah dibuat, masih kosong**).
- Nama scene: **`How To Play`** (pakai spasi, seperti `Level 1 Intro`).
- Akhir baris file: **LF** (sama seperti `Level Selection.unity`); jangan campur CRLF.
- Scenes In Build: tambahkan **setelah `Level Selection`**, sebelum `Level 1 Intro`. Urutan build
  scene menu tidak memengaruhi tombol restart, jadi posisi ini aman.

### 3.1 Hierarki lengkap

```
UI                      Canvas + Canvas Scaler + GraphicRaycaster + GameSceneController + HowToPlayController
├─ Background           Image (full screen) + BackgroundSlideshow
├─ Title                TMP "HOW TO PLAY"
├─ BackButton           Image board_button + Button + UIButtonInteractive
│  └─ Icon              Image icon_back, 100×100
├─ SlidesRoot           RectTransform (wadah slide)
│  ├─ Slide1 … Slide6   masing-masing CanvasGroup + Image Illustration + Panel
│  │  ├─ Illustration   Image 520×520 (Preserve Aspect)
│  │  └─ Panel          Image board_panel 780×600 (Preserve Aspect)
│  │     ├─ SlideTitle  TMP
│  │     └─ SlideBody   TMP
├─ PrevButton           Image board_button + Button + UIButtonInteractive
│  └─ PrevText          TMP "PREV"
├─ NextButton           Image board_button + Button + UIButtonInteractive
│  └─ NextText          TMP "NEXT"
└─ DotsRoot
   └─ Dot1 … Dot6       Image icon_star / icon_star_empty, 24×24
```

### 3.2 Angka RectTransform (persis)

Semua anchor/pivot default `(0.5, 0.5)` kecuali yang ditulis lain.

| Objek | Anchor | Pivot | Anched Position | Size Delta |
|---|---|---|---|---|
| `UI` (Canvas) | — | — | — | Canvas: Screen Space – Overlay; Scaler: Scale With Screen Size 1920×1080, Match 0.5 |
| `Background` | (0,0)–(1,1) | (0.5,0.5) | (0, 0) | offsetMin/Max = 0 |
| `Title` | (0.5, 1) | (0.5, 1) | (0, −30) | 900×120 |
| `BackButton` | (0, 1) | (0.5, 0.5) | (170, −150) | 260×260 |
| `BackButton/Icon` | (0.5,0.5) | (0.5,0.5) | (0, 5) | 100×100 |
| `SlidesRoot` | (0.5,0.5) | (0.5,0.5) | (0, 60) | 1500×620 |
| `SlideN` | (0,0)–(1,1) | (0.5,0.5) | (0, 0) | offsetMin/Max = 0 |
| `SlideN/Illustration` | (0.5,0.5) | (0.5,0.5) | (−380, 10) | 520×520 |
| `SlideN/Panel` | (0.5,0.5) | (0.5,0.5) | (330, 0) | 780×600 |
| `SlideN/Panel/SlideTitle` | (0.5, 1) | (0.5, 1) | (0, −90) | 600×120 |
| `SlideN/Panel/SlideBody` | (0.5, 1) | (0.5, 1) | (0, −230) | 600×320 |
| `PrevButton` | (0, 0) | (0.5,0.5) | (250, 120) | 300×260 |
| `PrevButton/PrevText` | (0.5,0.5) | (0.5,0.5) | (0, 5) | 220×120 |
| `NextButton` | (1, 0) | (0.5,0.5) | (−250, 120) | 300×260 |
| `NextButton/NextText` | (0.5,0.5) | (0.5,0.5) | (0, 5) | 220×120 |
| `DotsRoot` | (0.5, 0) | (0.5,0.5) | (0, 120) | 400×40 |
| `Dot1` … `Dot6` | (0.5,0.5) | (0.5,0.5) | x = −85, −51, −17, 17, 51, 85; y = 0 | 24×24 |

Pemeriksaan ruang (semua angka dalam koordinat Canvas 1920×1080, pusat = 0,0):

- `Title` (anchor atas, pivot atas) di (0, −30) tinggi 120 → menempati y **510 … 390**.
- `SlidesRoot` di (0, 60), tinggi 620 → menempati y **370 … −250**; lebarnya 1500 → x **−750 … 750**.
  Jadi ada jarak 20 px antara judul dan area slide.
- Panel 780×600 dengan sprite `board_panel` (1283×1226, rasio 1,046) + Preserve Aspect → gambar
  terpakai **628×600** (dibatasi tinggi), masih di dalam tinggi slide (620).
- Panel di x = +330 → gambar menempati x **16 … 644**, batas kanan `SlidesRoot` 750 → aman.
- Illustration 520×520 di (−380, 10) → x **−640 … −120**, batas kiri −750 → aman; tidak menabrak
  panel (mulai di 16).
- Di dalam panel (tinggi 600, pusat 0): `SlideTitle` di (0, −90) tinggi 120 → y **210 … 90**;
  `SlideBody` di (0, −230) tinggi 320 → y **70 … −250**. Sisa margin bawah 50 px.
- Tombol PREV/NEXT 300×260 + `board_button` (rasio 3,1) → plank terlihat 300×96, y = 120 dari bawah
  → pusat y **−420**, plank **−372 … −468**. Slides berakhir di −250 → jarak 122 px; sisa ke tepi
  bawah layar (−540) 72 px.
- Tombol kiri di x **−550** (plank −700 … −400), tombol kanan di x **550** (plank 400 … 700).
- Dots di (0, 120) → pusat y −420, lebar total 6×24 + 5×10 = **194 px** (x −97 … 97), tidak
  menyentuh kedua tombol.

### 3.3 Komponen per objek

| Objek | Komponen & nilai |
|---|---|
| `UI` | `Canvas` (Render Mode: Screen Space – Overlay, Pixel Perfect off); `CanvasScaler` (Scale With Screen Size, 1920×1080, Match 0.5); `GraphicRaycaster`; `GameSceneController` (untuk Back); `HowToPlayController` (§4) |
| `Background` | `Image` (sprite latar menu, warna putih, Raycast Target **off** — supaya tidak menghalangi klik); `BackgroundSlideshow` (sama seperti Home Screen) |
| `Title` | `TextMeshProUGUI`: font `Assets/Fonts/font_lilitaone_regular_sdf.asset`, Bold, fontSize **100**, Alignment Center/Middle, warna putih, teks `HOW TO PLAY` |
| `BackButton` | `Image` (sprite `board_button`, Simple, Preserve Aspect, warna putih); `Button` (Transition: **None**); `UIButtonInteractive` (nilai default: hover scale 1.1, hover color (1,1,0.8,1)) |
| `BackButton/Icon` | `Image` (`Assets/Art/UI/Icons/Menu/icon_back.png`, Preserve Aspect, Raycast Target off) |
| `SlideN` | `CanvasGroup` (alpha 1 untuk Slide1, 0 untuk Slide2–6); `RectTransform` stretch |
| `SlideN/Illustration` | `Image` (sprite `slide_N`, Preserve Aspect, Raycast Target off) |
| `SlideN/Panel` | `Image` (sprite `board_panel`, Simple, Preserve Aspect, Raycast Target off) |
| `SlideN/.../SlideTitle` | TMP, Bold, fontSize **64**, Alignment Center, warna putih, line spacing 0 |
| `SlideN/.../SlideBody` | TMP, Bold, fontSize **40**, Alignment Center (Middle), warna putih alpha **0.92**, line spacing **12**, enable word wrap |
| `PrevButton` / `NextButton` | sama seperti `BackButton`, ditambah TMP label fontSize **44** |
| `DotN` | `Image` (`icon_star` untuk halaman aktif, `icon_star_empty` untuk yang lain), 24×24, Raycast Target off |

Catatan: `UIButtonInteractive` **mematikan** transition bawaan Button sendiri saat Awake dan mengatur
`raycastPadding` sesuai letterbox sprite, jadi area klik mengikuti bentuk plank, bukan kotak 450 px.
Tidak perlu menyentuh nilainya.

---

## 4. Script `Assets/Scripts/UI/HowToPlayController.cs`

Konvensi file: `[Header]` Inggris, `[Tooltip]` Indonesia, komentar Indonesia, `#region` seperti file
UI lain. Tidak perlu asmdef (semua runtime ada di `Assembly-CSharp`).

### 4.1 Field Inspector

```csharp
[Header("Slides")]
[Tooltip("RectTransform tiap slide, urut dari slide 1. Objek slide sebaiknya punya CanvasGroup.")]
[SerializeField] private RectTransform[] slides;
[Tooltip("CanvasGroup tiap slide untuk animasi fade. Kosongkan untuk mengambil CanvasGroup di tiap slide.")]
[SerializeField] private CanvasGroup[] slideGroups;

[Header("Navigation")]
[Tooltip("Tombol pindah ke slide sebelumnya.")]
[SerializeField] private Button prevButton;
[Tooltip("Tombol pindah ke slide berikutnya.")]
[SerializeField] private Button nextButton;
[Tooltip("Gambar indikator halaman (bintang), urut sesuai slide.")]
[SerializeField] private Image[] dots;
[Tooltip("Warna bintang halaman aktif.")]
[SerializeField] private Color activeDotColor = Color.white;
[Tooltip("Warna bintang halaman lain.")]
[SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.35f);
[Tooltip("Jika aktif, NEXT di slide terakhir kembali ke slide pertama dan PREV di slide pertama ke slide terakhir. Jika mati, tombolnya dinonaktifkan di ujung.")]
[SerializeField] private bool wrapAround = false;

[Header("Animation")]
[Tooltip("Lama animasi geser antar slide (detik, waktu nyata).")]
[Min(0f)] [SerializeField] private float slideDuration = 0.25f;
[Tooltip("Jarak geser slide keluar/masuk (satuan UI). Harus cukup besar agar slide keluar dari layar.")]
[Min(0f)] [SerializeField] private float slideDistance = 1600f;

[Header("Keyboard")]
[Tooltip("Aktifkan tombol panah kiri/kanan untuk pindah slide dan Esc untuk kembali.")]
[SerializeField] private bool enableKeyboard = true;

[Header("Scene")]
[Tooltip("Scene yang dibuka tombol Back. Harus terdaftar di Scenes In Build.")]
[SerializeField] private string homeScene = "Home Screen";
```

### 4.2 Perilaku

- `Awake`/`OnEnable`: tampilkan **slide pertama tanpa animasi** (slide lain `alpha = 0`,
  `blocksRaycasts = false`, dan `gameObject.SetActive(false)`).
- `Next()` / `Prev()`: abaikan kalau animasi masih berjalan. Hitung indeks tujuan
  (`wrapAround` atau dijepit di ujung), lalu jalankan coroutine animasi:
  - slide keluar bergeser ke kiri/kanan sejauh `slideDistance` dan alpha 1→0,
  - slide masuk mulai dari sisi berlawanan (`±slideDistance`) lalu bergeser ke 0 dan alpha 0→1,
  - easing `1 − (1−t)³` untuk masuk, `t³` untuk keluar (sama seperti `UIPopupSlide`),
  - waktu memakai `Time.unscaledDeltaTime`, jadi tetap jalan saat `Time.timeScale = 0`.
  - Setelah selesai: slide lama `SetActive(false)`, `anchoredPosition` direset ke 0.
- `UpdateDots()`: bintang aktif pakai `activeDotColor` + sprite `icon_star`, yang lain
  `inactiveDotColor` + `icon_star_empty`.
- `UpdateButtons()`: `prevButton.interactable = wrapAround || index > 0`,
  `nextButton.interactable = wrapAround || index < slides.Length − 1`.
- Keyboard (di `Update`, hanya kalau `enableKeyboard` dan tidak sedang animasi):
  `←` atau `A` → `Prev()`, `→` atau `D` → `Next()`, `Esc` → `Back()`.
  Pakai `Keyboard.current` (Input System baru).
- `Back()`: `Time.timeScale = 1f;` lalu `SceneManager.LoadScene(homeScene)`; validasi dulu dengan
  `Application.CanStreamedLevelBeLoaded(homeScene)` dan `Debug.LogError` kalau belum terdaftar
  (pola yang sama dengan `LevelHudController.BackToLevelSelection`).
- Tidak ada suara khusus: SFX klik/hover sudah datang dari `UIButtonInteractive`.

### 4.3 Wiring On Click

| Tombol | On Click () |
|---|---|
| `PrevButton` | `HowToPlayController.Prev()` |
| `NextButton` | `HowToPlayController.Next()` |
| `BackButton` | `GameSceneController.LoadHome()` (objek `UI`) |

`GameSceneController` perlu satu method baru (lihat §6).

---

## 5. Isi slide

Satu topik per slide, satu ilustrasi. Teks final (Inggris, siap tempel):

### Slide 1 — Grow the Plant
> **Pick up the seed and carry it to a dirt pile. When the plant grows, the level is complete.**

Ilustrasi: benih (`Seed`) di samping `TumpTanah` yang sudah menumbuhkan tanaman.

### Slide 2 — Stick to Any Surface
> **Move with A / D or the arrow keys. Webi sticks to every surface: floors, walls, and ceilings.**

Ilustrasi: Webi (prefab `Webi`) menempel di dinding, satu panah arah.

### Slide 3 — Swing with Your Web
> **Hold F to aim, then left-click to shoot. Each shot uses one Move, so watch the counter in the HUD.**

Ilustrasi: garis bidik putih + Webi berayun di ujung benang.

### Slide 4 — Seed & Soil
> **Walk over a seed to pick it up. Carry it into the dirt pile and Webi plants it automatically.**

Ilustrasi: Webi membawa benih di dekat `TumpTanah`.

### Slide 5 — Watch Out
> **Thorns kill on contact. Oil makes Webi lose grip and fall. Ice is slippery, so stop early.**

Ilustrasi: tiga panel kecil — `Spike_1`, permukaan oil, permukaan es.

### Slide 6 — Enemies & Tools
> **Touching a Bug or a Fly is fatal. Portals teleport Webi, and levers open gates.**

Ilustrasi: `Bug` + `Fly` di kiri, `GreenPortal`/`BluePortal` + `Gate`/`Lever` di kanan.

### Opsional — Slide 7 — Look Around
> **Hold right-click and drag to move the camera. Left-click stays free for your web.**

Kalau dipakai, semua indeks dot/array bertambah satu dan `slideDistance` tetap.

---

## 6. Perubahan di luar scene baru

### 6.1 `GameSceneController`

Tambah field dan method (pola sama dengan `LoadLevelSelect`):

```csharp
[Tooltip("Nama scene panduan bermain (How To Play). Dipakai tombol How to Play di Home Screen.")]
[SerializeField] private string howToPlayScene = "How To Play";

/// <summary>Membuka scene panduan bermain.</summary>
public void LoadHowToPlay()
{
    LoadScene(howToPlayScene);
}
```

### 6.2 Scenes In Build

`File → Build Profiles` (Unity 6: Build Settings) → tambahkan `Assets/Scenes/Menus/How To Play.unity`
setelah `Level Selection`.

Tanpa langkah ini, tombol How to Play akan gagal memuat scene (dan log error dari `LoadScene`).

---

## 7. Aset yang perlu disiapkan

### 7.1 Ikon tombol (opsional)

Belum ada ikon "buku/tanda tanya" di `Assets/Art/UI/Icons/Menu`. Kalau mau ikon di tombol:
buat `icon_help.png` (persegi, gaya sama seperti `icon_settings.png`) di folder itu, lalu pakai di
`Icon` tombol How to Play.

### 7.2 Ilustrasi slide

Folder baru: `Assets/Art/UI/HowToPlay/`.

| File | Isi | Ukuran |
|---|---|---|
| `slide_1_grow.png` | benih + tumpukan tanah + tanaman | 1120×1120 |
| `slide_2_wall.png` | Webi di dinding | 1120×1120 |
| `slide_3_swing.png` | garis bidik + Webi berayun | 1120×1120 |
| `slide_4_carry.png` | Webi membawa benih | 1120×1120 |
| `slide_5_hazards.png` | duri + oil + es | 1120×1120 |
| `slide_6_enemies.png` | Bug + Fly + portal + gate | 1120×1120 |

Cara termurah dan paling akurat: **screenshot in-game** (Game view 1120×1120 atau crop persegi),
lalu import sebagai **Sprite (2D and UI)**, PPU 100, Filter Bilinear, **Compression: None** (supaya
tidak buram di panel). Latar boleh transparan atau potongan level — yang penting Webi/objeknya
terbaca di ukuran 520×520.

Kalau nanti asetnya diganti, cukup ganti sprite di `Illustration`; tidak ada angka layout yang
bergantung pada isi gambar (semuanya Preserve Aspect).

---

## 8. Urutan pengerjaan (checklist)

1. [ ] Buka `Assets/Scenes/Menus/How To Play.unity` (sekarang kosong).
2. [ ] `GameObject → UI → Canvas`; set Canvas Scaler 1920×1080, Match 0.5. Unity otomatis membuat
       `EventSystem`; pastikan modulnya **InputSystemUIInputModule** (Input System baru) seperti di
       Home Screen, dan jangan sampai scene punya dua `EventSystem`.
3. [ ] Buat `Background` + `Title` + `BackButton` (§3.2, §3.3).
4. [ ] Buat `SlidesRoot`, lalu `Slide1`; lengkapi `Illustration`, `Panel`, `SlideTitle`, `SlideBody`.
5. [ ] Duplikat `Slide1` menjadi `Slide2`…`Slide6`; isi teks §5 dan sprite ilustrasinya.
6. [ ] Buat `PrevButton`, `NextButton`, `DotsRoot` + 6 bintang.
7. [ ] Tambah `HowToPlayController` di `UI`, seret semua field.
8. [ ] Tambah `GameSceneController` di `UI`, isi `Home Scene` = `Home Screen`.
9. [ ] Siapkan aset ilustrasi (§7.2) dan pasang.
10. [ ] Daftarkan scene di Scenes In Build (§6.2).
11. [ ] Home Screen: tambah tombol How to Play (§2.2 atau §2.3) + wiring `LoadHowToPlay()`.
12. [ ] Kompilasi: `dotnet build Assembly-CSharp.csproj` → 0 error.
13. [ ] Uji manual di Play (lihat §9).

---

## 9. Verifikasi manual (setelah diimplementasikan)

- [ ] Dari Home Screen, tombol How to Play membuka scene How To Play, bukan popup.
- [ ] Scene terbuka di slide 1, hanya slide 1 yang terlihat, bintang 1 aktif.
- [ ] NEXT maju sampai slide 6; di slide 6 tombol NEXT mati (kalau `wrapAround` mati).
- [ ] PREV kembali sampai slide 1; di slide 1 tombol PREV mati.
- [ ] `←`/`→` dan `A`/`D` pindah slide; `Esc` kembali ke Home Screen.
- [ ] Klik pada area transparan plank tidak memicu tombol (hit area mengikuti gambar).
- [ ] Back kembali ke `Home Screen` dan musik menu tidak putus (AudioManager ada di Home Screen —
      pastikan scene yang dibuka dari Home tetap punya AudioManager; kalau tidak, tidak ada BGM
      selama di How To Play, dan itu perlu diputuskan).
- [ ] Teks tidak terpotong di 16:9, dan tetap terbaca di 4:3 (Canvas Scaler Match 0.5).
- [ ] Tidak ada error/warning baru di Console.

---

## 10. Jebakan yang perlu diingat

- **Jangan pakai `Time.deltaTime`** di coroutine animasi slide: menu ini bisa dibuka saat
  `timeScale` 0 (mis. dari pause), animasinya akan membeku.
- `UIPopupSlide` menjepit root popup ke full screen; **jangan** dipakai di scene ini (menu ini bukan
  popup). Animasi slide ditulis sendiri di `HowToPlayController`.
- `UIButtonInteractive` memakai `raycastPadding` dan `alphaHitTestMinimumThreshold`, jadi sprite
  tombol harus **readable** (`Read/Write Enabled`) supaya area klik presisi. `board_button.png`
  sudah `isReadable: 1` — jangan diubah.
- Jangan daftarkan scene menu ini sebagai level; `LevelSelectionController` mengunci tombol
  berdasarkan bintang dan tidak berhubungan dengan How to Play.
- Kalau scene dibuka langsung (Play dari scene How To Play), `AudioManager` tidak ada → SFX tombol
  hanya bunyi lewat `AudioSource` cadangan (kalau field-nya diisi). Ini normal untuk pengujian.
