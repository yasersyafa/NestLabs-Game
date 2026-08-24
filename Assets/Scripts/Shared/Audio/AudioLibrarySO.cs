using System;
using UnityEngine;

namespace NestLabs.Audio
{
    /// <summary>
    /// Every clip the game can play, plus the per-sound tuning (volume, pitch variation). Lives in
    /// an asset so sound design changes without a recompile, mirroring PlayerConfigSO.
    /// No logic belongs here — this is data only.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "NestLabs/Audio/Audio Library")]
    public sealed class AudioLibrarySO : ScriptableObject
    {
        [Serializable]
        public struct SfxEntry
        {
            public SfxId Id;

            [Tooltip("One clip is picked at random per play so repeated hits don't sound identical.")]
            public AudioClip[] Clips;

            [Range(0f, 1f)]
            public float Volume;

            [Tooltip("Random pitch multiplier range applied per play, e.g. (0.95, 1.05).")]
            public Vector2 PitchRange;
        }

        [Serializable]
        public struct MusicEntry
        {
            public MusicId Id;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume;
        }

        public SfxEntry[] SfxEntries = Array.Empty<SfxEntry>();
        public MusicEntry[] MusicEntries = Array.Empty<MusicEntry>();
    }
}
