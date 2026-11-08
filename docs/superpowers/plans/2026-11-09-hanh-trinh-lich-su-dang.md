# Hành trình Lịch sử Đảng — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Multiplayer real-time board-game web app (63 provinces + 2 islands, quiz → dice → move) for the VNR202 class presentation at 10:00 on 2026-11-09.

**Architecture:** One Blazor Web App (.NET 8, Interactive Server, global). Pure C# game engine in `Game/` (injected `Random` + clock, no Blazor deps) driven by an in-memory singleton `RoomManager`; components subscribe to room `Changed` events and call `InvokeAsync(StateHasChanged)`. EF Core persists finished matches (SQLite locally, Postgres/Neon on Render).

**Tech Stack:** .NET 8 (pin via `global.json`, SDK 8.0.423 installed), Blazor Server, EF Core 8 (Sqlite + Npgsql), xUnit, OpenMoji SVGs, Docker.

**Spec:** `docs/superpowers/specs/2026-11-09-hanh-trinh-lich-su-dang-design.md` — read it; section numbers below (§) refer to it.

## Global Constraints

- Deadline: milestones §11 — M1 core playable ~06:00, M2 effects ~07:30, M3 leaderboard ~08:15, M4 deployed + multi-device tested ≤09:00. Never break a working milestone; commit at every green step.
- All UI text Vietnamese. Every page has a button labelled exactly **"Quay lại"**. Leave-match confirm text exactly: **"Bạn chắc chắn muốn rời trận?"**.
- Map note exact text: **"63 tỉnh thành theo địa giới hành chính trước 7/2025"**. Hoàng Sa + Trường Sa drawn far offshore, labelled.
- Room list shows **"Phòng của <tên chủ phòng>"** + `n/60`; never the room code. Max 60 players; Start enabled at ≥1 player; max 20 rooms server-wide; 1 hosted room per browser token.
- Name 1–16 chars after trim, duplicates allowed, profanity-filtered.
- All game rules decided server-side; correct answer index never sent to client before answering.
- `giao-trinh/` (textbook PDF) is gitignored — never commit it.
- No extra UI/JS libraries. Only packages: `Microsoft.EntityFrameworkCore.Sqlite`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design` (8.x), xUnit template packages.
- Target screens 1366×768 and 1920×1080 desktop; fonts readable on projector (base ≥18px).

## Review Focus

1. Two players clicking the same icon at the same instant → only first wins, second sees it greyed and is told to pick again (Task 5 test `PickIcon_SecondClaimFails`).
2. Player refreshes / loses connection mid-question or mid-cooldown → returns to the same phase with remaining cooldown, same question (Task 5 test `Rejoin_RestoresSameSeatAndPhase`).
3. Host leaves lobby / everyone leaves → host transfers / room deleted; host leaving mid-match does not stop the match (Task 5 tests `HostLeave_TransfersHost`, `LastLeave_DeletesRoom`, `HostLeaveDuringMatch_MatchContinues`).
4. Rapid double-click on an answer → second click ignored, no double roll (Task 4 test `Answer_IgnoredWhenNotInQuestionPhase`).
5. Pool exhaustion for a player who plays many matches → seen-set resets instead of crashing (Task 4 test `Picker_ResetsWhenPoolExhausted`).

---

## File Structure

```
global.json                      pins SDK 8.0.423
.gitignore                       bin/ obj/ giao-trinh/ *.db
Dockerfile, .dockerignore
HanhTrinh.sln
docs/DEPLOY.md                   Render + Neon + UptimeRobot guide (Vietnamese)
docs/questions-review.md         generated human-readable question list
src/HanhTrinh.Web/
  Program.cs                     DI, /healthz, DB provider switch, migrate on start
  Data/provinces.json            65 tiles (index,name,region,tags,x,y)
  Data/questions.json            pool (≥300, target 500)
  Data/seasonal-questions.json   glowing-province bonus questions
  Data/icons.json                60 icon ids (OpenMoji hex codes)
  Game/Content.cs                Tile, Question, SeasonalQuestion records + ContentStore loader
  Game/QuestionPicker.cs         no-repeat random picker + SeenRegistry
  Game/Effects.cs                EffectKind enum + EffectCatalog (name, description, kind, weight)
  Game/Seasons.cs                Season enum + SeasonCatalog (title, pros, cons, glowing provinces)
  Game/Dice.cs                   DiceCalc.Compute
  Game/PlayerState.cs            per-player state
  Game/Match.cs                  match rules (answer, roll, move, effects, timers, end, ranking)
  Game/Room.cs                   lobby state, icons, lock, kick, host, countdown, owns Match
  Game/RoomManager.cs            singleton: create/join/list/rejoin, 250 ms ticker
  Game/NameFilter.cs             profanity filter
  Persistence/AppDb.cs           DbContext + MatchRecord + MatchResultRecord
  Persistence/MatchArchive.cs    save finished match, query records/history
  Components/App.razor, Routes.razor, Layout/MainLayout.razor
  Components/Pages/Home.razor (/), Rules.razor (/luat-choi), Leaderboard.razor (/xep-hang),
     MatchDetail.razor (/xep-hang/{id:int}), NamePage.razor (/choi), Rooms.razor (/phong),
     RoomPage.razor (/phong/{code}) — icon pick, lobby, countdown, game, results by room state
  Components/Game/BoardMap.razor, QuestionPopup.razor, DiceRoll.razor, SidePanel.razor, ResultsView.razor
  Components/Shared/BackButton.razor
  Services/PlayerSession.cs      scoped: token + name via ProtectedLocalStorage
  wwwroot/app.css, wwwroot/icons/*.svg
tests/HanhTrinh.Tests/
  ContentTests.cs, PickerTests.cs, DiceTests.cs, MatchTests.cs, EffectsTests.cs,
  SeasonTests.cs, RoomTests.cs, NameFilterTests.cs, ArchiveTests.cs
  TestClock.cs                   settable clock helper
tools/ExportReview/              tiny console app: questions.json → docs/questions-review.md
```

---

## Milestone 1 — core playable

### Task 1: Scaffold, repo, health check

**Files:** Create `global.json`, `.gitignore`, `HanhTrinh.sln`, `src/HanhTrinh.Web/*` (template), `tests/HanhTrinh.Tests/*`, `tests/HanhTrinh.Tests/TestClock.cs`.

**Interfaces — Produces:** `GET /healthz` → 200 `"ok"`; `TestClock { DateTime Now; void Advance(TimeSpan) }`, used as `Func<DateTime> now = () => clock.Now`.

- [ ] **Step 1:** `git init`; `global.json` `{"sdk":{"version":"8.0.423","rollForward":"latestPatch"}}`; `.gitignore` (dotnet standard + `giao-trinh/`, `*.db`).
- [ ] **Step 2:** `dotnet new blazor -n HanhTrinh.Web -o src/HanhTrinh.Web --interactivity Server --all-interactive --empty`; `dotnet new xunit -n HanhTrinh.Tests -o tests/HanhTrinh.Tests`; sln + project reference.
- [ ] **Step 3:** Add `app.MapGet("/healthz", () => "ok");` and `<html lang="vi">`.
- [ ] **Step 4:** `dotnet build` and `dotnet test` → succeed.
- [ ] **Step 5:** Commit `chore: scaffold Blazor app and tests` (include `docs/`).

### Task 2: Content model, provinces, loader, data validation

**Files:** Create `Game/Content.cs`, `Data/provinces.json`, `Data/questions.json` (≥8 placeholder questions taken from the textbook), `Data/seasonal-questions.json` (empty array), `Data/icons.json`; Test `tests/HanhTrinh.Tests/ContentTests.cs`. Mark `Data/*.json` as `CopyToOutputDirectory=PreserveNewest`.

**Interfaces — Produces:**
```csharp
public enum Region { Bac, Trung, TayNguyen, Nam }
[Flags] public enum TileTags { None=0, VenBien=1, Dao=2, MienNuiPhiaBac=4, BacTrungBo=8 }
public record Tile(int Index, string Name, Region Region, TileTags Tags, double X, double Y);
public record Question(string Id, string Text, string[] Options, int Answer, string Explanation, int Page, int Chapter);
public record SeasonalQuestion(string Id, string Province, Season Season, string Text, string[] Options, int Answer, string Explanation, int Page);
public sealed class ContentStore {
  public static ContentStore Load(string dataDir);
  public IReadOnlyList<Tile> Tiles { get; }            // index 1..65 at position [i-1]
  public IReadOnlyList<Question> Questions { get; }
  public IReadOnlyList<SeasonalQuestion> Seasonal { get; }
  public IReadOnlyList<string> Icons { get; }          // 60 ids
}
```
(`Season` enum defined here too: `public enum Season { Xuan, Ha, Thu, Dong }`.) JSON camelCase, enums as strings.

- [ ] **Step 1: Write failing tests** in `ContentTests`:
  - `Tiles_Are65InSpecOrder`: count 65; `Tiles[0].Name=="Hà Giang"`, `Tiles[64].Name=="Cà Mau"`; index of "Hoàng Sa" == index of "Đà Nẵng"+1; "Trường Sa" == "Khánh Hòa"+1; regions contiguous (Bac 1–25, Trung 26–41, TayNguyen 42–46, Nam 47–65).
  - `Tags_MatchSpec`: exactly 28 tiles `VenBien`, 2 `Dao`, 14 `MienNuiPhiaBac`, 6 `BacTrungBo` (lists per §5.2).
  - `Questions_AreWellFormed`: unique ids; 4 distinct non-empty options; `Answer` in 0..3; non-empty explanation; `Page` 1..500; Chapter 0..3. Same rules for `Seasonal`, and every `Province` exists in `Tiles`.
  - `Icons_Are60Unique`.
- [ ] **Step 2:** `dotnet test` → FAIL (types missing).
- [ ] **Step 3:** Implement records + `ContentStore.Load` with `System.Text.Json` (`JsonStringEnumConverter`). Write `provinces.json` from §5.1 table and §5.2 tags; X/Y coordinates in a 1000×1600 viewBox tracing an S shape, Tây Nguyên as inland branch, islands far right offshore.
- [ ] **Step 4:** Download 60 OpenMoji colour SVGs (animals/plants/neutral objects, §4.6) from `https://cdn.jsdelivr.net/npm/openmoji@15.1.0/color/svg/<HEX>.svg` into `wwwroot/icons/`; list hex ids in `icons.json`. If download fails, fall back to emoji characters in `icons.json` rendered as text (note `shortcut:` comment).
- [ ] **Step 5:** `dotnet test` → PASS. Commit `feat: content model, provinces and icons`.

### Task 3: Question authoring (runs in PARALLEL with Tasks 4–9)

**Files:** `Data/questions.json`, `Data/seasonal-questions.json`, `tools/ExportReview/Program.cs`, `docs/questions-review.md`.

Source: `giao-trinh/Giao trinh lich su dang cong san viet nam.pdf`. Dispatch one subagent per chapter range (Nhập môn+Ch1, Ch2, Ch3 split in two), each writing a separate JSON fragment `Data/q-<part>.json` with id prefix (`c1-001`…), merged into `questions.json`.

Rules each author gets verbatim: §6.1 format and quality rules; §6.3 exclusions (Hiệp định Pari date, Nghị quyết Chiến lược biển date, tr.364 "đến năm 2000", nothing after Đại hội XIII); `page` = printed page number in the book; explanation ≤ 2 sentences quoting/paraphrasing the textbook; distractors same type as the answer; no "Tất cả đều…". Target: ≥300 total (stretch 500), proportional to chapter length. Seasonal: ≥4 per (province, season) pair in §5.9; author must verify each event's date in the textbook and **drop pairs the textbook does not mention**, reporting dropped pairs (Task 9 reads the surviving pairs from data, not a hard-coded list).

- [ ] **Step 1:** Dispatch authors; each returns its fragment path + dropped/uncertain items.
- [ ] **Step 2:** Merge; run `dotnet test --filter ContentTests` → PASS (fix malformed items).
- [ ] **Step 3:** `tools/ExportReview` (console, references Web project's `ContentStore`) writes `docs/questions-review.md`: per question `### id · tr.N`, question, options with ✔ on correct, explanation. Run it.
- [ ] **Step 4:** Commit `content: question bank from giáo trình`.

### Task 4: Match engine core (no effects/seasons yet)

**Files:** Create `Game/QuestionPicker.cs`, `Game/PlayerState.cs`, `Game/Dice.cs`, `Game/Match.cs`; Test `PickerTests.cs`, `MatchTests.cs`, `DiceTests.cs`.

**Interfaces — Produces:**
```csharp
public sealed class SeenRegistry { public HashSet<string> For(string token); }   // thread-safe, singleton
public sealed class QuestionPicker {
  public QuestionPicker(IReadOnlyList<Question> pool, Random rng);
  public Question Next(HashSet<string> seen);   // marks seen; clears set when all seen
}
public enum Phase { Question, Cooldown, Delay, Bonus, Finished, Left }
public record SeenEntry(string QuestionId, string Text, string[] Options, int Chosen, int Answer, string Explanation, int Page);
public sealed class PlayerState {
  string Token; string Name; string IconId; int Tile; Phase Phase; DateTime PhaseEndsAt;
  int Correct; int Wrong; int Streak; TimeSpan? FinishTime; DateTime ReachedTileAt;
  List<EffectKind> Bag; List<EffectKind> Pending; List<SeenEntry> History;
  Question? Current; int[] Order /*shuffled option order*/; HashSet<int> Removed /*eliminated options*/;
}
public record DiceRoll(int Raw, int Steps, string Formula, int From, int To);
public record AnswerOutcome(bool Accepted, bool Correct, int CorrectIndex, string Explanation, int Page, DiceRoll? Roll, IReadOnlyList<string> Events);
public record RankRow(int Rank, string Token, string Name, string IconId, int Tile, string TileName, bool Finished, TimeSpan? FinishTime, int Correct, int Wrong, bool Left) { public double Percent => Correct+Wrong==0 ? 0 : 100.0*Correct/(Correct+Wrong); }
public sealed class Match {
  public Match(ContentStore content, IEnumerable<(string Token,string Name,string IconId)> players, SeenRegistry seen, Random rng, Func<DateTime> now, Season season);
  public DateTime StartedAt { get; } public Season Season { get; }
  public PlayerState Player(string token);
  public AnswerOutcome Answer(string token, int displayIndex);   // index into displayed (shuffled) options
  public void Leave(string token);
  public void Tick();                       // expires phases, applies end conditions
  public bool IsOver { get; } public DateTime? FirstFinishAt { get; }
  public IReadOnlyList<RankRow> Ranking();
}
public static class DiceCalc { public static (int Steps, string Formula) Compute(int raw, IReadOnlyList<(string Label, Func<int,int> Apply)> mods); } // M1: no mods; final min 1
```
Constants in `Match`: `Cooldown = 5s`, `GraceAfterFirstFinish = 1m30s`, `HardCap = 20m`, `FinishTile = 65`.

- [ ] **Step 1: Write failing tests** (use `TestClock`, `new Random(seed)`, tiny ContentStore built in-test):
  - `Picker_NeverRepeatsUntilExhausted` (pool 5 → 5 distinct), `Picker_ResetsWhenPoolExhausted` (6th call works, set reset).
  - `Correct_RollsAndMoves`: answering the right display index → `Correct`, `Roll.To == 1 + Roll.Steps`, phase `Question` with a new question, `Correct==1`.
  - `Wrong_Cooldown5sThenNewQuestionSameTile`: phase `Cooldown`, `PhaseEndsAt==now+5s`; tile unchanged; after `clock.Advance(5s); Tick()` phase `Question` with a different question id.
  - `Answer_IgnoredWhenNotInQuestionPhase`: answering during cooldown → `Accepted==false`, counts unchanged.
  - `Overshoot_FinishesAtTile65` with `FinishTime == now - StartedAt`.
  - `Solo_EndsWhenFinished`; `Multi_EndsWhenAllFinished`; `Multi_Ends90sAfterFirstFinish`; `LeftPlayers_IgnoredForAllFinished`; `HardCap20m_Ends`.
  - `Ranking_FinishedByTimeThenTileThenPercentThenReachedEarlier`; `Ranking_IncludesLeftPlayers` (flag `Left`).
  - `History_RecordsChosenAndAnswer` (for results review).
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement. `Match` is not thread-safe; caller (`Room`) locks. Option order shuffled per question; `Answer` maps display index → original.
- [ ] **Step 4:** Run → PASS. Commit `feat: match engine core`.

### Task 5: Rooms, lobby, rejoin

**Files:** Create `Game/Room.cs`, `Game/RoomManager.cs`; Test `RoomTests.cs`.

**Interfaces — Consumes:** `Match`, `ContentStore`, `SeenRegistry`. **Produces:**
```csharp
public enum RoomStatus { Lobby, Countdown, Playing, Results }
public record Seat(string Token, string Name, string? IconId, DateTime JoinedAt);
public sealed class Room {
  public string Code { get; }   public string HostToken { get; }   public bool Locked { get; }
  public RoomStatus Status { get; } public DateTime CountdownEndsAt { get; }
  public IReadOnlyList<Seat> Seats { get; } public Match? Match { get; } public int? LastMatchId { get; set; }
  public event Action? Changed;            // raised after every mutation, outside the lock
  public object Gate { get; }               // lock for Match calls from UI
  public bool PickIcon(string token, string iconId);
  public void SetLocked(string hostToken, bool locked);
  public void Kick(string hostToken, string token);   // kicked token banned from this room
  public bool Start(string hostToken);      // ≥1 seat with icon; status Countdown for 5s, season rolled now
  public void BackToLobby(string token);    // "Chơi lại"
}
public enum JoinResult { Ok, NotFound, Full, Locked, Playing, Banned, LeftMatch }
public sealed class RoomManager {        // singleton, owns a 250 ms PeriodicTimer calling Tick on all rooms
  public Room? Create(string token, string name);     // null if 20 rooms or token already hosts a room
  public (JoinResult, Room?) JoinByCode(string code, string token, string name);   // code works even when locked
  public (JoinResult, Room?) JoinFromList(string hostToken, string token, string name);  // locked → Locked
  public IReadOnlyList<RoomListing> List();           // record RoomListing(string HostToken, string HostName, int Count, bool Locked, RoomStatus Status) — no code
  public Room? FindByToken(string token);              // rejoin
  public void Leave(string token);
  public event Action? ListChanged;
  public event Action<Room>? MatchEnded;               // Task 10 subscribes to archive
}
```
Room code: 6 digits via `RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")`, unique among live rooms. Seat with no icon yet counts toward 60. Countdown 5 s then `Match` created (Playing); `Match.IsOver` → Results + `MatchEnded`.

