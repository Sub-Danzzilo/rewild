# Rewild di Android — Kontrol Sentuh, Build, dan Pengujian

Dokumen ini melengkapi `AGENTS.md`. Isinya: apa yang sudah dibuat untuk versi Android, cara build
ke Android maupun Windows, cara menguji kontrol tanpa HP, dan apa yang masih perlu digambar/diisi
manual.

Teks yang tampil di game tetap **bahasa Inggris** (menyusul menu yang sudah ada); dokumen ini
bahasa Indonesia.

---

## 1. Jawaban singkat

**1. Cara build ke Android dan Windows**

- Keduanya lewat **`File > Build Profiles`** (Unity 6 menggantikan "Build Settings").
- Windows: profil `Windows` sudah ada di proyek ini, tinggal `Build`.
- Android: `Add Build Profile > Android > Add Build Profile > Switch Profile`, atur Player Settings
  (nama paket, orientasi Landscape, IL2CPP + ARM64), lalu `Build` (APK) atau centang
  `Build App Bundle (Google Play)` untuk AAB.
- Android butuh modul **Android Build Support + OpenJDK + Android SDK & NDK** yang dipasang lewat
  Unity Hub. Tanpa modul itu, Android tidak muncul sebagai pilihan platform.

**2. Cara menguji kontrol Android di Unity**

- Tekan **Play** seperti biasa. Overlay tombol sentuh muncul di Editor karena
  `TouchControlsView > Show In Editor` hidup. Mouse dipakai sebagai satu jari: klik-tahan tombol
  jalan, tahan-drag analog bidik lalu lepas untuk menembak.
- Untuk mengecek ukuran di layar HP yang berbeda: **`Window > General > Device Simulator`**
  (pasang paket *Device Simulator* lewat Package Manager kalau belum ada), pilih perangkat seperti
  Pixel 7, lalu Play.
- **Multi-touch** (jalan sambil membidik) tidak bisa diuji dengan mouse karena Editor hanya punya
  satu pointer. Bagian itu wajib diuji di HP: `Build And Run` lalu cek langsung.

Rinciannya di §5 dan §6.

---

## 2. Apa yang sudah dibuat

| Berkas | Isi |
|---|---|
| `Assets/Scripts/UI/TouchControlsInput.cs` | Jembatan state antara tombol layar dan gameplay (statis, per scene). |
| `Assets/Scripts/UI/TouchControlsView.cs` | Akar overlay: visibilitas per platform dan transparansi tombol diam vs aktif. |
| `Assets/Scripts/UI/TouchMoveButton.cs` | Tombol tahan kiri/kanan. |
| `Assets/Scripts/UI/TouchAimStick.cs` | Analog bidik gaya MOBA + deteksi area batal. |
| `Assets/Scripts/UI/TouchCameraDragArea.cs` | Area kosong layar untuk menggeser kamera. |
| `Assets/Resources/UI/Touch Controls.prefab` | Prefab overlay lengkap dengan sprite terpasang. |
| `Assets/Art/UI/Controls/` | Sprite tombol (hasil rename dari `_Unused`). |
| `Docs/Android.md` | Dokumen ini. |

Yang **tidak** dibuat: scene baru, salinan UI di sepuluh scene level, dan perubahan tata letak level.
Prefab dimuat saat runtime oleh `LevelHudController`, dengan pola yang sama seperti
`LockedLevelPopup` di `Resources/UI`.

### 2.1 Nama sprite

| Sebelum (`Assets/Art/UI/_Unused/`) | Sesudah (`Assets/Art/UI/Controls/`) | Dipakai untuk |
|---|---|---|
| `tombol kiri.png` | `touch_move_left.png` | latar tombol kiri |
| `tmnol kanan.png` | `touch_move_right.png` | latar tombol kanan |
| `kiri.png` | `touch_arrow_left.png` | ikon panah kiri |
| `kanan.png` | `touch_arrow_right.png` | ikon panah kanan |
| `tombol jaring.png` | `touch_aim_pad.png` | latar analog bidik **dan** latar area batal |
| `jaring.png` | `touch_aim_knob.png` | art asli knob; disimpan sebagai sumber, tidak dipakai prefab lagi |
| — | `touch_aim_knob_web.png` | bagian jaring besar knob (selalu terlihat) |
| — | `touch_aim_knob_tail.png` | bagian tali + jaring kecil knob (muncul saat analog ditahan) |
| `tombol X.png` | `touch_cancel_x.png` | ikon silang di area batal |

