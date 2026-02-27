# FAQ — Câu hỏi thường gặp

> Các vấn đề thực tế khi làm việc với project eAndon C# WinForms.

---

## 🔧 Cài đặt và Build

### Q1: Build bị lỗi `missing reference` hoặc `could not find assembly SharedLib`

**Nguyên nhân**: Visual Studio chưa restore NuGet packages, hoặc project reference bị lỗi.

**Cách sửa**:
1. Chuột phải vào Solution (dòng trên cùng trong Solution Explorer) → **Restore NuGet Packages**
2. Ctrl+Shift+B để build lại
3. Nếu vẫn lỗi: **Build → Clean Solution** → rồi build lại

Nếu lỗi cụ thể là `System.Data.SQLite`:
```
Tools → NuGet Package Manager → Package Manager Console
PM> Install-Package System.Data.SQLite -ProjectName SharedLib
```

### Q2: Lỗi `The type or namespace 'SharedLib' could not be found`

**Nguyên nhân**: `AndonTerminal` hoặc `AndonDashboard` chưa có `<ProjectReference>` đến `SharedLib`.

**Cách sửa**:
1. Chuột phải lên project `AndonTerminal` → **Add → Project Reference**
2. Tích chọn `SharedLib` → OK
3. Build lại

### Q3: Lỗi `The target framework 'net8.0-windows' is not installed`

**Cách sửa**: Cài .NET 8 SDK từ https://dotnet.microsoft.com/download/dotnet/8.0. Sau đó restart Visual Studio.

### Q4: Build thành công nhưng chạy lỗi ngay `FileNotFoundException: settings.txt`

