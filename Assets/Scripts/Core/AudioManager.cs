using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Audio Sources")]
    [Tooltip("AudioSource untuk musik latar. Biasanya objek ini memakai AudioSource dengan Loop aktif.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("AudioSource untuk efek suara (SFX). Suara tombol dan gameplay diputar lewat sumber ini.")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("AudioSource khusus suara tombol UI. Biarkan kosong; dibuat otomatis saat dibutuhkan. " +
             "Sumber terpisah ini tetap berbunyi saat SFX gameplay dibungkam selama intro level, " +
             "supaya klik tombol pause/retry tetap terdengar.")]
    [SerializeField] private AudioSource uiSfxSource;

    [Header("Default Volume")]
    [Tooltip("Volume musik awal saat game pertama kali dijalankan, sebelum pemain mengubahnya di Settings.")]
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.8f;

    [Tooltip("Volume SFX awal saat game pertama kali dijalankan, sebelum pemain mengubahnya di Settings.")]
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.9f;

    [Header("Music Playlist")]
    [Tooltip("Lama fade saat musik mulai atau berganti track, dalam detik waktu nyata.")]
    [SerializeField, Min(0f)] private float musicFadeDuration = 0.8f;

    [Tooltip("Nama asset MusicLibrary di dalam folder Resources. Biasanya cukup dibiarkan \"MusicLibrary\".")]
    [SerializeField] private string musicLibraryResourcePath = "MusicLibrary";

    #endregion

    #region Tuning

    // Pengali volume musik untuk semua scene (trim). Slider Settings tetap 0..1, tetapi hasil akhir
    // musik dikali angka ini, jadi musik tidak pernah melebihi batas ini.
    //
    // Sengaja const, BUKAN field [SerializeField]: field serialized menyimpan nilainya di dalam
    // scene per komponen, sehingga mengubah nilai default di script tidak mengubah scene yang sudah
    // ada (Play tetap memakai angka lama) — itu sebabnya dua penurunan volume sebelumnya tidak
    // terdengar. Dengan const, satu-satunya sumber kebenaran adalah angka di bawah ini.
    // 0.2 -> volume musik efektif 1.0 x 0.7 x 0.2 = 0.14 saat slider Settings penuh.
    private const float MusicGain = 0.2f;

    #endregion

    #region PlayerPrefs Keys

    // Kunci penyimpanan PlayerPrefs supaya pengaturan audio tetap tersimpan antar sesi.
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string MusicMutedKey = "MusicMuted";
    private const string SfxMutedKey = "SfxMuted";

    #endregion

    #region Runtime State

    // Volume musik dari Settings disimpan terpisah dari volume AudioSource, karena AudioSource juga
    // dipakai untuk volume per-track dan fade. Nilai akhirnya = musicVolume * playlistVolumeScale * fadeFactor.
    private float musicVolume = 1f;
    private float playlistVolumeScale = 1f;
    private float fadeFactor = 1f;

    private bool musicPaused;

    private MusicLibrary musicLibrary;
    private Coroutine switchRoutine;

    // Track terakhir per scene, supaya Retry / masuk ulang tidak memutar track yang sama dua kali berturut-turut.
    private readonly Dictionary<string, int> lastTrackByScene = new Dictionary<string, int>();

    // Nama scene pemilik entri playlist yang musiknya sedang diputar (null = belum ada musik).
    private string currentMusicScene;

    // True kalau scene yang barusan dibuka tidak punya playlist sendiri (mis. Help), sehingga
    // musik scene sebelumnya sengaja dibiarkan berjalan.
    private bool arrivedFromSceneWithoutMusic;

    // True selama fase yang memang harus senyap (intro kamera level sampai hitung mundur 3-2-1
    // selesai). Bukan bagian dari Settings: nilainya tidak disimpan di PlayerPrefs dan tidak
    // mengubah slider. Dipakai bersama mute Settings di ApplySfxMute(), dan dibaca juga oleh
    // ProximitySfx / MovingPlatform supaya loop mereka ikut diam.
    private bool sfxSuppressed;

    // True selama dunia dibekukan pemain (popup pause, game over, menang). Dipakai bersama
    // sfxSuppressed supaya loop portal/musuh/platform diam walau Time.timeScale sudah 0.
    private bool sfxFrozen;

    #endregion

    #region Public Properties

    public static AudioManager Instance { get; private set; }

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxSource != null ? sfxSource.volume : 0f;
    public bool IsMusicMuted { get; private set; }
    public bool IsSfxMuted { get; private set; }

    /// <summary>
    /// True saat semua SFX sedang dibungkam sementara (intro kamera level). Berbeda dari
    /// <see cref="IsSfxMuted"/> yang berasal dari tombol Settings dan tersimpan di PlayerPrefs.
    /// </summary>
    public bool IsSfxSuppressed => sfxSuppressed;

    /// <summary>
    /// True saat SFX harus diam karena dunia sedang dibekukan (pause, game over, menang).
    /// Berbeda dari <see cref="IsSfxSuppressed"/> yang khusus fase intro level.
    /// </summary>
    public bool IsSfxSilenced => sfxSuppressed || sfxFrozen;

    #endregion

    #region Unity Events

    private void Awake()
    {
        // Satu AudioManager saja yang hidup; duplikat dari scene berikutnya dihapus.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume));
        SetSfxVolume(PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume));
        SetMusicMuted(PlayerPrefs.GetInt(MusicMutedKey, 0) == 1);
        SetSfxMuted(PlayerPrefs.GetInt(SfxMutedKey, 0) == 1);
    }

    #endregion

    #region Public Methods

    /// <summary>Mengatur volume musik (0..1) sekaligus menyimpannya ke PlayerPrefs.</summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyMusicVolume();

        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
    }

    /// <summary>Mengatur volume SFX (0..1) sekaligus menyimpannya ke PlayerPrefs.</summary>
    public void SetSfxVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
        }

        if (uiSfxSource != null)
        {
            uiSfxSource.volume = Mathf.Clamp01(volume);
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    /// <summary>Menyalakan atau mematikan musik tanpa mengubah nilai volumenya.</summary>
    public void SetMusicMuted(bool muted)
    {
        IsMusicMuted = muted;

        if (musicSource != null)
        {
            musicSource.mute = muted;
        }

        PlayerPrefs.SetInt(MusicMutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>Menyalakan atau mematikan SFX tanpa mengubah nilai volumenya.</summary>
    public void SetSfxMuted(bool muted)
    {
        IsSfxMuted = muted;
        ApplySfxMute();

        PlayerPrefs.SetInt(SfxMutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Membungkam semua SFX sementara tanpa menyentuh slider maupun status mute Settings.
    /// Dipakai intro kamera level: selama zoom ke tumpukkan tanah sampai hitung mundur 3-2-1
    /// selesai, tidak ada SFX yang boleh berbunyi (dengung portal, loop musuh, moving ground,
    /// maupun one-shot). Setelah intro selesai, panggil dengan false dan SFX kembali normal.
    /// </summary>
    public void SetSfxSuppressed(bool suppressed)
    {
        if (sfxSuppressed == suppressed)
        {
            return;
        }

        sfxSuppressed = suppressed;
        ApplySfxMute();
    }

    /// <summary>
    /// Membungkam semua SFX selama dunia dibekukan (popup pause, game over, menang) tanpa menyentuh
    /// slider maupun status mute Settings. Berbeda dari <see cref="SetSfxSuppressed"/> yang dipakai
    /// intro, dan keduanya boleh aktif bersamaan.
    /// </summary>
    public void SetSfxFrozen(bool frozen)
    {
        if (sfxFrozen == frozen)
        {
            return;
        }

        sfxFrozen = frozen;
        ApplySfxMute();

        // Buang one-shot yang sedang berjalan supaya tidak menyeletuk lagi saat resume.
        if (frozen && sfxSource != null)
        {
            sfxSource.Stop();
        }
    }

    /// <summary>Memutar SFX sekali dengan pitch acak di antara minPitch dan maxPitch.</summary>
    public void PlaySfx(AudioClip clip, float minPitch = 1f, float maxPitch = 1f)
    {
        PlaySfx(clip, minPitch, maxPitch, 1f);
    }

    /// <summary>
    /// Memutar SFX sekali dengan pitch acak dan volume khusus. Volume ini dikalikan dengan
    /// volume SFX global, jadi slider Settings tetap berpengaruh.
    /// </summary>
    public void PlaySfx(AudioClip clip, float minPitch, float maxPitch, float volumeScale)
    {
        // Selama dibungkam (intro level) clip tidak diputar sama sekali, bukan hanya volume 0,
        // supaya tidak ada sisa suara yang menyeletuk saat bungkamnya dilepas.
        if (IsSfxSilenced || sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.pitch = Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    /// <summary>
    /// Memutar SFX tombol UI dengan pitch acak dan volume khusus. Berbeda dari <see cref="PlaySfx"/>,
    /// suara ini tetap berbunyi walau SFX gameplay sedang dibungkam (intro kamera level), karena
    /// klik tombol tetap harus terdengar. Mute SFX dari Settings tetap berlaku.
    /// </summary>
    public void PlayUiSfx(AudioClip clip, float minPitch, float maxPitch, float volumeScale)
    {
        if (clip == null || IsSfxMuted)
        {
            return;
        }

        AudioSource source = GetUiSfxSource();
        if (source == null)
        {
            return;
        }

        source.pitch = Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    #endregion

    #region Music Playlist

    /// <summary>
    /// Memutar musik untuk sebuah scene. Satu track dipilih acak dari daftar scene tersebut, lalu
    /// diputar berulang sampai scene berganti. Dipanggil setiap scene dibuka, jadi Retry dan masuk
    /// ulang level otomatis mendapat track baru (dan tidak mengulang track yang barusan diputar).
    /// Scene tanpa entri playlist (mis. Help) tidak menyentuh musik yang sedang berjalan;
    /// kembali ke scene pemilik musik itu lalu melanjutkan track dari posisi terakhir, bukan dari awal.
    /// </summary>
    public void PlaySceneMusic(string sceneName)
    {
        if (musicSource == null || string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        MusicLibrary library = GetMusicLibrary();
        MusicLibrary.Entry entry = library != null ? library.Find(sceneName) : null;
        if (entry == null)
        {
            // Scene ini tidak punya playlist: biarkan musik scene sebelumnya terus berbunyi.
            arrivedFromSceneWithoutMusic = true;
            return;
        }

        // Balik dari scene tanpa musik (mis. Home Screen -> Help -> Home Screen): kalau track
        // yang sama masih diputar, lanjutkan saja supaya tidak mengulang dari detik 0.
        if (arrivedFromSceneWithoutMusic && switchRoutine == null &&
            currentMusicScene == entry.sceneName && musicSource.clip != null)
        {
            arrivedFromSceneWithoutMusic = false;
            playlistVolumeScale = entry.volumeScale;
            ApplyMusicVolume();
            return;
        }

        AudioClip clip = PickTrack(entry);
        if (clip == null)
        {
            return;
        }

        playlistVolumeScale = entry.volumeScale;
        musicPaused = false;
        musicSource.UnPause();
        ApplyMusicVolume();

        currentMusicScene = entry.sceneName;
        arrivedFromSceneWithoutMusic = false;

        if (switchRoutine != null)
        {
            StopCoroutine(switchRoutine);
        }

        switchRoutine = StartCoroutine(SwitchTrackRoutine(clip));
    }

    /// <summary>
    /// Menghentikan musik sementara tanpa kehilangan posisi track. Dipakai popup pause:
    /// setelah Resume dan hitung mundur 3-2-1 selesai, musik lanjut dari detik yang sama.
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource == null || musicPaused)
        {
            return;
        }

        musicPaused = true;
        musicSource.Pause();
    }

    /// <summary>Melanjutkan musik yang di-pause dari posisi terakhirnya.</summary>
    public void ResumeMusic()
    {
        if (musicSource == null || !musicPaused)
        {
            return;
        }

        musicPaused = false;
        musicSource.UnPause();
    }

    /// <summary>
    /// Memudarkan lalu menghentikan musik sepenuhnya, dan melepas track yang sedang diputar. Dipakai
    /// scene intro: musik menu tidak boleh berbunyi di belakang video intro, sedangkan scene level
    /// berikutnya memutar playlist-nya sendiri lewat <see cref="PlaySceneMusic"/>.
    /// </summary>
    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        if (switchRoutine != null)
        {
            StopCoroutine(switchRoutine);
        }

        switchRoutine = StartCoroutine(StopMusicRoutine());
    }

    /// <summary>
    /// Menghentikan musik seketika, tanpa fade. Dipakai popup game over: musik harus hilang pada
    /// frame yang sama saat popup muncul, bukan memudar di belakangnya. Track ikut dilepas, jadi
    /// Retry memilih lagu lewat jalur normal seperti saat level baru dibuka.
    /// </summary>
    public void StopMusicImmediate()
    {
        if (switchRoutine != null)
        {
            StopCoroutine(switchRoutine);
            switchRoutine = null;
        }

        ReleaseCurrentTrack();
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Mute AudioSource SFX gabungan: tombol SFX di Settings dimatikan, sedang dibungkam intro,
    /// atau dunia sedang dibekukan (pause/game over/menang). Satu AudioSource dipakai bersama, jadi
    /// semua kondisi harus dihitung di satu tempat supaya tidak saling menimpa.
    /// </summary>
    private void ApplySfxMute()
    {
        if (sfxSource != null)
        {
            sfxSource.mute = IsSfxMuted || IsSfxSilenced;
        }

        // Tombol UI hanya mengikuti mute Settings, bukan bungkam sementara intro level.
        if (uiSfxSource != null)
        {
            uiSfxSource.mute = IsSfxMuted;
        }
    }

    /// <summary>
    /// AudioSource terpisah untuk suara tombol. Dibuat otomatis sebagai anak objek ini, supaya
    /// pembungkaman SFX gameplay (intro level) tidak ikut membungkam klik tombol.
    /// </summary>
    private AudioSource GetUiSfxSource()
    {
        if (uiSfxSource != null)
        {
            return uiSfxSource;
        }

        GameObject holder = new GameObject("UI SFX Source");
        holder.transform.SetParent(transform, false);

        uiSfxSource = holder.AddComponent<AudioSource>();
        uiSfxSource.playOnAwake = false;
        uiSfxSource.spatialBlend = 0f;
        uiSfxSource.mute = IsSfxMuted;
        uiSfxSource.volume = sfxSource != null ? sfxSource.volume : 1f;

        return uiSfxSource;
    }

    private MusicLibrary GetMusicLibrary()
    {
        if (musicLibrary == null && !string.IsNullOrEmpty(musicLibraryResourcePath))
        {
            musicLibrary = Resources.Load<MusicLibrary>(musicLibraryResourcePath);
        }

        return musicLibrary;
    }

    /// <summary>
    /// Memilih satu clip dari daftar scene: acak kalau diizinkan, dan tidak memilih track yang baru
    /// saja diputar di scene itu supaya Retry terdengar berbeda.
    /// </summary>
    private AudioClip PickTrack(MusicLibrary.Entry entry)
    {
        if (entry.tracks == null || entry.tracks.Length == 0)
        {
            return null;
        }

        // Hanya slot yang berisi clip yang ikut diundi.
        List<int> candidates = new List<int>();
        for (int i = 0; i < entry.tracks.Length; i++)
        {
            if (entry.tracks[i] != null)
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1 || !entry.randomTrack)
        {
            int single = candidates[0];
            lastTrackByScene[entry.sceneName] = single;
            return entry.tracks[single];
        }

        int lastIndex;
        if (lastTrackByScene.TryGetValue(entry.sceneName, out lastIndex))
        {
            candidates.Remove(lastIndex);
        }

        int chosen = candidates[Random.Range(0, candidates.Count)];
        lastTrackByScene[entry.sceneName] = chosen;
        return entry.tracks[chosen];
    }

    /// <summary>
    /// Volume akhir musik = slider Settings x volume track x fade yang sedang berjalan x trim global.
    /// Trim (<see cref="MusicGain"/>) dipakai supaya musik selalu berada di bawah SFX tanpa
    /// bergantung pada nilai yang tersimpan di PlayerPrefs maupun di scene.
    /// </summary>
    private void ApplyMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(musicVolume * playlistVolumeScale * fadeFactor * MusicGain);
        }
    }

    private IEnumerator SwitchTrackRoutine(AudioClip clip)
    {
        yield return FadeTo(0f);

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();

        // Kalau pemain menekan pause tepat saat track berganti, track baru jangan ikut berbunyi.
        if (musicPaused)
        {
            musicSource.Pause();
        }

        yield return FadeTo(1f);

        switchRoutine = null;
    }

    /// <summary>
    /// Fade out lalu hentikan musik. Track dilepas (clip dikosongkan) supaya track lama tidak
    /// dianggap masih berjalan saat scene berikutnya menentukan musiknya sendiri.
    /// </summary>
    private IEnumerator StopMusicRoutine()
    {
        yield return FadeTo(0f);

        ReleaseCurrentTrack();
        switchRoutine = null;
    }

    /// <summary>
    /// Memberhentikan track yang sedang diputar dan melupakan state-nya: clip dikosongkan, fade
    /// direset, dan penanda "musik scene ini" dibersihkan. Dipakai bersama oleh henti seketika
    /// maupun henti ber-fade supaya keduanya meninggalkan kondisi yang sama.
    /// </summary>
    private void ReleaseCurrentTrack()
    {
        fadeFactor = 0f;

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            ApplyMusicVolume();
        }

        musicPaused = false;
        currentMusicScene = null;
        arrivedFromSceneWithoutMusic = false;
    }

    /// <summary>Fade volume musik memakai waktu nyata, jadi tetap jalan saat Time.timeScale = 0.</summary>
    private IEnumerator FadeTo(float target)
    {
        if (musicFadeDuration <= 0f)
        {
            fadeFactor = target;
            ApplyMusicVolume();
            yield break;
        }

        float start = fadeFactor;
        float elapsed = 0f;

        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / musicFadeDuration);
            fadeFactor = Mathf.Lerp(start, target, progress);
            ApplyMusicVolume();
            yield return null;
        }

        fadeFactor = target;
        ApplyMusicVolume();
    }

    #endregion
}
