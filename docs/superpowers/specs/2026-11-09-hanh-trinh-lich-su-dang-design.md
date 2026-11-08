# Thiết kế: Web game bàn cờ "Hành trình Lịch sử Đảng"

Ngày: 2026-11-09 · Trạng thái: **đã duyệt** (02:30, 9/11/2026)

> Quy ước: mục đánh dấu **[Quyết định của Claude]** là đề xuất của Claude mà người dùng chưa trực tiếp chọn — người dùng có thể sửa. Mọi mục khác là người dùng đã chốt.

## 1. Mục đích và bối cảnh

- Sản phẩm sáng tạo (SPST) cho môn Lịch sử Đảng Cộng sản Việt Nam (VNR202).
- Nội dung chuẩn: *Giáo trình Lịch sử Đảng Cộng sản Việt Nam* (NXB Chính trị quốc gia Sự thật, 2021, hệ không chuyên). Mọi câu hỏi, giải thích, mốc thời gian lấy từ giáo trình, ghi số trang.
- Cách dùng: người dùng lên thuyết trình, chiếu màn hình của mình lên máy chiếu và **chơi như một người bình thường**; cả lớp chơi cùng lúc trên máy tính. Giảng viên không tham gia.
- Thành công = cả lớp vào phòng nhanh, chơi trọn một trận ~10 phút không lỗi, vừa vui vừa ôn được kiến thức; kết quả lưu lại được.

Quy định SPST phải tuân thủ (anti-goal nếu vi phạm): đảm bảo tư tưởng chính trị, tuân thủ pháp luật và quy định nhà trường; không vi phạm đạo đức, bản quyền, thuần phong mỹ tục; thông điệp tích cực, mang tính xây dựng; tuyệt đối an toàn.

## 2. Mục tiêu / Ngoài phạm vi / Anti-goal

**Mục tiêu:** game bàn cờ nhiều người chơi thời gian thực, 1–60 người/phòng, chạy trên trình duyệt máy tính; ~500 câu hỏi chất lượng từ giáo trình; bảng kỷ lục và lịch sử trận.

**Ngoài phạm vi (bản đầu):**
- Đăng nhập / tài khoản. Trang quản trị.
- Giao diện điện thoại (chỉ cần chạy tốt trên máy tính, màn 1366×768 trở lên) **[Quyết định của Claude]**.
- Phát lại diễn biến trận (chỉ xem bảng kết quả cuối).
- Âm thanh **[Quyết định của Claude]**.
- Đưa lên hosting — người dùng sẽ tính sau. Nhưng code phải sẵn sàng để triển khai (mục 10).
- Sự kiện sau Đại hội XIII (2021).

**Anti-goal:**
- Câu hỏi sai so với giáo trình, hoặc hỏi vào các chỗ giáo trình tự mâu thuẫn (mục 8.3).
- Bản đồ thiếu Hoàng Sa, Trường Sa; dùng ảnh bản đồ không rõ nguồn.
- Tên hiệu ứng/thông điệp tô vẽ thiên tai, chiến tranh, hay xúc phạm.
- Tên người chơi tục tĩu hiện lên máy chiếu.
- Học sinh gian lận được bằng cách sửa code trên trình duyệt (luật chơi phải chạy trên server).

## 3. Cách xây dựng (Approaches)

| Hướng | Mô tả | Đánh đổi |
|---|---|---|
| **A. Blazor Server (đã chọn)** | Một project ASP.NET Core .NET 8, Blazor Web App chế độ Interactive Server; SignalR có sẵn đẩy cập nhật; trạng thái phòng/trận trong bộ nhớ server; SQLite + EF Core cho kỷ lục và lịch sử. | Ít code nhất, toàn C#, luật chơi chỉ chạy trên server nên không gian lận được. Mỗi người giữ một kết nối liên tục; mạng chập chờn dễ rớt → cần cơ chế vào lại. Chỉ chạy được 1 instance. |
| B. Blazor WebAssembly + SignalR Hub | UI chạy trên máy người chơi, server giữ luật. | Server nhẹ, chịu mạng yếu tốt hơn; nhưng 3 project, tự viết giao thức, ~1,5 lần code, phải tải vài MB đầu. |
| C. ASP.NET Core + SignalR + HTML/JS thuần | Giao diện viết bằng JavaScript. | Nhẹ nhất nhưng phần lớn code là JS — trái yêu cầu "code bằng C#". |

Người dùng chọn **A**. Vercel đã được xem xét và loại (không chạy .NET, không giữ WebSocket, không có ổ đĩa lâu dài).

## 4. Luồng màn hình

Mọi trang đều có nút **"Quay lại"**. Toàn bộ giao diện tiếng Việt. Chữ đủ to để đọc được trên máy chiếu.

