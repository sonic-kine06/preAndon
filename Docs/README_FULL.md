# README_FULL.md — Tài liệu đầy đủ hệ thống eAndon C#

## 1. Giới thiệu

**eAndon C# WinForms** là hệ thống quản lý sự cố sản xuất (Production Incident Management) được xây dựng theo mô hình Andon truyền thống của Toyota, chuyển đổi từ VB.NET sang C# với nhiều tính năng nâng cấp.

### Kiến trúc tổng quan

```
┌─────────────────────┐     File Data/       ┌──────────────────────┐
│   AndonTerminal     │ ──────────────────►  │  AndonDashboard      │
│                     │  terminal01.txt       │                      │
│  (Operator/KTV use) │                       │  (Manager/Leader use)│
└──────────┬──────────┘                       └──────────┬───────────┘
           │                                             │
           │         ┌──────────────────────┐           │
           └────────►│      SharedLib       │◄──────────┘
                     │                      │
                     │  IncidentService     │
                     │  DailyStatsService   │
                     │  SettingsReader      │
                     │  AlarmLogger         │
                     └──────────┬───────────┘
                                │
                    ┌───────────┴───────────┐
                    │                       │
               ┌────┴────┐           ┌──────┴─────┐
               │ SQLite  │           │ Text Files  │
               │ eandon  │           │ Logs/       │
               │  .db    │           │ alarmlog_*  │
               └─────────┘           └────────────┘
```

---

## 2. Cài đặt và chạy

### 2.1 Yêu cầu hệ thống
- Windows 10/11 (64-bit)
- .NET 6.0 Runtime (Windows Desktop Runtime)
- Visual Studio 2022 (để phát triển)

### 2.2 Build từ Source
```bash
# Clone repository
git clone https://github.com/sonic-kine06/preAndon.git
cd preAndon

# Build tất cả projects
dotnet build eAndonCSharp.sln

# Chạy Terminal
dotnet run --project AndonTerminal

# Chạy Dashboard
dotnet run --project AndonDashboard
```

### 2.3 Cấu hình lần đầu
1. Sửa `Assets/Workstations_terminals.txt` — thêm line/terminal của nhà máy
2. Sửa `Assets/Lines_stations.txt` — thêm trạm cho từng line
3. Sửa `Assets/settings.txt` — cấu hình số alarm, tên, âm thanh
4. Chạy Dashboard trước, sau đó chạy Terminal

---

## 3. Mô tả chi tiết từng Form

### 3.1 TerminalMainForm (AndonTerminal)

**Mục đích**: Màn hình chính tại xưởng sản xuất, đặt ở touchscreen hoặc PC tại line.

**Grid layout**:
```
            | Alarm 1    | Alarm 2    | Alarm 3    | ...
───────────────────────────────────────────────────────
Line 1(010) | [🟢  ✓   ] | [🟡 12m05s] | [🟢  ✓   ]
Line 2(020) | [🔴 05m30s] | [🟢  ✓   ] | [🟢  ✓   ]
Line 3(030) | [🟢  ✓   ] | [🟠 Sửa  ] | [🟢  ✓   ]
```

**Timer**: Cập nhật mỗi 5 giây, ghi file `Data/terminalXX.txt`.

**Khi đóng form**: Tự động log tất cả alarm đang mở vào file Logs/.

### 3.2 StationSelectForm

**Hiển thị khi**: Line có nhiều hơn 1 station trong Lines_stations.txt.

**UI**: Nền tối, mỗi station = 1 nút lớn (320×70px), màu xanh dương.

### 3.3 AlarmTypeForm

**Hiển thị khi**: Bước 3 — chọn mức độ sự cố.

**UI**: 2 panel lớn ngang nhau:
- **Panel trái** (vàng 🟡): Vẫn chạy được
- **Panel phải** (đỏ 🔴): Đã dừng hoàn toàn
- **Nút Hủy** ở dưới giữa

### 3.4 EmployeeInputForm

**Dùng cho**: Operator (bước 4), KTV (bước 5), Leader (bước 7).

**Validate**: Cả 2 trường (Mã NV + Họ tên) bắt buộc, không được trống.

### 3.5 FixCompleteForm

**Hiển thị khi**: KTV bấm ô Cam (bước 6).

**UI**: TextBox multiline (không bắt buộc điền), nút "Đã sửa xong".

