using System.Security.Cryptography;

namespace HanhTrinh.Web.Game;

public enum RoomStatus { Lobby, Countdown, Playing }

public sealed class Seat(string token, string name, DateTime joinedAt)
{
    public string Token { get; } = token;
    public string Name { get; set; } = name;
    public string? IconId { get; set; }
    public DateTime JoinedAt { get; } = joinedAt;
}

public enum JoinResult { Ok, NotFound, Full, Locked, Playing, Banned, LeftMatch, LimitReached }

public record RoomListing(string HostToken, string HostName, int Count, bool Locked, RoomStatus Status);

/// <summary>Trạng thái một phòng. Mọi thay đổi đi qua RoomManager (một lock chung).</summary>
public sealed class Room
{
    public const int MaxSeats = 60;
    public static readonly TimeSpan CountdownLength = TimeSpan.FromSeconds(5);

    internal Room(string code, Seat host) { Code = code; HostToken = host.Token; Seats.Add(host); }

    public string Code { get; }
    public string HostToken { get; internal set; }
    public bool Locked { get; internal set; }
    public RoomStatus Status { get; internal set; } = RoomStatus.Lobby;
    public DateTime CountdownEndsAt { get; internal set; }
    public Season NextSeason { get; internal set; }
    public List<Seat> Seats { get; } = [];
    internal HashSet<string> Banned { get; } = [];
    internal HashSet<string> LeftMatch { get; } = [];
    /// <summary>Trận đang chơi hoặc trận vừa kết thúc (để xem kết quả).</summary>
    public Match? Match { get; internal set; }
    public int MatchSerial { get; internal set; }
    public int? LastMatchId { get; internal set; }

    public event Action? Changed;
    internal void RaiseChanged() => Changed?.Invoke();

    public Seat? SeatOf(string token) => Seats.FirstOrDefault(s => s.Token == token);
    public string HostName => SeatOf(HostToken)?.Name ?? "";
    public bool IsHost(string token) => token == HostToken;
    public bool IconTaken(string icon) => Seats.Any(s => s.IconId == icon);
}

public sealed class RoomManager : BackgroundService
{
    public const int MaxRooms = 20;

    private readonly object _gate = new();
    private readonly Dictionary<string, Room> _rooms = new();
    private readonly Dictionary<string, Room> _byToken = new();
    private readonly ContentStore _content;
    private readonly SeenRegistry _seen;
    private readonly Func<DateTime> _now;
    private readonly Random _rng;

    public RoomManager(ContentStore content, SeenRegistry seen) : this(content, seen, () => DateTime.UtcNow, Random.Shared) { }

    internal RoomManager(ContentStore content, SeenRegistry seen, Func<DateTime> now, Random rng)
    {
        _content = content; _seen = seen; _now = now; _rng = rng;
    }

    /// <summary>Danh sách phòng thay đổi.</summary>
    public event Action? ListChanged;
    /// <summary>Một trận vừa kết thúc (để lưu lịch sử). Gọi ngoài lock.</summary>
    public event Action<Room, Match>? MatchEnded;

    /// <summary>Lock chung cho mọi thao tác đọc trạng thái phòng/trận từ UI.</summary>
    public object Gate => _gate;
    public ContentStore Content => _content;

    public IReadOnlyList<RoomListing> List()
    {
        lock (_gate)
            return _rooms.Values.OrderBy(r => r.Seats.Min(s => s.JoinedAt))
                .Select(r => new RoomListing(r.HostToken, r.HostName, r.Seats.Count, r.Locked, r.Status)).ToList();
    }

    public Room? FindByToken(string token) { lock (_gate) return _byToken.GetValueOrDefault(token); }

    public (JoinResult Result, Room? Room) Create(string token, string name)
    {
        List<Room> touched = [];
        Room? room = null;
        var result = JoinResult.Ok;
        lock (_gate)
        {
            LeaveInternal(token, touched);
            if (_rooms.Count >= MaxRooms) result = JoinResult.LimitReached;
            else
            {
                string code;
                do code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6"); while (_rooms.ContainsKey(code));
                room = new Room(code, new Seat(token, name, _now()));
                _rooms[code] = room;
                _byToken[token] = room;
            }
        }
        Notify(touched, list: true);
        return (result, room);
    }

    public (JoinResult Result, Room? Room) JoinByCode(string code, string token, string name) =>
        Join(token, name, () => _rooms.GetValueOrDefault(code.Trim()), viaCode: true);

    public (JoinResult Result, Room? Room) JoinFromList(string hostToken, string token, string name) =>
        Join(token, name, () => _rooms.Values.FirstOrDefault(r => r.HostToken == hostToken), viaCode: false);

