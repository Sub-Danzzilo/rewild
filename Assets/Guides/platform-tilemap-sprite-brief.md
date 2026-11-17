# Brief Sprite Tilemap Platform

Dokumen ini menjadi acuan nama dan deskripsi sprite untuk generate aset AI, lalu menyusunnya sebagai Tile Palette di Unity.

## Kondisi aset sekarang

- Prefab [Platform.prefab](../Prefabs/Environment/Platform.prefab) memiliki satu `BoxCollider2D` untuk badan platform dan visual terpisah untuk tanah serta lapisan rumput di atas, bawah, kiri, dan kanan.
- [env_ground_base.png](../Art/Environment/env_ground_base.png) adalah tekstur tanah/batu untuk badan platform.
- [env_surface_grass.png](../Art/Environment/env_surface_grass.png) berisi strip rumput panjang. Sprite utama bernama `Lapisan_Rumput_0`.
- [env_surface_oil.png](../Art/Environment/_Unused/env_surface_oil.png) berisi strip oli panjang (`Lapisan_Oli_2`) dan dua potongan kecil. Aset ini saat ini berada di `_Unused`.
- [env_surface_ice.png](../Art/Environment/_Unused/env_surface_ice.png) berisi strip es/salju panjang (`Lapisan_Salju_0`) dan satu potongan kecil. Aset ini saat ini berada di `_Unused`.
- Aset strip tersebut bukan set tile persegi lengkap. Tile AI sebaiknya dibuat sebagai potongan modular yang bisa disambung, bukan satu strip sepanjang platform.

## Nama dan deskripsi sprite

Pakai nama konsisten dengan pola `tile_<material>_<bagian>[_<varian>]` (huruf kecil semua, pemisah garis bawah). `ground` adalah material netral di bawah lapisan permukaan; `<material>` untuk tipe permukaan adalah `grass`, `oil`, atau `ice`. Nama file dan nama aset di Unity harus sama persis.

Ketiga material boleh muncul pada sisi platform mana pun yang terbuka: atas, bawah, kiri, dan kanan. Variasi seperti “hanya atas”, “atas dan bawah”, atau “semua sisi” adalah hasil susunan tile mengikuti bentuk platform, **bukan sprite kombinasi khusus**. Tile edge yang menghadap luar diputar sesuai arah sisi.

| Nama sprite | Deskripsi / penggunaan |
|---|---|
| `tile_ground_fill` | Tile persegi untuk badan tanah/batu. Tekstur netral yang dapat dipakai bersama oleh platform rumput, oli, dan es. Dipasang pada Tilemap dasar. |
| `tile_grass_edge` | Segmen tepi lurus satu cell dengan lapisan rumput di sisi yang menghadap keluar dan area transparan ke arah fill. Putar untuk sisi atas, bawah, kiri, atau kanan. |
| `tile_oil_edge` | Segmen tepi lurus satu cell dengan lapisan oli gelap dan mengilap di sisi yang menghadap keluar. Putar untuk semua arah; bentuknya harus terbaca sebagai lapisan licin. |
| `tile_ice_edge` | Segmen tepi lurus satu cell dengan lapisan es/salju kebiruan di sisi yang menghadap keluar. Putar untuk semua arah. |
| `tile_grass_corner_outer` | Sudut luar cembung untuk menyambungkan dua tepi rumput yang berbelok. Putar untuk keempat pojok. |
| `tile_oil_corner_outer` | Sudut luar cembung oli yang menyambungkan dua tepi oli. Putar untuk keempat pojok. |
| `tile_ice_corner_outer` | Sudut luar cembung es yang menyambungkan dua tepi es. Putar untuk keempat pojok. |
| `tile_grass_corner_inner` | Sudut dalam cekung untuk lekukan pada tepi rumput. Putar untuk arah lekukan yang diperlukan. |
| `tile_oil_corner_inner` | Sudut dalam cekung untuk lekukan pada tepi oli. Putar untuk arah lekukan yang diperlukan. |
| `tile_ice_corner_inner` | Sudut dalam cekung untuk lekukan pada tepi es. Putar untuk arah lekukan yang diperlukan. |