### 3.6 DashboardMainForm (AndonDashboard)

**Mục đích**: Màn hình tổng quan cho quản lý/Leader, thường đặt ở màn hình lớn.

**Tự động cập nhật**: FileSystemWatcher theo dõi thư mục `Data/`, cập nhật ngay khi Terminal ghi file.

**Timer**: 1 giây cập nhật đồng hồ và thời gian đếm.

### 3.7 TicketDetailForm

**Hiển thị khi**: Double-click ô trên Dashboard.

**Thông tin**: Tất cả các bước từ 1-7, thời gian từng bước, tên nhân viên.

### 3.8 StatisticsForm

**Lọc**: Khoảng ngày (từ-đến) + mã line.

**Hiển thị**: DataGridView với các cột MTTR, MTBF, Availability%.

**Tô màu dòng**:
- Đỏ nhạt: Availability < 90%
- Vàng nhạt: Availability 90-95%
- Bình thường: Availability > 95%

---

## 4. Format file Data/terminalXX.txt

File giao tiếp giữa Terminal và Dashboard (format gốc mở rộng):

```
# eAndon Terminal Data — terminal01 — 2026-02-27 14:30:00
010;1;1;TKT-20260227-143015-123;2026-02-27 14:30:15
010;2;0;;
020;1;2;TKT-20260227-141200-456;2026-02-27 14:12:00
020;2;0;;
```

**Format mỗi dòng**:
```
<lineNumber>;<alarmTypeIndex>;<status>;<ticketId>;<startTime>
```

**Status values**:
- 0 = Green
- 1 = Yellow
- 2 = Red
- 3 = Repairing (Orange)
- 4 = WaitLeader (Blue)
- 5 = Closed

---

## 5. Format file Logs/alarmlog_*.txt

Format gốc eAndon, giữ nguyên tương thích:

```
Event DateTime | 2026-02-27 14:30:15; Workstation | 010 - Tram han diem; Alarm type | Ho tro Bao tri; New Color | Red; Length of alarm (seconds) | 0
Event DateTime | 2026-02-27 15:20:00; Workstation | 010 - Tram han diem; Alarm type | Ho tro Bao tri; New Color | Green; Length of alarm (seconds) | 3105
```

---

## 6. Cấu trúc code quan trọng

### Luồng tạo ticket (IncidentService.OpenIncident)
```csharp
1. Tạo TicketId = "TKT-{yyyyMMdd}-{HHmmss}-{random}"
2. INSERT vào bảng Tickets
3. Gọi AlarmLogger.LogAlarm() → ghi log txt
4. Trả về IncidentTicket object
```

### Luồng đóng ticket (IncidentService.LeaderConfirm)
```csharp
1. UPDATE Tickets SET Status=5, LeaderConfirmedAt=now, ...
2. Gọi UpdateDailyStatsForTicket() → UPSERT DailyStats
3. Gọi AlarmLogger.LogTicketClosed() → ghi log txt đóng
```

### DailyStats UPSERT (DailyStatsService.UpdateForLine)
```csharp
1. SELECT tất cả Tickets đã Closed của line+ngày
2. Tính TotalDowntime, ResponseTime, RepairTime
3. Tính MTTR, MTBF, Availability
4. INSERT OR REPLACE INTO DailyStats ...
```

---

## 7. FAQ

**Q: Terminal có thể chạy song song nhiều instance không?**  
A: Có, mỗi instance dùng terminal name khác nhau (tham số command line). Mỗi instance ghi file Data/terminalXX.txt riêng.

**Q: Nếu không cần Leader xác nhận?**  
A: Đặt `Require Leader Confirmation : false` trong settings.txt. Sau khi KTV hoàn thành (bước 6), ticket tự đóng ngay.

**Q: Có thể thêm nhiều hơn 10 loại alarm không?**  
A: Không, giới hạn là 10 theo thiết kế gốc. Tuy nhiên có thể mở rộng bằng cách sửa SettingsReader.

**Q: Database bị hỏng hoặc mất?**  
A: Xóa file `Data/eandon.db`, hệ thống sẽ tự tạo lại khi khởi động. Lịch sử sẽ mất nhưng cấu hình không ảnh hưởng.

**Q: Có thể xem Dashboard từ máy tính khác?**  
A: Dashboard và Terminal cần truy cập vào cùng thư mục Data/ (qua mạng nội bộ/shared folder).
