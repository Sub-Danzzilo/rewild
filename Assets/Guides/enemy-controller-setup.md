# Enemy Controller Setup Guide

Panduan ini menyiapkan `Assets/Scripts/Gameplay/EnemyController.cs`. Satu script mendukung tiga jenis musuh: terbang, bergerak mengikuti titik di sekitar platform, dan diam seperti duri.

## Hasil Akhir

- Musuh bergerak melewati waypoint dengan pola patrol yang dipilih.
- Musuh terbang dapat berhenti sebelum collider penghalang dan berbalik.
- Musuh `SurfaceAttached` mengikuti collider platform dan menuju waypoint yang kamu susun.
- Musuh dapat bereaksi ketika menyentuh player bertag `Player`.

## Sebelum Memulai

- Pastikan `EnemyController.cs` tidak memiliki error compile.
- Siapkan scene 2D, GameObject player, dan collider platform bila diperlukan.
- Buka tab Scene dan Game. Scene dipakai untuk menyusun posisi waypoint; Game dipakai untuk mencoba hasil.

Waypoint adalah GameObject Transform yang menandai titik tujuan. Tipe `Flying` bergerak menuju posisi waypoint berdasarkan Rigidbody; `SurfaceAttached` dapat memproyeksikan waypoint ke batas collider platform terlebih dahulu.

## 1. Buat Enemy

1. Buat atau pilih GameObject musuh di Hierarchy.
2. Tambahkan `Rigidbody2D` dan satu `Collider2D` yang sesuai ukuran sprite.
3. Tambahkan script `EnemyController`.
4. Pastikan `Rigidbody2D` dan collider berada di GameObject yang sama dengan script.

`EnemyController` memiliki atribut `RequireComponent`, sehingga Unity biasanya menambahkan Rigidbody2D dan Collider2D otomatis saat script dipasang. Tetap periksa keduanya di Inspector. Untuk musuh terbang yang berhenti dengan jarak dari platform, collider sebaiknya mendekati ukuran bentuk fisik musuh; jarak berhenti dihitung dari collider, bukan sprite.

## 2. Atur Rigidbody2D

Pada component `Rigidbody2D`, gunakan nilai berikut:

- `Body Type`: `Dynamic`.
- `Simulated`: aktif.
- `Gravity Scale`: untuk `Flying`, script mengaturnya ke `0`; untuk `SurfaceAttached`, gunakan nilai positif agar musuh dapat jatuh ke platform sebelum patrol. Tipe ini mengatur gravitasi ke `0` setelah mendarat.
- `Collision Detection`: `Continuous`.
- `Constraints > Freeze Position`: jangan dicentang.
- `Constraints > Freeze Rotation Z`: jangan dicentang untuk `SurfaceAttached` jika `Rotate To Waypoint` aktif. Tipe `Flying` membekukan rotasinya melalui script.

Rigidbody tetap `Dynamic` agar script dapat memindahkan musuh. Script mengatur Collision Detection ke `Continuous` dan Interpolate ke `Interpolate`. Tipe `Flying` mengatur Gravity Scale ke 0 dan membekukan rotasi. Tipe `SurfaceAttached` perlu jatuh ke permukaan sebelum mulai patrol, jadi biarkan gravitasi aktif sampai musuh mendarat; setelah itu script mengaturnya ke 0. Jika posisi dibekukan, gerak waypoint tidak akan berjalan.

## 3. Siapkan Layer Penghalang

Langkah ini terutama diperlukan untuk tipe `Flying`.

1. Pilih platform yang akan dianggap sebagai penghalang.
2. Dari dropdown `Layer` di Inspector, pilih `Add Layer...` dan buat layer `Platform` jika belum ada.
3. Atur GameObject platform beserta collider yang dipakai ke layer `Platform`.
4. Pastikan platform memiliki Collider2D dan `Is Trigger` tidak dicentang.
5. Pilih enemy. Pada field `Obstacle Layer` di bagian `Flying`, centang layer `Platform`.