```
Menu ── Chơi ──► Đặt tên ──► Chọn phòng ──► (vào phòng) Chọn icon ──► Phòng chờ
  │                              │                                     │ Bắt đầu
  ├─ Xếp hạng (Kỷ lục | Lịch sử trận)                                  ▼
  └─ Luật chơi                                  Đếm ngược 5s + thông báo mùa
                                                                       ▼
                                                    Màn chơi ──► Màn kết quả
                                                                  ├─ Chơi lại → Phòng chờ cũ
                                                                  └─ Về Menu
```

### 4.1 Menu
Ba nút: **Chơi**, **Xếp hạng**, **Luật chơi**.

### 4.2 Xếp hạng
- Tab **Kỷ lục**: top 5 người về đích nhanh nhất; top 5 người có % đúng cao nhất. **[Quyết định của Claude]** Bảng % đúng chỉ tính người đã về đích (tránh 1/1 = 100%); hòa % thì ai về đích nhanh hơn xếp trên.
- Tab **Lịch sử trận**: danh sách trận đã kết thúc, mới nhất trước, mỗi dòng ghi mã phòng + thời gian bắt đầu (+ mùa, số người **[Quyết định của Claude]**). Bấm vào xem **bảng kết quả cuối** của trận đó với **đủ tất cả người chơi** (cả người chưa về đích, người đã rời) — giống màn kết quả cuối trận (không phát lại diễn biến). Chỉ riêng bảng Kỷ lục % đúng mới lọc người đã về đích. Phân trang 20 trận/trang **[Quyết định của Claude]**.

### 4.3 Luật chơi
Ghi rõ toàn bộ luật ở tài liệu này (lượt chơi, ô, buff/debuff, mùa, kết thúc, xếp hạng), nguồn câu hỏi (giáo trình), ghi công OpenMoji (CC BY-SA 4.0) và ghi chú bản đồ. Builder soạn nội dung từ tài liệu này.

### 4.4 Đặt tên
- Tên 1–16 ký tự sau khi cắt khoảng trắng; cho phép chữ (có dấu), số, khoảng trắng **[Quyết định của Claude]**.
- **Lọc tên không phù hợp** (danh sách từ tục/nhạy cảm tiếng Việt + tiếng Anh, so khớp không dấu, không phân biệt hoa thường, bỏ ký tự chen giữa) **[Quyết định của Claude]**.
- Tên **được phép trùng** — icon mới là thứ phân biệt người trong phòng.
- Tên lưu trong trình duyệt để lần sau điền sẵn **[Quyết định của Claude]**.

### 4.5 Chọn phòng
- Nút **Tạo phòng**: tạo phòng mới với mã **6 chữ số ngẫu nhiên**, không trùng phòng đang tồn tại.
- Ô **nhập mã** + nút vào phòng.
- **Danh sách phòng**: mỗi dòng hiện **"Phòng của <tên chủ phòng>"** + số người/60. **Không bao giờ hiện mã phòng** ở đây. Phòng khóa vẫn hiện kèm 🔒 và không bấm vào được (chỉ vào bằng mã). **[Quyết định của Claude]** Phòng đang chơi hiện nhãn "Đang chơi", không vào được; phòng đầy không vào được. Danh sách tự cập nhật thời gian thực.
- **[Quyết định của Claude]** Giới hạn: mỗi trình duyệt chỉ là chủ của 1 phòng; toàn server tối đa 20 phòng cùng lúc.

### 4.6 Chọn icon (ngay sau khi vào phòng)
- Mỗi phòng có cùng một bộ **60 icon OpenMoji**; icon đã có người chọn thì **tô xám**, không chọn được. Cập nhật thời gian thực; nếu hai người bấm cùng lúc, server chỉ nhận người đến trước, người sau được báo chọn lại.
- **[Quyết định của Claude]** Bộ 60 icon là động vật, cây, đồ vật trung tính — không cờ, không biểu tượng tôn giáo/chính trị/vũ khí. File SVG đóng gói trong `wwwroot`, ghi công OpenMoji.

### 4.7 Phòng chờ
- Hiện **mã phòng** (để chủ phòng đọc cho cả lớp), số người/60, tên + icon từng người.
- Chủ phòng có: nút **Khóa/Mở khóa**, nút **Đuổi** cạnh từng người, nút **Bắt đầu**.
- **Bắt đầu sáng khi có ít nhất 1 người** (chơi một mình được). Tối đa 60 người.
- Phòng **tự xóa khi không còn ai**.
- **[Quyết định của Claude]** Người bị đuổi quay về màn Chọn phòng và không vào lại được phòng đó. Chủ phòng rời phòng chờ → quyền chủ phòng chuyển cho người vào sớm nhất còn lại.

### 4.8 Đếm ngược
**5 giây**, trong lúc đếm hiện **thông báo mùa** (tên mùa, lợi, hại). Cả phòng vào trận cùng lúc.

### 4.9 Màn chơi

