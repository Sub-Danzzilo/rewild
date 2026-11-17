# Player Controller Setup Guide

Panduan ini menyiapkan `Assets/Scripts/Gameplay/PlayerController.cs`. Player berjalan di permukaan platform, membidik dan menarik diri dengan web, serta dapat memakai portal yang saling terhubung.

## Hasil Akhir

- Player jatuh ke platform saat scene dimulai, lalu berjalan di permukaan dengan A/D atau tombol panah.
- Tekan F untuk membidik. Klik kiri pada platform yang valid untuk menembakkan web dan menarik player.
- Player dapat berjalan melewati sudut yang terdeteksi dan mendarat di platform tujuan.
- Bidikan dan ayunan dapat melewati portal yang terhubung.

## Sebelum Memulai

- Pastikan project memakai Input System package dan script tidak memiliki error compile.
- Siapkan scene 2D, GameObject player, platform dengan collider, serta Main Camera bertag `MainCamera`.
- Fokuskan Game view saat menguji input.

Jika input tidak terbaca, buka `Edit > Project Settings > Player`, cari `Active Input Handling`, lalu pilih `Input System Package (New)` atau `Both`. Restart Editor jika Unity memintanya.

## 1. Siapkan GameObject Player

1. Pilih GameObject player di Hierarchy.
2. Tambahkan `Rigidbody2D`, satu `Collider2D`, `PlayerController`, dan `LineRenderer` pada GameObject yang sama.
3. Tambahkan `Animator` dan `SpriteRenderer` pada GameObject yang sama jika player memakai animasi dan sprite.
4. Pada `PlayerController`, isi field `Line Renderer` dengan LineRenderer tersebut.

Script mengambil Rigidbody2D dan Collider2D dari GameObject yang sama saat mulai. Field Line Renderer wajib diisi. Animator dan SpriteRenderer dipakai secara opsional untuk animasi dan pembalikan arah sprite.

Atur Rigidbody2D ke `Dynamic`, `Simulated` aktif, Collision Detection `Continuous`, Interpolate `Interpolate`, dan jangan bekukan posisi atau rotasi Z. Saat mulai script mengatur Gravity Scale ke `1` agar player dapat jatuh ke platform. Setelah terdeteksi mendarat, gravitasi diatur ke `0` dan player mengikuti permukaan.

## 2. Siapkan Platform dan Layer

1. Buat layer bernama `Platform` melalui dropdown `Layer > Add Layer...` bila belum ada.
2. Atur GameObject collider platform ke layer `Platform`. Pastikan collider yang sebenarnya juga berada pada layer tersebut.
3. Gunakan Collider2D solid; matikan `Is Trigger` pada collider permukaan.
4. Pada PlayerController, centang layer `Platform` di field `Platform Layer`.
5. Untuk platform level yang diam, gunakan Rigidbody2D bertipe `Static` atau jangan pasang Rigidbody2D.

LayerMask ini digunakan untuk mencari pijakan, target bidik, transisi sudut, dan posisi mendarat portal. Layer yang tidak dicentang tidak akan dikenali sebagai permukaan.

## 3. Atur LineRenderer

1. Aktifkan `Use World Space` pada LineRenderer.
2. Atur material yang terlihat pada Game view, warna, sorting layer/order, serta lebar yang sesuai skala scene.
3. Hubungkan LineRenderer ke field `Line Renderer`.
4. Isi `Web Corner Sprite` jika ingin menampilkan sprite jaring pada ujung web; script membuat renderer sprite saat runtime.
5. `Portal Line Renderer` boleh dikosongkan. Script membuat segmen garis kedua secara otomatis untuk bagian web setelah portal.

Garis bidik adalah garis utuh, bukan garis putus-putus. Saat bidikan valid warnanya memakai `Aim Line Color`; target yang tidak valid memakai `Invalid Aim Line Color` dan material `Invalid Aim Material` bila tersedia.

## 4. Animasi (Opsional)

Animator pada GameObject player menggunakan parameter berikut:

