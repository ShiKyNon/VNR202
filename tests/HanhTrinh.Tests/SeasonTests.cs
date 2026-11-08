using HanhTrinh.Web.Game;
using static HanhTrinh.Tests.TestData;

namespace HanhTrinh.Tests;

public class SeasonTests
{
    private static int Idx(string n) => Real.Tiles.Single(t => t.Name == n).Index;

    private static (Match M, TestClock C, PlayerState P) Setup(Season s, string province, ContentStore? content = null, int seed = 1)
    {
        var (m, c) = NewMatch(s, 1, content, seed);
        foreach (var t in Enumerable.Range(1, 65)) m.SetMark(t, TileMark.None);
        Put(m, "t1", Idx(province));
        var p = m.Player("t1")!;
        if (p.Phase == Phase.Delay) { c.Advance(TimeSpan.FromSeconds(5)); m.Tick(); }
        return (m, c, p);
    }

    private static AnswerOutcome Force(Match m, int steps)
    {
        m.Player("t1")!.Pending.Add(EffectKind.NamQuyenChuDong);
        return m.Answer("t1", CorrectIdx(m, "t1"), steps);
    }

    [Fact]
    public void Xuan_NomAm_Minus1InBacTrung_Min1()
    {
        var (m, _, _) = Setup(Season.Xuan, "Hà Nội");
        Assert.Equal(1, Force(m, 1).Roll!.Steps);
        var (m2, _, _) = Setup(Season.Xuan, "Thanh Hóa");
        Assert.Equal(4, Force(m2, 5).Roll!.Steps);
        var (m3, _, _) = Setup(Season.Xuan, "Cần Thơ");
        Assert.Equal(5, Force(m3, 5).Roll!.Steps);
    }