- [ ] **Step 1: Write failing tests:** `Create_SixDigitUniqueCode`; `Create_LimitedTo20Rooms`; `Create_OneHostedRoomPerToken`; `List_HidesCode_ShowsHostName`; `Locked_JoinFromListRejected_JoinByCodeOk`; `Join_FullAt60`; `PickIcon_SecondClaimFails`; `Kick_RemovesAndBans`; `Start_NeedsOneSeatWithIcon_HostOnly`; `Countdown5s_ThenPlaying`; `HostLeave_TransfersHost` (earliest JoinedAt); `LastLeave_DeletesRoom`; `HostLeaveDuringMatch_MatchContinues`; `Rejoin_RestoresSameSeatAndPhase` (FindByToken returns room; player phase/cooldown unchanged); `LeaveDuringMatch_CannotRejoin` (JoinResult.LeftMatch); `PlayAgain_ReturnsToLobbyKeepsSeats`.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement (all mutations under `Gate`; raise events after releasing). **Step 4:** PASS. Commit `feat: rooms and lobby`.

### Task 6: Menu, rules, name, room list pages

**Files:** Create `Game/NameFilter.cs`, `Services/PlayerSession.cs`, `Components/Shared/BackButton.razor`, `Pages/Home.razor`, `Rules.razor`, `NamePage.razor`, `Rooms.razor`, `wwwroot/app.css`; Modify `Program.cs` (register `ContentStore` singleton from `AppContext.BaseDirectory/Data`, `SeenRegistry`, `RoomManager` singletons, `PlayerSession` scoped); Test `NameFilterTests.cs`.

