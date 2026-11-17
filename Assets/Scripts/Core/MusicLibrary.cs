using System;
using UnityEngine;

/// <summary>
/// Daftar musik untuk setiap scene. Asset-nya ada di Assets/Resources/MusicLibrary.asset dan dimuat
/// otomatis oleh AudioManager lewat Resources.Load, jadi musik tidak perlu diseret ke tiap scene.
///
/// Satu entri mewakili satu scene. Untuk level, isi semua track yang boleh muncul: setiap kali level
/// dibuka (termasuk Retry) satu track dipilih acak lalu diputar berulang sampai scene berganti.
/// </summary>
[CreateAssetMenu(fileName = "MusicLibrary", menuName = "Rewild/Music Library")]
public class MusicLibrary : ScriptableObject
{
    #region Nested Types

    /// <summary>Playlist satu scene: kumpulan track yang boleh diputar dan volume relatifnya.</summary>
    [Serializable]
    public class Entry
    {
        [Tooltip("Nama scene persis seperti di Scenes In Build, mis. \"Level 1\" atau \"Level Selection\".")]
        public string sceneName = "";

        [Tooltip("Daftar track yang boleh diputar di scene ini. Satu track dipilih acak setiap kali " +
                 "scene dibuka, lalu diulang terus sampai player keluar atau Retry.")]
        public AudioClip[] tracks = new AudioClip[0];

        [Tooltip("Volume track relatif terhadap slider Music di Settings (0 = senyap, 1 = penuh).")]
        [Range(0f, 1f)]
        public float volumeScale = 0.7f;

        [Tooltip("Pilih satu track acak setiap kali scene dibuka. Matikan untuk selalu memakai track pertama.")]
        public bool randomTrack = true;
    }

    #endregion

    #region Inspector Fields

    [Tooltip("Satu entri per scene. Scene tanpa entri tidak memutar musik.")]
    public Entry[] entries = new Entry[0];

    #endregion

    #region Public Methods

    /// <summary>Mencari entri untuk nama scene. Mengembalikan null kalau scene tidak punya musik.</summary>
    public Entry Find(string sceneName)
    {
        if (entries == null || string.IsNullOrEmpty(sceneName))
        {
            return null;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].sceneName == sceneName)
            {
                return entries[i];
            }
        }

        return null;
    }

    #endregion
}
