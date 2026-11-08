using System.Text.Json;
using System.Text.Json.Serialization;

namespace HanhTrinh.Web.Game;

public enum Region { Bac, Trung, TayNguyen, Nam }

[Flags]
public enum TileTags { None = 0, VenBien = 1, Dao = 2, MienNuiPhiaBac = 4, BacTrungBo = 8 }

public enum Season { Xuan, Ha, Thu, Dong }

public record Tile(int Index, string Name, string Short, Region Region, TileTags Tags, double X, double Y);

public record Question(string Id, string Text, string[] Options, int Answer, string Explanation, int Page, int Chapter);

public record SeasonalQuestion(string Id, string Province, Season Season, string Text, string[] Options, int Answer, string Explanation, int Page)
{
    public Question AsQuestion() => new(Id, Text, Options, Answer, Explanation, Page, -1);
}

public sealed class ContentStore
{
    public required IReadOnlyList<Tile> Tiles { get; init; }
    public required IReadOnlyList<Question> Questions { get; init; }
    public required IReadOnlyList<SeasonalQuestion> Seasonal { get; init; }
    public required IReadOnlyList<string> Icons { get; init; }

    public Tile Tile(int index) => Tiles[index - 1];

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static ContentStore Load(string dataDir)
    {
        T Read<T>(string file) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(dataDir, file)), Json)!;
        var raw = Read<List<RawTile>>("provinces.json");
        return new ContentStore
        {
            Tiles = raw.Select((t, i) => new Tile(i + 1, t.Name, t.Short ?? t.Name, t.Region,
                (t.Tags ?? []).Aggregate(TileTags.None, (a, s) => a | Enum.Parse<TileTags>(s)), t.X, t.Y)).ToList(),
            Questions = Read<List<Question>>("questions.json"),
            Seasonal = Read<List<SeasonalQuestion>>("seasonal-questions.json"),
            Icons = Read<List<string>>("icons.json"),
        };
    }

    private record RawTile(string Name, string? Short, Region Region, string[]? Tags, double X, double Y);
}
