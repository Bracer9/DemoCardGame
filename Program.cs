using Microsoft.Extensions.FileProviders;
using TinyPixelFights.Api;
using TinyPixelFights.Domain;
using TinyPixelFights.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TraitRegistry>();
builder.Services.AddSingleton<RoleActionRegistry>();
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddSingleton<GameSession>();
builder.Services.AddSingleton<OnlineGameSession>();
builder.Services.AddSingleton<GameViewFactory>();
builder.Services.AddSingleton<AttackPreviewService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "assets")),
    RequestPath = "/assets"
});

// Online mode uses its own room/session boundary but shares GameEngine, DTOs, traits and assets.
app.MapPost("/api/online/room/create", (CreateRoomRequest request, OnlineGameSession session) =>
    Online(() => Results.Ok(new ApiEnvelope<RoomIdentity>(session.CreateRoom(request.PlayerName)))));

app.MapPost("/api/online/room/join", (JoinRoomRequest request, OnlineGameSession session) =>
    Online(() => Results.Ok(new ApiEnvelope<RoomIdentity>(session.JoinRoom(request.Code, request.PlayerName)))));

app.MapGet("/api/online/room", (HttpRequest request, OnlineGameSession session) =>
    Online(() => Results.Ok(new ApiEnvelope<RoomView>(session.ReadRoom(Token(request))))));

app.MapPost("/api/online/game/deal", (HttpRequest request, OnlineGameSession session) =>
    Online(() =>
    {
        session.BeginDeal(Token(request));
        return Results.Ok(new { data = new { dealStarted = true } });
    }));

app.MapGet("/api/online/game/state", (HttpRequest request, OnlineGameSession session, GameViewFactory views) =>
    Online(() => Results.Ok(new ApiEnvelope<GameView>(session.ReadGame(Token(request),
        (state, seat, index) => views.Create(state, state.Players[index].Id, seat.IsHost))))));

app.MapPost("/api/online/game/new", (HttpRequest request, OnlineGameSession session, GameViewFactory views) =>
    Online(() =>
    {
        var token = Token(request);
        session.NewGame(token);
        return Results.Ok(new ApiEnvelope<GameView>(session.ReadGame(token,
            (state, seat, index) => views.Create(state, state.Players[index].Id, seat.IsHost)),
            Message: L10n.Text("message.newGame")));
    }));

app.MapGet("/api/online/game/preview", (Guid attackerId, Guid defenderId, HttpRequest request,
    OnlineGameSession session, AttackPreviewService previews) =>
    Online(() => Results.Ok(new ApiEnvelope<AttackPreview>(session.ReadGame(Token(request), (state, _, index) =>
    {
        if (state.Players[index].Id != state.ActivePlayerId)
            throw new OnlineSessionException(L10n.Text("error.opponentTurn"), 403);
        return previews.Create(state, attackerId, defenderId);
    })))));

app.MapPost("/api/online/game/attack", (AttackRequest attack, HttpRequest request,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) =>
    OnlineGameAction(() => session.WriteGame(Token(request), true, (state, seat, index) =>
    {
        var result = engine.Attack(state, attack.AttackerId, attack.DefenderId);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(result.State, result.State.Players[index].Id, seat.IsHost), result.Outcome));
    })));

app.MapPost("/api/online/game/shield", (HttpRequest request, OnlineGameSession session,
    GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(request), true, (state, seat, index) =>
    {
        engine.DeployShield(state);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.shieldDeployed")));
    })));

app.MapPost("/api/online/game/end-turn", (HttpRequest request, OnlineGameSession session,
    GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(request), true, (state, seat, index) =>
    {
        engine.EndTurn(state);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.turnEnded")));
    })));

app.MapPost("/api/online/game/reward/select", (SelectRewardRequest reward, HttpRequest request,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(request), true, (state, seat, index) =>
    {
        engine.SelectReward(state, reward.InstanceId);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.rewardPurchased")));
    })));

app.MapPost("/api/online/game/reward/reset", (HttpRequest request,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(request), true, (state, seat, index) =>
    {
        engine.ResetRewardWindow(state);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.rewardReset")));
    })));

app.MapPost("/api/online/game/reward/skip", (HttpRequest request,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(request), true, (state, seat, index) =>
    {
        engine.SkipRewardWindow(state);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.rewardSkipped")));
    })));

app.MapPost("/api/online/game/role-action/upgrade", (SelectRoleActionUpgradeRequest request, HttpRequest httpRequest,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(httpRequest), true, (state, seat, index) =>
    {
        engine.SelectRoleActionUpgrade(state, request.CharacterId, request.RoleActionId);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.roleActionUnlocked")));
    })));

