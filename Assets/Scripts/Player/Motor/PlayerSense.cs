namespace NestLabs.Player
{
    /// <summary>
    /// Immutable snapshot of what the player can feel around it this frame, produced by
    /// <see cref="PlayerSensor"/>. States read this instead of casting rays themselves.
    /// </summary>
    public readonly struct PlayerSense
    {
        /// <summary>-1 when a wall is on the left, +1 when on the right, 0 when touching neither.</summary>
        public readonly int WallSide;

        public readonly bool Grounded;

        /// <summary>Distance to the nearest wall, or <see cref="float.PositiveInfinity"/> when none is in range.</summary>
        public readonly float WallDistance;

        public readonly PlayerCollisionFlags Flags;

        public PlayerSense(int wallSide, bool grounded, float wallDistance, PlayerCollisionFlags flags)
        {
            WallSide = wallSide;
            Grounded = grounded;
            WallDistance = wallDistance;
            Flags = flags;
        }

        public bool OnWall => WallSide != 0;

        public static PlayerSense Nothing =>
            new PlayerSense(0, false, float.PositiveInfinity, PlayerCollisionFlags.None);
    }
}
