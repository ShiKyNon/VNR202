# Đưa game lên web (Render + Neon)

Thời gian: khoảng 15–20 phút. Cần: tài khoản GitHub, Render (https://render.com), Neon (https://neon.tech).

## 1. Đẩy code lên GitHub

1. Tạo repo mới trên GitHub (Private hay Public đều được), **không** tick "Add README".
2. Trong thư mục `D:\Lam\Github\VNR202`:

   ```
   git remote add origin https://github.com/<tài-khoản>/<tên-repo>.git
   git push -u origin main
   ```

   Thư mục `giao-trinh/` (PDF giáo trình) đã nằm trong `.gitignore`, sẽ **không** bị đẩy lên.

## 2. Tạo database Neon (để lưu kỷ lục và lịch sử trận)

1. Đăng nhập https://console.neon.tech bằng GitHub → **Create project**.
2. Region: chọn **AWS Asia Pacific (Singapore)**. Tên tùy ý.
3. Ở trang project, mục **Connection string**: copy chuỗi dạng
   `postgresql://...@ep-xxxx.ap-southeast-1.aws.neon.tech/neondb?sslmode=require`
   (bỏ tick "Pooled connection" nếu có — dùng chuỗi direct cũng được).
4. **Không dán chuỗi này vào chat hay vào code.** Chỉ dán ở bước 3.5 bên dưới.

## 3. Tạo Web Service trên Render

1. https://dashboard.render.com → **New +** → **Web Service** → kết nối GitHub → chọn repo vừa đẩy.
2. **Language / Runtime: Docker** (Render tự thấy `Dockerfile` ở gốc repo).
3. **Region: Singapore**. **Instance type: Free**.
4. Mục **Advanced → Health Check Path**: `/healthz`.
5. Mục **Environment Variables** → Add:
   - Key: `ConnectionStrings__Default`
   - Value: chuỗi Neon ở bước 2.3
6. **Create Web Service**. Lần build đầu mất khoảng 5–8 phút. Khi log hiện `Application started`, mở link dạng `https://<tên>.onrender.com`.

Không có biến `ConnectionStrings__Default` thì game vẫn chạy, nhưng dùng SQLite trong container → **mất kỷ lục mỗi lần Render khởi động lại**.

## 4. Giữ server không ngủ

Render free cho server ngủ sau ~15 phút không có ai truy cập (lần mở lại mất 30–60 giây).

1. Đăng ký https://uptimerobot.com (miễn phí) → **Add New Monitor** → loại **HTTP(s)**.
2. URL: `https://<tên>.onrender.com/healthz`, Interval: **5 minutes**.

Dù vậy, **mở web khoảng 5 phút trước giờ thuyết trình** để chắc chắn server đã thức.

## 5. Kiểm tra trước giờ thuyết trình

- Mở link trên 2–3 máy/điện thoại khác nhau, tạo phòng, vào bằng mã, chơi thử một trận.
- Vào **Xếp hạng → Lịch sử trận** xem trận vừa chơi đã được lưu.

## Lưu ý khi chơi trên lớp

- Mỗi trình duyệt là một người chơi (mã ẩn lưu trong trình duyệt). Muốn thử nhiều người trên một máy thì dùng các cửa sổ ẩn danh/trình duyệt khác nhau.
- Rớt mạng: mở lại đúng link là vào lại trận.
- Cập nhật code: `git push` là Render tự build lại (trong lúc build, phòng đang chơi sẽ mất — đừng push trong giờ thuyết trình).
