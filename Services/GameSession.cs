using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using TinyPixelFights.Domain;

namespace TinyPixelFights.Services;

public sealed class GameSession
{
    private const string LocalSessionHeader = "X-Local-Session";
    private const string FallbackSessionToken = "default-local-session";
    private const int CleanupInterval = 64;
    private const int MaxSessions = 64;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(12);

    private readonly ConcurrentDictionary<string, LocalGameSlot> _sessions = new(StringComparer.Ordinal);
    private readonly GameEngine _engine;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private long _accessCount;

    public GameSession(GameEngine engine, IHttpContextAccessor httpContextAccessor)
    {
        _engine = engine;
        _httpContextAccessor = httpContextAccessor;
    }

    public T Read<T>(Func<GameState, T> action)
    {
        var slot = GetSlot();
        lock (slot.Sync)
        {
            slot.LastSeenUtc = DateTime.UtcNow;
            return action(slot.State);
        }
    }

    public T Write<T>(Func<GameState, T> action)
    {
        var slot = GetSlot();
        lock (slot.Sync)
        {
            slot.LastSeenUtc = DateTime.UtcNow;
            return action(slot.State);
        }
    }

    public GameState NewGame()
    {
        var slot = GetSlot();
        lock (slot.Sync)
        {
            slot.LastSeenUtc = DateTime.UtcNow;
            slot.State = _engine.CreateGame();
            return slot.State;
        }
    }

    public GameState NewAiGame()
    {
        var slot = GetSlot();
        lock (slot.Sync)
        {
            slot.LastSeenUtc = DateTime.UtcNow;
            slot.State = _engine.CreateGame();
            slot.State.LocalViewerPlayerId = slot.State.Players[0].Id;
            slot.State.AiPlayerId = slot.State.Players[1].Id;
            slot.State.Players[1].Name = "AI";
            return slot.State;
        }
    }

    public GameState NewTestGame()
    {
        var slot = GetSlot();
        lock (slot.Sync)
        {
            slot.LastSeenUtc = DateTime.UtcNow;
            slot.State = _engine.CreateTestGame();
            return slot.State;
        }
    }

    private LocalGameSlot GetSlot()
    {
        var now = DateTime.UtcNow;
        if (Interlocked.Increment(ref _accessCount) % CleanupInterval == 0)
            Cleanup(now);

        var token = CurrentToken();
        return _sessions.GetOrAdd(token, _ => new LocalGameSlot(_engine.CreateGame(), now));
    }

    private string CurrentToken()
    {
        var token = _httpContextAccessor.HttpContext?.Request.Headers[LocalSessionHeader].ToString();
        return NormalizeToken(token);
    }

    private static string NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return FallbackSessionToken;

        var token = value.Trim();
        if (token.Length is < 16 or > 128)
            return FallbackSessionToken;

        return token.All(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            ? token
            : FallbackSessionToken;
    }

    private void Cleanup(DateTime now)
    {
        foreach (var session in _sessions)
        {
            if (session.Key == FallbackSessionToken)
                continue;
            if (now - session.Value.LastSeenUtc > SessionTtl)
                _sessions.TryRemove(session.Key, out _);
        }

        if (_sessions.Count <= MaxSessions)
            return;

        var sessionsToRemove = _sessions.Count - MaxSessions;
        foreach (var session in _sessions
            .Where(session => session.Key != FallbackSessionToken)
            .OrderBy(session => session.Value.LastSeenUtc)
            .Take(sessionsToRemove))
        {
            _sessions.TryRemove(session.Key, out _);
        }
    }

    private sealed class LocalGameSlot(GameState state, DateTime lastSeenUtc)
    {
        public object Sync { get; } = new();
        public GameState State { get; set; } = state;
        public DateTime LastSeenUtc { get; set; } = lastSeenUtc;
    }
}
