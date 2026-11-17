# Rewild — Rencana & Prompt Musik

Dokumen ini melengkapi `guides/sfx-prompts.md`. Isinya: cara kerja sistem musik yang sudah dipasang,
tempat menaruh file, durasi yang disarankan, dan **prompt bahasa Inggris siap tempel** untuk Home
Screen, Level Selection, dan kumpulan track tiap level (10 level).

Gaya acuan seluruh soundtrack: **cozy 2D nature platformer**, akustik dan organik (kayu, senar,
kulintang/marimba, seruling), hangat, tidak bombastis, tanpa vokal. Semua level harus terasa satu
dunia suara — bedanya ada di warna dan tempo, bukan genre.

---

## 1. Ringkasan sistem (sudah terpasang)

| Bagian | Perilaku |
|---|---|
| Mulai level | Musik mulai saat intro kamera dimulai (zoom in ke tumpukan tanah), bukan saat scene selesai dimuat. |
| Pilih track | Tiap kali scene dibuka, **satu track dipilih acak** dari daftar scene itu, lalu diputar berulang sampai player keluar atau Retry. |
| Intro kamera + 3-2-1 | Musik sudah berjalan selama zoom, tahan, geser ke Webi, zoom out, dan hitung mundur. |
| Pause | Musik **berhenti sementara** (`AudioSource.Pause`), posisinya disimpan, bukan diulang dari awal. |
| Resume | Setelah menekan Resume, musik tetap diam selama hitung mundur 3-2-1, lalu **lanjut dari detik terakhirnya**. |
| Retry | Scene dimuat ulang, jadi track **diundi ulang** — tracknya berubah, dan tetap berbunyi selama 3-2-1 setelah Retry. |
| Pindah scene | Musik scene baru memudar masuk (fade 0,8 detik bawaan), musik lama memudar keluar. |
| Popup mati / menang | Musik tetap berjalan (tidak di-pause). Kalau mau dikecilkan atau dihentikan saat popup, bilang — itu tambahan kecil. |
| Volume | Volume akhir = slider Music × `Volume Scale` entri (0,7) × fade, jadi mute dan slider tetap berlaku. |

File yang mengatur:

| File | Peran |
|---|---|
| `Assets/Resources/MusicLibrary.asset` | **Satu-satunya tempat memasang musik.** 12 entri scene, tiap entri punya daftar track. |
| `Assets/Scripts/Core/MusicLibrary.cs` | Definisi asset (entri per scene: daftar track, volume, pilih acak). |
| `Assets/Scripts/Core/AudioManager.cs` | Pemilih track acak, fade, pause/resume, ganti track antar scene. |
| `Assets/Scripts/Core/SceneMusicAutoPlay.cs` | Memulai musik scene menu secara otomatis. |
| `Assets/Scripts/Core/LevelStartCameraIntro.cs` | Memulai musik level tepat saat intro kamera mulai. |
| `Assets/Scripts/UI/LevelHudController.cs` | Pause musik saat popup pause, resume setelah 3-2-1. |

### Cara memilih track

Tiap entri scene berisi daftar track. Setiap kali scene dibuka, AudioManager mengundi **satu** track
dari daftar itu, memutarnya berulang (loop), dan tidak menggantinya selama player masih di scene
tertentu. Jadi:

- masuk Level 1 → dapat satu track, diputar terus;
- Retry Level 1 → track **diundi ulang** (dan tidak memilih track yang barusan diputar);
- keluar lalu masuk lagi → diundi ulang lagi.

Semua level sekarang berisi **10 track yang sama** (Lush V1/V2, Pine V1/V2, Bare V1/V2, Dry V1,
Dry (Dusk) V2, Night V1/V2), sesuai permintaan: tema apa pun boleh muncul di level mana pun.
Kalau nanti mau musik reaktif (mis. track "danger" saat Fly dekat), itu butuh logika tambahan.

---

## 2. Cara memasang (tanpa mengubah kode)