`Obstacle Layer` menentukan collider yang diperiksa saat musuh terbang bergerak. Collider trigger diabaikan oleh pemeriksaan ini. Untuk tipe selain Flying, field ini tidak mengatur jalur.

Untuk `SurfaceAttached`, `Platform Layer` dipakai untuk menemukan permukaan saat waypoint di-snap, mendeteksi pijakan, dan mengikuti platform. Collider platform harus solid, berada pada layer yang dicentang, dan memiliki permukaan atas yang bisa didarati saat musuh mulai. Waypoint tetap menentukan rute dan sisi tujuan.

## 4. Buat dan Susun Waypoint

1. Klik kanan area kosong di Hierarchy, lalu pilih `Create Empty`.
2. Namai titik pertama, misalnya `EnemyA`.
3. Ulangi untuk `EnemyB`, lalu buat titik tambahan bila rutenya membutuhkan lebih dari dua titik.
4. Pilih setiap waypoint dan ubah posisi `Transform`-nya di Inspector, atau geser gizmo di Scene view.
5. Pilih enemy. Di component `EnemyController`, ubah ukuran array `Waypoints` sesuai jumlah titik.
6. Seret setiap waypoint dari Hierarchy ke element array dengan urutan rute yang diinginkan.
7. Pastikan setiap element tidak bernilai `None`.

Contoh rute:

```text
Waypoints[0] = EnemyA
Waypoints[1] = EnemyB
Waypoints[2] = EnemyC
```

Posisikan waypoint pada jalur yang aman dan terlihat di Scene. Untuk musuh bergerak, siapkan setidaknya dua waypoint. Untuk `Flying`, jika `Start At First Waypoint` tidak aktif, enemy mulai dari Transform-nya dan menuju waypoint element 1. `SurfaceAttached` memilih waypoint terdekat setelah berhasil mendarat.

## 5. Pilih Jenis Enemy dan Pola Patrol

Pilih `Enemy Type` di Inspector:

- `Stationary`: diam di posisi dan rotasi Transform. Waypoint tidak diperlukan.
- `Flying`: bergerak bebas di antara waypoint dan memeriksa obstacle layer.
- `SurfaceAttached`: mendarat di platform dan mengikuti permukaannya menuju waypoint yang di-snap; sisi yang boleh dilalui ditentukan oleh `Allowed Surface Sides`.

Pilih `Patrol Mode` untuk menentukan urutan setelah sebuah waypoint dicapai:

| Mode | Urutan contoh | Perilaku |
| --- | --- | --- |
| `PingPong` | `A -> B -> C -> B -> A` | Bergerak ke ujung lalu kembali melewati titik sebelumnya. |
| `Loop` | `A -> B -> C -> A` | Bergerak ke element berikutnya lalu kembali ke awal. |
| `Once` | `A -> B -> C -> berhenti` | Berhenti setelah waypoint terakhir. |
| `Random` | `A -> C -> B -> ...` | Memilih waypoint lain secara acak, tidak memilih titik yang sama dua kali berturut-turut. |

Untuk `Stationary`, pola patrol dan waypoint tidak dipakai.

## 6. Setup Musuh Terbang

1. Atur `Enemy Type` ke `Flying`.
2. Atur `Patrol Mode` ke `PingPong`.
3. Isi minimal dua waypoint dan susun rutenya.
4. Atur field berikut sebagai titik awal:

   ```text
   Move Speed:              2
   Waypoint Reach Distance: 0.05
   Start At First Waypoint: sesuai kebutuhan
   Obstacle Layer:          Platform
   Obstacle Stop Distance:  0.2
   Reverse On Obstacle:     aktif
   ```

5. Jalankan scene dan amati jalur musuh.

