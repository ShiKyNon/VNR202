namespace HanhTrinh.Web.Game;

public enum EffectKind
{
    // buff câu hỏi (vào túi)
    BinhDanHocVu, DietGiacDot, KimChiNam, DoiMoi, PheBinh, ChopThoiCo,
    // buff tự kích hoạt
    ThanToc, TiepVienTruongSon, DoanTauKhongSo, NamQuyenChuDong, DiBatBien, TheTranLongDan,
    // debuff
    KhungBoTrang, BaoVayCamVan, KhungHoangKinhTe, NganSongCamCho, ChuQuanNongVoi, DienBienHoaBinh,
}

public record EffectInfo(EffectKind Kind, string Name, string Description, bool IsBuff, bool IsBagItem, int Weight, string Icon);

public static class EffectCatalog
{
    public static readonly IReadOnlyList<EffectInfo> All =
    [
        new(EffectKind.BinhDanHocVu, "Bình dân học vụ", "Loại 1 đáp án sai", true, true, 3, "📖"),
        new(EffectKind.DietGiacDot, "Diệt giặc dốt", "Loại 2 đáp án sai", true, true, 3, "📚"),
        new(EffectKind.KimChiNam, "Kim chỉ nam", "Loại cả 3 đáp án sai (hiếm)", true, true, 1, "🧭"),
        new(EffectKind.DoiMoi, "Đổi mới", "Đổi sang câu hỏi khác", true, true, 3, "🔄"),
        new(EffectKind.PheBinh, "Phê bình và tự phê bình", "Chọn sai thì được chọn lại 1 lần, không bị chờ", true, true, 3, "🪞"),
        new(EffectKind.ChopThoiCo, "Chớp thời cơ", "Bỏ qua câu hỏi này, gieo xúc xắc luôn", true, true, 3, "⚡"),
        new(EffectKind.ThanToc, "Thần tốc", "Lần gieo kế tiếp được nhân đôi", true, false, 3, "🚀"),
        new(EffectKind.TiepVienTruongSon, "Tiếp viện Trường Sơn", "Tiến thêm 2 ô ngay", true, false, 3, "🚚"),
        new(EffectKind.DoanTauKhongSo, "Đoàn tàu không số", "Nhảy ngay đến ô ven biển kế tiếp", true, false, 3, "🚢"),
        new(EffectKind.NamQuyenChuDong, "Nắm quyền chủ động", "Lần gieo kế tiếp được tự chọn số 1–6", true, false, 3, "🎯"),
        new(EffectKind.DiBatBien, "Dĩ bất biến, ứng vạn biến", "Miễn chờ cho lần trả lời sai kế tiếp", true, false, 3, "🛡"),
        new(EffectKind.TheTranLongDan, "Thế trận lòng dân", "Chặn 1 debuff kế tiếp từ ô 🔴 hoặc ô đảo", true, false, 3, "🤝"),
        new(EffectKind.KhungBoTrang, "Khủng bố trắng", "Lùi 2 ô", false, false, 3, "⬅"),
        new(EffectKind.BaoVayCamVan, "Bao vây cấm vận", "Lần sai kế tiếp phải chờ 10 giây", false, false, 3, "⛓"),
        new(EffectKind.KhungHoangKinhTe, "Khủng hoảng kinh tế", "Lần gieo kế tiếp tối đa 3", false, false, 3, "📉"),
        new(EffectKind.NganSongCamCho, "Ngăn sông cấm chợ", "Phải trả lời đúng 2 câu mới được gieo", false, false, 3, "🚧"),
        new(EffectKind.ChuQuanNongVoi, "Chủ quan, nóng vội", "Câu kế tiếp chỉ có 10 giây, hết giờ tính là sai", false, false, 3, "⏳"),
        new(EffectKind.DienBienHoaBinh, "Diễn biến hòa bình", "Mất 1 buff đang giữ", false, false, 3, "🌫"),
    ];

    public static EffectInfo Get(EffectKind k) => All[(int)k];

    public static EffectKind DrawBuff(Random r) => Draw(r, All.Where(e => e.IsBuff));
    public static EffectKind DrawDebuff(Random r) => Draw(r, All.Where(e => !e.IsBuff));
    public static EffectKind DrawBagItem(Random r) => Draw(r, All.Where(e => e.IsBagItem));

    private static EffectKind Draw(Random r, IEnumerable<EffectInfo> items)
    {
        var list = items.ToList();
        var roll = r.Next(list.Sum(e => e.Weight));
        foreach (var e in list) { if (roll < e.Weight) return e.Kind; roll -= e.Weight; }
        return list[^1].Kind;
    }
}
