using HanhTrinh.Web.Game;
using Microsoft.EntityFrameworkCore;

namespace HanhTrinh.Web.Persistence;

public class MatchRecord
{
    public int Id { get; set; }
    public string RoomCode { get; set; } = "";
    public string Season { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int PlayerCount { get; set; }
    public List<MatchResultRecord> Results { get; set; } = [];
}

public class MatchResultRecord
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public MatchRecord? Match { get; set; }
    public int Rank { get; set; }
    public string Name { get; set; } = "";
    public string IconId { get; set; } = "";
    public int FinalTile { get; set; }
    public string TileName { get; set; } = "";
    public bool Finished { get; set; }
    public long? FinishTimeMs { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public bool Left { get; set; }

    public double Percent => Correct + Wrong == 0 ? 0 : 100.0 * Correct / (Correct + Wrong);
    public RankRow ToRow() => new(Rank, $"r{Id}", Name, IconId, FinalTile, TileName, Finished,
        FinishTimeMs is { } ms ? TimeSpan.FromMilliseconds(ms) : null, Correct, Wrong, Left);
}

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<MatchRecord> Matches => Set<MatchRecord>();
    public DbSet<MatchResultRecord> Results => Set<MatchResultRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<MatchRecord>().HasMany(m => m.Results).WithOne(r => r.Match).HasForeignKey(r => r.MatchId);
        b.Entity<MatchResultRecord>().HasIndex(r => r.FinishTimeMs);
        b.Entity<MatchRecord>().HasIndex(m => m.EndedAt);
    }
}
