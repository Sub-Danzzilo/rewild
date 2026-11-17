# Menu, Settings, dan Level Setup

Panduan ini menyusun alur scene berikut. Scene adalah file yang memuat satu layar atau bagian permainan, misalnya menu utama atau level.

```text
Home -> Play -> LevelSelect -> Level1Intro -> Level1
  |                   ^
  +-> Settings -------+
```

Di akhir panduan, tombol menu dapat berpindah scene, pengaturan volume tersimpan, dan intro level dapat dilewati atau berakhir otomatis.

## Script yang Digunakan

- `Assets/Scripts/Core/GameSceneController.cs`: membuka scene dengan nama yang ditentukan di Inspector.
- `Assets/Scripts/Core/AudioManager.cs`: menjaga AudioSource tetap hidup saat scene berganti dan menyimpan volume ke `PlayerPrefs`.
- `Assets/Scripts/UI/SettingsController.cs`: membaca nilai volume untuk Slider dan meneruskan perubahan ke `AudioManager`.
- `Assets/Scripts/UI/LevelIntroController.cs`: memulai level setelah timer atau saat tombol Skip ditekan.

## Sebelum Memulai

- Pastikan script di atas tidak memiliki error compile di Console.
- Siapkan clip musik. Clip efek suara boleh disiapkan terpisah.
- Untuk membuat UI, gunakan `GameObject > UI` atau tombol UI pada Hierarchy. Unity biasanya membuat Canvas dan EventSystem jika belum tersedia; pastikan keduanya terlihat di Hierarchy.
- Simpan pekerjaan secara berkala dengan `Ctrl+S`.

Istilah singkat: `GameObject` adalah objek di scene, `Component` menambahkan kemampuan ke objek, dan `Inspector` adalah panel untuk mengubah pengaturan objek terpilih.

## 1. Buat dan Daftarkan Scene

1. Pilih `File > New Scene`, lalu simpan scene melalui `File > Save As...` di folder project, misalnya `Assets/Scenes`.
2. Buat lima scene dengan nama file persis seperti berikut:

   ```text
   Home
   LevelSelect
   Settings
   Level1Intro
   Level1
   ```

3. Buka `File > Build Profiles`, pilih platform yang sedang digunakan, lalu tambahkan kelima scene ke daftar scene/build scene.
4. Atur urutannya seperti ini agar scene awal untuk pengujian jelas:

   ```text
   0 Home
   1 LevelSelect
   2 Settings
   3 Level1Intro
   4 Level1
   ```

Script ini membuka scene berdasarkan nama, jadi ejaan dan kapitalisasi nama file harus sama. Jika kamu memakai nama lain, ubah field scene yang sesuai pada `GameSceneController` atau `LevelIntroController`.

## 2. Buat AudioManager

`AudioManager` harus dibuat lebih dulu, sebelum scene Settings mencoba membaca volume.

1. Buka scene `Home`.
2. Di Hierarchy, klik kanan area kosong lalu pilih `Create Empty`. Namai objeknya `AudioManager`.
3. Dengan objek itu terpilih, pilih `Add Component` pada Inspector dan tambahkan script `AudioManager`.
4. Buat dua GameObject kosong sebagai child dari `AudioManager`; namai `MusicSource` dan `SfxSource`. Jadikan child dengan menyeretnya ke atas objek `AudioManager` di Hierarchy.
5. Tambahkan component `AudioSource` ke masing-masing child.
6. Pada `MusicSource`, seret clip musik ke field `Audio Clip`, lalu aktifkan `Loop` dan `Play On Awake`. Musik akan mulai saat AudioManager dibuat.
7. Pilih objek `AudioManager`. Di component `AudioManager`, seret child `MusicSource` ke field `Music Source` dan `SfxSource` ke field `Sfx Source`.
8. Pastikan kedua field tersebut tidak bertuliskan `None`.