`Start At First Waypoint` aktif akan memindahkan posisi enemy ke waypoint element 0 saat game dimulai. Jika nonaktif, enemy tetap mulai dari posisi Transform-nya dan tujuan pertama adalah element 1.

Musuh tidak mencari rute alternatif. Jika penghalang ada di antara A dan B, ia berhenti dekat penghalang lalu berbalik saat `Reverse On Obstacle` aktif. Untuk membuat jalur memutari penghalang, susun titik tambahan, misalnya `A -> C -> D -> B`.

### Obstacle Stop Distance

Nilai ini adalah jarak collider enemy dari collider penghalang:

- `0`: pemeriksaan jarak berhenti tambahan dinonaktifkan; jangan gunakan nilai ini bila kamu mengandalkan jarak aman sebelum obstacle.
- `0.1`: jarak dekat.
- `0.2`: nilai awal yang disarankan.
- `0.5`: musuh berbalik lebih jauh dari obstacle.

Jika sprite tampak terlalu dekat meskipun jaraknya benar, periksa ukuran collider enemy. `Reverse On Obstacle` aktif membuat enemy memilih arah kembali. Jika nonaktif, enemy tidak membalik arah dan berhenti saat pemeriksaan mendeteksi jalur terhalang.

## 7. Setup Musuh SurfaceAttached

1. Pastikan platform memiliki Collider2D solid dan layer-nya termasuk dalam field `Platform Layer`.
2. Buat waypoint di dekat permukaan yang ingin dilewati. Susun setidaknya dua titik dan letakkan titik di sisi platform yang dimaksud, termasuk waypoint terpisah sebelum dan sesudah sudut.
3. Pada enemy, atur:

   ```text
   Enemy Type:        SurfaceAttached
   Patrol Mode:       PingPong
   Move Speed:        2
   Waypoints:         titik rute dalam urutan yang benar
   Platform Layer:    Platform
   Snap Waypoints To Surface: aktif
   Waypoint Snap Radius:      cukup untuk menemukan platform tujuan
   Rotate To Waypoint: aktif
   Allowed Surface Sides: sisi yang akan dipakai
   ```

4. Letakkan enemy di atas platform yang ingin dipijak saat mulai, dengan collider tidak bertumpuk dengan platform. Script menunggu kontak sisi atas sebelum mulai bergerak.
5. Atur `Start At First Waypoint` jika posisi awal harus dipindahkan ke waypoint pertama. Saat mendarat, script memilih waypoint terdekat dari posisi enemy.
6. Uji jalurnya, terutama transisi antar-sisi. Untuk titik yang tidak menemukan platform dalam radius snap, dekatkan waypoint atau naikkan `Waypoint Snap Radius`.

Jika `Snap Waypoints To Surface` aktif, tiap waypoint mencari collider platform terdekat dalam `Waypoint Snap Radius` lalu memakai titik batas permukaannya. `Platform Layer` juga dipakai untuk menjaga pijakan dan mendeteksi sudut. Musuh bisa berpindah ke sisi platform lain yang diizinkan; sisi yang tidak diizinkan tidak dapat dipakai untuk transisi permukaan.

### Allowed Surface Sides

`Allowed Surface Sides` memilih arah normal permukaan yang boleh dipakai: `Top`, `Right`, `Bottom`, `Left`, atau kombinasi. Untuk memilih kombinasi, gunakan kotak centang enum flags di Inspector. Contoh: pilih `Top` dan `Right` untuk rute yang melewati sisi atas dan kanan. `Surface Probe Distance` mengatur seberapa jauh pemeriksaan pijakan/sudut.

Script menentukan sisi tujuan dari posisi waypoint relatif ke platform yang ditemukan. Saat snap dinonaktifkan atau tidak ada platform yang cocok, posisi Transform waypoint dipakai langsung. `Rotate To Waypoint` aktif membuat rotasi enemy mengikuti normal permukaan; jika nonaktif, collider tidak diputar otomatis.

