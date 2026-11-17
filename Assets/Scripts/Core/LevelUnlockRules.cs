using UnityEngine;

/// <summary>
/// Aturan buka level berdasarkan total bintang. Tabelnya sengaja hanya ada di satu tempat:
/// tombol level di Level Selection, popup "level terkunci", dan tombol Next Level di popup menang
/// membaca angka yang sama. Jadi tidak ada lagi angka syarat bintang yang tersebar di dalam scene
/// (dulu tiap tombol menyimpan Required Stars sendiri, sehingga mudah berbeda dengan popup).
///
/// Syaratnya sengaja lebih rendah dari 3 x (level - 1) supaya pemain tidak harus sempurna di semua
/// level sebelumnya. Contoh: Level 10 butuh 21 dari maksimal 27 bintang — semua level bernilai
/// 2 bintang saja belum cukup (18), tetapi menambah tiga level bernilai 3 bintang sudah cukup.
/// </summary>
public static class LevelUnlockRules
{
    #region Tabel Syarat

    // Indeks = nomor level, isi = total bintang minimal untuk membukanya. Indeks 0 hanya penampung
    // supaya nomor level sama dengan indeks array. Ubah angka di sini kalau syaratnya mau
    // dilonggarkan atau diperketat; semua tampilan (tombol, popup, Next Level) ikut menyesuaikan.
    private static readonly int[] RequiredStarsByLevel = { 0, 0, 2, 4, 7, 9, 11, 14, 16, 18, 21 };

    #endregion

    #region Public Properties

    /// <summary>Nomor level tertinggi yang diatur tabel ini (level terakhir game).</summary>
    public static int MaxLevel => RequiredStarsByLevel.Length - 1;

    #endregion

    #region Public Methods

    /// <summary>Total bintang minimal agar level tersebut terbuka.</summary>
    public static int GetRequiredStars(int levelNumber)
    {
        if (levelNumber <= 0)
        {
            return 0;
        }

        if (levelNumber >= RequiredStarsByLevel.Length)
        {
            return Mathf.Max(0, RequiredStarsByLevel[RequiredStarsByLevel.Length - 1]);
        }

        return Mathf.Max(0, RequiredStarsByLevel[levelNumber]);
    }

    /// <summary>Berapa bintang lagi yang dibutuhkan; 0 berarti syaratnya sudah terpenuhi.</summary>
    public static int GetMissingStars(int levelNumber, int totalStars)
    {
        return Mathf.Max(0, GetRequiredStars(levelNumber) - Mathf.Max(0, totalStars));
    }

    /// <summary>True kalau total bintang pemain sudah memenuhi syarat level tersebut.</summary>
    public static bool IsUnlocked(int levelNumber, int totalStars)
    {
        return Mathf.Max(0, totalStars) >= GetRequiredStars(levelNumber);
    }

    /// <summary>Total bintang pemain saat ini, dihitung sampai level terakhir.</summary>
    public static int GetTotalStars()
    {
        return LevelProgress.GetTotalStars(MaxLevel);
    }

    /// <summary>
    /// Mengambil nomor level dari nama scene, mis. "Level 7" menjadi 7. Dipakai tombol Next Level
    /// supaya scene level tidak perlu menyimpan nomor level berikutnya satu per satu.
    /// </summary>
    public static bool TryGetLevelNumber(string sceneName, out int levelNumber)
    {
        levelNumber = 0;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        // Ambil deretan angka di ujung nama scene; "Level 10" -> 10.
        int end = sceneName.Length;
        int start = end;
        while (start > 0 && char.IsDigit(sceneName[start - 1]))
        {
            start--;
        }

        if (start == end)
        {
            return false;
        }

        return int.TryParse(sceneName.Substring(start, end - start), out levelNumber) && levelNumber > 0;
    }

    #endregion
}
