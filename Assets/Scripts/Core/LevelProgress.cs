using UnityEngine;

/// <summary>
/// Penyimpanan progres bintang per level memakai PlayerPrefs, sekaligus hitungan total bintang
/// yang dipakai untuk membuka level berikutnya. Kelas ini statis, jadi bisa dipanggil dari scene
/// mana pun, termasuk dari logika selesai level di dalam Level 1.
///
/// Aturan penyimpanan: yang disimpan adalah bintang TERBAIK tiap level. Main ulang dengan hasil
/// lebih jelek tidak menurunkan total bintang pemain.
/// </summary>
public static class LevelProgress
{
    #region PlayerPrefs Keys

    // Prefix kunci PlayerPrefs. Nomor level ditempel di belakangnya, contoh: "Level.Stars.3".
    private const string StarKeyPrefix = "Level.Stars.";

    #endregion

    #region Public Methods

    /// <summary>Bintang terbaik yang pernah didapat pada satu level (0 jika belum pernah dimainkan).</summary>
    public static int GetStars(int levelNumber)
    {
        if (levelNumber < 1)
        {
            return 0;
        }

        return Mathf.Max(0, PlayerPrefs.GetInt(StarKeyPrefix + levelNumber, 0));
    }

    /// <summary>Jumlah bintang dari level 1 sampai level tertentu. Dipakai untuk syarat buka level.</summary>
    public static int GetTotalStars(int highestLevelNumber)
    {
        int total = 0;

        for (int level = 1; level <= highestLevelNumber; level++)
        {
            total += GetStars(level);
        }

        return total;
    }

    /// <summary>
    /// Menyimpan bintang hasil main untuk satu level. Nilai dibatasi 0 sampai maxStarsPerLevel dan
    /// hanya dipakai kalau lebih tinggi dari yang tersimpan. Return true jika data berubah.
    /// </summary>
    public static bool AwardStars(int levelNumber, int stars, int maxStarsPerLevel)
    {
        if (levelNumber < 1)
        {
            return false;
        }

        int clamped = Mathf.Clamp(stars, 0, Mathf.Max(0, maxStarsPerLevel));

        if (clamped <= GetStars(levelNumber))
        {
            return false;
        }

        PlayerPrefs.SetInt(StarKeyPrefix + levelNumber, clamped);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Menghapus semua progres bintang dari level 1 sampai level tertentu.</summary>
    public static void ResetAll(int highestLevelNumber)
    {
        for (int level = 1; level <= highestLevelNumber; level++)
        {
            PlayerPrefs.DeleteKey(StarKeyPrefix + level);
        }

        PlayerPrefs.Save();
    }

    #endregion
}