File `.meta` ikut dipindah, jadi GUID-nya tidak berubah dan tidak ada referensi yang putus.
Kalau salah satu sprite diganti nanti, cukup ganti di prefab; nama berkasnya tidak dipakai script.

---

## 3. Tata letak dan perilaku tombol

Semua angka di bawah memakai kanvas **1920×1080** (Canvas Scaler proyek: Scale With Screen Size,
Match 0.5), sama seperti HUD level.

| Objek | Anchor | Posisi | Ukuran | Catatan |
|---|---|---|---|---|
| `Camera Drag Area` | stretch penuh | — | — | transparan, paling belakang |
| `Button Left` | kiri bawah | (300, 245) | 215×235 | ikon panah 105×105 |
| `Button Right` | kiri bawah | (565, 245) | 215×235 | ikon panah 105×105 |
| `Aim Pad` | kanan bawah | (−345, 285) | 330×330 | jaring besar 112×112 (`Web`, di pusat pad) + `Tail`, radius geser 64 |
| `Cancel Zone` | kanan atas | (−275, −265) | 250×250 | latar `touch_aim_pad` + ikon X 150×150 |

Semua tombol sengaja **tidak menempel pojok**: jaraknya ±250–350 px dari tepi, jadi masih nyaman
dijangkau ibu jari tanpa menutupi tepi level.

### 3.1 Alur kontrol

- **Jalan** — tahan `Button Left` / `Button Right`. Gerak memakai nilai −1/0/+1 yang sama dengan
  `A`/`D`, jadi perilaku di dinding dan langit-langit identik dengan versi Windows.
- **Bidik** — tahan `Aim Pad`, geser ke arah target. Garis bidik Webi mengikuti arah analog
  (putih = target valid, merah = bukan platform), sama seperti saat menahan `F`.
  Saat diam knob hanya tampak sebagai jaring besar; begitu analog ditahan, bagian tali dan jaring
  kecilnya memudar masuk (0,1 detik) sebagai penanda bahwa analog sedang aktif.
- **Tembak** — lepas jari. Tembakan hanya terjadi kalau ujung garis bidik mengenai platform
  (layer 3), persis seperti klik kiri di Windows.
- **Batal** — geser jari ke `Cancel Zone` (silang di kanan atas) lalu lepas. Area silang hanya
  muncul selama analog ditahan.
- **Kamera** — tahan-drag di bagian layar mana pun yang bukan tombol/HUD.
- Analog di tengah (dalam dead zone 20 px) dianggap belum mengarah ke mana pun; melepasnya berarti
  batal, bukan menembak.

### 3.2 Transparansi (UX)

| Keadaan | Alpha |
|---|---|
| Diam (tidak disentuh) | **0,4** |
| Sedang ditekan / dipakai | **0,9** |
| Saat game di-pause, mati, atau menang (`Time.timeScale = 0`) | 0 |
| Area batal saat membidik | 0,9 (selain itu 0) |
| Tali knob (`Aim Knob > Tail`) | 0 saat diam, 1 selama analog ditahan (`tailFadeSpeed` 10) |

Alasannya: 0,4 cukup samar untuk melihat level di bawah tombol, tetapi siluet tombol masih terbaca
di layar terang maupun gelap. Nilai aktif sengaja **tidak 1** supaya platform dan musuh di bawah
tombol tetap terlihat saat pemain menahan tombol — di game platformer, informasi di bawah ibu jari
sering lebih penting daripada tombolnya sendiri. Perubahan alpha memakai `Time.unscaledDeltaTime`
selama 0,12 detik, jadi tetap mulus walau `timeScale` 0.

Angka ini bisa diubah di prefab: `TouchControlsView > Idle Alpha`, `Active Alpha`, `Fade Duration`.
Tombol juga mengecil ke skala 0,94 saat ditekan (`TouchMoveButton > Pressed Scale`) sebagai umpan
balik tambahan, karena di layar sentuh tidak ada kursor.

---

## 4. Yang berubah di script lama

Perubahannya kecil dan dijaga tetap kompatibel dengan Windows:

