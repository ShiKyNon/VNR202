using HanhTrinh.Web.Game;

namespace HanhTrinh.Tests;

public sealed class TestClock
{
    public DateTime Now { get; set; } = new(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);
    public void Advance(TimeSpan t) => Now += t;
    public Func<DateTime> Fn => () => Now;
}

public static class TestData
{
    private static ContentStore? _real;
    public static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");
    public static ContentStore Real => _real ??= ContentStore.Load(DataDir);

    /// <summary>Bản đồ thật, pool câu hỏi giả (đáp án luôn ở chỉ số 0), câu mùa tùy chọn.</summary>
    public static ContentStore Fake(int questions = 50, IReadOnlyList<SeasonalQuestion>? seasonal = null) => new()
    {
        Tiles = Real.Tiles,
        Questions = Enumerable.Range(1, questions).Select(i => new Question($"q{i}", $"Câu {i}?", ["Đúng", "Sai1", "Sai2", "Sai3"], 0, "Giải thích", 10, 1)).ToList(),
        Seasonal = seasonal ?? [],
        Icons = Real.Icons,
    };

    public static SeasonalQuestion Sq(string province, Season s, int n = 1) =>
        new($"s-{province}-{s}-{n}", province, s, "Câu lịch sử?", ["Đúng", "Sai1", "Sai2", "Sai3"], 0, "gt", 100);

    public static (Match M, TestClock Clock) NewMatch(Season season = Season.Xuan, int players = 1, ContentStore? content = null, int seed = 1)
    {
        var clock = new TestClock();
        var list = Enumerable.Range(1, players).Select(i => ($"t{i}", $"P{i}", $"i{i}"));
        var m = new Match(content ?? Fake(), list, new SeenRegistry(), new Random(seed), clock.Fn, season);
        return (m, clock);
    }

    public static int CorrectIdx(Match m, string token) => Match.DisplayIndexOf(m.Player(token)!, m.Player(token)!.Current!.Answer);
    public static int WrongIdx(Match m, string token)
    {
        var p = m.Player(token)!;
        return Enumerable.Range(0, 4).First(i => p.Order[i] != p.Current!.Answer && !p.Removed.Contains(p.Order[i]));
    }

    /// <summary>Đặt người chơi lên ô, xóa mark ngẫu nhiên ảnh hưởng (dùng reflection-free: dịch bằng Tile setter).</summary>
    public static void Put(Match m, string token, int tile)
    {
        var p = m.Player(token)!;
        p.Tile = tile;
    }
}
