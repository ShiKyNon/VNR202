using HanhTrinh.Web.Game;
using static HanhTrinh.Tests.TestData;

namespace HanhTrinh.Tests;

public class PickerTests
{
    private static readonly List<Question> Pool = Enumerable.Range(1, 5).Select(i => new Question($"q{i}", "t", ["a", "b", "c", "d"], 0, "e", 1, 1)).ToList();

    [Fact]
    public void Picker_NeverRepeatsUntilExhausted()
    {
        var seen = new HashSet<string>();
        var p = new QuestionPicker(Pool, new Random(3));
        Assert.Equal(5, Enumerable.Range(0, 5).Select(_ => p.Next(seen).Id).Distinct().Count());
    }

    [Fact]
    public void Picker_ResetsWhenPoolExhausted()
    {
        var seen = new HashSet<string>();
        var p = new QuestionPicker(Pool, new Random(3));
        for (var i = 0; i < 5; i++) p.Next(seen);
        Assert.NotNull(p.Next(seen));
        Assert.Single(seen);
    }
}

public class DiceTests
{
    [Fact]
    public void Dice_Order_MultiplyCapAddMin1()
    {
        Assert.Equal(3, DiceCalc.Compute(6, [new(ModKind.Multiply, 2, "Thần tốc"), new(ModKind.Cap, 3, "Khủng hoảng kinh tế")]).Steps);
        Assert.Equal(1, DiceCalc.Compute(1, [new(ModKind.Add, -1, "Nồm ẩm")]).Steps);
        var (s, f) = DiceCalc.Compute(6, [new(ModKind.Multiply, 2, "Thần tốc"), new(ModKind.Add, -1, "Nồm ẩm")]);
        Assert.Equal(11, s);
        Assert.Equal("6 ×2 (Thần tốc) − 1 (Nồm ẩm) = 11", f);
        Assert.Equal(3, DiceCalc.Compute(6, [new(ModKind.Cap, 4, "Gió Lào"), new(ModKind.Cap, 3, "KH")]).Steps);
    }
}

public class MatchTests
{
    // Mùa Thu, đứng ở đồng bằng Bắc Bộ không có hiệu ứng mùa tác động (trừ ven biển/miền núi).
    private static (Match, TestClock) Plain(int players = 1, int seed = 1)
    {
        var (m, c) = NewMatch(Season.Thu, players, seed: seed);
        foreach (var t in Enumerable.Range(1, 65)) m.SetMark(t, TileMark.None);
        return (m, c);
    }

    private static DiceRoll Force(Match m, string tok, int steps)
    {
        m.Player(tok)!.Pending.Add(EffectKind.NamQuyenChuDong);
        var o = m.Answer(tok, CorrectIdx(m, tok), steps);
        Assert.True(o.Correct);
        return o.Roll!;
    }

    [Fact]
    public void Correct_RollsAndMoves()
    {
        var (m, _) = Plain();
        var o = m.Answer("t1", CorrectIdx(m, "t1"));
        Assert.True(o.Accepted && o.Correct);
        Assert.NotNull(o.Roll);
        Assert.Equal(1 + o.Roll!.Steps, o.Roll.To);
        var p = m.Player("t1")!;
        Assert.Equal(1, p.Correct);
        Assert.True(p.Phase is Phase.Question or Phase.Delay);
    }

    [Fact]
    public void Wrong_Cooldown5sThenNewQuestionSameTile()
    {
        var (m, c) = Plain();
        Put(m, "t1", 17); // Hà Nội: không ven biển/miền núi
        var p = m.Player("t1")!;
        var first = p.Current!.Id;
        var o = m.Answer("t1", WrongIdx(m, "t1"));
        Assert.False(o.Correct);
        Assert.Equal(Phase.Cooldown, p.Phase);
        Assert.Equal(c.Now.AddSeconds(5), p.PhaseEndsAt);
        Assert.NotNull(p.LastWrong);
        c.Advance(TimeSpan.FromSeconds(5)); m.Tick();
        Assert.Equal(Phase.Question, p.Phase);
        Assert.NotEqual(first, p.Current!.Id);
        Assert.Equal(17, p.Tile);
        Assert.Equal(1, p.Wrong);
    }