- **`PlayerController`**
  - Input `A`/`D`/panah dan `F` tetap jalan; ditambah pembacaan `TouchControlsInput`.
  - `Keyboard.current == null` tidak lagi membuat `Update` langsung keluar (di Android tidak ada
    keyboard, jadi sebelumnya input sentuh tidak akan pernah terbaca).
  - Arah bidik: analog sentuh kalau ada, kalau tidak posisi mouse.
  - Menembak: klik kiri **atau** lepas analog.
  - `SetMovementLocked(true)` membuang permintaan sentuh yang masuk selama intro kamera.
- **`CameraDragController`** — delta jari dari `Camera Drag Area` menggeser kamera; mouse tetap
  memakai `dragButton` per level; kamera tetap terkunci saat membidik (`TouchControlsInput.AimHeld`
  ikut dihitung walau `playerController` kosong).
- **`LevelHudController`** — memuat `Resources/UI/Touch Controls` saat `Awake`, menaruhnya tepat di
  bawah objek HUD supaya popup pause/game over/menang tetap di atas. Kalau scene sudah punya
  instance sendiri, tidak dibuat dobel.
- **`HowToPlayController`** — teks body slide otomatis diganti versi sentuh di Android/iOS (§7).

Prefab overlay **tidak** perlu ditambahkan manual ke scene level mana pun. Kalau nanti mau
mematikan kontrol sentuh di satu level saja, tambahkan prefabnya sendiri atau kosongkan
`LevelHudController > Touch Controls Resource Path` di level itu.

---

## 5. Build

### 5.1 Windows

1. `File > Build Profiles`, pilih (atau tambah) profil **Windows**.
2. Pastikan daftar scene berisi 15 scene yang sekarang (Home Screen … Testing).
3. `Build`, pilih folder kosong (mis. `Builds/Windows`), hasilnya `Rewild.exe` + folder
   `Rewild_Data`.
4. Untuk memakai overlay kontrol sentuh di build Windows (mis. demo di laptop layar sentuh), hidupkan
   `Touch Controls.prefab > Show On Desktop`. Default-nya mati.

### 5.2 Android — sekali saja (persiapan)

1. **Unity Hub → Installs → ikon gerigi pada 6000.2.9f1 → Add modules**:
   `Android Build Support` + `Android SDK & NDK Tools` + `OpenJDK`.
2. Kalau Android tidak muncul di Build Profiles, modul di atas belum terpasang.

### 5.3 Android — setiap kali build

1. `File > Build Profiles > Add Build Profile > Android > Add Build Profile`, lalu
   **`Switch Profile`**.
2. **Player Settings** (`Edit > Project Settings > Player`, tab Android):
   - *Identification*: `Package Name` wajib diganti, mis. `com.namastudio.rewild` (tidak boleh
     `com.DefaultCompany...`), `Version` sesuka hati.
   - *Resolution and Presentation*: `Default Orientation` = **Landscape Left** (atau
     `Auto Rotation` dengan hanya Landscape Left/Right dicentang). Game ini HUD-nya 16:9, jadi
     portrait tidak dipakai. `Render outside safe area` boleh dibiarkan seperti bawaan.
   - *Other Settings*: `Scripting Backend` = **IL2CPP**, `Target Architectures` = **ARM64**
     (wajib untuk Google Play; ARMv7 hanya kalau perlu HP lama), `Minimum API Level` 23 ke atas,
     `Api Compatibility Level` = .NET Standard 2.1.
   - *Other Settings → Auto Graphics API*: matikan, lalu urutkan `Vulkan` di atas `OpenGLES3`.
     Kalau ada HP yang tampilannya rusak/blanko, hapus Vulkan dan sisakan OpenGLES3.
   - *Publishing Settings*: untuk uji cukup biarkan Unity membuat debug keystore; untuk rilis
     buat keystore sendiri dan simpan di luar proyek.
3. **Build Profiles**: centang `Build App Bundle (Google Play)` hanya kalau mau mengunggah ke Play
   Store. Untuk memasang langsung ke HP, biarkan mati (hasilnya `.apk`).
4. HP: aktifkan `Developer options` → `USB debugging`, sambungkan kabel, izinkan komputer.
5. `Build And Run` (langsung jalan di HP) atau `Build` lalu pasang manual:
   `adb install -r Builds/Android/Rewild.apk`.

Catatan: proyek ini sudah memakai **Input System baru** (`activeInputHandler = 1`), dan EventSystem
tiap scene memakai `InputSystemUIInputModule`. Sentuhan di UGUI (tombol, popup) langsung jalan tanpa
perubahan setting. Tidak ada asmdef khusus, jadi tidak ada yang perlu diubah di sisi build script.

