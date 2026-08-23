namespace NestLabs.Player
{
    /// <summary>
    /// Reports nothing, ever. Swapped in for cutscenes and on death, and used as the default in
    /// tests that do not care about input.
    /// </summary>
    public sealed class NullPlayerInput : IPlayerInput
    {
        public static readonly NullPlayerInput Instance = new NullPlayerInput();

        public bool Enabled { get; set; }

        public bool HasBufferedTap(float bufferDuration) => false;

        public void ConsumeTap() { }

        public void ClearTap() { }
    }
}