    [Fact]
    public void Answer_IgnoredWhenNotInQuestionPhase()
    {
        var (m, _) = Plain();
        m.Answer("t1", WrongIdx(m, "t1"));
        var o = m.Answer("t1", 0);
        Assert.False(o.Accepted);
        Assert.Equal(1, m.Player("t1")!.Wrong);
        Assert.Equal(0, m.Player("t1")!.Correct);
    }

    [Fact]
    public void Overshoot_FinishesAtTile65_SoloEnds()
    {
        var (m, c) = Plain();
        Put(m, "t1", 63);
        c.Advance(TimeSpan.FromSeconds(42));
        Force(m, "t1", 6);
        var p = m.Player("t1")!;
        Assert.Equal(65, p.Tile);
        Assert.Equal(Phase.Finished, p.Phase);
        Assert.Equal(TimeSpan.FromSeconds(42), p.FinishTime);
        Assert.True(m.IsOver);
    }

    [Fact]
    public void Multi_EndsWhenAllFinished()
    {
        var (m, _) = Plain(2);
        Put(m, "t1", 64); Put(m, "t2", 64);
        Force(m, "t1", 1);
        Assert.False(m.IsOver);
        Force(m, "t2", 1);
        Assert.True(m.IsOver);
    }

    [Fact]
    public void Multi_Ends90sAfterFirstFinish()
    {
        var (m, c) = Plain(2);
        Put(m, "t1", 64);
        Force(m, "t1", 1);
        c.Advance(TimeSpan.FromSeconds(89)); m.Tick();
        Assert.False(m.IsOver);
        c.Advance(TimeSpan.FromSeconds(1)); m.Tick();
        Assert.True(m.IsOver);
    }

    [Fact]
    public void LeftPlayers_IgnoredForAllFinished()
    {
        var (m, _) = Plain(2);
        m.Leave("t2");
        Put(m, "t1", 64);
        Force(m, "t1", 1);
        Assert.True(m.IsOver);
    }

    [Fact]
    public void HardCap40m_Ends()
    {
        var (m, c) = Plain(2);
        c.Advance(TimeSpan.FromMinutes(40) - TimeSpan.FromSeconds(1)); m.Tick();
        Assert.False(m.IsOver);
        c.Advance(TimeSpan.FromSeconds(1)); m.Tick();
        Assert.True(m.IsOver);
    }

    [Fact]
    public void Ranking_FinishedFirstThenTileThenPercent_IncludesLeft()
    {
        var (m, c) = Plain(4);
        Put(m, "t1", 64); c.Advance(TimeSpan.FromSeconds(1)); Force(m, "t1", 1);
        Put(m, "t2", 30); Put(m, "t3", 40); Put(m, "t4", 40);
        m.Player("t3")!.Correct = 1; m.Player("t3")!.Wrong = 1;
        m.Player("t4")!.Correct = 2;
        m.Leave("t2");
        var r = m.Ranking();
        Assert.Equal(["t1", "t4", "t3", "t2"], r.Select(x => x.Token));
        Assert.True(r[3].Left);
        Assert.True(r[0].Finished);
    }

    [Fact]
    public void History_RecordsChosenAndAnswer()
    {
        var (m, _) = Plain();
        var p = m.Player("t1")!;
        var wrongDisp = WrongIdx(m, "t1");
        var orig = p.Order[wrongDisp];
        m.Answer("t1", wrongDisp);
        Assert.Single(p.History);
        Assert.Equal(orig, p.History[0].Chosen);
        Assert.Equal(0, p.History[0].Answer);
    }
}
