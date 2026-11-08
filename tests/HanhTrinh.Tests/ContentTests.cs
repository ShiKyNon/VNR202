using HanhTrinh.Web.Game;

namespace HanhTrinh.Tests;

public class ContentTests
{
    private static ContentStore C => TestData.Real;
    private static int Idx(string n) => C.Tiles.Single(t => t.Name == n).Index;

    [Fact]
    public void Tiles_Are65InSpecOrder()
    {
        Assert.Equal(65, C.Tiles.Count);
        Assert.Equal("Hà Giang", C.Tiles[0].Name);
        Assert.Equal("Cà Mau", C.Tiles[64].Name);
        Assert.Equal(Idx("Đà Nẵng") + 1, Idx("Hoàng Sa"));
        Assert.Equal(Idx("Khánh Hòa") + 1, Idx("Trường Sa"));
        Assert.All(C.Tiles.Where(t => t.Index <= 25), t => Assert.Equal(Region.Bac, t.Region));
        Assert.All(C.Tiles.Where(t => t.Index is >= 26 and <= 41), t => Assert.Equal(Region.Trung, t.Region));
        Assert.All(C.Tiles.Where(t => t.Index is >= 42 and <= 46), t => Assert.Equal(Region.TayNguyen, t.Region));
        Assert.All(C.Tiles.Where(t => t.Index >= 47), t => Assert.Equal(Region.Nam, t.Region));
        Assert.Equal(65, C.Tiles.Select(t => t.Name).Distinct().Count());
    }

    [Fact]
    public void Tags_MatchSpec()
    {
        Assert.Equal(28, C.Tiles.Count(t => t.Tags.HasFlag(TileTags.VenBien)));
        Assert.Equal(2, C.Tiles.Count(t => t.Tags.HasFlag(TileTags.Dao)));
        Assert.Equal(14, C.Tiles.Count(t => t.Tags.HasFlag(TileTags.MienNuiPhiaBac)));
        Assert.Equal(6, C.Tiles.Count(t => t.Tags.HasFlag(TileTags.BacTrungBo)));
    }

    [Fact]
    public void Questions_AreWellFormed()
    {
        Assert.True(C.Questions.Count >= 300, $"only {C.Questions.Count}");
        var all = C.Questions.Select(q => (q.Id, q.Text, q.Options, q.Answer, q.Explanation, q.Page))
            .Concat(C.Seasonal.Select(q => (q.Id, q.Text, q.Options, q.Answer, q.Explanation, q.Page))).ToList();
        Assert.Equal(all.Count, all.Select(q => q.Id).Distinct().Count());
        foreach (var q in all)
        {
            Assert.False(string.IsNullOrWhiteSpace(q.Text), q.Id);
            Assert.Equal(4, q.Options.Length);
            Assert.Equal(4, q.Options.Distinct().Count());
            Assert.All(q.Options, o => Assert.False(string.IsNullOrWhiteSpace(o)));
            Assert.InRange(q.Answer, 0, 3);
            Assert.False(string.IsNullOrWhiteSpace(q.Explanation), q.Id);
            Assert.InRange(q.Page, 1, 440);
        }
        Assert.All(C.Questions, q => Assert.InRange(q.Chapter, 0, 3));
    }

    [Fact]
    public void Seasonal_ProvincesExist_AtLeast4PerPair()
    {
        var names = C.Tiles.Select(t => t.Name).ToHashSet();
        Assert.All(C.Seasonal, s => Assert.Contains(s.Province, names));
        Assert.All(C.Seasonal.GroupBy(s => (s.Province, s.Season)), g => Assert.True(g.Count() >= 4, $"{g.Key}"));
    }

    [Fact]
    public void Icons_Are60Unique()
    {
        Assert.Equal(60, C.Icons.Count);
        Assert.Equal(60, C.Icons.Distinct().Count());
    }
}