> Belum ada tombol keluar (Quit) untuk Android di menu. Tombol fisik *Back* pada HP tidak otomatis
> berfungsi sebagai "kembali"; kalau perlu, itu tambahan terpisah.

### 5.4 Setting build per field (`File > Build Profiles`)

Dua hal yang sering tertukar:

- **Build Profiles** menyimpan beberapa profil build sekaligus. Hanya satu yang menjadi target
  Editor, ditandai badge hijau **`Active`**. Kalau tombol di kanan bawah masih **`Switch Platform`**,
  target belum pindah — tekan sekali (Unity reimport aset ±1–2 menit), baru tombolnya berubah jadi
  `Build` / `Build And Run`.
- **Platform Settings** di tengah jendela adalah pengaturan **khusus profil itu**. Kalau sebuah
  baris tidak di-*override*, nilainya ikut Player Settings. Nama paket, orientasi, ikon, dan
  keystore **tidak** ada di sini, melainkan di Player Settings.

#### Windows Settings

| Field | Nilai | Catatan |
|---|---|---|
| Architecture | **Intel 64-bit** | sudah benar |
| Build And Run On | Local Machine | sudah benar |
| Copy PDB files | mati | hidupkan hanya kalau mau membaca stack trace crash |
| Create Visual Studio Solution | mati | perlu hanya untuk debug lewat Visual Studio |
| Development Build | mati untuk rilis, hidup saat uji | Development Build menambah tombol debug & log |
| Autoconnect Profiler | mati | |
| Deep Profiling Support | mati | sangat memperlambat game |
| Script Debugging | mati | hidupkan hanya saat melacak bug script |
| Compression Method | Default (LZ4HC untuk rilis) | LZ4HC: build lebih lama, ukuran lebih kecil |

#### Android™ Settings

| Field | Nilai | Catatan |
|---|---|---|
| Texture Compression | **Use Player Settings** | formatnya diatur sekali di Player Settings (Android → ASTC) |
| Export Project | **mati** | hidupkan hanya kalau build lewat Android Studio |
| Build App Bundle (Google Play) | **mati** untuk uji HP, hidup untuk Play Store | APK bisa langsung `adb install`, AAB tidak |
| Debug Symbols | **Full** saat uji, **None** saat rilis | lihat catatan warning di bawah |
| Run Device | HP yang terhubung (tekan **Refresh**) | USB debugging harus aktif; kalau tidak terdeteksi, pilih `Any Android Device` |
| Development Build | mati untuk rilis, hidup saat uji | |
| Autoconnect Profiler / Deep Profiling / Script Debugging | mati | sama seperti Windows |
| Compression Method | **LZ4** (LZ4HC untuk rilis) | nilai bawaan Android di proyek ini |
| Diagnostics Data | Enabled (Project Default) | matikan kalau tidak memakai Unity Cloud Diagnostics |

**Warning kuning "Unity Cloud Diagnostics service is enabled, and it requires Debug Symbols…"**

Bukan error dan tidak menghalangi build. Artinya: Diagnostics aktif, tetapi `Debug Symbols =
Disabled`, jadi laporan crash tidak lengkap. Pilih salah satu:

- butuh stack trace lengkap saat uji → `Debug Symbols = Full` (build lebih besar), atau
- tidak butuh → `Diagnostics Data = Disabled`, atau biarkan seperti sekarang.

#### Kondisi proyek sekarang vs yang perlu diubah

Dibaca dari `ProjectSettings/ProjectSettings.asset` (bisa berubah kalau setting diubah di Editor):

| Setting | Sekarang | Untuk Android |
|---|---|---|
| `Package Name` (Android) | belum diisi → jadi `com.DefaultCompany.Rewild` | **ganti**, mis. `com.namastudio.rewild`. Tidak bisa diubah setelah unggah ke Play |
| `Company Name` | `DefaultCompany` | nama studio (ikut membentuk package name bawaan) |
| `Default Orientation` | `Auto Rotation` | **Landscape Left** (HUD game 16:9; portrait akan berantakan) |
| `Scripting Backend` (Android) | IL2CPP | sudah benar, jangan diubah |
| `Target Architectures` | ARM64 | sudah benar (wajib untuk Play) |
| `Minimum API Level` | 23 (Android 6.0) | sudah benar |
| `Api Compatibility Level` | .NET Standard 2.1 | sudah benar |
| `Color Space` | Linear | sudah benar |
| `Active Input Handling` | Input System Package (New) | sudah benar, jangan diubah ke Input Manager lama |
| `Bundle Version Code` | 1 | naikkan setiap unggah ke Play |
| Texture Compression (Android) | bawaan | set **ASTC**; kalau sprite UI terlihat buram, importer teksturnya di-set `Compression = None` |

