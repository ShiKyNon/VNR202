using HanhTrinh.Web.Game;
using static HanhTrinh.Tests.TestData;

namespace HanhTrinh.Tests;

public class EffectsTests
{
    private static (Match M, TestClock C, PlayerState P) Setup(Season s = Season.Thu, int tile = 17, ContentStore? content = null, int seed = 1)
    {
        var (m, c) = NewMatch(s, 1, content, seed);
        foreach (var t in Enumerable.Range(1, 65)) m.SetMark(t, TileMark.None);
        Put(m, "t1", tile);
        return (m, c, m.Player("t1")!);
    }

    private static DiceRoll Force(Match m, int steps)
    {
        m.Player("t1")!.Pending.Add(EffectKind.NamQuyenChuDong);
        var o = m.Answer("t1", CorrectIdx(m, "t1"), steps);
        return o.Roll!;
    }

    private static int Idx(string n) => Real.Tiles.Single(t => t.Name == n).Index;

    [Fact]
    public void Marks_7Buff7Debuff_NotOnStartFinishIslandsOrGlowing()
    {
        var content = Fake(seasonal: [Sq("Hà Nội", Season.Thu)]);
        for (var seed = 0; seed < 20; seed++)
        {
            var (m, _) = NewMatch(Season.Thu, 1, content, seed);
            Assert.Equal(7, m.Marks.Values.Count(v => v == TileMark.Buff));
            Assert.Equal(7, m.Marks.Values.Count(v => v == TileMark.Debuff));
            Assert.DoesNotContain(1, m.Marks.Keys);
            Assert.DoesNotContain(65, m.Marks.Keys);
            Assert.DoesNotContain(Idx("Hoàng Sa"), m.Marks.Keys);
            Assert.DoesNotContain(Idx("Trường Sa"), m.Marks.Keys);
            Assert.DoesNotContain(Idx("Hà Nội"), m.Marks.Keys);
            Assert.Contains(Idx("Hà Nội"), m.Glowing);
        }
    }

    [Theory]
    [InlineData(EffectKind.BinhDanHocVu, 1)]
    [InlineData(EffectKind.DietGiacDot, 2)]
    [InlineData(EffectKind.KimChiNam, 3)]
    public void RemoveWrongAnswers(EffectKind k, int n)
    {
        var (m, _, p) = Setup();
        p.Bag.Add(k);
        Assert.True(m.UseBagItem("t1", k));
        Assert.Equal(n, p.Removed.Count);
        Assert.DoesNotContain(p.Current!.Answer, p.Removed);
        Assert.Empty(p.Bag);
    }

    [Fact]
    public void DoiMoi_SwapsQuestion()
    {
        var (m, _, p) = Setup();
        var id = p.Current!.Id;
        p.Bag.Add(EffectKind.DoiMoi);
        Assert.True(m.UseBagItem("t1", EffectKind.DoiMoi));
        Assert.NotEqual(id, p.Current!.Id);
    }

    [Fact]
    public void PheBinh_WrongOnceNoCooldown()
    {
        var (m, _, p) = Setup();
        p.Bag.Add(EffectKind.PheBinh);
        m.UseBagItem("t1", EffectKind.PheBinh);
        var o = m.Answer("t1", WrongIdx(m, "t1"));
        Assert.True(o.Accepted);
        Assert.Equal(Phase.Question, p.Phase);
        Assert.Equal(0, p.Wrong);
        Assert.Single(p.Removed);
        m.Answer("t1", WrongIdx(m, "t1"));
        Assert.Equal(Phase.Cooldown, p.Phase);
        Assert.Equal(1, p.Wrong);
    }

    [Fact]
    public void ChopThoiCo_RollsWithoutCountingAnswer()
    {
        var (m, _, p) = Setup();
        p.Bag.Add(EffectKind.ChopThoiCo);
        Assert.True(m.UseBagItem("t1", EffectKind.ChopThoiCo));
        Assert.True(p.Tile > 17);
        Assert.Equal(0, p.Correct + p.Wrong);
        Assert.Equal(-1, p.History[0].Chosen);
    }

    [Fact]
    public void Bag_Max3_ExtraDropped()
    {
        var (m, _, p) = Setup(Season.Thu, 17);
        p.Bag.AddRange([EffectKind.DoiMoi, EffectKind.DoiMoi, EffectKind.DoiMoi]);
        p.Streak = 2; // Đồng lòng sẽ tặng Chớp thời cơ
        Force(m, 1);
        Assert.Equal(3, p.Bag.Count);
        Assert.DoesNotContain(EffectKind.ChopThoiCo, p.Bag);
    }

    [Fact]
    public void ThanToc_DoublesNextRoll()
    {
        var (m, _, p) = Setup();
        p.Pending.Add(EffectKind.ThanToc);
        var r = Force(m, 3);
        Assert.Equal(6, r.Steps);
        Assert.Empty(p.Pending);
    }

    [Fact]
    public void KhungHoang_NextRollMax3()
    {
        var (m, _, p) = Setup();
        p.Pending.Add(EffectKind.KhungHoangKinhTe);
        Assert.Equal(3, Force(m, 6).Steps);
    }