```
┌────────────────────────────────────────────┬────────────────────────┐
│ ⏱ 05:32                                     │ 🏆 TOP 5               │
│                                            │ 1. 🦁 Lan    Huế       │
│      BẢN ĐỒ CHỮ S (chiếm ~70% màn)         │ 2. ⭐ Hùng   Quảng Nam │
│   🟢 ô buff  🔴 ô debuff  ✨ tỉnh phát sáng  │ ...        [Xem thêm]  │
│   ❓ Hoàng Sa / Trường Sa                   ├────────────────────────┤
│        ┌────────────────────────┐          │ 🌸 TRĂM HOA ĐUA NỞ     │
│        │ Câu 13 · Quảng Bình    │          │ ＋ ... − ...           │
│        │ <câu hỏi>              │          ├────────────────────────┤
│        │ [A] [B] [C] [D]        │          │ 👤 BẠN  ✔12 ✘3 (80%)   │
│        │ 🎒 Dùng buff câu hỏi    │          │ 🎒 Túi: ...            │
│        └────────────────────────┘          │ ✨ Hiệu ứng đang có     │
└────────────────────────────────────────────┴────────────────────────┘
```

- **Trái: bản đồ** chiếm phần lớn màn hình. Quân cờ của mọi người (icon) hiện trên ô của họ; nhiều người cùng ô thì xếp chồng/thu nhỏ **[Quyết định của Claude]**.
- **Câu hỏi là popup** ở giữa vùng bản đồ khi quân cờ dừng ở ô; không che cột phải. Popup có các nút dùng **buff câu hỏi** trong túi.
- Trả lời đúng → popup chuyển thành **hoạt ảnh gieo xúc xắc** (~1 giây, CSS), hiện kết quả kèm điều chỉnh (vd. `6 − 1 (Nồm ẩm) = 5`) → quân cờ đi từng ô.
- Trả lời sai → cooldown (mặc định 5 giây); trong lúc chờ hiện **đáp án đúng + giải thích + số trang**.
- **Phải: bảng xếp hạng** — nổi bật top 5, nút **"Xem thêm"** mở danh sách đầy đủ giảm dần, cuộn bằng con lăn; **khung mùa** (lợi/hại, ghim suốt trận); **khung của bạn**: số đúng/sai/%, túi buff, các hiệu ứng **đang tác động lên bạn ở ô hiện tại**.
- **Không** có nút xem lại câu đã gặp trong trận.
- Nút "Quay lại" trong trận hỏi **"Bạn chắc chắn muốn rời trận?"**.
- Đồng hồ trận đếm lên từ lúc bắt đầu; khi có người về đích, hiện thêm đồng hồ đếm ngược 1:30.

### 4.10 Màn kết quả
- Bảng kết quả cuối: hạng, icon, tên, vị trí (tỉnh) / thời gian về đích, số câu đúng, sai, % đúng.
- **Xem lại các câu mình đã gặp** (chỉ ở màn này): câu hỏi, đáp án mình chọn, đáp án đúng, giải thích, số trang.
- Hai nút: **Chơi lại** (về phòng chờ cũ) và **Về Menu**.
- **[Quyết định của Claude]** Danh sách câu đã gặp không lưu vào Lịch sử trận (không có đăng nhập).

## 5. Luật chơi

### 5.1 Bản đồ
- 63 tỉnh thành theo địa giới hành chính **trước 7/2025** — ghi chú trên bản đồ đúng nguyên văn: **"63 tỉnh thành theo địa giới hành chính trước 7/2025"**.
- 65 ô: 63 tỉnh + **ô Hoàng Sa** (ngay sau Đà Nẵng) + **ô Trường Sa** (ngay sau Khánh Hòa). Xuất phát **Hà Giang** (ô 1), đích **Cà Mau** (ô 65).
- Bản đồ **cách điệu hình chữ S**, vẽ bằng SVG tự làm (không lấy ảnh bản đồ từ nguồn khác). Hoàng Sa và Trường Sa vẽ rõ là quần đảo của Việt Nam, có nhãn tên, đặt **xa ra ngoài biển** so với đất liền (đường đi nối ra đảo rồi quay vào).
- Đường đi tuyến tính Bắc → Nam, mỗi miền là một đoạn liền nhau. Thứ tự đề xuất **[Quyết định của Claude]** (builder được chỉnh thứ tự trong cùng một miền cho đẹp hình, nhưng giữ các ràng buộc trên):

| Miền | Ô | Thứ tự |
|---|---|---|
| Bắc (25) | 1–25 | Hà Giang, Cao Bằng, Lạng Sơn, Quảng Ninh, Bắc Giang, Thái Nguyên, Bắc Kạn, Tuyên Quang, Yên Bái, Lào Cai, Lai Châu, Điện Biên, Sơn La, Hòa Bình, Phú Thọ, Vĩnh Phúc, Hà Nội, Bắc Ninh, Hải Dương, Hải Phòng, Thái Bình, Hưng Yên, Hà Nam, Nam Định, Ninh Bình |
| Trung (14 + 2 đảo) | 26–41 | Thanh Hóa, Nghệ An, Hà Tĩnh, Quảng Bình, Quảng Trị, Thừa Thiên Huế, Đà Nẵng, **Hoàng Sa**, Quảng Nam, Quảng Ngãi, Bình Định, Phú Yên, Khánh Hòa, **Trường Sa**, Ninh Thuận, Bình Thuận |
| Tây Nguyên (5) | 42–46 | Kon Tum, Gia Lai, Đắk Lắk, Đắk Nông, Lâm Đồng |
| Nam (19) | 47–65 | Bình Phước, Tây Ninh, Bình Dương, Đồng Nai, Bà Rịa – Vũng Tàu, TP. Hồ Chí Minh, Long An, Tiền Giang, Bến Tre, Trà Vinh, Vĩnh Long, Đồng Tháp, An Giang, Kiên Giang, Cần Thơ, Hậu Giang, Sóc Trăng, Bạc Liêu, Cà Mau |

