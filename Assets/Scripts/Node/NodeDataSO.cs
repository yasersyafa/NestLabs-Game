using UnityEngine;

namespace NestLabs.Node
{
    /// <summary>
    /// Tuning for one kind of grapple node. Lives in an asset so a prefab variant is just a
    /// different asset reference, the same way player skins work.
    /// No logic belongs here, this is data only.
    /// </summary>
    [CreateAssetMenu(fileName = "NodeData", menuName = "NestLabs/Node/Node Data")]
    public sealed class NodeDataSO : ScriptableObject
    {
        [Header("Launch")]
        [Tooltip("Speed the player is pulled at, in units/sec. Held constant for the whole pull.")]
        [Min(0f)] public float LaunchForce = 30f;

        [Tooltip("How close the player must be to use this node, in world units.")]
        [Min(0f)] public float Radius = 2f;

        [Tooltip("Seconds before this node can be used again. 0 = always ready.")]
        [Min(0f)] public float ReuseCooldown = 0f;

        [Header("Look")]
        public Color Tint = Color.white;

        [Tooltip("Shown while the node is on cooldown.")]
        public Color SpentTint = new Color(1f, 1f, 1f, 0.35f);

        [Tooltip("Spawned at the node when a launch fires. Optional.")]
        public GameObject LaunchVfx;
    }
}
