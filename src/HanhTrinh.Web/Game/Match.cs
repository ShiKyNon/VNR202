namespace HanhTrinh.Web.Game;

public enum TileMark { None, Buff, Debuff }

/// <summary>Luật một trận. Không thread-safe: Room gọi trong lock.</summary>
public sealed class Match
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan GraceAfterFirstFinish = TimeSpan.FromSeconds(90);
    public static readonly TimeSpan HardCap = TimeSpan.FromMinutes(20);
    public const int FinishTile = 65;
    public const int BagMax = 3;
    public const int MarksPerKind = 7;

    private readonly ContentStore _c;
    private readonly SeenRegistry _seen;
    private readonly Random _rng;
    private readonly Func<DateTime> _now;
    private readonly QuestionPicker _picker;
    private readonly Dictionary<string, PlayerState> _players = new();
    private readonly List<PlayerState> _order = [];
    private int _rollSeq;

    public DateTime StartedAt { get; }
    public DateTime? EndedAt { get; private set; }
    public Season Season { get; }
    private readonly Dictionary<int, TileMark> _marks = new();
    public IReadOnlyDictionary<int, TileMark> Marks => _marks;
    internal void SetMark(int tile, TileMark mark) { if (mark == TileMark.None) _marks.Remove(tile); else _marks[tile] = mark; }
    public IReadOnlySet<int> Glowing { get; }
    public bool IsOver => EndedAt is not null;
    public DateTime? FirstFinishAt { get; private set; }
    public IReadOnlyList<PlayerState> Players => _order;

    public Match(ContentStore content, IEnumerable<(string Token, string Name, string IconId)> players,
        SeenRegistry seen, Random rng, Func<DateTime> now, Season season)
    {
        _c = content; _seen = seen; _rng = rng; _now = now; Season = season;
        _picker = new QuestionPicker(content.Questions, rng);
        StartedAt = now();

        var glowNames = content.Seasonal.Where(q => q.Season == season).Select(q => q.Province).ToHashSet();
        Glowing = content.Tiles.Where(t => glowNames.Contains(t.Name)).Select(t => t.Index).ToHashSet();

        var free = content.Tiles
            .Where(t => t.Index != 1 && t.Index != FinishTile && !t.Tags.HasFlag(TileTags.Dao) && !Glowing.Contains(t.Index))
            .Select(t => t.Index).OrderBy(_ => rng.Next()).ToList();
        var marks = _marks;
        foreach (var i in free.Take(MarksPerKind)) marks[i] = TileMark.Buff;
        foreach (var i in free.Skip(MarksPerKind).Take(MarksPerKind)) marks[i] = TileMark.Debuff;


        foreach (var (token, name, icon) in players)
        {
            var p = new PlayerState(token, name, icon) { ReachedTileAt = StartedAt };
            _players[token] = p; _order.Add(p);
            EnterQuestion(p, TimeSpan.Zero);
        }
    }

    public PlayerState? Player(string token) => _players.GetValueOrDefault(token);
    public Tile TileOf(PlayerState p) => _c.Tile(p.Tile);
    public Tile TileAt(int index) => _c.Tile(index);

    // ---------- câu hỏi ----------
    private void EnterQuestion(PlayerState p, TimeSpan arrivalDelay)
    {
        p.Current = _picker.Next(_seen.For(p.Token));
        p.CurrentIsBonus = false;
        SetupQuestion(p);
        var delay = arrivalDelay + (Season == Season.Ha ? TimeSpan.FromSeconds(1) : TimeSpan.Zero);
        if (delay > TimeSpan.Zero) { p.Phase = Phase.Delay; p.PhaseEndsAt = _now() + delay; }
        else Reveal(p);
    }

    private void SetupQuestion(PlayerState p)
    {
        p.Order = Enumerable.Range(0, 4).OrderBy(_ => _rng.Next()).ToArray();
        p.Removed.Clear();
        p.QuestionDeadline = null;
    }

    private void Reveal(PlayerState p)
    {
        p.Phase = Phase.Question;
        if (p.Pending.Remove(EffectKind.ChuQuanNongVoi)) p.QuestionDeadline = _now() + TimeSpan.FromSeconds(10);
    }

    public static int DisplayIndexOf(PlayerState p, int original) => Array.IndexOf(p.Order, original);

    public AnswerOutcome Answer(string token, int displayIndex, int? chosenSteps = null)
    {
        var p = Player(token);
        if (IsOver || p is null || p.Phase != Phase.Question || p.Current is null || displayIndex is < 0 or > 3) return AnswerOutcome.Rejected;
        var orig = p.Order[displayIndex];
        if (p.Removed.Contains(orig)) return AnswerOutcome.Rejected;
        var q = p.Current;
        var correct = orig == q.Answer;
        var events = new List<string>();
        var correctDisplay = DisplayIndexOf(p, q.Answer);

        if (!correct && p.PheBinhActive)
        {
            p.PheBinhActive = false;
            p.Removed.Add(orig);
            Log(p, events, "🪞 Phê bình và tự phê bình: được chọn lại!");
            return new AnswerOutcome(true, false, -1, "", 0, null, events);
        }
        p.PheBinhActive = false;
        p.History.Add(new SeenEntry(q.Id, q.Text, q.Options, orig, q.Answer, q.Explanation, q.Page, p.CurrentIsBonus));
        var wasBonus = p.CurrentIsBonus;

        if (!correct)
        {
            p.Wrong++; p.Streak = 0;
            DiceRoll? r = null;
            if (wasBonus) { Log(p, events, "Câu lịch sử chưa đúng — không sao, đi tiếp!"); r = Roll(p, chosenSteps, events); }
            else StartCooldown(p, events);
            return new AnswerOutcome(true, false, correctDisplay, q.Explanation, q.Page, r, events);
        }

        p.Correct++; p.Streak++;
        var tile = TileOf(p);
        if (Season == Season.Thu && p.Streak >= 3) { p.Streak = 0; Log(p, events, "🍂 Đồng lòng: 3 câu đúng liên tiếp!"); AddBag(p, EffectKind.ChopThoiCo, events); }
        if (Season == Season.Ha && tile.Region == Region.Nam && _rng.NextDouble() < 0.3) { Log(p, events, "🥭 Mùa trái cây!"); GiveBuff(p, EffectCatalog.DrawBuff(_rng), events, allowMove: false); }
        if (Season == Season.Dong && _rng.NextDouble() < 0.5) { p.TetBonus = true; Log(p, events, "🧧 Chuẩn bị Tết: +1 vào lần gieo"); }

        if (wasBonus)
        {
            Log(p, events, "✨ Đúng câu lịch sử: nhận 2 buff!");
            GiveBuff(p, EffectCatalog.DrawBuff(_rng), events, allowMove: false);
            GiveBuff(p, EffectCatalog.DrawBuff(_rng), events, allowMove: false);
        }
        else if (Glowing.Contains(p.Tile) && !p.BonusUsedHere && NextSeasonal(p) is { } sq)
        {
            p.BonusUsedHere = true;
            p.Current = sq.AsQuestion(); p.CurrentIsBonus = true;
            SetupQuestion(p); p.Phase = Phase.Question;
            Log(p, events, $"✨ {tile.Name} đang phát sáng: thêm 1 câu lịch sử!");
            return new AnswerOutcome(true, true, correctDisplay, q.Explanation, q.Page, null, events);
        }
        else if (p.NeedCorrect > 0)
        {
            p.NeedCorrect--;
            Log(p, events, "🚧 Ngăn sông cấm chợ: cần thêm 1 câu đúng nữa");
            EnterQuestion(p, TimeSpan.Zero);
            return new AnswerOutcome(true, true, correctDisplay, q.Explanation, q.Page, null, events);
        }

        var roll = Roll(p, chosenSteps, events);
        return new AnswerOutcome(true, true, correctDisplay, q.Explanation, q.Page, roll, events);
    }

    private SeasonalQuestion? NextSeasonal(PlayerState p)
    {
        var name = TileOf(p).Name;
        var seen = _seen.For(p.Token);
        var list = _c.Seasonal.Where(s => s.Season == Season && s.Province == name).ToList();
        if (list.Count == 0) return null;
        var fresh = list.Where(s => !seen.Contains(s.Id)).ToList();
        if (fresh.Count == 0) { foreach (var s in list) seen.Remove(s.Id); fresh = list; }
        var pick = fresh[_rng.Next(fresh.Count)];
        seen.Add(pick.Id);
        return pick;
    }

    private void StartCooldown(PlayerState p, List<string> events)
    {
        p.LastWrong = p.History[^1];
        if (p.Pending.Remove(EffectKind.DiBatBien))
        {
            Log(p, events, "🛡 Dĩ bất biến, ứng vạn biến: không phải chờ!");
            p.LastWrong = null;
            EnterQuestion(p, TimeSpan.Zero);
            return;
        }
        var t = TileOf(p);
        var cd = Cooldown;
        if (p.Pending.Remove(EffectKind.BaoVayCamVan)) cd = Max(cd, TimeSpan.FromSeconds(10));
        if (Season == Season.Xuan && t.Region is Region.TayNguyen or Region.Nam) cd = Max(cd, TimeSpan.FromSeconds(8));
        if (Season == Season.Dong && t.Region == Region.Trung && !t.Tags.HasFlag(TileTags.Dao)) cd = Max(cd, TimeSpan.FromSeconds(7));
        p.Phase = Phase.Cooldown;
        p.PhaseEndsAt = _now() + cd;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    // ---------- túi ----------
    public bool UseBagItem(string token, EffectKind kind, int? chosenSteps = null)
    {
        var p = Player(token);
        if (IsOver || p is null || p.Phase != Phase.Question || p.Current is null || !p.Bag.Contains(kind)) return false;
        var events = new List<string>();
        var info = EffectCatalog.Get(kind);
        switch (kind)
        {
            case EffectKind.BinhDanHocVu or EffectKind.DietGiacDot or EffectKind.KimChiNam:
                var n = kind == EffectKind.BinhDanHocVu ? 1 : kind == EffectKind.DietGiacDot ? 2 : 3;
                var wrong = Enumerable.Range(0, 4).Where(i => i != p.Current.Answer && !p.Removed.Contains(i))
                    .OrderBy(_ => _rng.Next()).Take(n).ToList();
                if (wrong.Count == 0) return false;
                foreach (var i in wrong) p.Removed.Add(i);
                break;
            case EffectKind.DoiMoi:
                if (p.CurrentIsBonus) return false;
                p.Current = _picker.Next(_seen.For(p.Token));
                SetupQuestion(p);
                break;
            case EffectKind.PheBinh:
                if (p.PheBinhActive) return false;
                p.PheBinhActive = true;
                break;
            case EffectKind.ChopThoiCo:
                var q = p.Current;
                p.History.Add(new SeenEntry(q.Id, q.Text, q.Options, -1, q.Answer, q.Explanation, q.Page, p.CurrentIsBonus));
                p.NeedCorrect = 0;
                p.Bag.Remove(kind);
                Log(p, events, $"{info.Icon} Dùng {info.Name}: gieo xúc xắc luôn!");
                Roll(p, chosenSteps, events);
                return true;
            default: return false;
        }
        p.Bag.Remove(kind);
        Log(p, events, $"{info.Icon} Dùng {info.Name}");
        return true;
    }

    // ---------- xúc xắc & di chuyển ----------
    private DiceRoll Roll(PlayerState p, int? chosenSteps, List<string> events)
    {
        p.Current = null; p.CurrentIsBonus = false; p.QuestionDeadline = null;
        int raw;
        var chosen = false;
        if (chosenSteps is >= 1 and <= 6 && p.Pending.Remove(EffectKind.NamQuyenChuDong)) { raw = chosenSteps.Value; chosen = true; }
        else raw = _rng.Next(1, 7);

        if (Season == Season.Xuan && !chosen && raw is 1 or 6) { Log(p, events, "🌸 Tết đoàn viên!"); AddBag(p, EffectCatalog.DrawBagItem(_rng), events); }

        var tile = TileOf(p);
        var mods = new List<DiceMod>();
        if (p.Pending.Remove(EffectKind.ThanToc)) mods.Add(new(ModKind.Multiply, 2, "Thần tốc"));
        if (p.Pending.Remove(EffectKind.KhungHoangKinhTe)) mods.Add(new(ModKind.Cap, 3, "Khủng hoảng kinh tế"));
        if (Season == Season.Ha && tile.Tags.HasFlag(TileTags.BacTrungBo)) mods.Add(new(ModKind.Cap, 4, "Gió Lào"));
        if (Season == Season.Xuan && tile.Region is Region.Bac or Region.Trung && !tile.Tags.HasFlag(TileTags.Dao)) mods.Add(new(ModKind.Add, -1, "Nồm ẩm"));
        if (Season == Season.Thu && tile.Tags.HasFlag(TileTags.MienNuiPhiaBac)) mods.Add(new(ModKind.Add, 1, "Lúa chín"));
        if (Season == Season.Dong && tile.Tags.HasFlag(TileTags.MienNuiPhiaBac)) mods.Add(new(ModKind.Add, -1, "Rét đậm"));
        if (p.TetBonus) { p.TetBonus = false; mods.Add(new(ModKind.Add, 1, "Chuẩn bị Tết")); }

        var (steps, formula) = DiceCalc.Compute(raw, mods);
        var from = p.Tile;
        var to = Math.Min(FinishTile, from + steps);
        var roll = new DiceRoll(raw, steps, formula, from, to, ++_rollSeq);
        p.LastRoll = roll;
        MoveTo(p, to);
        if (p.Phase != Phase.Finished) Land(p, events);
        return roll;
    }

    private void MoveTo(PlayerState p, int to)
    {
        p.Tile = Math.Clamp(to, 1, FinishTile);
        p.ReachedTileAt = _now();
        p.BonusUsedHere = false;
        if (p.Tile == FinishTile) Finish(p);
    }

    private void Finish(PlayerState p)
    {
        p.Phase = Phase.Finished;
        p.Current = null;
        p.FinishTime = _now() - StartedAt;
        FirstFinishAt ??= _now();
        CheckEnd();
    }

    // Chỉ di chuyển bằng xúc xắc mới gọi Land (không kích hoạt dây chuyền, spec §5.4).
    private void Land(PlayerState p, List<string> events)
    {
        var tile = TileOf(p);
        if (tile.Tags.HasFlag(TileTags.Dao))
        {
            Log(p, events, $"🏝 {tile.Name}: bốc thẻ!");
            if (_rng.Next(2) == 0) GiveBuff(p, EffectCatalog.DrawBuff(_rng), events, allowMove: true);
            else ApplyDebuff(p, EffectCatalog.DrawDebuff(_rng), events);
        }
        else if (Marks.GetValueOrDefault(p.Tile) == TileMark.Buff) GiveBuff(p, EffectCatalog.DrawBuff(_rng), events, allowMove: true);
        else if (Marks.GetValueOrDefault(p.Tile) == TileMark.Debuff) ApplyDebuff(p, EffectCatalog.DrawDebuff(_rng), events);
        if (p.Phase == Phase.Finished) return;

        if (Season == Season.Ha && TileOf(p).Tags.HasFlag(TileTags.VenBien))
        {
            Log(p, events, "🏖 Mùa du lịch biển: tiến thêm 1 ô");
            MoveTo(p, p.Tile + 1);
            if (p.Phase == Phase.Finished) return;
        }
        var storm = Season == Season.Thu && (TileOf(p).Tags & (TileTags.VenBien | TileTags.Dao)) != 0;
        if (storm) Log(p, events, "🌀 Trú bão: chờ 3 giây");
        EnterQuestion(p, storm ? TimeSpan.FromSeconds(3) : TimeSpan.Zero);
    }

    private void AddBag(PlayerState p, EffectKind kind, List<string> events)
    {
        var info = EffectCatalog.Get(kind);
        if (p.Bag.Count >= BagMax) { Log(p, events, $"🎒 Túi đầy — bỏ {info.Name}"); return; }
        p.Bag.Add(kind);
        Log(p, events, $"🎒 {info.Icon} Nhận {info.Name}");
    }

    private void GiveBuff(PlayerState p, EffectKind kind, List<string> events, bool allowMove)
    {
        var info = EffectCatalog.Get(kind);
        if (info.IsBagItem) { AddBag(p, kind, events); return; }
        Log(p, events, $"🟢 {info.Icon} {info.Name}: {info.Description}");
        switch (kind)
        {
            case EffectKind.TiepVienTruongSon when allowMove:
                MoveTo(p, p.Tile + 2); break;
            case EffectKind.DoanTauKhongSo when allowMove:
                var next = _c.Tiles.FirstOrDefault(t => t.Index > p.Tile && t.Tags.HasFlag(TileTags.VenBien));
                if (next is not null) MoveTo(p, next.Index); else Log(p, events, "Không còn ô ven biển phía trước");
                break;
            case EffectKind.TiepVienTruongSon or EffectKind.DoanTauKhongSo:
                // nhận lúc đang trả lời (không phải lúc đứng ô) → không di chuyển giữa câu hỏi
                p.Pending.Add(EffectKind.ThanToc); Log(p, events, "→ đổi thành Thần tốc"); break;
            default:
                p.Pending.Add(kind); break;
        }
    }

    private void ApplyDebuff(PlayerState p, EffectKind kind, List<string> events)
    {
        var info = EffectCatalog.Get(kind);
        if (p.Pending.Remove(EffectKind.TheTranLongDan)) { Log(p, events, $"🤝 Thế trận lòng dân đã chặn {info.Name}!"); return; }
        Log(p, events, $"🔴 {info.Icon} {info.Name}: {info.Description}");
        switch (kind)
        {
            case EffectKind.KhungBoTrang: MoveTo(p, Math.Max(1, p.Tile - 2)); break;
            case EffectKind.NganSongCamCho: p.NeedCorrect += 1; break;
            case EffectKind.DienBienHoaBinh:
                if (p.Bag.Count > 0)
                {
                    var k = p.Bag[_rng.Next(p.Bag.Count)]; p.Bag.Remove(k);
                    Log(p, events, $"Mất {EffectCatalog.Get(k).Name}");
                }
                else if (p.Pending.Where(k => EffectCatalog.Get(k).IsBuff).ToList() is [var b, ..])
                {
                    p.Pending.Remove(b);
                    Log(p, events, $"Mất {EffectCatalog.Get(b).Name}");
                }
                break;
            default: p.Pending.Add(kind); break;
        }
    }

    private static void Log(PlayerState p, List<string> events, string text)
    {
        events.Add(text);
        p.Log.Add(text);
        if (p.Log.Count > 6) p.Log.RemoveAt(0);
    }

    // ---------- thời gian & kết thúc ----------
    public void Leave(string token)
    {
        if (Player(token) is { Phase: not Phase.Finished } p) { p.Phase = Phase.Left; p.Current = null; }
        CheckEnd();
    }

    /// <summary>Gọi định kỳ. Trả về true nếu có thay đổi.</summary>
    public bool Tick()
    {
        if (IsOver) return false;
        var now = _now();
        var changed = false;
        foreach (var p in _order)
        {
            switch (p.Phase)
            {
                case Phase.Delay when now >= p.PhaseEndsAt:
                    Reveal(p); changed = true; break;
                case Phase.Cooldown when now >= p.PhaseEndsAt:
                    p.LastWrong = null; EnterQuestion(p, TimeSpan.Zero); changed = true; break;
                case Phase.Question when p.QuestionDeadline is { } d && now >= d && p.Current is { } q:
                    p.History.Add(new SeenEntry(q.Id, q.Text, q.Options, -1, q.Answer, q.Explanation, q.Page, p.CurrentIsBonus));
                    p.Wrong++; p.Streak = 0; p.QuestionDeadline = null; p.PheBinhActive = false;
                    var ev = new List<string>();
                    Log(p, ev, "⏳ Hết 10 giây — tính là sai");
                    if (p.CurrentIsBonus) Roll(p, null, ev); else StartCooldown(p, ev);
                    changed = true; break;
            }
        }
        return CheckEnd() || changed;
    }

    private bool CheckEnd()
    {
        if (IsOver) return false;
        var now = _now();
        var active = _order.Where(p => p.Phase != Phase.Left).ToList();
        var over = active.All(p => p.Phase == Phase.Finished)
                   || (FirstFinishAt is { } f && now >= f + GraceAfterFirstFinish)
                   || now >= StartedAt + HardCap;
        if (over) EndedAt = now;
        return over;
    }

    public IReadOnlyList<RankRow> Ranking()
    {
        var sorted = _order
            .OrderByDescending(p => p.Phase == Phase.Finished)
            .ThenBy(p => p.FinishTime ?? TimeSpan.MaxValue)
            .ThenByDescending(p => p.Tile)
            .ThenByDescending(p => p.Percent)
            .ThenBy(p => p.ReachedTileAt)
            .ToList();
        return sorted.Select((p, i) => new RankRow(i + 1, p.Token, p.Name, p.IconId, p.Tile, _c.Tile(p.Tile).Name,
            p.Phase == Phase.Finished, p.FinishTime, p.Correct, p.Wrong, p.Phase == Phase.Left)).ToList();
    }
}