### Paket generate yang disarankan

Untuk uji bentuk platform persegi sederhana, generate dulu:

1. `tile_ground_fill`
2. `tile_grass_edge`
3. `tile_oil_edge`
4. `tile_ice_edge`
5. Satu `tile_<material>_corner_outer` untuk tiap material yang akan diuji.

Tambahkan `tile_<material>_corner_inner` untuk tiap material jika bentuk level memiliki lekukan. Jika tiap material memang punya badan/fill berbeda, buat fill per material (`tile_grass_fill`, dst.); jika tidak, gunakan satu `tile_ground_fill` bersama.

Tilemap menyusun kombinasi permukaan dari tile dasar dan tile tepi pada layer terpisah. Contoh konsep, `G` adalah sel tepi rumput dan `F` adalah fill:

```text
Hanya sisi atas:        Atas dan bawah:        Semua sisi:
  GGG                     GGG                   GGG
  FFF                     FFF                   GFG
                          GGG                   GGG
```

Mengganti `G` dengan edge oli atau es menghasilkan variasi material yang sama. Tidak perlu membuat sprite bernama seperti `tile_grass_toponly` atau `tile_ice_allsides`.

## Ukuran dan bentuk tile yang direkomendasikan

Sebagai titik awal, gunakan **cell Tilemap 1 x 1 Unity unit**. Ini cocok dengan ukuran cell standar Grid Unity dan cukup praktis untuk menyusun level. Pertahankan `Pixels Per Unit` 100 seperti mayoritas sprite environment saat ini, sehingga satu tile penuh berukuran **100 x 100 piksel**. Ukuran ini rekomendasi awal, bukan batas wajib; uji dulu di satu scene bersama Webi dan platform yang ada. Jika tile terlihat terlalu besar atau kecil, ubah Grid/PPU secara konsisten sebelum membuat seluruh set.

Buat edge sebagai potongan modular pendek berukuran 100 x 100 piksel per cell, lalu ulangi di sepanjang tepi. Pasang `tile_ground_fill` pada Tilemap dasar dan tile permukaan pada Tilemap overlay di Grid yang sama. Edge overlay hanya menggambar lapisan permukaan di sisi cell yang menghadap keluar; bagian transparannya membiarkan fill di bawahnya terlihat. Untuk sisi kiri/kanan dan orientasi sudut, putar tile 90 derajat sesuai kebutuhan.

```text
Tile pendek yang diulang:
Fill:       [ F ][ F ][ F ][ F ][ F ]
Edge atas:  [ E ][ E ][ E ][ E ][ E ]
              satu tile = satu cell

Satu strip panjang:
Edge atas:  [             E             ]
              satu sprite menutup seluruh sisi
```

Pilih **tile pendek yang diulang**, bukan satu strip sepanjang platform. Dengan begitu panjang platform bebas berubah, dan tile bisa dipakai kembali di banyak level. `E` adalah edge material (grass/oil/ice); `F` adalah fill tanah. Pakai tile corner outer/inner untuk menyambungkan tepi yang berbelok.

## Batas gambar dan transparansi

- Minta output **PNG RGBA dengan alpha transparan sungguhan**; jangan gunakan latar putih atau pola kotak-kotak sebagai pengganti transparansi.
- Untuk fill, isi kanvas 100 x 100 piksel sampai ke keempat batas dan buat teksturnya seamless ketika diulang. Jangan sisakan bingkai transparan di sekeliling fill.
- Untuk edge, rapatkan siluet tepi ke batas luar cell pada sisi yang memang harus tersambung/menempel. Ujung kiri dan kanan harus bisa bertemu tanpa celah atau garis sambungan. Arahkan fitur material ke luar cell; area transparan mengarah ke badan platform.
- Hindari padding transparan yang tidak disengaja di luar siluet, karena itu membuat tile tampak renggang. Transparansi **di dalam** cell edge diperlukan agar badan/fill platform di bawahnya terlihat; jangan crop bagian transparan yang memang membentuk area kosong tersebut.
- Jika generator tidak bisa mengontrol ukuran tepat, minta satu tile saja dengan rasio 1:1, lalu ekspor atau crop ke kanvas persegi 100 x 100 piksel dengan mempertahankan bagian visual dan alpha yang memang diperlukan. Jangan gabungkan banyak varian dalam satu gambar karena hasilnya perlu dipotong manual.
- Sebelum generate satu set penuh, uji satu fill dan satu edge yang diulang beberapa kali di Unity untuk mengecek skala, sambungan, dan transparansi.