`AudioSource` adalah pemutar audio. `AudioManager` mengatur volumenya dan menyimpan perubahan segera ke `PlayerPrefs`, yaitu penyimpanan sederhana milik Unity. Objek AudioManager bertahan saat scene berubah; jangan menambahkan salinannya ke scene lain. `SfxSource` siap untuk dipakai script pemanggil efek suara, tetapi panduan ini belum menghubungkan efek suara ke tombol.

## 3. Buat Navigasi Home

1. Buka scene `Home` dan buat Canvas jika belum ada.
2. Di dalam Canvas, buat dua Button. Namai masing-masing `PlayButton` dan `SettingsButton`; ubah teks anaknya agar tampil sebagai `Play` dan `Settings`.
3. Buat GameObject kosong bernama `SceneController`, lalu tambahkan component `GameSceneController`.
4. Pilih `PlayButton`. Di component `Button`, cari bagian `On Click ()` atau `On Click () Event`, lalu klik `+` untuk menambah event.
5. Seret GameObject `SceneController` dari Hierarchy ke kotak object event yang masih bertuliskan `None`.
6. Buka dropdown fungsi yang biasanya bertuliskan `No Function`, lalu pilih `GameSceneController > LoadLevelSelect()`.
7. Ulangi untuk `SettingsButton`, tetapi pilih `GameSceneController > LoadSettings()`.
8. Simpan scene.

Event Button memanggil fungsi publik pada component objek yang kamu seret. Jika fungsi tidak muncul di daftar, pastikan objek yang diseret memiliki `GameSceneController` dan tidak ada error compile.

## 4. Buat Navigasi LevelSelect

1. Buka scene `LevelSelect` dan buat Canvas jika belum tersedia.
2. Buat Button `Level1Button` dan `BackButton`; ubah teks tampilannya menjadi `Level 1` dan `Back`.
3. Buat objek `SceneController` di scene ini dan tambahkan `GameSceneController`.
4. Pada event Button `Level1Button`, seret `SceneController` lalu pilih `GameSceneController > LoadLevel1()`.
5. Pada event Button `BackButton`, pilih `GameSceneController > LoadHome()`.
6. Simpan scene.

`LoadLevel1()` sengaja membuka `Level1Intro`, bukan `Level1` langsung. Tombol Back membawa pemain kembali ke Home.

## 5. Buat Settings dan Slider

1. Buka scene `Settings`. Buat Canvas jika belum tersedia.
2. Buat dua Slider melalui menu UI. Namai objeknya `MusicSlider` dan `SfxSlider`, lalu tambahkan label yang mudah dikenali jika diperlukan.
3. Pilih `MusicSlider`. Di component `Slider`, atur `Min Value` ke `0`, `Max Value` ke `1`, dan pastikan `Interactable` aktif. Atur nilai awal sementara, misalnya `1`.
4. Atur `SfxSlider` dengan rentang yang sama.
5. Buat GameObject `SettingsController` dan tambahkan script `SettingsController`.
6. Seret `MusicSlider` dan `SfxSlider` dari Hierarchy ke field `Music Slider` dan `Sfx Slider` pada script. Pastikan bukan `None`.
7. Pada component `MusicSlider`, buka event `On Value Changed (Single)`, klik `+`, lalu seret objek `SettingsController` ke slot object.
8. Pilih fungsi `SettingsController > SetMusicVolume(float)`.
9. Ulangi pada `SfxSlider` dan pilih `SettingsController > SetSfxVolume(float)`.
10. Buat Button `BackButton`. Tambahkan `GameSceneController` ke GameObject baru, atau gunakan GameObject pengendali yang sama jika sudah ada di scene. Hubungkan event Button ke `GameSceneController > LoadLevelSelect()`.
11. Simpan scene.

Slider menghasilkan angka dalam rentang 0 sampai 1. Nilai 0 mematikan sumber audio dan 1 menggunakan volume penuh. Saat Settings dibuka, script mengisi posisi Slider dari volume yang tersimpan tanpa memicu event perubahan lagi. Tombol Back dari Settings kembali ke LevelSelect sesuai alur contoh ini.