(Trên bản đồ, đoạn Tây Nguyên vẽ thành nhánh trong đất liền nối từ Bình Thuận lên, rồi đi xuống Bình Phước.)

### 5.2 Nhãn vùng
Mỗi ô có **1 miền** (Bắc / Trung / Tây Nguyên / Nam) và **một tập nhãn**:
- **Ven biển** (28 tỉnh): Quảng Ninh, Hải Phòng, Thái Bình, Nam Định, Ninh Bình, Thanh Hóa, Nghệ An, Hà Tĩnh, Quảng Bình, Quảng Trị, Thừa Thiên Huế, Đà Nẵng, Quảng Nam, Quảng Ngãi, Bình Định, Phú Yên, Khánh Hòa, Ninh Thuận, Bình Thuận, Bà Rịa – Vũng Tàu, TP. Hồ Chí Minh, Tiền Giang, Bến Tre, Trà Vinh, Sóc Trăng, Bạc Liêu, Cà Mau, Kiên Giang.
- **Đảo**: Hoàng Sa, Trường Sa.
- **Miền núi phía Bắc** (14): Hà Giang, Cao Bằng, Bắc Kạn, Lạng Sơn, Tuyên Quang, Thái Nguyên, Phú Thọ, Bắc Giang, Lào Cai, Yên Bái, Lai Châu, Điện Biên, Sơn La, Hòa Bình.
- **Bắc Trung Bộ** (6): Thanh Hóa → Thừa Thiên Huế.
- **Nam Bộ** = miền Nam (19 tỉnh). **Trung Bộ** = miền Trung (14 tỉnh, không gồm đảo).
- Có thể thêm nhãn **Đồng bằng / Cao nguyên** để dành cho sau; bản đầu chỉ dùng các nhãn trên.

### 5.3 Lượt chơi
- Mỗi người **tự chạy theo nhịp riêng**, không chờ lượt người khác.
- Bắt đầu ở Hà Giang: trả lời 1 câu.
- **Đúng** → gieo xúc xắc (1–6, server quyết định) → đi → đến ô mới → hiệu ứng ô (nếu có) → câu hỏi mới → …
- **Sai** → cooldown 5 giây (hiện đáp án đúng, giải thích, số trang) → câu hỏi khác ở cùng ô.
- **Không gặp lại câu đã gặp**: chọn ngẫu nhiên trong pool, loại các câu người đó đã gặp. **[Quyết định của Claude]** Nhớ theo người chơi (mã ẩn trong trình duyệt) qua các trận suốt thời gian server chạy; khi đã gặp hết pool thì xóa danh sách và bắt đầu lại.
- Câu hỏi thường **không giới hạn thời gian** (trừ debuff "Chủ quan, nóng vội").
- **Về đích** = đến hoặc vượt ô 65 (Cà Mau). Thời gian về đích = thời gian từ lúc trận bắt đầu.

### 5.4 Ô buff 🟢 / debuff 🔴
- Đầu mỗi trận rải ngẫu nhiên **7 ô buff và 7 ô debuff**, hiện icon cho mọi người thấy ngay từ đầu. Không rải vào ô 1, ô 65, 2 ô đảo, và các tỉnh đang phát sáng của mùa (5.7).
- Dừng ở ô 🟢 → nhận 1 buff ngẫu nhiên; ô 🔴 → nhận 1 debuff ngẫu nhiên. Ô không mất sau khi kích hoạt — ai dừng cũng bị.
- **[Quyết định của Claude]** Icon chỉ cho biết ô buff hay debuff; loại cụ thể bốc khi dừng vào. Chỉ **dừng lại** mới kích hoạt, đi ngang qua thì không.
- **Ô đảo Hoàng Sa / Trường Sa**: bốc ngẫu nhiên từ cả hai bảng buff và debuff (50/50 **[Quyết định của Claude]**).
- **[Quyết định của Claude]** Di chuyển do hiệu ứng (lùi ô, tiến thêm ô, nhảy ô) **không kích hoạt dây chuyền** ô buff/debuff/thêm bước ở ô mới; câu hỏi vẫn hiện ở ô mới. Chỉ di chuyển từ gieo xúc xắc mới kích hoạt ô.