**Interfaces — Produces:** `NameFilter.Validate(string raw) -> (bool Ok, string Name, string? Error)`; `PlayerSession { Task<string> GetTokenAsync(); Task<string?> GetNameAsync(); Task SetNameAsync(string) }` (ProtectedLocalStorage keys `token`, `name`; token = `Guid.NewGuid().ToString("N")`).

- [ ] **Step 1: Failing tests:** `Rejects_Empty_And_Over16`; `Trims`; `Accepts_VietnameseDiacritics` ("Nguyễn Văn An"); `Rejects_Profanity_AccentAndCaseInsensitive` (e.g. "ĐM", "d.m", "Địt" variants; English "fuck"); `Rejects_SymbolsOutsideLettersDigitsSpace`.
- [ ] **Step 2:** FAIL → **Step 3:** implement (normalize: remove diacritics via `NormalizationForm.FormD`, `đ→d`, lowercase, strip non-letters; match against an embedded word list) → **Step 4:** PASS.
- [ ] **Step 5:** Pages: Home (Chơi→/choi, Xếp hạng→/xep-hang, Luật chơi→/luat-choi); Rules page content from §5 + sources + OpenMoji CC BY-SA 4.0 credit + map note; NamePage → /phong; Rooms: "Tạo phòng", code input + "Vào phòng", live list (`ListChanged`), locked rows show 🔒 disabled, Playing rows "Đang chơi" disabled; errors from `JoinResult` in Vietnamese. Every page has `<BackButton/>` ("Quay lại", `history.back` fallback to `/`). Session token read in `OnAfterRenderAsync(firstRender)` (ProtectedLocalStorage requirement).
- [ ] **Step 6:** `dotnet run`, click through Menu → Name → Rooms in browser; no exceptions. Commit `feat: menu, rules, name and room list`.

