namespace HanhTrinh.Web.Game;

public record SeasonInfo(Season Season, string Title, string Emoji, string Css, IReadOnlyList<string> Pros, IReadOnlyList<string> Cons);

public static class SeasonCatalog
{
    private static readonly SeasonInfo[] Items =
    [
        new(Season.Xuan, "Trăm hoa đua nở", "🌸", "xuan",
            ["Tết đoàn viên: gieo ra 1 hoặc 6 → nhận 1 buff câu hỏi"],
            ["Nồm ẩm (miền Bắc, Trung): mỗi lần gieo −1 bước", "Khô hạn, xâm nhập mặn (Tây Nguyên, Nam): sai phải chờ 8 giây"]),
        new(Season.Ha, "Nắng hạ biển xanh", "☀️", "ha",
            ["Mùa du lịch biển: dừng ở ô ven biển → tiến thêm 1 ô", "Mùa trái cây (Nam Bộ): trả lời đúng có 30% nhận thêm 1 buff"],
            ["Gió Lào (Bắc Trung Bộ): mỗi lần gieo tối đa 4", "Nắng nóng: câu hỏi hiện chậm 1 giây"]),
        new(Season.Thu, "Đồng lòng vượt bão", "🍂", "thu",
            ["Đồng lòng: đúng 3 câu liên tiếp → nhận buff Chớp thời cơ", "Lúa chín vùng cao (miền núi phía Bắc): +1 bước"],
            ["Trú bão (ô ven biển và 2 đảo): chờ 3 giây trước câu tiếp theo"]),
        new(Season.Dong, "Rét ngọt chờ Xuân", "❄️", "dong",
            ["Chuẩn bị Tết: mỗi câu đúng có 50% được +1 vào lần gieo"],
            ["Rét đậm (miền núi phía Bắc): −1 bước", "Mưa lũ cuối năm (Trung Bộ): sai phải chờ 7 giây"]),
    ];

    public static SeasonInfo Get(Season s) => Items[(int)s];
}
