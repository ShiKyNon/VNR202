using HanhTrinh.Web.Game;
using Microsoft.EntityFrameworkCore;

namespace HanhTrinh.Web.Persistence;

public sealed class MatchArchive(IDbContextFactory<AppDb> factory)
{
    public async Task<int> SaveAsync(string roomCode, Match m)
    {
        var rows = m.Ranking();
        var rec = new MatchRecord
        {
            RoomCode = roomCode,
            Season = SeasonCatalog.Get(m.Season).Title,
            StartedAt = m.StartedAt,
            EndedAt = m.EndedAt ?? DateTime.UtcNow,
            PlayerCount = rows.Count,
            Results = rows.Select(r => new MatchResultRecord
            {
                Rank = r.Rank, Name = r.Name, IconId = r.IconId, FinalTile = r.Tile, TileName = r.TileName,
                Finished = r.Finished, FinishTimeMs = (long?)r.FinishTime?.TotalMilliseconds,
                Correct = r.Correct, Wrong = r.Wrong, Left = r.Left,
            }).ToList(),
        };
        await using var db = await factory.CreateDbContextAsync();
        db.Matches.Add(rec);
        await db.SaveChangesAsync();
        return rec.Id;
    }

    public async Task<IReadOnlyList<MatchResultRecord>> FastestAsync(int n = 5)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Results.Where(r => r.Finished && r.FinishTimeMs != null)
            .OrderBy(r => r.FinishTimeMs).ThenBy(r => r.Id).Take(n).AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<MatchResultRecord>> BestPercentAsync(int n = 5)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Results.Where(r => r.Finished && r.Correct + r.Wrong > 0)
            .OrderByDescending(r => (double)r.Correct / (r.Correct + r.Wrong)).ThenBy(r => r.FinishTimeMs).ThenBy(r => r.Id)
            .Take(n).AsNoTracking().ToListAsync();
    }

    public async Task<(IReadOnlyList<MatchRecord> Items, int Total)> HistoryAsync(int page, int pageSize = 20)
    {
        await using var db = await factory.CreateDbContextAsync();
        var total = await db.Matches.CountAsync();
        var items = await db.Matches.OrderByDescending(m => m.EndedAt).ThenByDescending(m => m.Id)
            .Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task<MatchRecord?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Matches.Include(m => m.Results.OrderBy(r => r.Rank)).AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
    }
}
