using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HanhTrinh.Web.Game;

public static partial class NameFilter
{
    public const int MaxLength = 16;

    // Từ cấm so khớp nguyên từ sau khi bỏ dấu (tránh chặn nhầm tên thật).
    private static readonly HashSet<string> BannedWords =
    [
        "dm", "dmm", "dcm", "dkm", "dcmm", "dit", "deo", "lon", "buoi", "cac", "cc", "cl", "clm", "vl", "vkl", "vcl", "vcc",
        "dume", "ditme", "fuck", "shit", "bitch", "dick", "porn", "sex", "nigga", "nigger",
    ];
    // Chuỗi cấm so khớp cả khi viết liền.
    private static readonly string[] BannedFragments =
        ["ditme", "ditcon", "dmm", "dcm", "vcl", "vkl", "fuck", "shit", "bitch", "porn", "dume", "conlon", "occho", "oc cho"];

    [GeneratedRegex(@"^[\p{L}\p{M}\p{N} ]+$")]
    private static partial Regex Allowed();

    public static (bool Ok, string Name, string? Error) Validate(string? raw)
    {
        var name = Regex.Replace((raw ?? "").Trim(), @"\s+", " ").Normalize(NormalizationForm.FormC);
        if (name.Length == 0) return (false, name, "Bạn chưa nhập tên.");
        if (name.Length > MaxLength) return (false, name, $"Tên tối đa {MaxLength} ký tự.");
        if (!Allowed().IsMatch(name)) return (false, name, "Tên chỉ gồm chữ, số và khoảng trắng.");
        var plain = Plain(name);
        var words = plain.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var compact = plain.Replace(" ", "");
        if (words.Any(BannedWords.Contains) || BannedFragments.Any(f => compact.Contains(f.Replace(" ", ""))))
            return (false, name, "Tên không phù hợp, bạn chọn tên khác nhé.");
        return (true, name, null);
    }

    private static string Plain(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch switch { 'đ' or 'Đ' => 'd', _ => char.ToLowerInvariant(ch) });
        }
        return sb.ToString();
    }
}
