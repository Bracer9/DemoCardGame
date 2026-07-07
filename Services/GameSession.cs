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

    public GameState NewAiGame()
    {
        lock (_sync)
        {
            _state = _engine.CreateGame();
            _state.LocalViewerPlayerId = _state.Players[0].Id;
            _state.AiPlayerId = _state.Players[1].Id;
            _state.Players[1].Name = "AI";
            return _state;
        }
    }

    public GameState NewTestGame()
    {
        lock (_sync) { _state = _engine.CreateTestGame(); return _state; }
    }
}
