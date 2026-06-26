using TinyPixelFights.Domain;

namespace TinyPixelFights.Services;

public sealed class GameSession
{
    private readonly object _sync = new();
    private readonly GameEngine _engine;
    private GameState _state;

    public GameSession(GameEngine engine) { _engine = engine; _state = engine.CreateGame(); }

    public T Read<T>(Func<GameState, T> action) { lock (_sync) return action(_state); }
    public T Write<T>(Func<GameState, T> action) { lock (_sync) return action(_state); }

    public GameState NewGame()
    {
        lock (_sync) { _state = _engine.CreateGame(); return _state; }
    }
}
