namespace NestLabs.Shared.Flow
{
    // Published through MessagePipe so HUD, spawners, input and audio can react to a flow change
    // without holding a reference to whatever caused it.
    // readonly struct: MessagePipe's generic brokers keep it unboxed.

    public readonly struct GameStateChangedEvent
    {
        public readonly GameState From;
        public readonly GameState To;

        public GameStateChangedEvent(GameState from, GameState to)
        {
            From = from;
            To = to;
        }
    }
}