## 8. Setup Duri Diam

1. Atur `Enemy Type` ke `Stationary`.
2. Atur posisi dan rotasi GameObject langsung melalui Transform.
3. Sesuaikan Collider2D dengan area berbahaya duri.
4. Pastikan `Stop On Player Hit` aktif jika kontak harus menghentikan enemy setelah mengenai player.

Waypoint tidak diperlukan untuk mode ini. Collider tetap dibutuhkan untuk mendeteksi kontak. Untuk duri di sisi kanan, kiri, atau bawah platform, rotasikan GameObject secara manual.

## 9. Atur Kontak dengan Player

1. Pilih GameObject player.
2. Di dropdown `Tag` pada Inspector, pilih `Player` pada GameObject yang memiliki collider yang menyentuh musuh.
3. Jika tag belum ada, pilih `Add Tag...`, tambahkan tag dengan ejaan persis `Player`, lalu pilih kembali GameObject player dan tetapkan tag itu.
4. Pastikan player dan enemy masing-masing memiliki Collider2D. Player juga memerlukan Rigidbody2D agar callback fisika 2D berjalan sesuai setup ini.
5. Pada enemy, tentukan nilai `Stop On Player Hit`.

Saat menyentuh objek bertag `Player`, script menonaktifkan GameObject player. Jika `Stop On Player Hit` aktif, enemy juga berhenti; jika nonaktif, enemy terus bergerak. `Stop Animation On Death` mengatur apakah Animator enemy membekukan animasi yang sedang berjalan saat enemy mati. Opsi ini juga berlaku saat patrol mode `Once` selesai. Ini tidak memainkan animasi kematian atau mengatur Game Over maupun respawn.

## 10. Field Umum

| Field | Fungsi dan saran |
| --- | --- |
| `Move Speed` | Kecepatan menuju waypoint. Nilai awal `2`; harus lebih besar dari 0 agar terlihat bergerak. |
| `Waypoint Reach Distance` | Toleransi untuk mencapai waypoint. `Flying` membandingkan posisi Rigidbody; `SurfaceAttached` memakai titik kontak permukaan dan toleransi permukaannya. |
| `Start At First Waypoint` | Jika aktif, posisi awal dipindahkan ke element 0. Tipe SurfaceAttached kemudian tetap menunggu kontak dengan permukaan atas; sesudah mendarat, waypoint terdekat dipilih. |
| `Stop On Player Hit` | Menghentikan enemy setelah player disentuh jika aktif. |
| `Stop Animation On Death` | Membekukan animasi yang sedang berjalan saat enemy mati jika aktif dan objek enemy memiliki Animator. |
| `Obstacle Layer` | Layer penghalang yang diperiksa oleh tipe Flying. |
| `Obstacle Stop Distance` | Jarak aman dari collider penghalang untuk Flying. |
| `Reverse On Obstacle` | Membalik arah patrol Flying saat penghalang terdeteksi. |
| `Platform Layer` | Layer collider solid untuk landing, snapping waypoint, pijakan, dan transisi tipe SurfaceAttached. |
| `Snap Waypoints To Surface` | Memproyeksikan tiap waypoint ke batas platform terdekat dalam radius pencarian. |
| `Waypoint Snap Radius` | Jarak maksimum pencarian platform untuk setiap waypoint. |
| `Surface Waypoint Tolerance` | Toleransi jarak saat SurfaceAttached menganggap waypoint sudah dicapai. |
| `Allowed Surface Sides` | Sisi platform yang boleh menjadi permukaan pijakan/tujuan. |
| `Rotate To Waypoint` | Mengatur apakah rotasi enemy mengikuti normal permukaan. |
| `Surface Probe Distance` | Jarak pemeriksaan untuk menemukan pijakan atau sisi platform berikutnya. |

Waypoint harus berisi Transform yang valid. Jumlah waypoint nol menonaktifkan gerak enemy; isi semua element, terutama element 0 jika `Start At First Waypoint` aktif.

