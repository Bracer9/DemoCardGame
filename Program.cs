using Microsoft.Extensions.FileProviders;
using TinyPixelFights.Api;
using TinyPixelFights.Domain;
using TinyPixelFights.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TraitRegistry>();
builder.Services.AddSingleton<RoleActionRegistry>();
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<GameSession>();
builder.Services.AddSingleton<OnlineGameSession>();
builder.Services.AddSingleton<GameViewFactory>();
builder.Services.AddSingleton<AttackPreviewService>();
builder.Services.AddSingleton<RoleActionPreviewService>();
builder.Services.AddSingleton<NormalAiService>();
builder.Services.AddSingleton<SimpleAiService>();

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

app.MapGet("/api/online/game/role-action/preview", (Guid actorId, string roleActionId,
    Guid? targetCharacterId, HttpRequest request, OnlineGameSession session,
    RoleActionPreviewService previews) =>
    Online(() => Results.Ok(new ApiEnvelope<RoleActionPreview>(session.ReadGame(Token(request), (state, _, index) =>
    {
        if (state.Players[index].Id != state.ActivePlayerId)
            throw new OnlineSessionException(L10n.Text("error.opponentTurn"), 403);
        return previews.Create(state, actorId, roleActionId, targetCharacterId);
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
        var rewardKind = engine.SelectReward(state, reward.InstanceId);
        var messageKey = rewardKind == RewardKind.DummyStatus ? "message.rewardPurchased" : "message.rewardSelected";
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text(messageKey)));
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
        var messageKey = state.RewardWindow?.PurchaseCount > 0 ? "message.rewardExited" : "message.rewardSkipped";
        engine.SkipRewardWindow(state);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text(messageKey)));
    })));

app.MapPost("/api/online/game/reward/back", (HttpRequest request,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(request), true, (state, seat, index) =>
    {
        engine.ReturnToRewardWindow(state, state.Players[index].Id);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.rewardBack")));
    })));

app.MapPost("/api/online/game/hero-draft/select", (SelectHeroDraftRequest request, HttpRequest httpRequest,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(httpRequest), true, (state, seat, index) =>
    {
        engine.SelectHeroDraft(state, state.Players[index].Id, request.CharacterKey);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.heroDraftSelected")));
    })));

app.MapPost("/api/online/game/hero-draft/reset", (HttpRequest httpRequest,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(httpRequest), true, (state, seat, index) =>
    {
        engine.ResetHeroDraft(state, state.Players[index].Id);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.rewardReset")));
    })));

app.MapPost("/api/online/game/hero-draft/soldier/select", (SelectSoldierDraftRequest request, HttpRequest httpRequest,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(httpRequest), true, (state, seat, index) =>
    {
        engine.SelectSoldierDraft(state, state.Players[index].Id, request.CharacterKeys);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.soldierDraftSelected")));
    })));

app.MapPost("/api/online/game/hero-draft/soldier/upgrade", (UpgradeSoldierDraftRequest request, HttpRequest httpRequest,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(httpRequest), true, (state, seat, index) =>
    {
        engine.UpgradeSoldierFromDraft(state, state.Players[index].Id, request.CharacterKey, request.TargetCharacterId);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.soldierRankUp")));
    })));

