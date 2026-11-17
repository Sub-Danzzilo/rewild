# Rewild — Rencana & Prompt SFX

Panduan ini berisi daftar SFX yang dipakai game, **prompt bahasa Inggris** siap tempel ke generator
SFX AI (ElevenLabs Sound Effects, Stable Audio, AudioGen, dsb.), durasi yang disarankan, nama file,
serta field volume yang sudah tersedia di script. Kamu tidak perlu mengubah kode lagi: setelah clip
diimpor, tinggal seret ke field yang disebut di tabel.

Semua SFX diputar lewat `AudioManager.PlaySfx(clip, minPitch, maxPitch, volumeScale)`, jadi volume
per-SFX di sini **dikalikan** dengan volume SFX global dari Settings (slider + tombol mute tetap
berlaku).

---

## 1. Ringkasan cepat

| # | SFX | File yang disarankan | Durasi | Volume awal | Field di Inspector |
|---|---|---|---|---|---|
| 1 | Hover tombol kayu | `sfx_ui_hover_wood.wav` | **0,06–0,12 s** (pakai 0,09 s) | 0,40 | `UIButtonInteractive → Hover Volume` |
| 2a | Webi menembakkan jaring | `sfx_webi_web_shoot.wav` | **0,18–0,30 s** (0,25 s) | 0,70 | `PlayerController → Web Shoot Sfx` |
| 2b | Webi mulai tertarik jaring | `sfx_webi_web_pull.wav` | **0,35–0,60 s** (0,45 s) | 0,70 | `PlayerController → Web Pull Sfx` |
| 3 | Tanaman tumbuh di tumpukan tanah | `sfx_seed_grow.wav` | **2,2–2,6 s** (2,4 s) | 0,70 | `SeedPlantingController → Growth Sfx` |
| 4a | Moving ground mulai bergerak | `sfx_moving_ground_start.wav` | **0,5–0,8 s** (0,7 s) | 0,70 | `MovingPlatform → Move Start Sfx` |
| 4b | Moving ground berhenti | `sfx_moving_ground_stop.wav` | **0,4–0,6 s** (0,5 s) | 0,70 | `MovingPlatform → Move Stop Sfx` |
| 5 | Tuas/gerbang kebuka | `sfx_lever_gate_open.wav` | **0,6–1,0 s** (0,8 s) | 0,80 | `GateLeverController → Gate Open Sfx` |
| 6 | Portal | `sfx_portal_teleport.wav` | **0,6–0,9 s** (0,75 s) | 0,80 | `PortalController → Teleport Sfx` |
| + | Ambil benih (sudah ada field) | `sfx_seed_pickup.wav` | 0,25–0,35 s | 1,00 | `SeedPlantingController → Pickup Sfx` |
| + | Klik tombol (sudah ada field) | `sfx_ui_click_wood.wav` | 0,10–0,15 s | 0,60 | `UIButtonInteractive → Active Sound` |

Semua angka volume di atas hanya titik awal; atur sesuai campuran akhir.

### Kenapa durasinya segitu

- **Hover** harus selesai sebelum kursor bisa pindah tombol lagi, jadi di bawah 0,12 s. Lebih dari
  itu terasa "menempel" dan mengganggu saat menyapu kursor di deretan tombol.
- **Tembakan jaring** mengikuti waktu ujung jaring melesat (`webShootSpeed = 25`), jadi ±0,2–0,3 s.
  Tarikan (`webPull`) lebih panjang karena perpindahan badan Webi makan waktu; 0,45 s terasa
  "kenyal" tanpa menutupi suara pendaratan.
- **Tumbuh tanaman**: jeda frame pertama 1 s, lalu 8 frame × 0,3 s = **2,4 s**. Clip 2,4 s pas
  dengan durasi visual; jangan ada hentakan keras di awal karena kamera sedang zoom ke tanah.
- **Moving ground**: satu clip pendek untuk "mulai" dan satu untuk "berhenti" (bukan loop) karena
  platform memutar SFX hanya saat statusnya berubah. Kalau nanti mau loop rumble terus-menerus,
  bilang saja — itu butuh AudioSource terpisah di prefab.