## 11. Uji dan Diagnosis

1. Mulai dengan dua waypoint yang terlihat jelas dan berjarak beberapa unit.
2. Pilih `Flying`, atur `PingPong`, lalu jalankan Play. Pastikan enemy bergerak A ke B dan kembali.
3. Tambahkan platform di jalur dan periksa perilaku obstacle layer serta jarak berhenti.
4. Uji `SurfaceAttached` dengan dua titik di sisi atas. Tambahkan sisi berbeda hanya setelah rute dasar bekerja.
5. Uji `Stationary` dan sentuh enemy dengan player bertag `Player`.
6. Periksa Console untuk error.

### Enemy tidak bergerak

- Untuk `Flying` atau `SurfaceAttached`, pastikan array `Waypoints` berisi minimal dua Transform.
- Pastikan `Move Speed` lebih besar dari 0 dan Rigidbody2D `Simulated` aktif.
- Pastikan Freeze Position tidak aktif.
- Pastikan setiap waypoint valid dan berada di posisi berbeda.
- Untuk `SurfaceAttached`, pastikan enemy mendarat pada permukaan atas solid di `Platform Layer`, gravitasi aktif sebelum landing, dan seluruh waypoint terisi.
- Jika waypoint tidak menempel ke permukaan yang diharapkan, pastikan collider berada pada layer `Platform Layer`, bukan trigger, lalu dekatkan waypoint atau naikkan `Waypoint Snap Radius`.

### Enemy mulai di posisi yang tidak diharapkan

- Jika `Start At First Waypoint` aktif, posisi awal akan ditimpa oleh posisi waypoint pertama.
- Jika tidak aktif, `Flying` mulai dari Transform saat ini dan bergerak ke waypoint element 1. `SurfaceAttached` memilih waypoint terdekat setelah mendarat.
- Pastikan posisi waypoint memang diinginkan dan tidak tertukar urutannya di array.

### Musuh terbang menembus atau berhenti terlalu jauh

- Pastikan collider obstacle berada pada layer yang dicentang di `Obstacle Layer`.
- Pastikan collider obstacle bukan trigger.
- Periksa ukuran collider enemy dan atur `Obstacle Stop Distance`.
- Rute obstacle avoidance hanya berbalik; enemy tidak menghitung jalur memutar otomatis.

### Musuh SurfaceAttached berbalik di waypoint

- Periksa `Allowed Surface Sides`, `Snap Waypoints To Surface`, dan apakah waypoint tersnap ke permukaan yang diinginkan. Sisi yang tidak diizinkan tidak dapat dipakai untuk transisi permukaan.
- Pastikan titik rute tersusun sesuai urutan patrol.
- Untuk waypoint yang berada pada collider berbeda, pastikan jalur di antara keduanya masih tersambung oleh permukaan yang bisa ditemukan melalui probe.

### Player tidak bereaksi saat menyentuh musuh

- Pastikan ejaan tag player persis `Player`.
- Pastikan kedua objek memiliki Collider2D dan interaksi layer tidak dimatikan pada Physics 2D settings.
- Pastikan collider yang digunakan bukan trigger tanpa Rigidbody2D pada salah satu objek; script mendukung collision dan trigger, tetapi callback fisika harus aktif.

## Nilai Awal Ringkas

```text
Flying:
  Patrol Mode:             PingPong
  Move Speed:              2
  Waypoint Reach Distance: 0.05
  Obstacle Stop Distance:  0.2
  Reverse On Obstacle:     aktif

SurfaceAttached:
  Patrol Mode:             PingPong
  Move Speed:              2
  Waypoint Reach Distance: 0.05
  Platform Layer:          Platform
  Snap Waypoints To Surface: aktif
  Rotate To Waypoint:      aktif

Stationary:
  Stop On Player Hit:      aktif
```
