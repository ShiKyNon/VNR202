using HanhTrinh.Web.Game;

namespace HanhTrinh.Tests;

public class RoomTests
{
    private readonly TestClock _clock = new();
    private readonly RoomManager _rm;
    private string Icon(int i) => TestData.Real.Icons[i];

    public RoomTests() => _rm = new RoomManager(TestData.Fake(), new SeenRegistry(), _clock.Fn, new Random(1));

    private Room Host(string tok = "h") => _rm.Create(tok, "Chủ").Room!;

    [Fact]
    public void Create_SixDigitUniqueCode()
    {
        var codes = Enumerable.Range(0, 20).Select(i => Host($"h{i}").Code).ToList();
        Assert.All(codes, c => Assert.Matches("^[0-9]{6}$", c));
        Assert.Equal(20, codes.Distinct().Count());
    }

    [Fact]
    public void Create_LimitedTo20Rooms()
    {
        for (var i = 0; i < 20; i++) Host($"h{i}");
        Assert.Equal(JoinResult.LimitReached, _rm.Create("x", "x").Result);
    }

    [Fact]
    public void Create_OneHostedRoomPerToken()
    {
        Host("h"); Host("h");
        Assert.Single(_rm.List());
    }

    [Fact]
    public void List_ShowsHostName()
    {
        Host();
        var l = Assert.Single(_rm.List());
        Assert.Equal("Chủ", l.HostName);
        Assert.Equal(1, l.Count);
    }

    [Fact]
    public void Locked_JoinFromListRejected_JoinByCodeOk()
    {
        var r = Host();
        _rm.SetLocked(r, "h", true);
        Assert.Equal(JoinResult.Locked, _rm.JoinFromList("h", "a", "A").Result);
        Assert.Equal(JoinResult.Ok, _rm.JoinByCode(r.Code, "a", "A").Result);
    }

    [Fact]
    public void Join_FullAt60()
    {
        var r = Host();
        for (var i = 1; i < 60; i++) Assert.Equal(JoinResult.Ok, _rm.JoinByCode(r.Code, $"p{i}", "P").Result);
        Assert.Equal(JoinResult.Full, _rm.JoinByCode(r.Code, "late", "L").Result);
    }

    [Fact]
    public void PickIcon_SecondClaimFails()
    {
        var r = Host();
        _rm.JoinByCode(r.Code, "a", "A");
        Assert.True(_rm.PickIcon(r, "h", Icon(0)));
        Assert.False(_rm.PickIcon(r, "a", Icon(0)));
        Assert.True(_rm.PickIcon(r, "a", Icon(1)));
    }

    [Fact]
    public void Kick_RemovesAndBans()
    {
        var r = Host();
        _rm.JoinByCode(r.Code, "a", "A");
        _rm.Kick(r, "h", "a");
        Assert.Null(r.SeatOf("a"));
        Assert.Null(_rm.FindByToken("a"));
        Assert.Equal(JoinResult.Banned, _rm.JoinByCode(r.Code, "a", "A").Result);
    }

    [Fact]
    public void Start_HostOnly_SoloAllowed_Countdown5sThenPlaying()
    {
        var r = Host();
        _rm.JoinByCode(r.Code, "a", "A");
        Assert.False(_rm.Start(r, "a"));
        _rm.Leave("a");
        Assert.True(_rm.Start(r, "h"));
        Assert.Equal(RoomStatus.Countdown, r.Status);
        Assert.NotNull(r.SeatOf("h")!.IconId); // tự gán icon nếu chưa chọn
        _clock.Advance(TimeSpan.FromSeconds(4.9)); _rm.Tick();
        Assert.Equal(RoomStatus.Countdown, r.Status);
        _clock.Advance(TimeSpan.FromSeconds(0.1)); _rm.Tick();
        Assert.Equal(RoomStatus.Playing, r.Status);
        Assert.NotNull(r.Match);
    }

    [Fact]
    public void HostLeave_TransfersHost()
    {
        var r = Host();
        _clock.Advance(TimeSpan.FromSeconds(1)); _rm.JoinByCode(r.Code, "a", "A");
        _clock.Advance(TimeSpan.FromSeconds(1)); _rm.JoinByCode(r.Code, "b", "B");
        _rm.Leave("h");
        Assert.Equal("a", r.HostToken);
    }

    [Fact]
    public void LastLeave_DeletesRoom()
    {
        Host();
        _rm.Leave("h");
        Assert.Empty(_rm.List());
    }

    private Room Playing()
    {
        var r = Host();
        _rm.JoinByCode(r.Code, "a", "A");
        _rm.Start(r, "h");
        _clock.Advance(TimeSpan.FromSeconds(5)); _rm.Tick();
        return r;
    }

    [Fact]
    public void HostLeaveDuringMatch_MatchContinues()
    {
        var r = Playing();
        _rm.Leave("h");
        Assert.Equal(RoomStatus.Playing, r.Status);
        Assert.Equal("a", r.HostToken);
        Assert.Equal(Phase.Left, r.Match!.Player("h")!.Phase);
    }

    [Fact]
    public void Rejoin_RestoresSameSeatAndPhase()
    {
        var r = Playing();
        var p = r.Match!.Player("a")!;
        r.Match.Answer("a", TestData.WrongIdx(r.Match, "a"));
        Assert.Same(r, _rm.FindByToken("a"));
        Assert.Equal(Phase.Cooldown, r.Match.Player("a")!.Phase);
        Assert.Same(p, r.Match.Player("a"));
    }

    [Fact]
    public void LeaveDuringMatch_CannotRejoin()
    {
        var r = Playing();
        _rm.Leave("a");
        Assert.Equal(JoinResult.LeftMatch, _rm.JoinByCode(r.Code, "a", "A").Result);
    }

    [Fact]
    public void JoinWhilePlaying_Rejected()
    {
        var r = Playing();
        Assert.Equal(JoinResult.Playing, _rm.JoinByCode(r.Code, "z", "Z").Result);
    }

    [Fact]
    public void MatchEnd_BackToLobbyKeepsSeats_RaisesMatchEnded()
    {
        var r = Playing();
        Match? ended = null;
        _rm.MatchEnded += (_, m) => ended = m;
        _clock.Advance(Match.HardCap); _rm.Tick();
        Assert.NotNull(ended);
        Assert.Equal(RoomStatus.Lobby, r.Status);
        Assert.Equal(2, r.Seats.Count);
        Assert.True(r.Match!.IsOver);
    }

    [Fact]
    public void AllLeaveDuringMatch_RoomRemovedAfterEnd()
    {
        var r = Playing();
        Match? ended = null;
        _rm.MatchEnded += (_, m) => ended = m;
        _rm.Leave("a"); _rm.Leave("h");
        _rm.Tick();
        Assert.NotNull(ended);
        Assert.Empty(_rm.List());
    }
}
