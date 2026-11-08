# Hướng dẫn soạn câu hỏi (cho agent soạn)

Nguồn DUY NHẤT: Giáo trình Lịch sử Đảng Cộng sản Việt Nam (NXB CTQG Sự thật 2021). Các trang đã render thành ảnh:
`D:/Lam/Github/VNR202/giao-trinh/img/p-NNN.jpg` (NNN = 3 chữ số, số file == số trang in trên sách, ví dụ p-095.jpg = trang 95).
Đọc từng ảnh bằng Read tool. Chỉ dùng thông tin ĐỌC THẤY trên trang; không dùng kiến thức ngoài giáo trình.

## Định dạng — ghi ra file JSON (mảng), UTF-8, hợp lệ
Câu thường:
{"id":"<prefix>-001","text":"...?","options":["...","...","...","..."],"answer":0,"explanation":"...","page":95,"chapter":1}
- options: đúng 4, khác nhau, ngắn gọn (≤ 15 từ mỗi đáp án). answer: chỉ số 0–3 của đáp án đúng (phân bố đều vị trí).
- explanation: ≤ 2 câu, diễn đạt lại/trích giáo trình, nêu vì sao đúng.
- page: số trang có thông tin đó. chapter: 0 = Nhập môn, 1, 2, 3; Kết luận dùng 3.
Câu tỉnh phát sáng:
{"id":"s-<slug>-1","province":"Tên tỉnh đúng như danh sách","season":"Xuan|Ha|Thu|Dong","text":...,"options":[...],"answer":0,"explanation":"...","page":N}

## Chất lượng
- Đa dạng: mốc thời gian, nhân vật, hội nghị/đại hội, nội dung nghị quyết/văn kiện, chủ trương, ý nghĩa, địa danh, khái niệm.
- Đáp án sai cùng loại với đáp án đúng và hợp lý (năm gần nhau, hội nghị khác có thật trong sách...). KHÔNG dùng "Tất cả đều đúng/sai", "Cả A và B".
- Câu hỏi đứng độc lập, rõ ràng, một đáp án đúng duy nhất, không mơ hồ. Tiếng Việt chuẩn, có dấu.
- Thông điệp tích cực, đúng quan điểm giáo trình. Không câu hỏi gây tranh cãi chính trị ngoài giáo trình.
- KHÔNG ra câu hỏi về: ngày ký Hiệp định Pari (sách tự mâu thuẫn 21/1 và 27/1/1973); thời điểm Nghị quyết Chiến lược biển (9/2/2007 vs 6/2009); câu "đến năm 2000" ở tr.364; bất kỳ sự kiện nào sau Đại hội XIII (2021).
- Không trùng ý với nhau trong file của mình.

Cuối cùng: kiểm tra JSON hợp lệ bằng `node -e "const a=require('<path>');console.log(a.length)"`, rồi báo lại: đường dẫn file, số câu, và mọi điểm còn nghi ngờ.