### 5.5 Buff
**Buff câu hỏi** — cất vào **túi (tối đa 3)**, người chơi tự bấm dùng **trong popup câu hỏi**:

| Tên | Hiệu ứng |
|---|---|
| Bình dân học vụ | loại 1 đáp án sai |
| Diệt giặc dốt | loại 2 đáp án sai |
| Kim chỉ nam *(hiếm)* | loại cả 3 đáp án sai |
| Đổi mới | đổi sang câu hỏi khác |
| Phê bình và tự phê bình | chọn sai thì được chọn lại 1 lần, không cooldown (dùng trước khi trả lời) |
| Chớp thời cơ | bỏ qua câu hỏi này, gieo xúc xắc luôn (tính như không trả lời — không cộng đúng/sai) **[Quyết định của Claude]** |

**Buff tự kích hoạt** (thành hiệu ứng chờ, mất khi đã dùng):

| Tên | Hiệu ứng |
|---|---|
| Thần tốc | lần gieo kế tiếp ×2 |
| Tiếp viện Trường Sơn | tiến thêm 2 ô ngay |
| Đoàn tàu không số | nhảy ngay đến ô ven biển kế tiếp phía trước (không có thì không tác dụng) |
| Nắm quyền chủ động | lần gieo kế tiếp tự chọn số 1–6 |
| Dĩ bất biến, ứng vạn biến | miễn cooldown cho lần sai kế tiếp |
| Thế trận lòng dân | chặn 1 debuff kế tiếp bốc từ ô 🔴 hoặc ô đảo (không chặn hiệu ứng mùa) |

**[Quyết định của Claude]** Túi đầy thì buff câu hỏi mới bị bỏ, hiện thông báo "Túi đầy". Trọng số bốc: Kim chỉ nam bằng 1/3 các buff khác; còn lại đều nhau. Cùng loại hiệu ứng chờ cộng dồn thành nhiều lần dùng liên tiếp.

### 5.6 Debuff

| Tên | Hiệu ứng |
|---|---|
| Khủng bố trắng | lùi 2 ô (không lùi quá ô 1) |
| Bao vây cấm vận | lần sai kế tiếp chờ 10 giây |
| Khủng hoảng kinh tế | lần gieo kế tiếp tối đa 3 |
| Ngăn sông cấm chợ | phải trả lời đúng **2 câu** mới được gieo xúc xắc (sai vẫn cooldown như thường) |
| Chủ quan, nóng vội | câu kế tiếp chỉ có 10 giây, hết giờ tính là sai |
| Diễn biến hòa bình | mất 1 buff ngẫu nhiên trong túi; túi rỗng thì mất 1 buff chờ; không có gì thì không tác dụng **[Quyết định của Claude]** |

### 5.7 Mùa
- Mỗi trận bốc ngẫu nhiên 1 mùa. Thông báo lúc đếm ngược 5 giây; bản đồ đổi tông màu theo mùa; khung mùa ghim ở cột phải suốt trận.
- Hiệu ứng theo vùng tính theo **ô người chơi đang đứng lúc gieo/lúc trả lời** **[Quyết định của Claude]**.

| Mùa | Lợi | Hại |
|---|---|---|
| 🌸 **Trăm hoa đua nở** (Xuân) | Gieo ra 1 hoặc 6 (số gieo thật, không tính "Nắm quyền chủ động") → nhận 1 buff câu hỏi ngẫu nhiên | **Nồm ẩm** (miền Bắc, Trung): mỗi lần gieo −1 bước · **Khô hạn, xâm nhập mặn** (Tây Nguyên, Nam): sai chờ 8 giây |
| ☀️ **Nắng hạ biển xanh** (Hạ) | **Mùa du lịch biển**: dừng ở ô ven biển (do gieo) thì tiến thêm 1 ô · **Mùa trái cây** (Nam Bộ): trả lời đúng có 30% được thêm 1 buff ngẫu nhiên | **Gió Lào** (Bắc Trung Bộ): mỗi lần gieo tối đa 4 · **Nắng nóng** (cả bản đồ): câu hỏi hiện chậm 1 giây |
| 🍂 **Đồng lòng vượt bão** (Thu) | **Đồng lòng**: đúng 3 câu liên tiếp → nhận buff "Chớp thời cơ" (đếm lại từ 0 sau khi nhận) · **Lúa chín vùng cao** (miền núi phía Bắc): +1 bước | **Trú bão** (mọi ô ven biển + 2 đảo): chờ 3 giây trước câu kế tiếp; ở ô đảo vẫn bốc thẻ như thường |
| ❄️ **Rét ngọt chờ Xuân** (Đông) | **Chuẩn bị Tết**: mỗi câu đúng có 50% được +1 vào lần gieo ngay sau đó | **Rét đậm** (miền núi phía Bắc): −1 bước · **Mưa lũ cuối năm** (Trung Bộ): sai chờ 7 giây |