    private (JoinResult, Room?) Join(string token, string name, Func<Room?> find, bool viaCode)
    {
        List<Room> touched = [];
        (JoinResult, Room?) res;
        lock (_gate)
        {
            var room = find();
            if (room is null) res = (JoinResult.NotFound, null);
            else if (room.SeatOf(token) is not null) res = (JoinResult.Ok, room);
            else if (room.Banned.Contains(token)) res = (JoinResult.Banned, null);
            else if (room.LeftMatch.Contains(token) && room.Status != RoomStatus.Lobby) res = (JoinResult.LeftMatch, null);
            else if (room.Status != RoomStatus.Lobby) res = (JoinResult.Playing, null);
            else if (room.Locked && !viaCode) res = (JoinResult.Locked, null);
            else if (room.Seats.Count >= Room.MaxSeats) res = (JoinResult.Full, null);
            else
            {
                LeaveInternal(token, touched);
                room.Seats.Add(new Seat(token, name, _now()));
                _byToken[token] = room;
                touched.Add(room);
                res = (JoinResult.Ok, room);
            }
        }
        Notify(touched, list: true);
        return res;
    }

    public bool PickIcon(Room room, string token, string iconId)
    {
        lock (_gate)
        {
            var seat = room.SeatOf(token);
            if (seat is null || room.Status != RoomStatus.Lobby || !_content.Icons.Contains(iconId)) return false;
            if (seat.IconId == iconId) return true;
            if (room.IconTaken(iconId)) return false;
            seat.IconId = iconId;
        }
        room.RaiseChanged();
        return true;
    }

    public void SetLocked(Room room, string hostToken, bool locked)
    {
        lock (_gate) { if (!room.IsHost(hostToken)) return; room.Locked = locked; }
        room.RaiseChanged(); ListChanged?.Invoke();
    }

    public void Kick(Room room, string hostToken, string token)
    {
        lock (_gate)
        {
            if (!room.IsHost(hostToken) || token == hostToken || room.Status != RoomStatus.Lobby) return;
            if (room.Seats.RemoveAll(s => s.Token == token) == 0) return;
            room.Banned.Add(token);
            _byToken.Remove(token);
        }
        room.RaiseChanged(); ListChanged?.Invoke();
    }

    public bool Start(Room room, string hostToken)
    {
        lock (_gate)
        {
            if (!room.IsHost(hostToken) || room.Status != RoomStatus.Lobby || room.Seats.Count < 1) return false;
            foreach (var s in room.Seats.Where(s => s.IconId is null))
                s.IconId = _content.Icons.First(i => !room.IconTaken(i));
            room.NextSeason = (Season)_rng.Next(4);
            room.Status = RoomStatus.Countdown;
            room.CountdownEndsAt = _now() + Room.CountdownLength;
            room.LeftMatch.Clear();
        }
        room.RaiseChanged(); ListChanged?.Invoke();
        return true;
    }

    public void Leave(string token)
    {
        List<Room> touched = [];
        lock (_gate) LeaveInternal(token, touched);
        Notify(touched, list: true);
    }

    private void LeaveInternal(string token, List<Room> touched)
    {
        if (!_byToken.Remove(token, out var room)) return;
        room.Seats.RemoveAll(s => s.Token == token);
        if (room.Status == RoomStatus.Playing && room.Match is { } m)
        {
            m.Leave(token);
            room.LeftMatch.Add(token);
        }
        if (room.HostToken == token && room.Seats.Count > 0)
            room.HostToken = room.Seats.MinBy(s => s.JoinedAt)!.Token;
        if (room.Seats.Count == 0 && room.Status != RoomStatus.Playing) _rooms.Remove(room.Code);
        touched.Add(room);
    }

    /// <summary>Một nhịp đồng hồ: đếm ngược, hẹn giờ trong trận, kết thúc trận.</summary>
    internal void Tick()
    {
        List<Room> changed = [];
        List<(Room, Match)> ended = [];
        lock (_gate)
        {
            foreach (var room in _rooms.Values.ToList())
            {
                if (room.Status == RoomStatus.Countdown && _now() >= room.CountdownEndsAt)
                {
                    room.Match = new Match(_content, room.Seats.Select(s => (s.Token, s.Name, s.IconId!)), _seen, _rng, _now, room.NextSeason);
                    room.MatchSerial++;
                    room.Status = RoomStatus.Playing;
                    changed.Add(room);
                }
                else if (room.Status == RoomStatus.Playing && room.Match is { } m)
                {
                    if (m.Tick()) changed.Add(room);
                    if (m.IsOver)
                    {
                        room.Status = RoomStatus.Lobby;
                        ended.Add((room, m));
                        if (!changed.Contains(room)) changed.Add(room);
                        if (room.Seats.Count == 0) _rooms.Remove(room.Code);
                    }
                }
            }
        }
        foreach (var (r, m) in ended) MatchEnded?.Invoke(r, m);
        Notify(changed, list: ended.Count > 0 || changed.Count > 0);
    }

    private void Notify(List<Room> rooms, bool list)
    {
        foreach (var r in rooms.Distinct()) r.RaiseChanged();
        if (list) ListChanged?.Invoke();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { Tick(); }
            catch (Exception ex) { Console.Error.WriteLine($"RoomManager tick failed: {ex}"); }
        }
    }
}
