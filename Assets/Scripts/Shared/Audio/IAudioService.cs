namespace NestLabs.Audio
{
    /// <summary>
    /// Playback contract only — nothing here knows which domain events exist. Callers (an
    /// AudioEventBinder, a settings screen) decide when to invoke it.
    /// </summary>
    public interface IAudioService
    {
        void PlaySfx(SfxId id);
        void PlayMusic(MusicId id, bool crossfade = true);
        void StopMusic(bool fadeOut = true);

        void SetMasterMuted(bool muted);
        void SetMusicVolume(float volume01);
        void SetSfxVolume(float volume01);

        bool IsMuted { get; }
    }
}