### Task 7: Room page — icon pick, lobby, countdown, game, results

**Files:** Create `Pages/RoomPage.razor`, `Components/Game/BoardMap.razor`, `QuestionPopup.razor`, `DiceRoll.razor`, `SidePanel.razor`, `ResultsView.razor`; Modify `app.css`.

**Interfaces — Consumes:** `Room`, `Match`, `PlayerState`, `RankRow`, `AnswerOutcome`.

- [ ] **Step 1:** `RoomPage` on init: `FindByToken` (rejoin) else redirect `/phong`. Subscribe `room.Changed` → `InvokeAsync(StateHasChanged)`; unsubscribe in `Dispose`. Render by `Status`:
  - Lobby: if my seat has no icon → 60-icon grid (taken = greyed, `disabled`); else lobby: code shown large, `n/60`, seats (icon+name), host controls Khóa/Mở khóa, Đuổi per seat, Bắt đầu (enabled ≥1 seat with icon).
  - Countdown: big 5→1 + season announcement (M1: season title only).
  - Playing: grid `left: BoardMap` (~70%), `right: SidePanel` (top 5 + "Xem thêm" scrollable full list, my stats ✔/✘/%, timer, 1:30 countdown when `FirstFinishAt` set). `QuestionPopup` centred over map when my phase is Question (options in display order, eliminated ones hidden); in Cooldown shows correct answer + explanation + "tr.N" + countdown. After correct: `DiceRoll` CSS 3D cube spin ~1 s, then formula text, then token steps tile-by-tile (~150 ms/tile, client-only animation over the already-decided `From→To`). "Quay lại" → `confirm("Bạn chắc chắn muốn rời trận?")` via JS interop → `Leave`.
  - Results: `ResultsView` full ranking table (all players incl. "đã rời"), my question review (from `History`), buttons **Chơi lại** (`BackToLobby`) and **Về Menu**.
