using UnityEngine;

namespace Nestlabs.Wall
{
    // Climbable terrain, not a hazard - the player's wall-latch mechanic (PlayerSensor /
    // LatchState) detects this purely by a non-trigger Collider2D on the "Solid" physics
    // layer, with no tag or component check. This class carries no behavior of its own;
    // it exists only as a typed prefab reference for WallPairSpawnRuleSO and for the
    // level generator's active-instance tracking/culling.
    [RequireComponent(typeof(Collider2D))]
    public class WallTerrain : MonoBehaviour
    {
    }
}