- **Tuas/gerbang**: animasi gerbang `animationDuration = 0,5 s` + animasi tuas; 0,8 s menutupi
  keduanya. Letakkan bunyi "klik mekanisme" di awal dan gesekan batu di ekor.
- **Portal**: efek teleport hanya 0,6–0,9 s supaya tidak menabrak SFX lain saat Webi langsung
  bergerak setelah keluar portal.

---

## 2. Prompt siap tempel (Inggris)

Gaya acuan untuk semua SFX: **cozy 2D nature platformer**, bahan kayu/tanah/serat organik, hangat,
tidak agresif, tanpa musik dan tanpa suara manusia. Selalu minta **mono**, dan **tanpa reverb**
kecuali disebut lain.

### 1) Hover tombol kayu — `sfx_ui_hover_wood.wav`

> Single soft wooden UI hover tick for a cozy 2D platformer, one light fingertip tap on a small
> hollow wooden plank, warm midrange knock with a tiny hollow resonance, dry and close-miked, no
> reverb, no music, no voice, very short, clean fast decay, about 0.09 seconds, mono, 3 variations.

Negatif (kalau tool-nya punya kolom negative prompt):
`music, melody, voice, reverb, echo, metallic clang, loud transient, long tail`

### 2a) Webi menembakkan jaring — `sfx_webi_web_shoot.wav`

> Short elastic web-shot whoosh, a sticky silk thread snapping forward through the air, quick
> rising swish with a soft silky friction tail, tactile and organic, small and close, no metallic
> sounds, no music, no voice, about 0.25 seconds, mono, 3 variations.

### 2b) Webi mulai tertarik jaring — `sfx_webi_web_pull.wav`

> Elastic web pull, a taut silk fiber stretching as a small creature is yanked forward, rising
> tension with a soft air swoosh and a light snap at the end, springy and organic, tactile, no
> metallic, no music, no voice, about 0.45 seconds, mono, 3 variations.

### 3) Tanaman tumbuh — `sfx_seed_grow.wav`

> Gentle magical plant growth, soft soil rustle with tender leaves unfurling, warm organic shimmer
> and a tiny wooden creak of a stem stretching upward, calm and cozy, slow build with a soft
> finish, no loud transient, no music, no voice, about 2.4 seconds, mono, 2 variations.

Catatan: hindari "sparkle" berlebihan — di level ini tumbuh tanaman terasa alami, bukan sihir kartun.

### 4a) Moving ground mulai bergerak — `sfx_moving_ground_start.wav`

> Heavy old stone-and-root platform grinding into motion, low earthy rumble with crumbling dirt and
> a soft stone scrape, slow believable weight, smooth start with no hard hit at the beginning, no
> music, no voice, about 0.7 seconds, mono.

### 4b) Moving ground berhenti — `sfx_moving_ground_stop.wav`

> Heavy stone platform settling to a stop, low rumble fading out with a soft thud of dirt and
> pebbles, weighty and grounded, no metallic, no music, no voice, about 0.5 seconds, mono.

### 5) Tuas/gerbang kebuka — `sfx_lever_gate_open.wav`

> Old wooden lever pulled with a chunky mechanical clunk, short dry wooden creak, then a metal
> latch releasing and a heavy stone gate starting to grind, satisfying and tactile, close-miked, no
> music, no voice, about 0.8 seconds, mono.

### 6) Portal — `sfx_portal_teleport.wav`

> Magical portal warp, airy shimmering whoosh with a soft reverse swell into a warm hum and a
> sparkling tail, mystical but gentle and organic, not sci-fi, no harsh highs, no music, no voice,
> about 0.75 seconds, mono, 2 variations.

---

## 3. Saran SFX tambahan (opsional, prompt sudah disiapkan)