---

## 6. Menguji kontrol tanpa HP

### 6.1 Play di Editor (paling cepat)

1. Buka scene level (mis. `Level 1`), tekan **Play**.
2. Overlay muncul karena `Show In Editor` hidup. Mouse = satu jari:
   - klik-tahan tombol kiri/kanan → Webi berjalan, lepas → berhenti;
   - tahan `Aim Pad`, geser → garis bidik mengikuti, lepas di area kosong → menembak;
   - tahan `Aim Pad`, geser ke `Cancel Zone` (kanan atas) → area silang membesar, lepas → batal;
   - tahan-drag di area kosong → kamera bergeser (setara tahan-klik-kanan di Windows).
3. Keyboard tetap aktif, jadi `A`/`D`/`F` masih bisa dipakai untuk membandingkan perilaku.

### 6.2 Device Simulator (cek ukuran & aspect ratio)

`Window > General > Device Simulator` → pilih perangkat (mis. Pixel 7, iPhone 15) → Play. Berguna
untuk melihat apakah tombol terlalu dekat tepi/notch di layar 20:9, karena kanvas proyek memakai
Match 0.5 (bukan 1:1 dengan 16:9).

Kalau paket belum ada: `Window > Package Manager > Unity Registry > Device Simulator > Install`.

Simulator juga punya tombol **`Rotate`** di toolbar (terlipat ke menu **⋮** kalau jendelanya sempit).
Bingkai perangkat berputar, tetapi isi layar ikut berputar hanya kalau auto-rotation aktif — jadi
lebih rapi kalau `Default Orientation` di Player Settings dikunci **Landscape Left**.

### 6.3 Multi-touch (wajib di HP)

Mouse hanya satu pointer, jadi kombinasi "tahan tombol kanan + tahan analog bidik" tidak bisa diuji
di Editor. Untuk itu:

1. `Build And Run` ke HP.
2. Uji berurutan: jalan sambil membidik, membidik sambil menggeser kamera (harus tidak bergeser),
   menekan dua tombol arah dengan dua jari (arah terakhir yang menang).
3. Kalau ada masalah, lihat log: `adb logcat -s Unity`.

---

## 7. How to Play untuk Android

Slide How To Play yang ada sekarang berisi gambar dan teks versi Windows (`A / D`, `F`,
`left-click`). Yang sudah diubah:

- `HowToPlayController` sekarang punya bagian **Touch (Android) Instructions**:
  - `Use Touch Instructions On Mobile` (default hidup) — di Android/iOS teks body slide diganti
    versi sentuh,
  - `Preview Touch Instructions In Editor` — hidupkan untuk melihat teks Android langsung di
    Editor tanpa build,
  - `Touch Slide Bodies` — teks pengganti per slide (urut slide 1). Kalau dibiarkan kosong, dipakai
    teks bawaan di dalam script (slide 2 dan 3 yang berubah: tombol panah + geser layar untuk
    kamera, dan analog bidik + area silang).

Teks bawaan versi sentuh sengaja dibuat sependek teks Windows (4 baris di kotak body 500×300,
font 40) supaya tidak menabrak judul slide. Sebagai pengaman, slide yang teksnya diganti juga
mendapat **auto-size TMP** (minimum 26), jadi kalimat yang lebih panjang akan mengecil sendiri
sampai muat; teks Windows di scene tidak ikut berubah.

Ilustrasi slide belum digambar ulang. Urutan yang disarankan saat art Android siap:

1. Gambar ulang `slide_2_wall.png` (tangan + tombol panah kiri bawah, panah geser kamera) dan
   `slide_3_swing.png` (analog bidik kanan bawah + area silang kanan atas) dengan komposisi sama
   seperti sekarang (persegi 1120×1120, latar transparan).
2. Simpan sebagai `slide_2_wall_touch.png` dan `slide_3_swing_touch.png` di
   `Assets/Art/UI/HowToPlay/`.