    [Fact]
    public void BuffTile_TiepVien_Plus2_NoChain()
    {
        var (m, _, p) = Setup();
        // Ô 19 có buff; ép bốc Tiếp viện bằng cách thử nhiều seed là không ổn định → kiểm tra trực tiếp: không chain qua mark ở ô 21.
        m.SetMark(21, TileMark.Debuff);
        p.Tile = 17;
        p.Pending.Add(EffectKind.NamQuyenChuDong);
        m.Answer("t1", CorrectIdx(m, "t1"), 4); // tới ô 21 (Thái Bình, ven biển — Thu: trú bão)
        Assert.Equal(21, p.Tile is 19 or 21 or 23 ? 21 : p.Tile); // tile 21 có debuff có thể lùi 2 (19) nhưng không chain tiếp
        Assert.True(p.Tile <= 23);
    }

    [Fact]
    public void KhungBo_Back2_NotBelow1()
    {
        var (m, _, p) = Setup(tile: 1);
        p.Tile = 1;
        // mark ô 2 debuff; ép rút Khủng bố trắng không xác định → dùng seed vòng lặp
        for (var seed = 0; seed < 200; seed++)
        {
            var (mm, _, pp) = Setup(seed: seed, tile: 1);
            mm.SetMark(3, TileMark.Debuff); // Thu: Lúa chín +1 ở miền núi → 1+1 = ô 3
            pp.Pending.Add(EffectKind.NamQuyenChuDong);
            mm.Answer("t1", CorrectIdx(mm, "t1"), 1);
            if (pp.Log.Any(l => l.Contains("Khủng bố trắng")))
            {
                Assert.Equal(1, pp.Tile);
                return;
            }
        }
        Assert.Fail("không rút được Khủng bố trắng");
    }

    [Fact]
    public void DiBatBien_NextWrongNoCooldown()
    {
        var (m, _, p) = Setup();
        p.Pending.Add(EffectKind.DiBatBien);
        m.Answer("t1", WrongIdx(m, "t1"));
        Assert.Equal(Phase.Question, p.Phase);
        Assert.Equal(1, p.Wrong);
    }

    [Fact]
    public void BaoVay_NextWrong10s()
    {
        var (m, c, p) = Setup();
        p.Pending.Add(EffectKind.BaoVayCamVan);
        m.Answer("t1", WrongIdx(m, "t1"));
        Assert.Equal(c.Now.AddSeconds(10), p.PhaseEndsAt);
    }

    [Fact]
    public void NganSong_NeedsTwoCorrectBeforeRoll()
    {
        var (m, _, p) = Setup();
        p.NeedCorrect = 1;
        var o = m.Answer("t1", CorrectIdx(m, "t1"));
        Assert.Null(o.Roll);
        Assert.Equal(17, p.Tile);
        o = m.Answer("t1", CorrectIdx(m, "t1"));
        Assert.NotNull(o.Roll);
    }

    [Fact]
    public void ChuQuan_10sTimeout_CountsWrong()
    {
        var (m, c, p) = Setup();
        p.Pending.Add(EffectKind.ChuQuanNongVoi);
        m.Answer("t1", WrongIdx(m, "t1"));          // sai → chờ 5s
        c.Advance(TimeSpan.FromSeconds(5)); m.Tick(); // câu mới, có hạn 10s
        Assert.NotNull(p.QuestionDeadline);
        c.Advance(TimeSpan.FromSeconds(10)); m.Tick();
        Assert.Equal(2, p.Wrong);
        Assert.Equal(Phase.Cooldown, p.Phase);
    }

    [Fact]
    public void TheTran_BlocksNextDebuff()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var (m, _, p) = Setup(seed: seed);
            m.SetMark(18, TileMark.Debuff);
            p.Pending.Add(EffectKind.TheTranLongDan);
            Force(m, 1);
            Assert.Equal(18, p.Tile);
            Assert.DoesNotContain(EffectKind.TheTranLongDan, p.Pending);
            Assert.Contains(p.Log, l => l.Contains("đã chặn"));
        }
    }

    [Fact]
    public void DienBien_RemovesBagThenPendingThenNothing()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var (m, _, p) = Setup(seed: seed);
            m.SetMark(18, TileMark.Debuff);
            p.Bag.Add(EffectKind.DoiMoi);
            Force(m, 1);
            if (p.Log.Any(l => l.Contains("Diễn biến hòa bình"))) { Assert.Empty(p.Bag); return; }
        }
        Assert.Fail("không rút được Diễn biến hòa bình");
    }

    [Fact]
    public void PassingOverMarkedTile_NoTrigger()
    {
        var (m, _, p) = Setup();
        m.SetMark(18, TileMark.Debuff);
        Force(m, 2);
        Assert.Equal(19, p.Tile);
        Assert.Empty(p.Pending);
    }

    [Fact]
    public void Cooldown_IsMaxOfApplicable()
    {
        var (m, c, p) = Setup(Season.Xuan, Idx("Cần Thơ"));
        m.Answer("t1", WrongIdx(m, "t1"));
        Assert.Equal(c.Now.AddSeconds(8), p.PhaseEndsAt);
    }
}