1. File musik ada di `Assets/Audio/Music/` dan SFX di `Assets/Audio/SFX/`.
2. **`Assets/Resources/MusicLibrary.asset` sudah terisi** untuk 12 entri: `Home Screen`,
   `Level Selection`, `Level 1` … `Level 10`.
3. Tiap level berisi 10 track; `Volume Scale` = 0,7 dan `Random Track` aktif, jadi satu track dipilih
   acak setiap level dibuka.
4. Kalau mau menambah atau mengganti track: seret clip ke slot `Tracks` di entri scene yang dituju,
   atau klik `+`. Slot kosong dilewati.
5. Play dari **Home Screen**, lalu masuk level, pause, resume, dan retry untuk mengecek perilakunya.

Catatan:

- Nama entri harus **sama persis** dengan nama scene (mis. `Level 1`, bukan `Level1`).
- Slot yang masih kosong dilewati otomatis, jadi boleh mengisi 2–3 track dulu.
- Scene `Level 1 Intro` sengaja tanpa entri karena suaranya berasal dari video intro.
- Menambah level baru = tambah satu entri baru di asset ini, tidak perlu menyentuh scene.
- Jangan memindahkan atau mengganti nama asset `MusicLibrary`; AudioManager memuatnya lewat
  `Resources.Load("MusicLibrary")`.

### Setting impor yang disarankan

| Properti | Nilai |
|---|---|
| Format file | OGG Vorbis (musik), 44,1 kHz, stereo |
| Load Type | `Streaming` untuk track panjang, `Compressed In Memory` kalau ingin hemat disk I/O |
| Compression Format | `Vorbis`, Quality 70–80 |
| Preload Audio Data | mati kalau `Streaming` |
| Loop | **jangan** pakai Loop di clip; loop diatur AudioManager per track |
| Loudness | sekitar −18 LUFS, peak di bawah −1 dBTP, tanpa fade di ujung kalau ingin loop mulus |

Durasi & format seluruh soundtrack:

| Kategori | Durasi tiap track | Loop | BPM | Catatan |
|---|---|---|---|---|
| Home Screen | 80 s | seamless | 76 | Hangat, tidak ramai; diputar berulang lama. |
| Level Selection | 80 s | seamless | 88 | Ceria dan mengundang, ada rasa "ayo mulai". |
| Level 1–10 | 70–91 s | seamless | 68–104 | 10 track per level; satu dipilih acak tiap masuk. |

**Kenapa 70–90 detik:** satu level Rewild selesai dalam 1–3 menit, jadi loop 80 detik berarti pemain
mendengar track yang sama paling banyak 2–3 kali sebelum pindah level. Loop lebih pendek dari 60 detik
terasa berulang, lebih panjang dari 2 menit membuang kerja karena jarang terdengar penuh.

**Kenapa 10 detik pertama harus lengang:** musik mulai bersamaan dengan zoom kamera dan hitung mundur
3-2-1. Kalau track langsung ramai, bunyi hitungan dan suara intro tertimbun. Prompt di bawah sudah
meminta bagian awal yang lapang.

---

## 3. Prompt menu

### Home Screen — `mus_home_rewild.ogg` (80 s, 76 BPM)

Latar Home Screen: layar biru senja gelap dengan papan kayu, logo Rewild, dan maskot laba-laba.
Musiknya harus terasa seperti "pintu masuk hutan", bukan lagu level.

> Cozy 2D nature platformer main menu music, warm and inviting dusk forest mood, felt piano and
> kalimba melody over soft nylon guitar arpeggios, gentle frame drum and shaker, warm low strings,
> 76 BPM, 80 seconds, seamless loop, sparse first 10 seconds while a logo fades in, no vocals, no
> drums kit, no sudden loud hits, 44.1 kHz stereo.

Alternatif kalau ingin lebih misterius: ganti "warm and inviting" jadi "quietly mysterious, like a
forest at blue hour" dan tambahkan `music box, low pad`.