Tên mùa Hạ/Thu/Đông là đề xuất của Claude, người dùng không phản đối. Tên hiệu ứng thời tiết theo hướng **ứng phó**, không tô vẽ thiên tai.

### 5.8 Công thức xúc xắc và cooldown **[Quyết định của Claude]**
- Số bước = số gieo (hoặc số tự chọn) → ×2 nếu Thần tốc → áp giới hạn trên (Khủng hoảng kinh tế ≤3, Gió Lào ≤4; lấy giới hạn nhỏ nhất) → cộng/trừ hiệu ứng mùa (±1) và Chuẩn bị Tết (+1) → **tối thiểu 1**.
- Popup hiện đầy đủ phép tính, vd. `6 ×2 (Thần tốc) − 1 (Nồm ẩm) = 11`.
- Cooldown khi sai = **lớn nhất** trong các giá trị đang áp dụng (5 mặc định, 7/8 theo mùa, 10 Bao vây cấm vận); "Dĩ bất biến" đưa về 0.

### 5.9 Tỉnh phát sáng theo mùa (thay cho ô lịch sử cố định)
- Không còn ô lịch sử cố định. Trong mỗi trận, các tỉnh có sự kiện diễn ra đúng mùa đó **phát sáng ✨**.
- Dừng ở tỉnh phát sáng: trả lời **đúng** câu thường → được thêm **1 câu lịch sử** về sự kiện của tỉnh đó. Đúng → nhận **2 buff** ngẫu nhiên. Sai → không mất gì, không cooldown. Sau đó gieo xúc xắc như thường. Mỗi lần dừng chỉ có 1 câu lịch sử.
- Mùa theo tháng: Xuân 2–4, Hạ 5–7, Thu 8–10, Đông 11–1.

| Mùa | Tỉnh phát sáng (sự kiện) |
|---|---|
| 🌸 Xuân | Quảng Ngãi (Khởi nghĩa Ba Tơ 3/1945), Đắk Lắk (Buôn Ma Thuột 10/3/1975), Thừa Thiên Huế (giải phóng 26/3/1975), Đà Nẵng (giải phóng 29/3/1975), TP. Hồ Chí Minh (30/4/1975) |
| ☀️ Hạ | Cao Bằng (Hội nghị TW8 5/1941), Điện Biên (7/5/1954), Quảng Trị (Thành cổ 1972), Hà Nội (Quốc hội nước Việt Nam thống nhất 6–7/1976) |
| 🍂 Thu | Lạng Sơn (Bắc Sơn 9/1940), Nghệ An và Hà Tĩnh (Xô viết Nghệ Tĩnh 9/1930), Tuyên Quang (Tân Trào 8/1945), Hà Nội (19/8 và 2/9/1945), Thừa Thiên Huế (23/8/1945), TP. Hồ Chí Minh (25/8 và 23/9/1945), Bắc Kạn (Việt Bắc thu đông 10/1947) |
| ❄️ Đông | Cao Bằng (Pác Bó 1/1941), Hà Nội (19/12/1946; 12 ngày đêm 12/1972), Bắc Kạn (Việt Bắc 12/1947), Tây Ninh (MTDTGPMNVN 20/12/1960), Bến Tre (Đồng khởi 1/1960), Tiền Giang (Ấp Bắc 1/1963), Bình Phước (Phước Long 1/1975) |

**Bắt buộc**: mọi ngày tháng và số trang trong bảng này phải được đối chiếu với giáo trình khi soạn nội dung; chỗ nào giáo trình ghi khác thì theo giáo trình, sự kiện nào giáo trình không nhắc thì bỏ.

### 5.10 Kết thúc trận và xếp hạng
- **1 người chơi**: về đích là kết thúc ngay.
- **Nhiều người**: kết thúc khi **tất cả đã về đích**, hoặc **1:30 sau khi người đầu tiên về đích**, tùy cái nào đến trước.
- **[Quyết định của Claude]** Người đã xác nhận rời trận không tính vào điều kiện "tất cả về đích". Giới hạn cứng 20 phút nếu chưa ai về đích.
- **Xếp hạng**: vị trí trên bản đồ trước (người về đích xếp trên, theo thời gian về đích), rồi đến % đúng. **[Quyết định của Claude]** Hòa nữa thì ai đến ô đó sớm hơn xếp trên. Người rời trận vẫn có trong bảng, ghi "đã rời".
- % đúng = đúng / (đúng + sai). Câu lịch sử của tỉnh phát sáng **có** tính vào đúng/sai **[Quyết định của Claude]**; câu bỏ qua bằng Chớp thời cơ không tính.

### 5.11 Rớt mạng và rời trận
- Trình duyệt giữ một **mã ẩn** (token ngẫu nhiên trong localStorage) để nhận lại người chơi — không dùng tên vì tên được trùng.
- Rớt mạng/tải lại trang → vào lại đúng phòng/trận, giữ nguyên vị trí, túi, hiệu ứng, thống kê. Trong lúc mất kết nối, quân cờ vẫn đứng yên trên bản đồ.
- Rời trận (đã xác nhận) → về Menu; **[Quyết định của Claude]** không vào lại trận đó được.

