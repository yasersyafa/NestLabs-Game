using NestLabs.Shared.Culling;
using UnityEngine;

namespace Nestlabs.Wall
{
    // Climbable terrain, not a hazard - the player's wall-latch mechanic (PlayerSensor /
    // LatchState) detects this purely by a non-trigger Collider2D on the "Solid" physics
    // layer, with no tag or component check. It exists mainly as a typed prefab reference for
    // WallPairSpawnRuleSO and for the level generator's active-instance tracking/culling; the
    // only behavior it carries is toggling its own sprite off-screen - the collider is never
    // touched, since terrain above the current view still has to stay latchable as the player
    // climbs toward it.
    [RequireComponent(typeof(Collider2D))]
    public class WallTerrain : MonoBehaviour, IScreenVisibility
    {
        private SpriteRenderer sprite;

        public bool IsScreenVisible { get; private set; } = true;

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
        }

        public void SetScreenVisible(bool visible)
        {
            IsScreenVisible = visible;
            if (sprite != null) sprite.enabled = visible;
        }
    }
}