- [ ] **Step 2:** `BoardMap`: inline SVG viewBox 0 0 1000 1600; polyline path through tile X/Y; circles per tile with short name; player icons (`<image href="/icons/{id}.svg">`) stacked with small offsets when sharing a tile; islands drawn offshore with labels; map note text bottom-left.
- [ ] **Step 3:** Manual test with 3 browser tabs (one private window per token): create, join by code, join from list, lock, kick, pick same icon, start, play to finish, results, Chơi lại; refresh a tab mid-cooldown → same state. Fix issues.
- [ ] **Step 4:** Commit `feat: playable room and game screen` — **Milestone 1 done.**

## Milestone 2 — effects and seasons

### Task 8: Buff/debuff tiles, bag, islands, dice modifiers

**Files:** Create `Game/Effects.cs`; Modify `Game/Match.cs`, `Game/Dice.cs`, `QuestionPopup.razor`, `SidePanel.razor`, `BoardMap.razor`; Test `EffectsTests.cs`, extend `DiceTests.cs`.

**Interfaces — Produces:**
```csharp
public enum EffectKind { BinhDanHocVu, DietGiacDot, KimChiNam, DoiMoi, PheBinh, ChopThoiCo,           // bag (question) buffs
  ThanToc, TiepVienTruongSon, DoanTauKhongSo, NamQuyenChuDong, DiBatBien, TheTranLongDan,         // auto buffs
  KhungBoTrang, BaoVayCamVan, KhungHoangKinhTe, NganSongCamCho, ChuQuanNongVoi, DienBienHoaBinh }  // debuffs
public record EffectInfo(EffectKind Kind, string Name, string Description, bool IsBuff, bool IsBagItem, int Weight);
public static class EffectCatalog { public static IReadOnlyList<EffectInfo> All; public static EffectInfo Get(EffectKind k); public static EffectKind DrawBuff(Random r); public static EffectKind DrawDebuff(Random r); }
public enum TileMark { None, Buff, Debuff }
// Match additions:
public IReadOnlyDictionary<int, TileMark> Marks { get; }          // 7 Buff + 7 Debuff
public bool UseBagItem(string token, EffectKind kind);             // only in Question phase
public AnswerOutcome Answer(string token, int displayIndex, int? chosenSteps = null); // chosenSteps for NamQuyenChuDong
```
Names/descriptions exactly as §5.5/§5.6 tables. Weights: KimChiNam 1, all other buffs 3, debuffs 3.

