using UnityEngine;

namespace NestLabs.Audio
{
    /// <summary>
    /// Fixed-size AudioSource pool. Acquire scans for a free (non-playing) source; if every source
    /// is busy it steals the oldest-acquired one round-robin rather than dropping the new sound.
    /// No coroutine/timer to return sources — checking isPlaying on the next scan is enough and
    /// costs zero allocations, which is the whole point on mobile.
    /// </summary>
    public sealed class SfxAudioSourcePool
    {
        private readonly AudioSource[] _sources;
        private int _nextStealIndex;

        public SfxAudioSourcePool(AudioSource[] sources)
        {
            _sources = sources;
        }

        public AudioSource Acquire()
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                if (!_sources[i].isPlaying)
                {
                    return _sources[i];
                }
            }

            AudioSource stolen = _sources[_nextStealIndex];
            _nextStealIndex = (_nextStealIndex + 1) % _sources.Length;
            return stolen;
        }
    }
}