### Level Selection — `mus_menu_rewild.ogg` (80 s, 88 BPM)

Latar Level Selection: lembah cerah, sungai dan air terjun, tunggul pohon dengan tunas muda —
tema "menghijaukan kembali". Musiknya harus terasa seperti janji pemulihan.

> Hopeful 2D platformer level-select music, sunny valley with a river, bright marimba and ukulele
> strums, playful pizzicato strings, light hand percussion, warm bass, 88 BPM, 80 seconds, seamless
> loop, upbeat but relaxed, no vocals, no heavy drums, no harsh synth leads, 44.1 kHz stereo.

---

## 4. Prompt per level (referensi kalau mau menambah track)

Isian yang terpasang sekarang memakai 10 track tema yang sama di setiap level, jadi tabel di bawah
tidak lagi menggambarkan isi library — pakai sebagai **ide** kalau nanti mau menambah track baru untuk
level tertentu. Pola nadanya masih berguna:

1. **Main** — tema utama, aransemen penuh.
2. **Wander** — variasi lebih lapang, tanpa melodi utama; enak didengar berulang saat pemain mengulang.
3. **Tension** — lebih gelap/tegang, untuk bagian berbahaya (musuh, spike, oil).
4. **High** — rasa kagum saat memanjat kanopi atau melintasi platform jauh.
5. **Calm** — tenang dan reflektif, mendekati akhir level.

Semua prompt di bawah memuat BPM dan durasi, plus "sparse first 10 seconds" supaya intro kamera dan
hitung mundur 3-2-1 tetap terdengar.

### Level 1 — Lush (hutan tropis cerah, `bg_forest_01_lush`)

Latar: langit biru cerah, kanopi bulat berlapis hijau tua–muda, pegunungan biru di kejauhan.
Kesan: segar, aman, gembira. Level pertama, jadi musiknya paling mudah diingat.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 75 s, 84 BPM | "Sunny lush forest platformer theme, bouncy marimba melody with ukulele and warm bass, brushed shaker, light claps, cheerful and adventurous, 84 BPM, 75 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 75 s, 84 BPM | "Lush forest exploration variation, kalimba and fingerpicked acoustic guitar with soft pads, no lead melody, gentle shaker pulse, relaxed and curious, 84 BPM, 75 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 70 s, 90 BPM | "Playful but tense forest chase cue, low pizzicato strings and muted marimba with ticking woodblock and soft low drum, sneaky and alert, not scary, 90 BPM, 70 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 80 s, 80 BPM | "Wonder-filled canopy theme, airy flute over harp arpeggios and warm sustained strings, wide and floating, gentle cymbal swells, 80 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 80 s, 76 BPM | "Peaceful forest resolve, felt piano with soft guitar harmonics and a warm pad, distant birds suggested by tiny wooden percussion, 76 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 2 — Pine (hutan pinus, `bg_forest_02_pine`)

Latar: perbukitan hijau dengan pohon pinus segitiga berlapis, langit biru dingin.
Kesan: rapi, berirama, sedikit lebih "hidup" daripada Level 1.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 75 s, 92 BPM | "Pine forest platformer theme, plucked strings and clarinet melody over walking bass, log drum and shaker groove, crisp and lively, 92 BPM, 75 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 75 s, 92 BPM | "Pine forest wandering variation, steady nylon guitar pattern with woodblock and soft bass, minimal melody, focused and calm, 92 BPM, 75 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 70 s, 98 BPM | "Alert pine forest cue, staccato low strings with wooden percussion and a repeating muted whistle note, tense but still playful, 98 BPM, 70 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 80 s, 84 BPM | "High pine ridge theme, warm pan flute over open fifths and slow string pads, breezy and expansive, light tambourine, 84 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 80 s, 78 BPM | "Quiet pine grove, soft felt piano and cello with a breathy flute line, very gentle, 78 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 3 — Bare (tanah gundul, `bg_forest_03_bare`)