**Nguyên nhân**: Đường dẫn từ `bin\Debug\net8.0-windows\` lùi 4 cấp không đúng.

**Kiểm tra**: Xem `Program.cs`, dòng `Path.Combine(baseDir, "..", "..", "..", "..", "Assets")`. Từ `bin\Debug\net8.0-windows\`, lùi 4 cấp ra phải đến thư mục gốc project.

**Debug nhanh**: Thêm `MessageBox.Show(assetsDir)` vào `Program.cs` để xem đường dẫn thực tế.

---

## 🖥️ Chạy Terminal

### Q5: Chạy Terminal không thấy Line nào trong grid

**Kiểm tra theo thứ tự**:
1. Có truyền tên terminal đúng không? `AndonTerminal.exe terminal01`
2. Mở `Assets/Workstations_terminals.txt` — cột thứ 4 (Terminal name) có trùng với tên truyền vào không? (không phân biệt hoa/thường)
3. Số dòng đầu file có đúng không? (phải bằng số dòng data thực tế)
4. Build lại sau khi sửa file cấu hình? (Không cần — chỉ cần sửa file txt rồi chạy lại app)

**Ví dụ lỗi hay gặp**:
```
File có: terminal01
Chạy: AndonTerminal.exe Terminal01   ← OK (không phân biệt hoa/thường)
Chạy: AndonTerminal.exe term01       ← KHÔNG KHỚP → grid rỗng
```

### Q6: Terminal hiển thị grid nhưng âm thanh không kêu khi báo lỗi

**Kiểm tra**:
1. File `Assets/alarm.wav` có tồn tại không?
2. `settings.txt` có dòng `Alarm sound file : alarm.wav` không? Tên file có đúng không?
3. Volume máy tính có bị tắt không?

### Q7: Muốn thêm 1 loại alarm mới (alarm thứ 6) — sửa file nào?

Chỉ sửa `Assets/settings.txt`:
```
Number of alarm types to display : 6
Alarm label 6 : Ten Alarm Moi
Alarm image file 6 : Icon6.png
```

Đặt file `Icon6.png` vào `Assets/`. Khởi động lại app. **Không cần sửa code.**

### Q8: Muốn thêm 1 Line mới vào nhà máy — sửa file nào?

Sửa **2 file**:
1. `Assets/Workstations_terminals.txt` — thêm dòng mới, cập nhật số đầu file
2. `Assets/Lines_stations.txt` — thêm Line mới với danh sách Trạm

Xem ví dụ chi tiết tại [`Assets/README.md`](../Assets/README.md).

---

## 🗃️ Database SQLite

### Q9: SQLite database bị lock — lỗi `database is locked`

**Nguyên nhân phổ biến**:
1. Có **2 instance app** cùng ghi vào DB (2 Terminal chạy song song → cùng ghi vào `eandon.db`)
2. Dùng **DB Browser for SQLite** (hoặc tool khác) mở file DB trong khi app đang chạy
3. App bị crash → không đóng connection → lock còn lại

**Cách sửa**:
1. Đóng tất cả tool đang mở `eandon.db`
2. Nếu app crash để lại lock: **xóa file `Data/eandon.db`** → app tự tạo lại DB mới (mất dữ liệu cũ)
3. Lâu dài: chỉ nên mở file DB bằng tool khi app đã tắt hoàn toàn

### Q10: Xem dữ liệu trong SQLite bằng cách nào?

**Cách đơn giản nhất**: Tải [DB Browser for SQLite](https://sqlitebrowser.org/) (miễn phí, mở SQLite file trực tiếp). Mở `Data/eandon.db`, xem bảng `Tickets` và `DailyStats`.

**Lưu ý**: Đóng app trước khi mở bằng DB Browser để tránh lock.

### Q11: Muốn xóa toàn bộ dữ liệu test

Chỉ cần **xóa file `Data/eandon.db`**. Lần chạy tiếp theo app tự tạo DB mới rỗng. File `Data/` và `Logs/` đều trong `.gitignore` nên không ảnh hưởng git.

---

## 📊 Dashboard

### Q12: Dashboard không cập nhật khi Terminal báo lỗi

**Kiểm tra theo thứ tự**:
1. Thư mục `Data/` có tồn tại không? (tự tạo khi Terminal khởi động)
2. File `Data/terminal01.txt` có được tạo/cập nhật không? (mở file bằng Notepad xem)
3. Dashboard có đang chạy trên cùng máy hoặc cùng network share không?
4. Thử restart Dashboard — khi khởi động Dashboard đọc lại tất cả file trong `Data/`

**FileSystemWatcher có thể bị miss event nếu**: Quá nhiều file thay đổi cùng lúc, hoặc thư mục `Data/` trên network drive có latency cao.

### Q13: Dashboard hiển thị Line không đúng thứ tự

Dashboard sắp xếp Lines theo thứ tự đọc file trong `Data/*.txt`. Thứ tự hiển thị phụ thuộc vào tên file terminal và thứ tự trong `Workstations_terminals.txt`.

### Q14: Muốn xem chi tiết 1 phiếu sự cố đang mở

Double-click vào ô màu trên Dashboard → `TicketDetailForm` hiện ra. Hoặc click "📊 Thống kê" để xem lịch sử tất cả ticket.

---

## 📧 Email

### Q15: Email không gửi được — kiểm tra gì?

**Checklist**:
1. `settings.txt` có điền đầy đủ `Email SMTP Server`, `Email Sender Address`, `Email Sender Password` chưa?
2. SMTP server có đúng không? (hỏi IT, thường là `mail.congty.com` hoặc `smtp.gmail.com`)
3. Port có đúng không? (Gmail: 587 hoặc 465; nội bộ: 25 hoặc 587)
4. `Email Use SSL : true` hay `false`? (Gmail cần SSL, nhiều server nội bộ không cần)
5. Thử dùng tool khác (như Outlook) gửi từ cùng account — nếu không được thì lỗi account, không phải code

**Debug**: Mở `Output` trong Visual Studio (View → Output), xem có exception liên quan đến `SmtpClient` không.

### Q16: Email gửi được nhưng nội dung không đúng

**Nguyên nhân**: `WeeklyReportBuilder` đọc dữ liệu từ `DailyStats` — nếu DailyStats rỗng (chưa có ticket nào được đóng hoàn chỉnh) thì báo cáo sẽ trống.

**Kiểm tra**: `DailyStats` chỉ được cập nhật khi Leader **hoàn tất Bước 7** (xác nhận đóng ticket). Ticket đang mở không tính vào stats.

---

## 🎨 Giao diện

### Q17: Muốn đổi màu của các trạng thái (xanh/vàng/đỏ...)

Xem [`Docs/UI_CUSTOMIZE.md`](UI_CUSTOMIZE.md) — có hướng dẫn chi tiết thay đổi màu sắc từng trạng thái.

### Q18: Muốn thay đổi kích thước font hoặc ô trong grid

Tất cả UI được tạo động trong `InitializeUI()` — không có file `.Designer.cs` hay `.resx`. Sửa trực tiếp trong `TerminalMainForm.cs` phần `InitializeUI()`.

---

## 🔄 Nhiều Terminal

### Q19: Chạy 2 Terminal song song — Dashboard có hiển thị cả 2 không?

Có, **tự động**. Mỗi Terminal ghi `Data/terminal01.txt` và `Data/terminal02.txt` riêng. Dashboard đọc tất cả `Data/*.txt`. Không cần cấu hình thêm gì.

### Q20: Terminal bị crash giữa chừng — ticket đang mở có bị mất không?

**Không mất**. Ticket đã được ghi vào SQLite ngay khi tạo (Bước 4). Khi Terminal khởi động lại, nó đọc lại trạng thái từ DB và hiển thị đúng màu sắc.

Tuy nhiên, **bộ đếm giờ** (X phút) sẽ reset về giá trị tính toán lại từ `ReportedAt` trong DB.
