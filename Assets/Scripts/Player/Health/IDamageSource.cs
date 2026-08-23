using UnityEngine;

namespace NestLabs.Player
{
    /// <summary>
    /// Anything that can hurt the player implements this. Keeps <see cref="PlayerHurtbox"/> free
    /// of tag comparisons and layer switches — a new hazard type needs no player-side change.
    /// </summary>
    public interface IDamageSource
    {
        int Damage { get; }

        /// <summary>World position the knockback is pushed away from.</summary>
        Vector2 Position { get; }
    }
}