| SFX | Dipakai untuk | Durasi | Prompt (Inggris) |
|---|---|---|---|
| Ambil benih | `SeedPlantingController.Pickup Sfx` (**field sudah ada**) | 0,25–0,35 s | "Soft pluck of a glowing seed lifted from the ground, quick organic pop with a tiny wooden tick and a light shimmer, cozy, no music, no voice, 0.3 seconds, mono." |
| Klik tombol | `UIButtonInteractive.Active Sound` (**field sudah ada**) | 0,10–0,15 s | "Single firm wooden UI click, small plank pressed by a fingertip, warm and dry, slightly lower than the hover tick, no reverb, no music, no voice, 0.12 seconds, mono." |
| Webi mendarat / menempel jaring | belum ada hook | 0,12–0,20 s | "Light sticky web attach on wood and stone, a small soft impact with a brief silky stick, close and tactile, no music, no voice, 0.15 seconds, mono." |
| Musuh Fly kalah | belum ada hook | 0,3–0,5 s | "Small insect-like creature bursting into a soft puff of dust and leaves, quick organic poof with tiny wing flutter fading out, playful not gory, no music, no voice, 0.4 seconds, mono." |
| Bintang / level selesai | belum ada hook | 1,8–2,5 s | "Warm rewarding level-complete chime, soft marimba and wooden bell notes rising gently into a cozy nature-inspired resolve, playful and calm, no voice, 2.2 seconds, mono." |
| Hitung mundur 3-2-1 | belum ada hook | 0,12–0,18 s per beep | "Short warm wooden countdown beep, single soft marimba tick with a light pitch drop from the third beep to the first, cozy, no music, no voice, 0.15 seconds, mono, 3 pitches." |
| Es: meluncur | belum ada hook | loop 1,0–2,0 s | "Smooth icy sliding loop, soft crystalline friction with a cold airy shimmer, low intensity, seamless loop, no music, no voice, 1.5 seconds, mono." |
| Oil: terpeleset | belum ada hook | 0,4–0,6 s | "Slippery oil slip, wet squelch and a quick whoosh as footing is lost, cartoonish but organic, no music, no voice, 0.45 seconds, mono." |
| Gerbang batu bergeser (loop) | belum ada hook | loop 1,0–2,0 s | "Heavy stone gate grinding loop, deep friction rumble with grit and small pebbles, steady, seamless loop, no music, no voice, 1.5 seconds, mono." |

Kalau tiga yang terakhir (es, oil, gerbang loop) mau dipakai, hook-nya belum ada — bilang saja mana
yang perlu aku pasang.

---

## 4. Yang sudah dipasang di kode

Semua field di bawah ini **sudah ada** di script. Begitu clip diimpor, seret dari jendela Project ke
field-nya. Tidak perlu menulis kode lagi.

### `Assets/Scripts/UI/UIButtonInteractive.cs`

```csharp
[Header("Audio Settings (SFX)")]
[SerializeField] private AudioClip hoverSound;
[Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.4f;
[SerializeField] private AudioClip activeSound;
[Range(0f, 1f)] [SerializeField] private float activeVolume = 0.6f;
```

Hover diputar di `OnPointerEnter` (hanya kalau tombol `interactable`), klik di `OnPointerDown`.
Keduanya lewat `AudioManager`, dengan pitch acak `minPitch..maxPitch` (0,95–1,05).

### `Assets/Scripts/Gameplay/PlayerController.cs`

```csharp
[Header("Audio")]
public AudioClip walkSfx;
public float walkSfxVolume = 0.7f;
public AudioClip webShootSfx;                 // saat jaring ditembakkan
[Range(0f, 1f)] public float webShootSfxVolume = 0.7f;
public AudioClip webPullSfx;                  // saat Webi mulai tertarik
[Range(0f, 1f)] public float webPullSfxVolume = 0.7f;
```

```csharp
// HandleAiming(): tepat setelah satu tembakan sah dimulai
ShotWeb?.Invoke(this);
PlaySfx(webShootSfx, webShootSfxVolume);

// HandleSwinging(): saat ujung jaring sampai di target dan tarikan dimulai
swingPhase = SwingPhase.PullingPlayer;
...
PlaySfx(webPullSfx, webPullSfxVolume);
```

Karena `PlayerController` ada di prefab **Webi**, mengisi field di prefab berarti berlaku di semua
level. Volume-nya bisa di-override per level lewat Inspector instance Webi.

### `Assets/Scripts/Gameplay/SeedPlantingController.cs`

