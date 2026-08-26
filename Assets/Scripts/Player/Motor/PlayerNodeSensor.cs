using System.Collections.Generic;
using NestLabs.Node;
using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Tracks which grapple nodes the player is currently inside. Membership is a set, not an
    /// event, so this only listens to Enter and Exit. Unlike <see cref="PlayerHurtbox"/> there is
    /// no Stay handler, and the cost is one component walk per radius crossing.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerNodeSensor : MonoBehaviour
    {
        [SerializeField] private Collider2D _trigger;

        private readonly List<NodeBase> _inRange = new List<NodeBase>();

        /// <summary>Exposed for EditMode tests, which stage nodes without real colliders.</summary>
        internal List<NodeBase> InRange => _inRange;

        private void Reset()
        {
            _trigger = GetComponent<Collider2D>();
            _trigger.isTrigger = true;
        }

        private void Awake()
        {
            if (_trigger == null) _trigger = GetComponent<Collider2D>();
            _trigger.isTrigger = true;
        }

        private void OnDisable()
        {
            // Exit callbacks do not fire for a disabled collider, so stale entries would survive
            // a respawn.
            _inRange.Clear();
        }

        /// <summary>
        /// Nearest ready node to <paramref name="from"/>, or false when none is in range. Also
        /// prunes nodes destroyed while overlapping, whose Exit callback never fired.
        /// </summary>
        public bool TryGetNearest(Vector2 from, out NodeBase nearest)
        {
            nearest = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = _inRange.Count - 1; i >= 0; i--)
            {
                NodeBase node = _inRange[i];

                if (node == null)
                {
                    _inRange.RemoveAt(i);
                    continue;
                }

                if (!node.IsReady) continue;

                float sqr = (node.Position - from).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                nearest = node;
            }

            return nearest != null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // GetComponentInParent so the node can put its radius collider on a child, which is
            // exactly how Node_Base is built.
            if (other.GetComponentInParent<NodeBase>() is NodeBase node && !_inRange.Contains(node))
            {
                _inRange.Add(node);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<NodeBase>() is NodeBase node)
            {
                _inRange.Remove(node);
            }
        }
    }
}
