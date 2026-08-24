using UnityEngine;

namespace NestLabs.Audio
{
    public sealed class PlayerPrefsAudioMuteStore : IAudioMuteStore
    {
        private const string MutedKey = "audio_muted";
        private const string MusicVolumeKey = "audio_music_volume";
        private const string SfxVolumeKey = "audio_sfx_volume";

        public bool LoadMuted() => PlayerPrefs.GetInt(MutedKey, 0) == 1;

        public void SaveMuted(bool muted)
        {
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public float LoadMusicVolume() => PlayerPrefs.GetFloat(MusicVolumeKey, 1f);

        public void SaveMusicVolume(float volume01)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, volume01);
            PlayerPrefs.Save();
        }

        public float LoadSfxVolume() => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

        public void SaveSfxVolume(float volume01)
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, volume01);
            PlayerPrefs.Save();
        }
    }
}
