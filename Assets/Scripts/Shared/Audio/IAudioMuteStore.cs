namespace NestLabs.Audio
{
    /// <summary>Persistence contract for the audio settings a player can change (mute, volumes).</summary>
    public interface IAudioMuteStore
    {
        bool LoadMuted();
        void SaveMuted(bool muted);

        float LoadMusicVolume();
        void SaveMusicVolume(float volume01);

        float LoadSfxVolume();
        void SaveSfxVolume(float volume01);
    }
}