app.MapPost("/api/online/game/role-action/use", (UseRoleActionRequest request, HttpRequest httpRequest,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(httpRequest), true, (state, seat, index) =>
    {
        engine.UseRoleAction(state, request.CharacterId, request.RoleActionId, request.TargetCharacterId);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.roleActionUsed")));
    })));

app.MapGet("/api/game/state", (GameSession session, GameViewFactory views) =>
    Results.Ok(new ApiEnvelope<GameView>(session.Read(views.Create))));

app.MapPost("/api/game/new", (GameSession session, GameViewFactory views) =>
    Results.Ok(new ApiEnvelope<GameView>(views.Create(session.NewGame()), Message: L10n.Text("message.newGame"))));

app.MapGet("/api/game/preview", (Guid attackerId, Guid defenderId, GameSession session, AttackPreviewService previews) =>
    Results.Ok(new ApiEnvelope<AttackPreview>(session.Read(state => previews.Create(state, attackerId, defenderId)))));

app.MapGet("/api/audio/voice-index", (IWebHostEnvironment environment) =>
{
    var voiceRoot = Path.Combine(environment.ContentRootPath, "assets", "audio", "voice");
    var pools = new Dictionary<string, Dictionary<string, VoicePoolIndex>>(StringComparer.OrdinalIgnoreCase);
    if (!Directory.Exists(voiceRoot))
        return Results.Ok(new { version = 1, pools });

    foreach (var file in Directory.EnumerateFiles(voiceRoot, "*.*", SearchOption.AllDirectories))
    {
        if (!IsVoiceFile(file))
            continue;

        var relative = Path.GetRelativePath(voiceRoot, file);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Length < 3)
            continue;

        var characterId = segments[0];
        var voiceType = segments[1];
        if (!pools.TryGetValue(characterId, out var characterPools))
        {
            characterPools = new Dictionary<string, VoicePoolIndex>(StringComparer.OrdinalIgnoreCase);
            pools[characterId] = characterPools;
        }

        if (!characterPools.TryGetValue(voiceType, out var pool))
        {
            pool = new VoicePoolIndex([]);
            characterPools[voiceType] = pool;
        }

        pool.Sources.Add(ToAssetUrl(Path.Combine("audio", "voice", relative)));
    }

    foreach (var pool in pools.Values.SelectMany(characterPools => characterPools.Values))
        pool.Sources.Sort(StringComparer.OrdinalIgnoreCase);

    return Results.Ok(new { version = 1, pools });
});

app.MapPost("/api/game/attack", (AttackRequest request, GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            var result = engine.Attack(state, request.AttackerId, request.DefenderId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(result.State), result.Outcome));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/shield", (GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.DeployShield(state);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.shieldDeployed")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/end-turn", (GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.EndTurn(state);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.turnEnded")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/reward/select", (SelectRewardRequest request, GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.SelectReward(state, request.InstanceId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.rewardPurchased")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/reward/reset", (GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.ResetRewardWindow(state);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.rewardReset")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/reward/skip", (GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.SkipRewardWindow(state);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.rewardSkipped")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/role-action/upgrade", (SelectRoleActionUpgradeRequest request, GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.SelectRoleActionUpgrade(state, request.CharacterId, request.RoleActionId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.roleActionUnlocked")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/role-action/use", (UseRoleActionRequest request, GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.UseRoleAction(state, request.CharacterId, request.RoleActionId, request.TargetCharacterId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.roleActionUsed")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapFallbackToFile("index.html");
app.Run();

static string Token(HttpRequest request) => request.Headers["X-Player-Token"].ToString();

static bool IsVoiceFile(string path)
{
    var extension = Path.GetExtension(path);
    return extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase);
}

static string ToAssetUrl(string relativeAssetPath)
{
    var segments = relativeAssetPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .Where(segment => !string.IsNullOrWhiteSpace(segment))
        .Select(Uri.EscapeDataString);
    return "/assets/" + string.Join('/', segments);
}

static IResult Online(Func<IResult> action)
{
    try { return action(); }
    catch (OnlineSessionException exception)
    {
        return Results.Json(new { error = exception.Error }, statusCode: exception.StatusCode);
    }
}

static IResult OnlineGameAction(Func<IResult> action)
{
    try { return action(); }
    catch (OnlineSessionException exception)
    {
        return Results.Json(new { error = exception.Error }, statusCode: exception.StatusCode);
    }
    catch (GameRuleException exception)
    {
        return Results.BadRequest(new { error = exception.Error });
    }
}

sealed record VoicePoolIndex(List<string> Sources);