## 6. Nội dung câu hỏi

### 6.1 Pool chung — ~500 câu
- Trắc nghiệm 4 đáp án, đúng 1. Mỗi câu có: nội dung, 4 đáp án, đáp án đúng, **giải thích ngắn**, **số trang giáo trình**, chương.
- Phân bố theo giáo trình (Chương nhập môn, Chương 1: 1930–1945, Chương 2: 1945–1975, Chương 3: 1975–nay đến Đại hội XIII) tỉ lệ theo độ dài chương **[Quyết định của Claude]**.
- Dạng câu đa dạng: mốc thời gian, nhân vật, nội dung nghị quyết/văn kiện, ý nghĩa, địa danh, khái niệm.
- Đáp án sai phải hợp lý (cùng loại với đáp án đúng), không dùng "Tất cả đều đúng/sai".
- Đáp án hiển thị theo thứ tự xáo trộn mỗi lần **[Quyết định của Claude]**.

### 6.2 Câu lịch sử của tỉnh phát sáng
- Mỗi cặp (tỉnh, mùa) trong bảng 5.9 có **ít nhất 4 câu** riêng về sự kiện đó, cùng định dạng 6.1. Không trùng với pool chung.

### 6.3 Chỗ giáo trình tự mâu thuẫn — KHÔNG ra câu hỏi
- Ngày ký Hiệp định Pari (tr.222 ghi 21/1/1973; tr.227–228 ghi 27/1/1973).
- Thời điểm Nghị quyết về Chiến lược biển (tr.315 ghi 9/2/2007; tr.379 ghi "(6/2009)").
- tr.364 "đến năm 2000" (nghi lỗi in).
- Không có sự kiện sau Đại hội XIII.

### 6.4 Lưu trữ nội dung
- File JSON trong repo (vd. `Data/questions.json`, `Data/seasonal-questions.json`, `Data/provinces.json`), nạp khi khởi động **[Quyết định của Claude]**.
- Test tự động kiểm tra: id không trùng, đủ 4 đáp án khác nhau, đúng 1 đáp án đúng, có giải thích và số trang hợp lệ, mỗi cặp (tỉnh, mùa) có ≥4 câu, `provinces.json` đủ 65 ô đúng thứ tự và nhãn.
- **File PDF giáo trình không được đưa vào repo** (bản quyền); người dùng cung cấp riêng khi soạn câu hỏi.
- **Người soạn**: Claude soạn toàn bộ câu hỏi từ giáo trình (người dùng đã giao). Cần có PDF trên máy trước khi bắt đầu soạn. Có thể làm phần game song song với vài câu hỏi mẫu.
- **[Quyết định của Claude]** Sinh thêm file `docs/questions-review.md` từ JSON (một câu mỗi mục: câu hỏi, đáp án, giải thích, trang) để người dùng đọc duyệt mà không cần đọc JSON.

## 7. Kiến trúc kỹ thuật

- **.NET 8**, một Blazor Web App, render mode **Interactive Server** toàn cục. Không thêm thư viện UI/JS ngoài.
- **[Quyết định của Claude]** Cấu trúc: `src/<App>.Web` (UI + server) chứa thư mục `Game/` là engine thuần C# (không phụ thuộc Blazor); `tests/<App>.Tests` (xUnit) test engine và dữ liệu.
- **Engine**: luật chơi thuần, nhận RNG và đồng hồ qua tham số để test được (xúc xắc, hiệu ứng, mùa, rải ô, kết thúc, xếp hạng).
- **RoomManager** (singleton): giữ các phòng trong bộ nhớ; mỗi phòng có khóa riêng để xử lý đồng thời; mọi hành động (trả lời, gieo, dùng buff) do server kiểm tra và quyết định. Đổi trạng thái → phát sự kiện → các component gọi `InvokeAsync(StateHasChanged)`.
- **Hẹn giờ** (cooldown, trú bão, 10 giây, 1:30, 20 phút) chạy ở server theo mốc thời gian; trình duyệt chỉ hiển thị đếm ngược.
- **Hoạt ảnh** xúc xắc và quân cờ đi từng ô làm bằng CSS; server đã quyết kết quả trước khi chạy hoạt ảnh.
- **Lưu trữ** EF Core, chỉ ghi khi trận kết thúc. Máy dev dùng **SQLite**; trên web dùng **PostgreSQL miễn phí (Neon)** qua Npgsql, chọn theo connection string trong biến môi trường — vì ổ đĩa của Render free bị xóa mỗi lần khởi động lại nên SQLite trên Render sẽ mất dữ liệu. Không dùng Firebase (phải viết lại tầng dữ liệu, không dùng được EF Core). Bảng:
  - `Match`: Id, RoomCode, Season, StartedAt, EndedAt, PlayerCount.
  - `MatchResult`: MatchId, Rank, Name, IconId, FinalTile, Finished, FinishTimeMs, Correct, Wrong, Left.
  - Kỷ lục tính bằng truy vấn trên `MatchResult`.