- [ ] **Step 1: Failing tests:**
  - `Marks_7Buff7Debuff_NotOnStartFinishIslandsOrGlowing`.
  - Per bag item: `BinhDanHocVu_RemovesOneWrong` (Removed.Count==1, never the answer), `DietGiacDot_RemovesTwo`, `KimChiNam_RemovesThree`, `DoiMoi_SwapsQuestion`, `PheBinh_WrongOnceNoCooldown` (first wrong keeps Question phase, no Wrong++), `ChopThoiCo_RollsWithoutCountingAnswer`; `Bag_Max3_ExtraDropped`.
  - Auto: `ThanToc_DoublesNextRoll`, `TiepVien_Plus2Immediately_NoChain`, `DoanTau_JumpsToNextVenBien_NoneAhead_NoEffect`, `NamQuyen_UsesChosenSteps`, `DiBatBien_NextWrongNoCooldown`, `TheTran_BlocksNextCardDebuff`.
  - Debuffs: `KhungBo_Back2_NotBelow1_NoChain`, `BaoVay_NextWrong10s`, `KhungHoang_NextRollMax3`, `NganSong_NeedsTwoCorrectBeforeRoll` (first correct → new question, no roll; second correct → roll), `ChuQuan_10sTimeout_CountsWrong`, `DienBien_RemovesBagThenPendingThenNothing`.
  - `Island_Draws5050` (seeded), `PassingOverMarkedTile_NoTrigger`.
  - `Dice_Order_MultiplyCapAddMin1`: raw 6, ThanToc, KhungHoang → `min(12,3)=3`; raw 1 with −1 → 1; formula string `"6 ×2 (Thần tốc) − 1 (Nồm ẩm) = 11"`.
  - `Cooldown_IsMaxOfApplicable`.
