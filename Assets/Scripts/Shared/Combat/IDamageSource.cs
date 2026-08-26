using UnityEngine;

namespace NestLabs.Shared.Combat
{
    /// <summary>
    /// Anything that can hurt the player implements this. Keeps the player hurtbox free of tag
    /// comparisons and layer switches, so a new hazard type needs no player-side change. Lives in
    /// Shared so obstacles can implement it without referencing the player assembly.
    /// </summary>
    public interface IDamageSource
    {
        int Damage { get; }

        /// <summary>World position the knockback is pushed away from.</summary>
        Vector2 Position { get; }
    }
}