Latar: langit hijau pucat, bukit rumput kusam, tunggul pohon, hanya sedikit pohon tersisa.
Kesan: sayu, sunyi, tetapi belum putus asa. Ini level paling "hening" di awal game.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 80 s, 72 BPM | "Melancholic bare hillside platformer theme, sparse felt piano with low cello and a lonely whistle melody, soft wind texture, 72 BPM, 80 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 80 s, 72 BPM | "Empty hillside exploration, muted guitar harmonics and low pad with occasional wooden tick, lots of space, 72 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 72 s, 84 BPM | "Uneasy bare ground cue, low tremolo strings and sparse frame drum, distant dissonant piano note, cold and watchful, 84 BPM, 72 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 84 s, 68 BPM | "Quiet wonder on a bare ridge, glassy celesta and warm strings with slow breathing pads, wide and lonely, 68 BPM, 84 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 84 s, 66 BPM | "Gentle after-rain calm, soft piano with cello swells and a single sustained flute note, hopeful underneath the sadness, 66 BPM, 84 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 4 — Dry (musim kering, `bg_forest_04_dry`)

Latar: langit oranye pekat, bukit cokelat keemasan, pohon-pohon bergerigi tanpa daun.
Kesan: hangat, berdebu, senja panjang. Musiknya bernuansa gurun, bukan hutan.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 75 s, 80 BPM | "Warm dry savanna platformer theme, nylon guitar and kalimba over soft hand drums, dusty shaker, mellow bass, adventurous with a bittersweet edge, 80 BPM, 75 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 75 s, 80 BPM | "Sun-baked exploration, muted guitar chords with dry seed-pod percussion and a low drone, sparse and patient, 80 BPM, 75 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 70 s, 88 BPM | "Heat haze tension cue, low ostinato strings with tight frame drum and a thin whistle, uneasy and dry, 88 BPM, 70 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 80 s, 76 BPM | "Golden hour overlook theme, warm brass-like pad with harp and airy flute, expansive and glowing, 76 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 80 s, 72 BPM | "Dusk in the dry woods, soft guitar and low strings with a slow heartbeat drum, reflective and warm, 72 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 5 — Night (hutan malam, `bg_forest_06_night`)

Latar: langit biru malam, bulan sabit, bintang, kanopi teal gelap berlapis.
Kesan: misterius, tenang, sedikit ajaib. Perkenalan suasana malam sebelum level-level akhir.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 80 s, 68 BPM | "Mysterious night forest platformer theme, music box melody with celesta and deep warm pads, soft brushed percussion, quiet bass pulse, 68 BPM, 80 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 80 s, 68 BPM | "Night forest wandering, low pad and kalimba fragments with a slow ticking pulse, spacious and calm, 68 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 72 s, 76 BPM | "Night creature alert, low strings and muted celesta with soft irregular percussion, suspenseful but gentle, 76 BPM, 72 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 84 s, 64 BPM | "Moonlit canopy theme, shimmering celesta over floating string pads and a breathy flute, dreamlike and wide, 64 BPM, 84 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 84 s, 60 BPM | "Starlit lullaby, music box with warm cello and a very slow heartbeat drum, intimate and safe, 60 BPM, 84 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 6 — Lush, second pass (hutan tropis, `bg_forest_01_lush`)

Latar sama dengan Level 1, tetapi pemain sudah lebih mahir. Musiknya versi "lebih dewasa" dari tema
Level 1 — lebih penuh, lebih bergerak, masih ramah.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 75 s, 96 BPM | "Bright lush forest theme, fuller arrangement than before, marimba and ukulele trading melody over walking bass, hand claps and shaker, confident and joyful, 96 BPM, 75 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 75 s, 96 BPM | "Lush forest groove without lead melody, fingerpicked guitar and kalimba ostinato with light percussion, steady and motivating, 96 BPM, 75 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 70 s, 104 BPM | "Fast playful danger cue in a green forest, staccato strings and marimba with driving woodblock, energetic but not aggressive, 104 BPM, 70 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 80 s, 88 BPM | "High canopy adventure theme, flute and harp runs over warm strings, uplifting and airy, 88 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 80 s, 80 BPM | "Warm forest rest, soft piano and guitar with a humming pad and distant wooden percussion, content and settled, 80 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 7 — Pine, second pass (hutan pinus, `bg_forest_02_pine`)

