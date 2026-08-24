namespace NestLabs.Player
{
    /// <summary>
    /// The animation-name contract every skin asset must satisfy. Skins are swapped by prefab
    /// variant, so the only thing binding a skin to the FSM is this set of strings.
    /// </summary>
    public static class PlayerAnimId
    {
        public const string Latch = "Latch";
        public const string Slide = "Slide";
        public const string Jump = "Jump";
        public const string Fall = "Fall";
        public const string Dash = "Dash";
        public const string Hit = "Hit";
        public const string Dead = "Dead";

        /// <summary>Every name a skin must define. Used by the startup validation in PlayerVisual.</summary>
        public static readonly string[] All = { Latch, Slide, Jump, Fall, Dash, Hit, Dead };

        /// <summary>Maps a state to its animation. Returns null for <see cref="PlayerStateId.None"/>.</summary>
        public static string For(PlayerStateId state)
        {
            switch (state)
            {
                case PlayerStateId.Latch: return Latch;
                case PlayerStateId.Slide: return Slide;
                case PlayerStateId.Jump: return Jump;
                case PlayerStateId.Fall: return Fall;
                case PlayerStateId.Dash: return Dash;
                case PlayerStateId.Hit: return Hit;
                case PlayerStateId.Dead: return Dead;
                default: return null;
            }
        }
    }
}