## Panduan prompt AI

Gunakan arahan visual yang sama untuk seluruh set agar sprite konsisten:

- 2D game platformer, gaya ilustrasi dan tingkat detail konsisten dengan aset Rewild yang sudah ada.
- Untuk edge: satu segmen tepi lurus satu cell, kanvas persegi 1:1, material menempel pada satu sisi luar cell, PNG RGBA transparan. Buat edge dapat diputar 90 derajat tanpa mengubah gaya.
- Untuk sudut: satu tile sudut outer atau inner yang menyambungkan dua edge material sama; tiap tile sudut dapat diputar 90 derajat untuk orientasi lain.
- Untuk fill: satu tile persegi 1:1 yang teksturnya seamless diulang ke segala arah, PNG RGBA.
- Satu potongan tile modular, bukan platform panjang, bukan mockup level, tanpa karakter, teks, bayangan jatuh, atau objek tambahan.
- Jangan sisakan margin/padding transparan yang tidak disengaja di luar area gambar; untuk edge dan sudut, pertahankan area transparan di dalam tile yang dibutuhkan agar fill terlihat.
- Ujung kiri dan kanan edge harus bisa disambungkan seamless saat tile diulang; untuk fill, keempat sisi harus seamless.
- Hasil AI perlu diperiksa dan dirapikan manual sebelum diimpor; jangan mengandalkan seamlessness hasil generasi tanpa pengecekan.

### Arahan khusus material

- **Grass:** lapisan tanah dengan rumput hijau lebat di tepi yang menghadap luar. Hindari bunga atau objek besar yang membuat pola tile terlalu kentara.
- **Oil:** lapisan cairan oli hitam/ungu gelap dengan highlight kecil mengilap. Jaga siluet tepi mudah diulang dan jangan menambahkan percikan besar di setiap tile.
- **Ice:** lapisan es/salju putih-biru dengan highlight dingin dan beberapa bentuk es kecil. Hindari stalaktit besar yang berulang mencolok.
- **Ground fill:** batu dan tanah cokelat dengan variasi ukuran batu, kontras yang cukup untuk terbaca tetapi tidak terlalu ramai.

## Thorn: hazard terpisah, bukan tipe permukaan platform

**Rekomendasi: jangan masukkan thorn ke set tile permukaan platform utama.** Thorn berfungsi sebagai hazard, bukan material pijakan seperti rumput, oli, atau es. Aset [env_surface_thorn.png](../Art/Environment/env_surface_thorn.png) berupa strip duri panjang, dan prefab [Duri.prefab](../Prefabs/Environment/Duri.prefab) memiliki `PolygonCollider2D`. Memisahkannya menjaga aturan collider dan gameplay hazard tetap jelas, serta mencegah tile duri terpasang tidak sengaja sebagai permukaan aman.

Jika level memang membutuhkan duri yang disusun berulang lewat Tilemap, siapkan **palette hazard terpisah**, misalnya:

| Nama sprite | Deskripsi / penggunaan |
|---|---|
| `tile_thorn_straight` | Potongan duri lurus seamless untuk deretan hazard. |
| `tile_thorn_end_left` | Ujung kiri deretan duri, dengan pangkal tertutup rapi. |
| `tile_thorn_end_right` | Ujung kanan deretan duri, dengan pangkal tertutup rapi. |
| `tile_thorn_corner` | Belokan deretan duri, hanya jika level memerlukannya. |

Gunakan Tilemap/collider hazard tersendiri dan pastikan konfigurasi collider-nya tetap memberikan perilaku hazard yang sama dengan prefab `Duri`. Kalau duri hanya dipasang sesekali sebagai jebakan tetap, prefab yang ada lebih sederhana dan lebih aman dipakai daripada membuatnya tile.