Latar sama dengan Level 2. Versi paling energik dari tema pinus: langkah cepat, ritme tegas, cocok
untuk level yang menuntut ayunan jaring beruntun.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 70 s, 104 BPM | "Driving pine forest platformer theme, clarinet and plucked strings over a pulsing bass, tight log drum and shaker groove, brisk and confident, 104 BPM, 70 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 70 s, 104 BPM | "Pine forest momentum loop, repeating plucked pattern with light percussion and no melody, focused and rhythmic, 104 BPM, 70 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 70 s, 112 BPM | "Sharp pine forest alert cue, low staccato strings with urgent woodblock and a rising whistle, fast and nervous, 112 BPM, 70 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 80 s, 96 BPM | "Windy pine ridge theme, pan flute and harp over open chords, tambourine pulse, soaring and free, 96 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 80 s, 84 BPM | "Sheltered pine calm, soft piano and cello with a quiet flute, breathing slowly, 84 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 8 — Bare, regrowth (tanah gundul, `bg_forest_03_bare`)

Latar sama dengan Level 3, tetapi kini terasa seperti awal pemulihan. Musiknya bertolak dari
kesunyian Level 3 dan perlahan menghangat — tunas pertama.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 80 s, 78 BPM | "Hopeful regrowth theme on bare hills, felt piano with warm string swells and a gentle whistle melody, soft frame drum, bittersweet but rising, 78 BPM, 80 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 80 s, 78 BPM | "First shoots exploration, kalimba and guitar harmonics over a soft pad, no lead melody, patient and warm, 78 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 72 s, 88 BPM | "Careful cue on recovering land, low tremolo strings and sparse drum with a muted piano note, tense yet hopeful, 88 BPM, 72 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 84 s, 74 BPM | "Growing canopy wonder, glassy celesta and warm strings with light harp, gently uplifting, 74 BPM, 84 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 84 s, 70 BPM | "Quiet gratitude, soft piano with cello and a single flute line, peaceful and full, 70 BPM, 84 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 9 — Dusk (senja, `bg_forest_05_dusk`)

Latar: langit merah muda–jingga dengan matahari rendah, kanopi teal dan ungu.
Kesan: golden hour, reflektif, sedikit dramatis. Musiknya paling "sinematik lembut".

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 85 s, 72 BPM | "Golden hour forest platformer theme, warm harp and electric-piano-like felt keys with lush string pads, soft shaker, nostalgic and glowing, 72 BPM, 85 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 85 s, 72 BPM | "Dusk wandering, slow guitar arpeggios and warm pad with a distant bell, spacious and dreamy, 72 BPM, 85 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 75 s, 82 BPM | "Twilight unease, low strings and muted piano with a soft insistent pulse, suspense with warmth, 82 BPM, 75 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 85 s, 68 BPM | "Sunset ridge theme, soaring warm strings and flute over harp, cinematic but intimate, 68 BPM, 85 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 85 s, 64 BPM | "Last light, soft piano and cello with a hummed-like pad (no voice), deeply calm, 64 BPM, 85 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

### Level 10 — Night finale (hutan malam, `bg_forest_06_night`)

Latar sama dengan Level 5, tetapi ini puncak permainan. Musiknya memakai warna malam Level 5 dan
menambah tekad: lebih dalam, lebih berlapis, tanpa kehilangan kehangatan.

