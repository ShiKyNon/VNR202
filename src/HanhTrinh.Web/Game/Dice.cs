namespace HanhTrinh.Web.Game;

public record DiceRoll(int Raw, int Steps, string Formula, int From, int To, int Seq);

public enum ModKind { Multiply, Cap, Add }
public record DiceMod(ModKind Kind, int Value, string Label);

public static class DiceCalc
{
    /// <summary>Số gieo → ×2 → giới hạn trên (nhỏ nhất) → cộng/trừ → tối thiểu 1 (spec §5.8).</summary>
    public static (int Steps, string Formula) Compute(int raw, IReadOnlyList<DiceMod> mods)
    {
        var v = raw;
        var parts = new List<string> { raw.ToString() };
        foreach (var m in mods.Where(m => m.Kind == ModKind.Multiply)) { v *= m.Value; parts.Add($"×{m.Value} ({m.Label})"); }
        var caps = mods.Where(m => m.Kind == ModKind.Cap).ToList();
        if (caps.Count > 0)
        {
            var cap = caps.MinBy(c => c.Value)!;
            if (v > cap.Value) { v = cap.Value; parts.Add($"→ tối đa {cap.Value} ({cap.Label})"); }
        }
        foreach (var m in mods.Where(m => m.Kind == ModKind.Add))
        {
            v += m.Value;
            parts.Add(m.Value >= 0 ? $"+ {m.Value} ({m.Label})" : $"− {-m.Value} ({m.Label})");
        }
        if (v < 1) v = 1;
        return (v, parts.Count == 1 ? $"{raw}" : $"{string.Join(" ", parts)} = {v}");
    }
}