app.MapPost("/api/online/game/hero-draft/soldier/cancel", (HttpRequest httpRequest,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(httpRequest), true, (state, seat, index) =>
    {
        engine.CancelSoldierRecruitDraft(state, state.Players[index].Id);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.soldierRecruitExited")));
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

app.MapPost("/api/online/game/deputy/assign", (AssignDeputyRequest request, HttpRequest httpRequest,
    OnlineGameSession session, GameEngine engine, GameViewFactory views) => OnlineGameAction(() =>
    session.WriteGame(Token(httpRequest), true, (state, seat, index) =>
    {
        engine.AssignDeputy(state, request.SoldierId, request.HeroId);
        return Results.Ok(new ApiEnvelope<GameView>(
            views.Create(state, state.Players[index].Id, seat.IsHost), Message: L10n.Text("message.deputyAssigned")));
    })));

app.MapGet("/api/game/state", (GameSession session, GameViewFactory views) =>
    Results.Ok(new ApiEnvelope<GameView>(session.Read(views.Create))));

app.MapPost("/api/game/new", (GameSession session, GameViewFactory views) =>
    Results.Ok(new ApiEnvelope<GameView>(views.Create(session.NewGame()), Message: L10n.Text("message.newGame"))));

app.MapPost("/api/game/test/new", (GameSession session, GameViewFactory views) =>
    Results.Ok(new ApiEnvelope<GameView>(views.Create(session.NewTestGame()), Message: L10n.Text("message.newGame"))));

app.MapPost("/api/game/ai/new", (NewAiGameRequest request, GameSession session, GameViewFactory views) =>
{
    var difficulty = string.Equals(request.Difficulty, "normal", StringComparison.OrdinalIgnoreCase)
        ? AiDifficulty.Normal
        : AiDifficulty.Easy;
    return Results.Ok(new ApiEnvelope<GameView>(
        views.Create(session.NewAiGame(difficulty)),
        Message: L10n.Text("message.newGame")));
});

app.MapPost("/api/game/ai/advance", (GameSession session, SimpleAiService ai, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            ai.Advance(state);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state)));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapGet("/api/game/preview", (Guid attackerId, Guid defenderId, GameSession session, AttackPreviewService previews) =>
    Results.Ok(new ApiEnvelope<AttackPreview>(session.Read(state => previews.Create(state, attackerId, defenderId)))));

app.MapGet("/api/game/role-action/preview", (Guid actorId, string roleActionId, Guid? targetCharacterId,
    GameSession session, RoleActionPreviewService previews) =>
    Results.Ok(new ApiEnvelope<RoleActionPreview>(session.Read(state =>
        previews.Create(state, actorId, roleActionId, targetCharacterId)))));

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
            var rewardKind = engine.SelectReward(state, request.InstanceId);
            var messageKey = rewardKind == RewardKind.DummyStatus ? "message.rewardPurchased" : "message.rewardSelected";
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text(messageKey)));
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
            var messageKey = state.RewardWindow?.PurchaseCount > 0 ? "message.rewardExited" : "message.rewardSkipped";
            engine.SkipRewardWindow(state);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text(messageKey)));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/reward/back", (GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.ReturnToRewardWindow(state, state.ActivePlayerId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.rewardBack")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/hero-draft/select", (SelectHeroDraftRequest request, GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.SelectHeroDraft(state, state.ActivePlayerId, request.CharacterKey);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.heroDraftSelected")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/hero-draft/reset", (GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.ResetHeroDraft(state, state.ActivePlayerId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.rewardReset")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/hero-draft/soldier/select", (SelectSoldierDraftRequest request, GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.SelectSoldierDraft(state, state.ActivePlayerId, request.CharacterKeys);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.soldierDraftSelected")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/hero-draft/soldier/upgrade", (UpgradeSoldierDraftRequest request, GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.UpgradeSoldierFromDraft(state, state.ActivePlayerId, request.CharacterKey, request.TargetCharacterId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.soldierRankUp")));
        });
    }
    catch (GameRuleException exception) { return Results.BadRequest(new { error = exception.Error }); }
});

app.MapPost("/api/game/hero-draft/soldier/cancel", (GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.CancelSoldierRecruitDraft(state, state.ActivePlayerId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.soldierRecruitExited")));
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

app.MapPost("/api/game/deputy/assign", (AssignDeputyRequest request, GameSession session, GameEngine engine, GameViewFactory views) =>
{
    try
    {
        return session.Write(state =>
        {
            engine.AssignDeputy(state, request.SoldierId, request.HeroId);
            return Results.Ok(new ApiEnvelope<GameView>(views.Create(state), Message: L10n.Text("message.deputyAssigned")));
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
