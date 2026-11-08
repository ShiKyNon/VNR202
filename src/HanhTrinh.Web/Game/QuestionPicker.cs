using System.Collections.Concurrent;

namespace HanhTrinh.Web.Game;

/// <summary>Câu đã gặp theo người chơi (mã ẩn), giữ suốt thời gian server chạy.</summary>
public sealed class SeenRegistry
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _seen = new();
    public HashSet<string> For(string token) => _seen.GetOrAdd(token, _ => []);
}

public sealed class QuestionPicker(IReadOnlyList<Question> pool, Random rng)
{
    public Question Next(HashSet<string> seen)
    {
        var fresh = pool.Where(q => !seen.Contains(q.Id)).ToList();
        if (fresh.Count == 0) { seen.Clear(); fresh = [.. pool]; }
        var q = fresh[rng.Next(fresh.Count)];
        seen.Add(q.Id);
        return q;
    }
}