    [Fact]
    public void Xuan_Raw1or6_GivesBagBuff()
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var (m, _, p) = Setup(Season.Xuan, "Cần Thơ", seed: seed);
            var o = m.Answer("t1", CorrectIdx(m, "t1"));
            if (o.Roll!.Raw is 1 or 6) { Assert.NotEmpty(p.Bag); return; }
        }
        Assert.Fail();
    }

    [Fact]
    public void Xuan_KhoHan_Cooldown8sInTayNguyenNam()
    {
        var (m, c, p) = Setup(Season.Xuan, "Gia Lai");
        m.Answer("t1", WrongIdx(m, "t1"));
        Assert.Equal(c.Now.AddSeconds(8), p.PhaseEndsAt);
    }

    [Fact]
    public void Ha_GioLao_Max4InBacTrungBo()
    {
        var (m, _, _) = Setup(Season.Ha, "Nghệ An");
        Assert.Equal(4, Force(m, 6).Roll!.Steps);
    }

    [Fact]
    public void Ha_NangNong_QuestionDelayed1s()
    {
        var (m, c, p) = Setup(Season.Ha, "Hà Nội");
        m.Answer("t1", WrongIdx(m, "t1"));
        c.Advance(TimeSpan.FromSeconds(5)); m.Tick();
        Assert.Equal(Phase.Delay, p.Phase);
        c.Advance(TimeSpan.FromSeconds(1)); m.Tick();
        Assert.Equal(Phase.Question, p.Phase);
    }

    [Fact]
    public void Ha_DuLich_Plus1OnVenBienLanding_NoChain()
    {
        var (m, _, p) = Setup(Season.Ha, "Hưng Yên"); // 22 → 24 Nam Định (ven biển) → +1 = 25 Ninh Bình (ven biển, không chain)
        Force(m, 2);
        Assert.Equal(Idx("Ninh Bình"), p.Tile);
    }

    [Fact]
    public void Thu_DongLong_3StreakGivesChopThoiCo_ResetsStreak()
    {
        var (m, _, p) = Setup(Season.Thu, "Hà Nội");
        p.Streak = 2;
        Force(m, 1);
        Assert.Contains(EffectKind.ChopThoiCo, p.Bag);
        Assert.Equal(0, p.Streak);
    }

    [Fact]
    public void Thu_LuaChin_Plus1InMienNui()
    {
        var (m, _, _) = Setup(Season.Thu, "Sơn La");
        Assert.Equal(4, Force(m, 3).Roll!.Steps);
    }

    [Fact]
    public void Thu_TruBao_3sDelayOnVenBien()
    {
        var (m, c, p) = Setup(Season.Thu, "Hưng Yên");
        Force(m, 2); // Nam Định ven biển
        Assert.Equal(Phase.Delay, p.Phase);
        Assert.Equal(c.Now.AddSeconds(3), p.PhaseEndsAt);
    }

    [Fact]
    public void Dong_RetDam_Minus1InMienNui_MuaLu7s()
    {
        var (m, _, _) = Setup(Season.Dong, "Sơn La");
        var o = Force(m, 3);
        Assert.InRange(o.Roll!.Steps, 2, 3); // −1 rét đậm, có thể +1 Tết
        var (m2, c2, p2) = Setup(Season.Dong, "Quảng Bình");
        m2.Answer("t1", WrongIdx(m2, "t1"));
        Assert.Equal(c2.Now.AddSeconds(7), p2.PhaseEndsAt);
    }

    [Fact]
    public void Glowing_CorrectThenBonus_CorrectGives2Buffs()
    {
        var content = Fake(seasonal: [Sq("Hà Nội", Season.Thu, 1), Sq("Hà Nội", Season.Thu, 2)]);
        var (m, _, p) = Setup(Season.Thu, "Hà Nội", content);
        var o = m.Answer("t1", CorrectIdx(m, "t1"));
        Assert.Null(o.Roll);
        Assert.True(p.CurrentIsBonus);
        var before = p.Bag.Count + p.Pending.Count;
        o = m.Answer("t1", CorrectIdx(m, "t1"));
        Assert.NotNull(o.Roll);
        Assert.Equal(2, p.Correct);
        Assert.Contains(p.Log, l => l.Contains("nhận 2 buff"));
    }

    [Fact]
    public void Glowing_BonusWrong_NoCooldown_ThenRoll()
    {
        var content = Fake(seasonal: [Sq("Hà Nội", Season.Thu)]);
        var (m, _, p) = Setup(Season.Thu, "Hà Nội", content);
        m.Answer("t1", CorrectIdx(m, "t1"));
        var o = m.Answer("t1", WrongIdx(m, "t1"));
        Assert.NotNull(o.Roll);
        Assert.NotEqual(Phase.Cooldown, p.Phase);
        Assert.Equal(1, p.Wrong);
    }

    [Fact]
    public void Glowing_BonusOncePerVisit()
    {
        var content = Fake(seasonal: [Sq("Hà Nội", Season.Thu)]);
        var (m, c, p) = Setup(Season.Thu, "Hà Nội", content);
        m.Answer("t1", WrongIdx(m, "t1"));
        c.Advance(TimeSpan.FromSeconds(5)); m.Tick();
        m.Answer("t1", CorrectIdx(m, "t1"));
        Assert.True(p.CurrentIsBonus);
        m.Answer("t1", CorrectIdx(m, "t1"));
        Assert.False(p.CurrentIsBonus);
    }

    [Fact]
    public void SeasonCatalog_TitlesExact()
    {
        Assert.Equal("Trăm hoa đua nở", SeasonCatalog.Get(Season.Xuan).Title);
        Assert.Equal("Nắng hạ biển xanh", SeasonCatalog.Get(Season.Ha).Title);
        Assert.Equal("Đồng lòng vượt bão", SeasonCatalog.Get(Season.Thu).Title);
        Assert.Equal("Rét ngọt chờ Xuân", SeasonCatalog.Get(Season.Dong).Title);
    }
}