- Trạng thái phòng/trận trong bộ nhớ: server khởi động lại thì mất các phòng đang mở (chấp nhận được).
- **Hiệu năng mục tiêu**: 60 người/phòng, tổng ~120 người đồng thời trên 1 instance; cập nhật vị trí đến mọi máy dưới ~300 ms trong mạng bình thường.

## 8. Bảo mật và an toàn
- Luật chơi chỉ chạy trên server; client chỉ gửi ý định (chọn đáp án X, dùng buff Y). Không gửi đáp án đúng xuống client trước khi trả lời.
- Lọc tên (4.4). Giới hạn số phòng (4.5). Mã phòng 6 số ngẫu nhiên bằng RNG an toàn.
- HTTPS khi triển khai.
- Không thu thập dữ liệu cá nhân ngoài tên tự đặt.

## 9. Kiểm thử
- Unit test engine: công thức xúc xắc (5.8), từng buff/debuff, hiệu ứng từng mùa, tỉnh phát sáng, rải ô hợp lệ, không lặp câu, điều kiện kết thúc (1 người / tất cả về đích / 1:30 / 20 phút), xếp hạng, vào lại.
- Test dữ liệu (6.4).
- Kiểm thử thủ công: mở nhiều tab trình duyệt để mô phỏng nhiều người; thử rớt mạng (tắt mạng tab) và vào lại.

## 10. Sẵn sàng triển khai (làm sau, nhưng code phải tương thích)
- Ngày thuyết trình, cả lớp vào game qua **web đã triển khai** (người dùng đã chốt đưa lên web, không chạy local trên laptop). Không cần màn "cách kết nối"/QR.
- **Chốt: Render (onrender.com) gói free, chạy Docker, deploy từ GitHub.** Database: Neon Postgres free; connection string đặt trong biến môi trường của Render, không commit.
- Chống ngủ đông: dùng dịch vụ ping ngoài (UptimeRobot / cron-job.org) gọi endpoint `/healthz` mỗi ~10 phút trong khung giờ cần; app có endpoint `/healthz` trả 200. (Ping 12 giây là thừa — Render chỉ ngủ sau ~15 phút không có request; ping từ chính app không đánh thức được app đã ngủ.)
- Chạy trên **1 instance**, cần **WebSocket** và **ổ đĩa lâu dài** cho file SQLite. Đường dẫn file DB lấy từ cấu hình (`appsettings` / biến môi trường). Tự chạy migration khi khởi động.
- Ứng viên đã bàn: Azure App Service (B1, Singapore, Azure for Students) hoặc Render (Docker). Vercel không dùng được.
- Có `Dockerfile` đơn giản **[Quyết định của Claude]** để không bị khóa vào một nhà cung cấp.

## 11. Thời hạn và thứ tự ưu tiên
- **Thuyết trình: 10:00 ngày 9/11/2026 (giờ Việt Nam).** Lúc viết mục này là ~02:20 cùng ngày → còn ~7,5 giờ, gồm cả triển khai và chơi thử.
- PDF giáo trình: `giao-trinh/Giao trinh lich su dang cong san viet nam.pdf` (thư mục `giao-trinh/` nằm trong `.gitignore`).
- Làm theo từng mốc, mỗi mốc phải chạy được trọn vẹn trước khi sang mốc sau:
  1. **Lõi chơi được (~06:00):** Menu, Đặt tên, Chọn phòng (tạo/mã/danh sách/khóa/đuổi), Chọn icon, Phòng chờ, đếm ngược, Màn chơi (bản đồ 65 ô, popup câu hỏi, xúc xắc, cooldown + giải thích), kết thúc trận, Màn kết quả + xem lại câu, vào lại khi rớt mạng, Luật chơi, lọc tên.
  2. **Hiệu ứng (~07:30):** 7+7 ô buff/debuff + túi, 4 mùa, tỉnh phát sáng.
  3. **Xếp hạng (~08:15):** SQLite, Kỷ lục, Lịch sử trận.
  4. **Triển khai + chơi thử nhiều máy (chậm nhất 09:00).**
- Câu hỏi soạn song song với code: tối thiểu **300 câu chung** + câu tỉnh phát sáng cho mốc 2; đủ 500 nếu kịp.
- Hết giờ ở mốc nào thì trình bày bản của mốc gần nhất đã chạy ổn.

## 12. Để builder tự quyết
- Tên project/namespace, cách tổ chức component.
- Màu sắc, font, kích thước cụ thể (miễn đọc rõ trên máy chiếu), vị trí tọa độ từng ô trên bản đồ SVG.
- Chọn 60 icon OpenMoji cụ thể (theo tiêu chí 4.6).
- Từ điển lọc tên.
- Tốc độ hoạt ảnh, cách xếp quân cờ cùng ô.
- Chi tiết UI thông báo (toast) khi nhận buff/debuff.
