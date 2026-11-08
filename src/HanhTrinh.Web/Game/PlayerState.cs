namespace HanhTrinh.Web.Game;

public enum Phase { Question, Cooldown, Delay, Finished, Left }

public record SeenEntry(string QuestionId, string Text, string[] Options, int Chosen, int Answer, string Explanation, int Page, bool Bonus);

public record AnswerOutcome(bool Accepted, bool Correct, int CorrectIndex, string Explanation, int Page, DiceRoll? Roll, IReadOnlyList<string> Events)
{
    public static readonly AnswerOutcome Rejected = new(false, false, -1, "", 0, null, []);
}

public record RankRow(int Rank, string Token, string Name, string IconId, int Tile, string TileName, bool Finished,
    TimeSpan? FinishTime, int Correct, int Wrong, bool Left)
{
    public double Percent => Correct + Wrong == 0 ? 0 : 100.0 * Correct / (Correct + Wrong);
}

public sealed class PlayerState(string token, string name, string iconId)
{
    public string Token { get; } = token;
    public string Name { get; } = name;
    public string IconId { get; } = iconId;
    public int Tile { get; set; } = 1;
    public Phase Phase { get; set; } = Phase.Question;
    public DateTime PhaseEndsAt { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Streak { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public DateTime ReachedTileAt { get; set; }
    public List<EffectKind> Bag { get; } = [];
    public List<EffectKind> Pending { get; } = [];
    public List<SeenEntry> History { get; } = [];

    public Question? Current { get; set; }
    public bool CurrentIsBonus { get; set; }
    /// <summary>Thứ tự hiển thị: Order[displayIndex] = chỉ số đáp án gốc.</summary>
    public int[] Order { get; set; } = [0, 1, 2, 3];
    /// <summary>Chỉ số gốc của các đáp án đã bị loại.</summary>
    public HashSet<int> Removed { get; } = [];
    public bool PheBinhActive { get; set; }
    public DateTime? QuestionDeadline { get; set; }
    public int NeedCorrect { get; set; }
    public bool BonusUsedHere { get; set; }
    public bool TetBonus { get; set; }

    public DiceRoll? LastRoll { get; set; }
    /// <summary>Câu vừa trả lời sai (hiện trong lúc chờ).</summary>
    public SeenEntry? LastWrong { get; set; }
    public List<string> Log { get; } = [];

    public double Percent => Correct + Wrong == 0 ? 0 : 100.0 * Correct / (Correct + Wrong);
}
