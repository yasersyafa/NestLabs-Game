using System;
using DG.Tweening;
using UnityEngine;
using VContainer;

namespace NestLabs.Audio
{
    /// <summary>
    /// Owns music playback and a pooled SFX AudioSource pool. MonoBehaviour rather than a plain
    /// class because it needs OnApplicationPause/Focus and owns real AudioSource components.
    /// Registered per-scope like every other service here — no singleton, no DontDestroyOnLoad.
    /// Deliberately knows nothing about MessagePipe or domain events; see AudioEventBinder.
    /// </summary>
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        private const float MusicCrossfadeDuration = 1f;
        private const int DefaultPoolSize = 12;

        [Header("Fallbacks")]
        [Tooltip("Used when no container injected a library — lets this run in a bare test scene.")]
        [SerializeField] private AudioLibrarySO _fallbackLibrary;

        private AudioLibrarySO _library;

        [Header("SFX pool (optional — auto-created at runtime if left empty)")]
        [SerializeField] private int _poolSize = DefaultPoolSize;
        [SerializeField] private AudioSource[] _sfxSourcesOverride;

        [Header("Music (optional — auto-created at runtime if left empty)")]
        [SerializeField] private AudioSource _musicSourceA;
        [SerializeField] private AudioSource _musicSourceB;

        private IAudioMuteStore _muteStore;
        private SfxAudioSourcePool _pool;

        private AudioLibrarySO.SfxEntry?[] _sfxById;
        private AudioLibrarySO.MusicEntry?[] _musicById;

        private AudioSource _activeMusicSource;
        private AudioSource _inactiveMusicSource;
        private MusicId? _currentMusicId;

        private bool _appPaused;
        private bool _muted;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        public bool IsMuted => _muted;

        [Inject]
        public void Construct(AudioLibrarySO library, IAudioMuteStore muteStore)
        {
            _library = library;
            _muteStore = muteStore;
        }

        private void Awake()
        {
            AudioSource[] sfxSources = _sfxSourcesOverride != null && _sfxSourcesOverride.Length > 0
                ? _sfxSourcesOverride
                : CreatePooledSfxSources(_poolSize);
            _pool = new SfxAudioSourcePool(sfxSources);

            if (_musicSourceA == null) _musicSourceA = CreateMusicSource("MusicSourceA");
            if (_musicSourceB == null) _musicSourceB = CreateMusicSource("MusicSourceB");
            _activeMusicSource = _musicSourceA;
            _inactiveMusicSource = _musicSourceB;
        }

        // Construct() is injected during GameLifetimeScope.Awake(), which Unity always finishes
        // before any Start() runs — unlike Awake() order across objects, which is unspecified.
        // Reading _library/_muteStore is safe here, not in Awake().
        private void Start()
        {
            if (_library == null) _library = _fallbackLibrary;
            if (_library == null)
            {
                Debug.LogWarning($"[AudioService] No AudioLibrarySO injected or assigned on '{name}'. PlaySfx/PlayMusic will no-op.", this);
            }

            BuildLookups();
            LoadPersistedSettings();
        }

        private void OnDestroy()
        {
            if (_musicSourceA != null) DOTween.Kill(_musicSourceA, false);
            if (_musicSourceB != null) DOTween.Kill(_musicSourceB, false);
        }

        private void OnApplicationPause(bool pauseStatus) => ApplyAppPause(pauseStatus);

        // Fallback for platforms where OnApplicationPause doesn't fire reliably.
        private void OnApplicationFocus(bool hasFocus) => ApplyAppPause(!hasFocus);

        public void PlaySfx(SfxId id)
        {
            AudioLibrarySO.SfxEntry? entry = _sfxById?[(int)id];
            if (entry == null || entry.Value.Clips == null || entry.Value.Clips.Length == 0) return;

            AudioSource source = _pool.Acquire();
            AudioClip clip = entry.Value.Clips[UnityEngine.Random.Range(0, entry.Value.Clips.Length)];

            source.clip = clip;
            source.volume = entry.Value.Volume * _sfxVolume;
            source.pitch = ResolvePitch(entry.Value.PitchRange);
            source.Play();
        }