```csharp
[Header("Audio")]
public AudioClip pickupSfx;
[Range(0f, 1f)] public float pickupSfxVolume = 1f;
public AudioClip growthSfx;
[Range(0f, 1f)] public float growthSfxVolume = 0.7f;
```

`growthSfx` diputar **setelah `firstFrameDelay`**, tepat sebelum frame tumbuh pertama tampil —
bukan saat benih ditanam — supaya bunyi sinkron dengan tanaman yang mulai kelihatan. Ada di prefab
**TumpTanah**.

### `Assets/Scripts/Gameplay/MovingPlatform.cs`

```csharp
[Header("Audio")]
public AudioClip moveStartSfx;
[Range(0f, 1f)] public float moveStartSfxVolume = 0.7f;
public AudioClip moveStopSfx;
[Range(0f, 1f)] public float moveStopSfxVolume = 0.7f;
```

```csharp
void FixedUpdate()
{
    MoveAlongPath();
    PlayMovementSfx();   // sekali saat mulai bergerak, sekali saat berhenti
    CarryRider();
}
```

"Bergerak" berarti `delta` pada step itu bukan nol. Jadi SFX berhenti juga berbunyi saat platform
diam sejenak di ujung lintasan (`Dwell Time`), lalu SFX mulai berbunyi lagi saat jalan lagi. Ada di
prefab **Moving Ground**.

### `Assets/Scripts/Gameplay/GateLeverController.cs`

```csharp
[Header("Audio")]
public AudioClip gateOpenSfx;
[Range(0f, 1f)] public float gateOpenSfxVolume = 0.8f;
```

Diputar di `OnTriggerEnter2D` saat Webi menyentuh tuas, bersamaan dengan mulainya animasi tuas dan
gerbang. Objek tuas ada di **scene level**, jadi isi field-nya per level (atau copy komponennya).

### `Assets/Scripts/Gameplay/PortalController.cs`

```csharp
[Header("Audio")]
public AudioClip teleportSfx;
[Range(0f, 1f)] public float teleportSfxVolume = 0.8f;
```

Diputar di `OnTriggerEnter2D` hanya jika teleport berhasil (`player.TryTeleportThroughPortal`
mengembalikan `true`); kalau portal tidak punya pasangan, Webi tetap di pintu masuk dan SFX tidak
berbunyi. Field-nya per objek portal di **scene level**.

---

## 5. Cara impor & setting clip

1. Simpan hasil generator sebagai **WAV 44,1 kHz / 16-bit / mono** (hindari stereo untuk SFX kecil;
   lebih ringan dan center-nya jelas).
2. Nama file mengikuti tabel di atas supaya mudah dicari.
3. Di Inspector clip (Project window):
   - **Load Type**: `Decompress On Load` untuk clip < 1 s; `Compressed In Memory` untuk clip lebih
     panjang seperti tumbuh tanaman dan portal.
   - **Compression Format**: `PCM` untuk clip sangat pendek (hover, klik) supaya tidak ada artefak;
     `Vorbis` untuk sisanya.
   - **Preload Audio Data**: aktif.
   - **Force To Mono**: aktif (kalau file sumber stereo).
4. Seret clip ke field sesuai tabel. Volume mulai dari nilai di tabel, lalu dengar di level yang
   paling ramai (Level 10 punya moving ground + portal + musuh sekaligus).
5. Cek juga tombol **SFX** di Settings: semua suara ini ikut slider itu, jadi campuran akhir harus
   enak di volume 0,5–0,7 juga.

### Catatan teknis (penting kalau nanti menambah suara)

`AudioManager` memutar semua SFX lewat **satu** `AudioSource` dengan `PlayOneShot`. Dua akibatnya:

- Banyak SFX bisa tumpang tindih, tetapi `pitch` hanya satu untuk sumber itu — pitch acak pada
  `UIButtonInteractive` ikut menggeser pitch SFX lain yang sedang berbunyi. Untuk SFX UI yang
  pendek ini tidak terasa; kalau nanti ada loop panjang, pakai `AudioSource` sendiri (contohnya
  `ProximitySfx` di musuh).
- Suara loop (es, gerbang batu) **tidak** cocok lewat `PlaySfx`; itu perlu `AudioSource` dengan
  `loop = true` seperti `ProximitySfx`.
