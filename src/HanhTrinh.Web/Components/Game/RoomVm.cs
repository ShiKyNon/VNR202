using HanhTrinh.Web.Game;

namespace HanhTrinh.Web.Components.Game;

// Ảnh chụp bất biến của phòng/trận, dựng trong lock để UI render không đụng trạng thái đang đổi.
public record SeatVm(string Token, string Name, string? IconId, bool IsHost);
public record PieceVm(string Token, string Name, string IconId, int Tile, bool Left, bool Finished);
public record QuestionVm(string Id, string Text, IReadOnlyList<string?> Options, bool Bonus, double? DeadlineLeft);
public record MeVm(Phase Phase, int Tile, string TileName, QuestionVm? Question, double PhaseLeft, SeenEntry? LastWrong,
    int Correct, int Wrong, IReadOnlyList<EffectKind> Bag, IReadOnlyList<EffectKind> Pending, IReadOnlyList<string> Log,
    DiceRoll? LastRoll, int NeedCorrect, bool PheBinhActive, TimeSpan? FinishTime);
public record GameVm(Season Season, IReadOnlyDictionary<int, TileMark> Marks, IReadOnlySet<int> Glowing,
    IReadOnlyList<PieceVm> Pieces, IReadOnlyList<RankRow> Ranking, MeVm Me, TimeSpan Elapsed, TimeSpan? GraceLeft, bool Over);
public record RoomVm(string Code, bool IsHost, bool Locked, RoomStatus Status, double CountdownLeft, Season NextSeason,
    IReadOnlyList<SeatVm> Seats, string? MyIcon, int MatchSerial, GameVm? Game);

public static class RoomVmBuilder
{
    /// <summary>Gọi trong lock(RoomManager.Gate).</summary>
    public static RoomVm Build(Room room, string token, DateTime now)
    {
        GameVm? game = null;
        if (room.Match is { } m && m.Player(token) is { } p)
        {
            QuestionVm? q = null;
            if (p.Phase == Phase.Question && p.Current is { } cur)
                q = new QuestionVm(cur.Id, cur.Text,
                    p.Order.Select(o => p.Removed.Contains(o) ? null : cur.Options[o]).ToList(),
                    p.CurrentIsBonus, p.QuestionDeadline is { } d ? Math.Max(0, (d - now).TotalSeconds) : null);
            var me = new MeVm(p.Phase, p.Tile, m.TileOf(p).Name, q,
                Math.Max(0, (p.PhaseEndsAt - now).TotalSeconds), p.LastWrong, p.Correct, p.Wrong,
                p.Bag.ToList(), p.Pending.ToList(), p.Log.ToList(), p.LastRoll, p.NeedCorrect, p.PheBinhActive, p.FinishTime);
            var end = m.EndedAt ?? now;
            game = new GameVm(m.Season, m.Marks.ToDictionary(), m.Glowing,
                m.Players.Select(x => new PieceVm(x.Token, x.Name, x.IconId, x.Tile, x.Phase == Phase.Left, x.Phase == Phase.Finished)).ToList(),
                m.Ranking(), me, end - m.StartedAt,
                m.FirstFinishAt is { } f && !m.IsOver ? (f + Match.GraceAfterFirstFinish - now) : null, m.IsOver);
        }
        return new RoomVm(room.Code, room.IsHost(token), room.Locked, room.Status,
            Math.Max(0, (room.CountdownEndsAt - now).TotalSeconds), room.NextSeason,
            room.Seats.Select(s => new SeatVm(s.Token, s.Name, s.IconId, room.IsHost(s.Token))).ToList(),
            room.SeatOf(token)?.IconId, room.MatchSerial, game);
    }

    public static string Clock(TimeSpan t) => $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
}