## 6. Buat Intro Level 1

1. Buka scene `Level1Intro` dan buat Canvas.
2. Tambahkan teks atau elemen visual intro.
3. Tambahkan Button `SkipButton` dengan teks `Skip`.
4. Buat GameObject kosong `Level1IntroController`, lalu tambahkan script `LevelIntroController`.
5. Di Inspector, isi `Level 1 Scene` dengan nama scene gameplay, yaitu `Level1`.
6. Isi `Intro Duration`, misalnya `5`. Angka ini adalah durasi dalam detik.
7. Pada event `On Click ()` milik `SkipButton`, seret objek `Level1IntroController`, lalu pilih `LevelIntroController > SkipIntro()`.
8. Simpan scene.

Jika durasi lebih dari 0, level dimulai otomatis setelah timer selesai. Jika pemain menekan Skip, level dimulai segera dan timer dibatalkan. Nilai durasi 0 atau kurang mematikan perpindahan otomatis; tombol Skip tetap dapat memulai level.

## 7. Jalankan dan Uji Alur

1. Simpan semua scene, lalu buka `Home`.
2. Tekan Play di Editor dan pastikan Game view aktif.
3. Tekan `Play`; pastikan `LevelSelect` terbuka.
4. Tekan `Level 1`; pastikan `Level1Intro` terbuka.
5. Ulangi, lalu tekan `Skip`; pastikan `Level1` terbuka sebelum timer selesai.
6. Ulangi sekali lagi tanpa menekan Skip; pastikan `Level1` terbuka setelah durasi intro.
7. Dari Home, buka Settings. Geser Music Slider dan dengarkan perubahan volume musik.
8. Hentikan Play Mode, jalankan lagi, lalu buka Settings untuk memastikan nilai Slider mengikuti nilai tersimpan.
9. Geser SFX Slider. Perubahan nilainya tersimpan, tetapi suara uji tidak akan terdengar kecuali ada script lain yang memanggil `AudioManager.PlaySfx()`.
10. Periksa Console. Tidak boleh ada error merah selama alur ini.

## Troubleshooting

### Scene tidak terbuka

- Pastikan scene ada di daftar scene Build Profiles untuk platform aktif.
- Cocokkan nama file scene dengan field scene pada `GameSceneController` atau `LevelIntroController`, termasuk kapitalisasi.
- Pastikan scene sudah disimpan sebelum dimasukkan ke daftar.

### Fungsi tidak muncul pada event Button atau Slider

- Pastikan GameObject yang diseret memiliki component controller yang benar.
- Pastikan nama fungsi dipilih dari component yang tepat dan event memakai fungsi dengan parameter yang sesuai.
- Periksa Console untuk error compile; script yang gagal compile mungkin tidak muncul di Inspector.

### Slider bergerak tetapi volume tidak berubah

- Pastikan satu `AudioManager` dibuat di Home dan tidak ada error ketika scene pertama dijalankan.
- Pastikan `Music Source` dan `Sfx Source` terisi AudioSource yang benar.
- Pastikan rentang Slider 0 sampai 1 dan event-nya terhubung ke method yang sesuai.
- Untuk menguji SFX, pastikan ada pemanggil `PlaySfx()`; slider sendiri tidak membuat atau memainkan clip.

### Slider kembali ke nilai awal atau tidak mengikuti nilai tersimpan

- Pastikan AudioManager dibuat di scene pertama yang dijalankan, yaitu Home pada panduan ini.
- Pastikan tidak ada AudioManager duplikat. Singleton akan menghancurkan salinan kedua, sehingga referensinya bisa membingungkan.
- Pastikan Settings tidak dijalankan langsung tanpa terlebih dahulu membuat AudioManager.

### Intro tidak otomatis berpindah

- Pastikan `Intro Duration` lebih besar dari 0.
- Pastikan ejaan `Level 1 Scene` sama persis dengan nama scene gameplay.
- Pastikan scene gameplay sudah disimpan dan terdaftar di Build Profiles.
