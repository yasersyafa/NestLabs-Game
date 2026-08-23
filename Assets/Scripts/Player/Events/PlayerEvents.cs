using UnityEngine;

namespace NestLabs.Player
{
    // Published through MessagePipe so camera shake, HUD, SFX, haptics and scoring can react to
    // the player without the player holding a reference to any of them.
    // All readonly structs: MessagePipe's generic brokers keep them unboxed.

    public readonly struct PlayerStateChangedEvent
    {
        public readonly PlayerStateId From;
        public readonly PlayerStateId To;

        public PlayerStateChangedEvent(PlayerStateId from, PlayerStateId to)
        {
            From = from;
            To = to;
        }
    }

    public readonly struct PlayerJumpedEvent
    {
        /// <summary>The wall the player pushed off: -1 left, +1 right.</summary>
        public readonly int FromWallSide;

        public PlayerJumpedEvent(int fromWallSide)
        {
            FromWallSide = fromWallSide;
        }
    }

    public readonly struct PlayerDashedEvent
    {
        /// <summary>Horizontal direction of the burst: -1 left, +1 right.</summary>
        public readonly int Direction;

        public PlayerDashedEvent(int direction)
        {
            Direction = direction;
        }
    }

    public readonly struct PlayerLatchedEvent
    {
        public readonly int WallSide;

        public PlayerLatchedEvent(int wallSide)
        {
            WallSide = wallSide;
        }
    }

    public readonly struct PlayerHitEvent
    {
        public readonly int Damage;
        public readonly int RemainingHealth;
        public readonly Vector2 Knockback;

        public PlayerHitEvent(int damage, int remainingHealth, Vector2 knockback)
        {
            Damage = damage;
            RemainingHealth = remainingHealth;
            Knockback = knockback;
        }
    }

    public readonly struct PlayerDiedEvent
    {
        /// <summary>Where the player died. Useful for a respawn marker or a death VFX spawn point.</summary>
        public readonly Vector2 Position;

        public PlayerDiedEvent(Vector2 position)
        {
            Position = position;
        }
    }
}