| Parameter | Jenis | Fungsi |
| --- | --- | --- |
| `IsWalking` | Bool | Aktif saat player berjalan. |
| `ShootWeb` | Trigger | Saat web mulai ditembakkan. |
| `PullWeb` | Trigger | Saat player mulai ditarik. |
| `LandWeb` | Trigger | Saat player mendarat setelah ayunan. |

Gunakan nama dan jenis parameter persis seperti tabel jika membuat Animator Controller sendiri. Tanpa Animator, gerak tetap berfungsi; hanya animasinya yang tidak diputar.

## 5. Field Utama

| Field | Fungsi |
| --- | --- |
| `Walk Speed` | Kecepatan berjalan sepanjang permukaan. |
| `Swing Speed` | Kecepatan tarikan player ke target. |
| `Web Shoot Speed` | Kecepatan ujung web menuju target. |
| `Web Retract Speed` | Kecepatan web kembali setelah gerakan selesai. |
| `Surface Offset` | Jarak tambahan player dari platform di luar ukuran collider. |
| `Surface Probe Distance` | Jarak pemeriksaan permukaan dan sudut. |
| `Platform Layer` | Layer yang dihitung sebagai permukaan/target. |
| `Line Renderer` | LineRenderer utama; wajib diisi. |
| `Portal Line Renderer` | Segmen web setelah portal; otomatis dibuat bila kosong. |
| `Invalid Aim Material` | Material opsional untuk bidikan yang tidak valid. |
| `Web Corner Sprite` / `Web Corner Scale` | Tampilan sprite di ujung web dan skalanya. |
| `Aim Line Color` / `Invalid Aim Line Color` | Warna garis bidik valid dan tidak valid. |
| `Aim Ray Distance` | Jarak maksimum pemeriksaan bidikan. |
| `Aim Line Width` | Lebar garis bidik dan segmen portal. |
| `Debug Mode` | Menampilkan informasi status untuk diagnosis. |

Mulai dengan nilai Inspector bawaan. Sesuaikan collider player, `Surface Offset`, dan `Surface Probe Distance` jika player tampak melayang atau transisi sudut tidak stabil.

## 6. Kontrol dan Portal

- A atau panah kiri: bergerak ke satu arah sepanjang permukaan.
- D atau panah kanan: bergerak ke arah sebaliknya.
- F: masuk/keluar mode bidik.
- Gerakkan mouse: pilih arah bidikan.
- Klik kiri: mulai ayunan jika garis mengenai collider non-trigger di layer `Platform`.

Untuk setup portal dan aturan arah masuk/keluar, lihat [Gameplay Scripts Guide](./gameplay-scripts.md#portalcontroller).

## 7. Uji dan Diagnosis

1. Tekan Play dan pastikan player jatuh lalu mendarat pada platform.
2. Uji jalan dengan A/D dan tombol panah.
3. Tekan F dan arahkan mouse ke platform lain. Pastikan garis bidik muncul.
4. Klik kiri pada platform yang dituju. Pastikan web bergerak, player tertarik, lalu kontrol berjalan pulih.
5. Jika memakai portal, tautkan dua portal dan uji bidikan serta perpindahan langsung.

### Player tidak berjalan atau terus jatuh

- Pastikan Game view fokus, Input System aktif, dan Rigidbody2D Simulated.
- Pastikan Collider2D player/platform aktif dan permukaan masuk ke `Platform Layer`.
- Pastikan player berada cukup dekat di atas permukaan agar collider dapat mendarat.
- Periksa Console dan konfigurasi komponen pada GameObject yang sama.

### Bidikan tidak terlihat atau tidak bisa memilih platform

- Pastikan `Line Renderer` terisi, `Use World Space` aktif, serta material dan warna terlihat.
- Pastikan ada Main Camera bertag `MainCamera` dan `Mouse.current` tersedia.
- Pastikan target memiliki Collider2D non-trigger pada layer yang dipilih di `Platform Layer`.

### Player gagal mendarat setelah ayunan

- Periksa ukuran collider player dan target platform.
- Pastikan tidak ada collider solid lain yang menutup ruang mendarat.
- Sesuaikan `Surface Offset` sedikit demi sedikit dan pastikan platform termasuk `Platform Layer`.
