using TinyPixelFights.Domain;

namespace TinyPixelFights.Services;

/// <summary>
/// Online room boundary. Combat rules remain entirely inside the shared GameEngine.
/// The current playtest server intentionally hosts one two-player room at a time.
/// </summary>
public sealed class OnlineGameSession
{
    private readonly object _sync = new();
    private readonly GameEngine _engine;
    private GameState _state;
    private string? _roomCode;
    private bool _started;
    private bool _dealStarted;
    private readonly OnlineSeat?[] _seats = new OnlineSeat?[2];

    public OnlineGameSession(GameEngine engine)
    {
        _engine = engine;
        _state = engine.CreateGame();
    }

    public RoomIdentity CreateRoom(string? playerName)
    {
        lock (_sync)
        {
            if (_roomCode is not null)
                throw new OnlineSessionException(L10n.Text("error.roomExists"), 409);

            _roomCode = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..6];
            var seat = new OnlineSeat(NewToken(), CleanName(playerName, "player.1"), true);
            _seats[0] = seat;
            return new RoomIdentity(seat.Token, _roomCode, true, 1);
        }
    }

    public RoomIdentity JoinRoom(string code, string? playerName)
    {
        lock (_sync)
        {
            if (_roomCode is null || !string.Equals(_roomCode, code.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new OnlineSessionException(L10n.Text("error.roomNotFound"), 404);
            if (_seats[1] is not null)
                throw new OnlineSessionException(L10n.Text("error.roomFull"), 409);

            var seat = new OnlineSeat(NewToken(), CleanName(playerName, "player.2"), false);
            _seats[1] = seat;
            return new RoomIdentity(seat.Token, _roomCode, false, 2);
        }
    }

    public RoomView ReadRoom(string token)
    {
        lock (_sync)
        {
            var (seat, index) = RequireSeat(token);
            seat.LastSeenUtc = DateTime.UtcNow;
            return new RoomView(_roomCode!, _started, _dealStarted, seat.IsHost, index + 1, seat.Name,
                _seats.Select((value, seatIndex) => new RoomPlayerView(
                    seatIndex + 1,
                    value?.Name,
                    value is not null && DateTime.UtcNow - value.LastSeenUtc < TimeSpan.FromSeconds(5))).ToArray());
        }
    }

    public T ReadGame<T>(string token, Func<GameState, OnlineSeat, int, T> action)
    {
        lock (_sync)
        {
            var (seat, index) = RequireSeat(token);
            EnsureStarted();
            seat.LastSeenUtc = DateTime.UtcNow;
            return action(_state, seat, index);
        }
    }

    public T WriteGame<T>(string token, bool requireActivePlayer, Func<GameState, OnlineSeat, int, T> action)
    {
        lock (_sync)
        {
            var (seat, index) = RequireSeat(token);
            EnsureStarted();
            if (requireActivePlayer && _state.Players[index].Id != _state.ActivePlayerId)
                throw new OnlineSessionException(L10n.Text("error.opponentTurn"), 403);

            seat.LastSeenUtc = DateTime.UtcNow;
            return action(_state, seat, index);
        }
    }

    public GameState NewGame(string token)
    {
        lock (_sync)
        {
            var (seat, _) = RequireSeat(token);
            if (!seat.IsHost)
                throw new OnlineSessionException(L10n.Text("error.hostOnly"), 403);
            if (_seats[1] is null)
                throw new OnlineSessionException(L10n.Text("error.waitingPlayer"), 409);

            _state = _engine.CreateGame();
            _state.Players[0].Name = _seats[0]!.Name;
            _state.Players[1].Name = _seats[1]!.Name;
            _started = true;
            _dealStarted = false;
            return _state;
        }
    }

    public void BeginDeal(string token)
    {
        lock (_sync)
        {
            var (seat, _) = RequireSeat(token);
            EnsureStarted();
            seat.LastSeenUtc = DateTime.UtcNow;
            _dealStarted = true;
        }
    }

    private (OnlineSeat Seat, int Index) RequireSeat(string token)
    {
        var index = Array.FindIndex(_seats, seat => seat?.Token == token);
        if (index < 0)
            throw new OnlineSessionException(L10n.Text("error.invalidAuth"), 401);
        return (_seats[index]!, index);
    }

    private void EnsureStarted()
    {
        if (!_started)
            throw new OnlineSessionException(L10n.Text("error.matchNotStarted"), 409);
    }

    private static string NewToken() => Convert.ToHexString(Guid.NewGuid().ToByteArray());

    private static string CleanName(string? value, string fallback)
    {
        var name = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return name.Length > 16 ? name[..16] : name;
    }
}

public sealed class OnlineSeat(string token, string name, bool isHost)
{
    public string Token { get; } = token;
    public string Name { get; } = name;
    public bool IsHost { get; } = isHost;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}

public sealed class OnlineSessionException(LocalizedText error, int statusCode) : Exception(error.Key)
{
    public LocalizedText Error { get; } = error;
    public int StatusCode { get; } = statusCode;
}

public sealed record RoomIdentity(string Token, string RoomCode, bool IsHost, int Seat);
public sealed record RoomPlayerView(int Seat, string? Name, bool IsConnected);
public sealed record RoomView(string RoomCode, bool Started, bool DealStarted, bool IsHost, int Seat, string PlayerName,
    IReadOnlyList<RoomPlayerView> Players);
public sealed record CreateRoomRequest(string? PlayerName);
public sealed record JoinRoomRequest(string Code, string? PlayerName);
