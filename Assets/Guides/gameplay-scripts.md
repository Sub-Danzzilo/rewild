# Gameplay Scripts Guide

Panduan referensi ini mencakup seluruh script di `Assets/Scripts/Gameplay/`:

| Script | Fungsi |
| --- | --- |
| [PlayerController.cs](../Scripts/Gameplay/PlayerController.cs) | Gerak Webi, bidikan web, tarikan, landing, dan integrasi portal. |
| [EnemyController.cs](../Scripts/Gameplay/EnemyController.cs) | Musuh terbang, patrol mengikuti permukaan, dan musuh diam. |
| [PortalController.cs](../Scripts/Gameplay/PortalController.cs) | Portal berpasangan untuk perpindahan player dan lintasan web. |
| [GateLeverController.cs](../Scripts/Gameplay/GateLeverController.cs) | Trigger tuas yang memainkan animasi dan membuka gate satu kali. |
| [SeedPlantingController.cs](../Scripts/Gameplay/SeedPlantingController.cs) | Mengambil seed, mengikuti player, menanam, dan memutar animasi tumbuh. |

Panduan konfigurasi rinci player dan enemy tersedia di [PlayerController Setup](./player-controller-setup.md) dan [EnemyController Setup](./enemy-controller-setup.md). Script pendukung seperti `CameraDragController` berada di folder Core, bukan folder Gameplay.

## PlayerController

Gunakan [PlayerController Setup](./player-controller-setup.md) untuk setup komponen, layer permukaan, Input System, animasi, kontrol, field, dan troubleshooting player. Input bawaan: A/D atau panah untuk berjalan, F untuk bidik, dan klik kiri untuk menembakkan web ke collider solid pada `Platform Layer`.

## EnemyController

Satu component menyediakan tiga tipe:

- `Flying`: bergerak bebas di antara waypoint dan memeriksa layer obstacle.
- `SurfaceAttached`: menunggu mendarat di sisi atas platform, lalu bergerak mengikuti sisi platform menuju waypoint.
- `Stationary`: diam di Transform-nya dan hanya bereaksi terhadap kontak player.

Lihat [EnemyController Setup](./enemy-controller-setup.md) untuk pembuatan waypoint, mode patrol, konfigurasi obstacle/platform, kontak player, dan diagnosis.

## PortalController

### Menyiapkan portal berpasangan

1. Buat dua GameObject portal, misalnya `BluePortal` dan `GreenPortal`. Pasang `SpriteRenderer` dan `PortalController` pada masing-masing.
2. Script memerlukan `BoxCollider2D` (Unity menambahkannya otomatis). Hubungkan `Portal Renderer` jika perlu; bila sprite tersedia, collider disesuaikan dengan batas sprite dan dibuat trigger saat runtime.
3. Pada portal biru, seret portal hijau ke `Linked Portal`; lakukan sebaliknya pada portal hijau. Hubungan harus dua arah dan tidak boleh menunjuk ke dirinya sendiri.
4. Atur `Local Front Direction` untuk menentukan sisi masuk, relatif terhadap rotasi portal. Nilai awal `(-1, 0)` berarti sisi depan mengarah ke kiri; `(0, 1)` berarti ke atas. Rotasi Transform portal ikut memutar arah ini.
5. Pastikan di sisi keluar portal tujuan terdapat platform solid pada layer `Platform Layer` milik player, dalam jarak `Exit Search Distance`, serta ada ruang kosong untuk mendarat.
6. Uji masuk dari sisi depan portal. Mendekati dari belakang tidak mengaktifkan perpindahan.

`Exit Offset` memberi jarak kecil bagi titik keluar web dari portal tujuan. `Exit Search Distance` menentukan jarak pencarian permukaan untuk teleport langsung. Portal juga dapat dilintasi oleh bidikan dan ayunan web; arah web diputar mengikuti orientasi kedua portal.

Teleport langsung hanya berhasil saat player berada pada state Walking/Falling, tidak sedang menarik web, dan menemukan permukaan keluar yang aman. Jika tidak ada platform atau ruang mendarat terhalang, player tetap di pintu masuk dan pesan diagnosis ditulis ke Console. Portal harus aktif dan memiliki linked portal yang valid.

## GateLeverController

### Menyiapkan tuas dan gate