        public void PlayMusic(MusicId id, bool crossfade = true)
        {
            AudioLibrarySO.MusicEntry? entry = _musicById?[(int)id];
            if (entry == null || entry.Value.Clip == null) return;
            if (_currentMusicId == id) return;

            _currentMusicId = id;
            float targetVolume = entry.Value.Volume * _musicVolume;

            if (!crossfade || !_activeMusicSource.isPlaying)
            {
                DOTween.Kill(_activeMusicSource, false);
                DOTween.Kill(_inactiveMusicSource, false);
                _activeMusicSource.clip = entry.Value.Clip;
                _activeMusicSource.loop = true;
                _activeMusicSource.volume = targetVolume;
                _activeMusicSource.Play();
                return;
            }

            AudioSource incoming = _inactiveMusicSource;
            AudioSource outgoing = _activeMusicSource;

            DOTween.Kill(incoming, false);
            incoming.clip = entry.Value.Clip;
            incoming.loop = true;
            incoming.volume = 0f;
            incoming.Play();
            incoming.DOFade(targetVolume, MusicCrossfadeDuration);

            DOTween.Kill(outgoing, false);
            outgoing.DOFade(0f, MusicCrossfadeDuration).OnComplete(outgoing.Stop);

            _activeMusicSource = incoming;
            _inactiveMusicSource = outgoing;
        }

        public void StopMusic(bool fadeOut = true)
        {
            _currentMusicId = null;
            DOTween.Kill(_activeMusicSource, false);
            DOTween.Kill(_inactiveMusicSource, false);

            if (!fadeOut)
            {
                _activeMusicSource.Stop();
                _inactiveMusicSource.Stop();
                return;
            }

            AudioSource source = _activeMusicSource;
            source.DOFade(0f, MusicCrossfadeDuration).OnComplete(source.Stop);
        }

        public void SetMasterMuted(bool muted)
        {
            _muted = muted;
            AudioListener.volume = muted ? 0f : 1f;
            _muteStore?.SaveMuted(muted);
        }

        public void SetMusicVolume(float volume01)
        {
            _musicVolume = Mathf.Clamp01(volume01);
            if (_currentMusicId != null)
            {
                AudioLibrarySO.MusicEntry? entry = _musicById?[(int)_currentMusicId.Value];
                if (entry != null) _activeMusicSource.volume = entry.Value.Volume * _musicVolume;
            }
            _muteStore?.SaveMusicVolume(_musicVolume);
        }

        public void SetSfxVolume(float volume01)
        {
            _sfxVolume = Mathf.Clamp01(volume01);
            _muteStore?.SaveSfxVolume(_sfxVolume);
        }

        private void ApplyAppPause(bool pause)
        {
            if (_appPaused == pause) return;
            _appPaused = pause;
            AudioListener.pause = pause;
        }

        private void BuildLookups()
        {
            _sfxById = new AudioLibrarySO.SfxEntry?[Enum.GetValues(typeof(SfxId)).Length];
            _musicById = new AudioLibrarySO.MusicEntry?[Enum.GetValues(typeof(MusicId)).Length];

            if (_library == null) return;

            foreach (AudioLibrarySO.SfxEntry entry in _library.SfxEntries)
            {
                _sfxById[(int)entry.Id] = entry;
            }

            foreach (AudioLibrarySO.MusicEntry entry in _library.MusicEntries)
            {
                _musicById[(int)entry.Id] = entry;
            }
        }

        private void LoadPersistedSettings()
        {
            if (_muteStore == null) return;

            _muted = _muteStore.LoadMuted();
            _musicVolume = _muteStore.LoadMusicVolume();
            _sfxVolume = _muteStore.LoadSfxVolume();
            AudioListener.volume = _muted ? 0f : 1f;
        }

        private static float ResolvePitch(Vector2 range)
        {
            // An un-authored SO entry defaults PitchRange to (0,0); pitch 0 would freeze
            // playback, so treat that as "no variation configured" and use 1 instead.
            if (range.x <= 0f && range.y <= 0f) return 1f;
            return UnityEngine.Random.Range(range.x, range.y);
        }

        private AudioSource[] CreatePooledSfxSources(int count)
        {
            var poolRoot = new GameObject("SfxPool").transform;
            poolRoot.SetParent(transform, false);

            var sources = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                var child = new GameObject($"SfxSource_{i}");
                child.transform.SetParent(poolRoot, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                sources[i] = source;
            }

            return sources;
        }

        private AudioSource CreateMusicSource(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            return source;
        }
    }
}