- [ ] **Step 2:** FAIL → **Step 3:** implement; landing on Buff/Debuff/island happens only after a dice move; effect-caused moves don't chain (§5.4) → **Step 4:** PASS.
- [ ] **Step 5:** UI: marks 🟢/🔴/❓ on tiles; popup shows bag buttons (name + tooltip description) and, for NamQuyenChuDong, a 1–6 picker; SidePanel lists bag + pending effects; toast line for each `AnswerOutcome.Events` string. Commit `feat: buffs, debuffs and tiles`.

### Task 9: Seasons and glowing provinces

**Files:** Create `Game/Seasons.cs`; Modify `Match.cs`, `Room.cs` (roll season at Start), countdown UI, `SidePanel.razor`, `BoardMap.razor` (season tint, ✨ glow), `QuestionPopup.razor` (bonus question style); Test `SeasonTests.cs`.

**Interfaces — Produces:**
```csharp
public record SeasonInfo(Season Season, string Title, string Emoji, IReadOnlyList<string> Pros, IReadOnlyList<string> Cons);
public static class SeasonCatalog { public static SeasonInfo Get(Season s); }
// Match: public IReadOnlySet<int> Glowing { get; }  — tiles whose province has ≥1 SeasonalQuestion for this.Season
```
Titles exactly: "Trăm hoa đua nở", "Nắng hạ biển xanh", "Đồng lòng vượt bão", "Rét ngọt chờ Xuân". Rules exactly per §5.7 table; region checks use the tile the player stands on when rolling/answering.

- [ ] **Step 1: Failing tests** (force season via constructor): `Xuan_Raw1or6_GivesBagBuff`, `Xuan_NomAm_Minus1InBacTrung_Min1`, `Xuan_KhoHan_Cooldown8sInTayNguyenNam`; `Ha_DuLich_Plus1OnVenBienLanding_NoChain`, `Ha_TraiCay_30PercentBuffInNam` (seeded), `Ha_GioLao_Max4InBacTrungBo`, `Ha_NangNong_QuestionDelayed1s` (phase Delay 1s); `Thu_DongLong_3StreakGivesChopThoiCo_ResetsStreak`, `Thu_LuaChin_Plus1InMienNui`, `Thu_TruBao_3sDelayOnVenBienAndIslands`; `Dong_ChuanBiTet_50PercentPlus1`, `Dong_RetDam_Minus1InMienNui`, `Dong_MuaLu_Cooldown7sInTrung`; `Glowing_FromSeasonalData`; `Glowing_CorrectThenBonus_CorrectGives2Buffs`, `Glowing_BonusWrong_NoCooldownNoPenalty_ThenRoll`, `Glowing_BonusOncePerVisit`, `Bonus_CountsInPercent`.
- [ ] **Step 2:** FAIL → **Step 3:** implement → **Step 4:** PASS.
- [ ] **Step 5:** UI: countdown shows season emoji/title/pros/cons; SidePanel pinned season box; map tinted per season; glowing tiles ✨ pulse. Manual 2-tab run per season. Commit `feat: seasons and glowing provinces` — **Milestone 2 done.**