1. Buat GameObject tuas dengan `SpriteRenderer`, `Animator`, dan `BoxCollider2D`. Tambahkan `GateLeverController`; component BoxCollider2D wajib dan diubah menjadi trigger serta disesuaikan dengan sprite tuas.
2. Buat objek gate terpisah dengan `SpriteRenderer`. Seret Transform-nya ke field `Gate`.
3. Pastikan gate memiliki collider solid. Jika `Gate Collider` dikosongkan, script mencari BoxCollider2D pada gate; bila tidak ada, script membuat BoxCollider2D pada GameObject SpriteRenderer gate.
4. Pilih `Open Direction` (Up, Down, Right, Left). Atur `Gate Travel Distance`; nilai `0` membuat script menghitung jarak dari ukuran sprite/collider gate ditambah jarak bebas.
5. Pasang clip tuas pada `Lever Animation`. Animator harus ada pada GameObject tuas yang sama dengan script. Controller Animator reguler tidak diperlukan untuk clip ini; script memutarnya melalui Playables.
6. Atur `Clipping Platform` ke collider platform yang membatasi tampilan gate, atau pilih layer yang benar pada `Platform Layer`. Mask clip mencari collider solid di arah pembukaan. Nilai default mask memakai bit layer index 3, bukan otomatis layer bernama `Platform`, jadi atur field ini secara eksplisit jika tidak mengisi `Clipping Platform`.
7. Uji dengan menyentuhkan player ber-`PlayerController` ke trigger tuas. Gate bergerak sekali dengan durasi `Animation Duration`, lalu collider gate dinonaktifkan.

`Gate Sorting Order` mengatur urutan gambar gate. Jika SpriteMask tidak memotong sprite gate seperti yang diharapkan, periksa sorting layer/order dan referensi platform clipping. Trigger mengenali PlayerController pada objek yang menyentuhnya atau parent-nya; tag `Player` tidak diperlukan untuk tuas.

## SeedPlantingController

### Menyiapkan seed dan area tanam

1. Buat GameObject tanah dengan `SpriteRenderer`, `BoxCollider2D`, dan `SeedPlantingController`. Collider tanah menjadi area tanam dan harus `Is Trigger`.
2. Buat GameObject seed dengan visual dan `BoxCollider2D` trigger sebagai area pengambilan. Isi `Seed Object`; isi juga `Seed Pickup Area` jika collider tidak berada langsung pada GameObject seed.
3. Isi `Player` dengan Transform player dan `Player Controller` dengan component player. Jika field controller kosong tetapi Transform telah terisi, script mencarinya pada GameObject tersebut.
4. Isi `Growth Animation` dengan clip yang berisi sprite-frame pertumbuhan dan memiliki frame rate positif. Assign `Soil Renderer`; komponen tanah dicari otomatis pada objek script jika field kosong.
5. `Growth Anchor` bersifat opsional. Tanpa anchor, visual tumbuh memakai Transform soil. Atur `Growth Anchor Offset` untuk titik dasar sprite dan `Growth Visual Scale` untuk ukurannya.
6. `Camera Drag Controller` bersifat opsional. Jika kosong, script mencarinya pada Main Camera. Saat menanam, kamera difokuskan ke tanah jika controller kamera tersedia.
7. Atur `First Frame Delay` (tidak negatif) dan `Frame Interval` (lebih besar dari nol). Nilai awal masing-masing 1 detik dan 0,3 detik.

Interaksi berlangsung otomatis ketika Transform player berada di dalam area seed/tanah dan `PlayerController.CanInteract` bernilai true; tidak ada tombol pengambilan atau penanaman. Pastikan kedua collider area adalah trigger. Seed mengambang sebelum diambil, lalu mengikuti player; setelah berada di area tanah, gerakan player dihentikan sementara dan animasi pertumbuhan diputar.

Sesudah tumbuh selesai, seed disembunyikan, sprite tanah asli disembunyikan, dan sprite frame terakhir pertumbuhan tetap tampil. Jika script dinonaktifkan sebelum selesai, controller membersihkan graph visual sementara, mengembalikan renderer, serta memulihkan gerakan player.

| Field | Fungsi |
| --- | --- |
| `Seed Follow Offset` | Posisi seed relatif ke player saat dibawa. |
| `Seed Follow Smooth Time` / `Seed Follow Max Speed` | Kehalusan dan batas kecepatan seed mengikuti player. |
| `Seed Bob Direction` / `Seed Bob Amplitude` / `Seed Bob Frequency` | Arah, tinggi, dan frekuensi gerak seed sebelum diambil. |
| `Carried Seed Sorting Order Offset` | Urutan render seed relatif terhadap sprite player saat dibawa. |
| `Growth Visual Scale` | Skala visual animasi tumbuh; setiap sumbu harus positif. |

## Checklist Integrasi

- Pastikan layer `Platform` konsisten pada player, enemy yang mengikuti permukaan, portal exit, dan gate clipping.
- Portal dan tuas memakai callback trigger fisika 2D; pastikan salah satu objek pasangan memiliki Rigidbody2D tersimulasi (player biasanya sudah memilikinya). Area seed/tanah diperiksa dengan query overlap, bukan callback trigger.
- Bedakan collider trigger untuk interaksi dari collider solid yang dipakai sebagai pijakan, penghalang, atau tujuan landing.
- Periksa Console setelah konfigurasi; controller seed dan gate menulis error setup secara eksplisit bila referensi wajib tidak tersedia.
