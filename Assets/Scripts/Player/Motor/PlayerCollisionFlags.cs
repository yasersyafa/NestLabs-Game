using System;

namespace NestLabs.Player
{
    /// <summary>What the motor was touching after the last <see cref="PlayerMotor.Move"/> resolve.</summary>
    [Flags]
    public enum PlayerCollisionFlags
    {
        None = 0,
        WallLeft = 1 << 0,
        WallRight = 1 << 1,
        Grounded = 1 << 2,
        Ceiling = 1 << 3,

        AnyWall = WallLeft | WallRight
    }
}