3. Ganti sprite `Illustration` pada Slide 2 dan Slide 3 di scene `How To Play` **hanya untuk build
   Android** — cara paling aman: buat variasi scene/slide terpisah, atau ganti sprite sebelum
   build Android lalu kembalikan sebelum build Windows. (Belum dibuat otomatis karena akan
   menambah field baru yang harus diisi manual; kalau mau otomatis, bisa ditambahkan nanti dengan
   pola yang sama seperti teks.)
4. Isi `Touch Slide Bodies` di Inspector kalau teks bawaannya mau diubah.

Kalau ingin menambah slide khusus Android (mis. "Look Around"), duplikat sebuah slide, isi
teksnya, lalu masukkan ke daftar `Slides` — `Touch Slide Bodies` mengikuti urutan yang sama.

---

## 8. Checklist uji di HP

- [ ] Menu Home → Level Selection → level terbuka, tombol level tetap bisa disentuh.
- [ ] Overlay muncul di level, transparan saat diam dan lebih pekat saat dipakai.
- [ ] Jalan kiri/kanan; Webi berhenti saat jari dilepas; bisa menempel di dinding dan langit-langit.
- [ ] Ambil benih dan tanam (butuh `CanInteract`, jadi Webi harus berhenti).
- [ ] Bidik: garis putih di platform, merah di udara; lepas = menembak, `Moves` berkurang 1.
- [ ] Lepas di area silang = batal, tidak ada tembakan dan tidak ada move yang hilang.
- [ ] Area silang hanya terlihat saat analog ditahan.
- [ ] Geser kamera di area kosong; kamera tidak bergeser saat membidik.
- [ ] Pause → tombol hilang; Resume → tombol muncul lagi dan tidak ada gerakan "hantu".
- [ ] Game over, menang, dan popup "level terkunci" tampil di atas tombol.
- [ ] Orientasi terkunci landscape, tidak ada layar terpotong di HP 20:9.
- [ ] Tidak ada error di `adb logcat -s Unity`.

---

## 9. Jebakan yang perlu diingat

- **Jangan menambah instance overlay ke scene level.** Prefab sudah dimuat otomatis; instance
  manual akan terdeteksi dan dilewati, tetapi lebih baik tidak ada supaya tidak bingung.
- **Mengubah tata letak = ubah prefab**, bukan script. Semua posisi/ukuran ada di prefab
  `Assets/Resources/UI/Touch Controls.prefab`.
- **Jangan memindahkan prefab keluar dari `Assets/Resources/UI/`** tanpa mengubah
  `LevelHudController > Touch Controls Resource Path`.
- **Notch / safe area**: area batal ada di kanan atas, dekat notch pada HP tertentu. Kalau terasa
  mengganggu, geser `Cancel Zone` (mis. ke `(−275, −340)`) atau tambahkan komponen safe-area
  sendiri. Belum ditangani otomatis.
- **Aspect ratio**: kanvas memakai Match 0.5 seperti HUD lain. Di layar 20:9 tombol jadi sedikit
  lebih jauh dari tepi — cek lewat Device Simulator sebelum mengubah angka.
- **Input System**: jangan mengubah `Active Input Handling` ke "Input Manager (Old)". Semua script
  memakai `Keyboard.current` / `Mouse.current`, dan EventSystem memakai `InputSystemUIInputModule`.
- **UI terlihat buram di HP**: sprite UI memakai kompresi otomatis Android (ASTC). Kalau tombol
  terlihat lembek, di importer tekstur tab Android set `Compression = None` atau `ASTC 4x4`.
- **Performa**: overlay ini hanya 11 objek UI tanpa animasi berat (alpha & lerp skala), jadi tidak
  menambah beban berarti.
- Uji perilaku (gerak, ayun, benam, popup) tetap **play test manual** — tidak ada test otomatis di
  proyek ini.

---

## 10. Saran lanjutan (belum dikerjakan)

1. Safe-area otomatis untuk notch (komponen kecil yang menggeser grup kanan atas).
2. Tombol *Back* HP → kembali ke Level Selection (butuh penanganan `Keyboard.current.escapeKey`
   atau `Application.wantsToQuit`/`InputSystem` back button).
3. Ilustrasi How To Play versi sentuh (§7).
4. Opsi slider transparansi tombol di menu Settings, kalau pemain mengeluh tombolnya terlalu samar.
