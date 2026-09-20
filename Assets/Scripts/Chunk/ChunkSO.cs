using System;
using System.Collections.Generic;
using NestLabs.Node;
using Nestlabs.Obstacle;
using UnityEngine;

namespace Nestlabs.Chunk
{
    // A fixed relative layout - node and obstacle placement authored together so the pair is
    // solvable by construction, instead of two independent rules reconciling X-position conflicts
    // at spawn time (see ChunkSpawnRuleSO's header comment for why that reactive approach broke
    // down). Entries are authored against referenceHalfWidth, set to the narrowest supported
    // corridor; ChunkSpawnRuleSO only ever scales positions up from there for a wider device, so a
    // chunk that is solvable at the reference width stays solvable everywhere it can spawn.
    [CreateAssetMenu(fileName = "Chunk", menuName = "NestLabs/Chunk/Chunk")]
    public sealed class ChunkSO : ScriptableObject
    {
        public enum EntryType
        {
            Node,
            IdleObstacle,
            MovingObstacle,
            SwingObstacle
        }

        [Serializable]
        public struct ChunkEntry
        {
            public EntryType type;

            [Tooltip("X position authored against referenceHalfWidth, before jitter/scale.")]
            public float relativeX;
            [Tooltip("Y offset from the chunk's fire position (spawnY).")]
            public float relativeY;
            [Tooltip("Random X offset applied on top of the scaled relativeX at fire time.")]
            public Vector2 jitterX;

            public NodeBase nodePrefab;
            public IdleObstacle idlePrefab;
            public MovingObstacle movingPrefab;
            [Tooltip("Moving obstacle's sweep endpoint, as a relative-X offset (same space as relativeX).")]
            public float movingEndRelativeX;
            public SwingObstacle swingPrefab;
        }

        [Header("Identity")]
        [Tooltip("Used for the anti-repeat window. Falls back to the asset name when blank.")]
        [SerializeField] private string chunkId;
        [SerializeField] private ChunkTier tier = ChunkTier.Easy;
        [Tooltip("Relative pick chance within its tier. 0 never spawns.")]
        [Min(0f)] [SerializeField] private float weight = 1f;

        [Header("Layout")]
        [Tooltip("Anchor-to-topmost-entry span. Must not exceed the owning rule's spawnYGapMin, or consecutive chunks can overlap in Y.")]
        [Min(0f)] [SerializeField] private float footprintHeight = 6f;
        [Tooltip("Corridor half-width this layout was authored/validated against - the narrowest supported aspect ratio.")]
        [SerializeField] private float referenceHalfWidth = 3.4f;
        [SerializeField] private ChunkEntry[] entries = Array.Empty<ChunkEntry>();

        public string ChunkId => string.IsNullOrEmpty(chunkId) ? name : chunkId;
        public ChunkTier Tier => tier;
        public float Weight => weight;
        public float FootprintHeight => footprintHeight;
        public float ReferenceHalfWidth => referenceHalfWidth;
        public IReadOnlyList<ChunkEntry> Entries => entries;
    }
}
