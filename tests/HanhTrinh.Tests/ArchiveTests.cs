using HanhTrinh.Web.Game;
using HanhTrinh.Web.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HanhTrinh.Tests;

public sealed class ArchiveTests : IDisposable
{
    private readonly SqliteConnection _conn = new("Data Source=:memory:");
    private readonly MatchArchive _archive;

    private sealed class Factory(DbContextOptions<AppDb> o) : IDbContextFactory<AppDb>
    {
        public AppDb CreateDbContext() => new(o);
    }

    public ArchiveTests()
    {
        _conn.Open();
        var opts = new DbContextOptionsBuilder<AppDb>().UseSqlite(_conn).Options;
        using (var db = new AppDb(opts)) db.Database.EnsureCreated();
        _archive = new MatchArchive(new Factory(opts));
    }

    public void Dispose() => _conn.Dispose();

    /// <summary>Trận 3 người: t1 về đích (thời gian finishSec), t2 đang giữa đường, t3 rời.</summary>
    private static Match Finished(int finishSec, int correct)
    {
        var (m, c) = TestData.NewMatch(Season.Thu, 3);
        foreach (var t in Enumerable.Range(1, 65)) m.SetMark(t, TileMark.None);
        m.Player("t2")!.Tile = 30;
        m.Leave("t3");
        var p = m.Player("t1")!;
        p.Tile = 64;
        p.Correct = correct - 1;
        c.Advance(TimeSpan.FromSeconds(finishSec));
        p.Pending.Add(EffectKind.NamQuyenChuDong);
        m.Answer("t1", TestData.CorrectIdx(m, "t1"), 1);
        c.Advance(TimeSpan.FromSeconds(90)); m.Tick();
        Assert.True(m.IsOver);
        return m;
    }

    [Fact]
    public async Task Save_StoresAllPlayersIncludingLeftAndUnfinished()
    {
        var id = await _archive.SaveAsync("123456", Finished(100, 10));
        var rec = await _archive.GetAsync(id);
        Assert.NotNull(rec);
        Assert.Equal(3, rec!.Results.Count);
        Assert.Contains(rec.Results, r => r.Left);
        Assert.Contains(rec.Results, r => !r.Finished && !r.Left);
        Assert.Equal("123456", rec.RoomCode);
    }

    [Fact]
    public async Task BestPercent_OnlyFinished_TieByFinishTime()
    {
        await _archive.SaveAsync("1", Finished(200, 10));
        await _archive.SaveAsync("2", Finished(100, 10));
        var best = await _archive.BestPercentAsync();
        Assert.All(best, r => Assert.True(r.Finished));
        Assert.Equal(2, best.Count);
        Assert.True(best[0].FinishTimeMs < best[1].FinishTimeMs);
    }

    [Fact]
    public async Task Fastest_Top5()
    {
        for (var i = 0; i < 7; i++) await _archive.SaveAsync($"{i}", Finished(100 + i * 10, 5));
        var fast = await _archive.FastestAsync();
        Assert.Equal(5, fast.Count);
        Assert.Equal(fast.OrderBy(r => r.FinishTimeMs).Select(r => r.Id), fast.Select(r => r.Id));
    }

    [Fact]
    public async Task History_NewestFirst_Paged()
    {
        for (var i = 0; i < 25; i++) await _archive.SaveAsync($"{i:D6}", Finished(100, 5));
        var (items, total) = await _archive.HistoryAsync(1);
        Assert.Equal(25, total);
        Assert.Equal(20, items.Count);
        var (p2, _) = await _archive.HistoryAsync(2);
        Assert.Equal(5, p2.Count);
    }
}

public class DbSetupTests
{
    [Fact]
    public void FromUrl_ConvertsNeonUrl()
    {
        var cs = HanhTrinh.Web.Persistence.DbSetup.FromUrl("postgresql://user%40x:p%3Ass@ep-cool.ap-southeast-1.aws.neon.tech/neondb?sslmode=require");
        Assert.Contains("Host=ep-cool.ap-southeast-1.aws.neon.tech", cs);
        Assert.Contains("Database=neondb", cs);
        Assert.Contains("Username=user@x", cs);
        Assert.Contains("Password=p:ss", cs);
        Assert.Contains("Port=5432", cs);
    }
}