## Milestone 3 — persistence

### Task 10: Archive, records, match history

**Files:** Create `Persistence/AppDb.cs`, `Persistence/MatchArchive.cs`, EF migration, `Pages/Leaderboard.razor`, `Pages/MatchDetail.razor`; Modify `Program.cs`, `Room.cs`/`RoomManager` wiring; Test `ArchiveTests.cs` (SQLite in-memory).

**Interfaces — Produces:**
```csharp
public class MatchRecord { int Id; string RoomCode; string Season; DateTime StartedAt; DateTime EndedAt; int PlayerCount; List<MatchResultRecord> Results; }
public class MatchResultRecord { int Id; int MatchId; int Rank; string Name; string IconId; int FinalTile; string TileName; bool Finished; long? FinishTimeMs; int Correct; int Wrong; bool Left; }
public sealed class MatchArchive {   // uses IDbContextFactory<AppDb>
  public Task<int> SaveAsync(Room room);                          // all players
  public Task<IReadOnlyList<MatchResultRecord>> FastestAsync(int n = 5);   // Finished, by FinishTimeMs
  public Task<IReadOnlyList<MatchResultRecord>> BestPercentAsync(int n = 5); // Finished only, by % desc then FinishTimeMs
  public Task<(IReadOnlyList<MatchRecord> Items, int Total)> HistoryAsync(int page, int pageSize = 20);
  public Task<MatchRecord?> GetAsync(int id);
}
```
Provider: `ConnectionStrings:Default` env/config; if it starts with `Host=` or `postgres` use `UseNpgsql`, else `UseSqlite` (default `Data Source=hanhtrinh.db`). `db.Database.Migrate()` on startup — generate migrations once for SQLite and keep provider-neutral types (use `EnsureCreated()` instead if migrations differ per provider: `shortcut: EnsureCreated, no schema upgrades — switch to per-provider migrations when the schema changes`).

- [ ] **Step 1: Failing tests:** `Save_StoresAllPlayersIncludingLeftAndUnfinished`; `BestPercent_OnlyFinished_TieByFinishTime`; `Fastest_Top5`; `History_NewestFirst_Paged`.
- [ ] **Step 2:** FAIL → **Step 3:** implement; `RoomManager.MatchEnded` → `SaveAsync` (fire-and-forget with logged exception), store `LastMatchId` → **Step 4:** PASS.
- [ ] **Step 5:** Leaderboard page: tabs Kỷ lục (two top-5 tables) / Lịch sử trận (room code, time, season, players; paged); MatchDetail shows full ranking table (reuse `ResultsView` table part). Commit `feat: records and match history` — **Milestone 3 done.**

## Milestone 4 — deploy

### Task 11: Docker, deploy guide, smoke test

**Files:** Create `Dockerfile`, `.dockerignore`, `docs/DEPLOY.md`.

- [ ] **Step 1:** Multi-stage Dockerfile (`mcr.microsoft.com/dotnet/sdk:8.0` → `aspnet:8.0`), `ASPNETCORE_URLS=http://+:${PORT:-8080}` (Render injects `PORT`); `ForwardedHeaders` enabled in `Program.cs` for proxy HTTPS.
- [ ] **Step 2:** `docker build` locally if Docker available; else `dotnet publish -c Release` succeeds.
- [ ] **Step 3:** `docs/DEPLOY.md` in Vietnamese: push to GitHub; Render → New Web Service → Docker, region Singapore, free; env var `ConnectionStrings__Default` = Neon connection string (Npgsql format `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require`); health check path `/healthz`; UptimeRobot/cron-job.org ping `/healthz` every 10 min; open site 5 min before 10:00.
- [ ] **Step 4:** After deploy: 3+ devices play one full match on the live URL; check results saved in Xếp hạng. Commit `chore: docker and deploy guide` — **Milestone 4 done.**