| # | Track | Durasi | Prompt (Inggris) |
|---|---|---|---|
| 1 | Main | 90 s, 88 BPM | "Finale night forest platformer theme, deep warm strings and piano with music box motif, confident frame drum pulse and low bass, determined and magical, 88 BPM, 90 seconds, seamless loop, sparse first 10 seconds, no vocals, 44.1 kHz stereo." |
| 2 | Wander | 90 s, 88 BPM | "Night finale momentum, driving low strings and kalimba ostinato with restrained percussion, no lead melody, purposeful, 88 BPM, 90 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 3 | Tension | 80 s, 96 BPM | "Final trial alert, urgent low strings and tight drums with dissonant celesta accents, dramatic but not horror, 96 BPM, 80 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 4 | High | 90 s, 80 BPM | "Moonlit summit theme, wide string pads, harp and flute with shimmering celesta, awe and release, 80 BPM, 90 seconds, seamless loop, no vocals, 44.1 kHz stereo." |
| 5 | Calm | 90 s, 72 BPM | "After the climb, gentle piano and warm strings with a slow music box line, grateful and complete, 72 BPM, 90 seconds, seamless loop, no vocals, 44.1 kHz stereo." |

---

## 5. Ringkasan durasi (untuk ditempel di dokumen kerja)

| Scene | Jumlah track | Durasi tiap track | Total musik |
|---|---|---|---|
| Home Screen | 1 | 80 s | 1:20 |
| Level Selection | 1 | 80 s | 1:20 |
| Level 1 | 5 | 70–80 s | ±6:20 |
| Level 2 | 5 | 70–80 s | ±6:20 |
| Level 3 | 5 | 72–84 s | ±6:40 |
| Level 4 | 5 | 70–80 s | ±6:20 |
| Level 5 | 5 | 72–84 s | ±6:40 |
| Level 6 | 5 | 70–80 s | ±6:20 |
| Level 7 | 5 | 70–80 s | ±6:20 |
| Level 8 | 5 | 72–84 s | ±6:40 |
| Level 9 | 5 | 75–85 s | ±7:00 |
| Level 10 | 5 | 80–90 s | ±7:00 |

Total ±75 menit musik. Kalau ingin lebih ringkas, cukup isi 3 track per level lebih dulu
(main, tension, calm) — slot kosong otomatis dilewati.

---

## 6. Kode yang sudah disiapkan

### Memulai musik level (dari intro kamera)

```csharp
// LevelStartCameraIntro.PlayIntro() / PlayRetryIntro()
isRunning = true;

// Musik scene dimulai di sini, bukan saat scene dimuat, supaya tepat saat zoom ke tumpukan tanah mulai.
StartSceneMusic();

FreezeWorld();
```

```csharp
private void StartSceneMusic()
{
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlaySceneMusic(gameObject.scene.name);
    }
}
```

### Pause dan resume di HUD

```csharp
// LevelHudController.Pause()
isPaused = true;
Time.timeScale = 0f;
AudioManager.Instance.PauseMusic();   // berhenti sementara, posisi disimpan

// LevelHudController.FinishResume() — dipanggil setelah 3-2-1 selesai
isPaused = false;
Time.timeScale = 1f;
AudioManager.Instance.ResumeMusic();  // lanjut dari detik terakhir
```

### Playlist di AudioManager

```csharp
public void PlaySceneMusic(string sceneName); // undi satu track dari daftar scene, lalu loop
public void PauseMusic();                     // AudioSource.Pause(), posisi tetap
public void ResumeMusic();                    // AudioSource.UnPause()
```

`PlaySceneMusic` dipanggil setiap scene dibuka (termasuk Retry), jadi selalu mengundi track baru.
Track terakhir per scene disimpan di memori supaya track yang sama tidak muncul dua kali berturut-turut.
Volume akhir = slider Music × `Volume Scale` entri (0,7) × fade.

### Alur yang perlu diketahui saat menambah level

Tidak ada yang perlu diubah di scene. Cukup:

1. Tambah entri baru di `MusicLibrary.asset` dengan nama scene yang persis sama.
2. Isi track-nya.
3. Scene level baru otomatis memakai intro kamera yang sudah ada, jadi musik ikut mulai di intro.
