# eAndon C# WinForms System

Hệ thống **eAndon** được chuyển đổi từ VB.NET (repo gốc: [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon)) sang **C# WinForms**, giữ nguyên kiến trúc gốc và bổ sung nhiều tính năng mới.

---

## Tổng quan

eAndon là hệ thống cảnh báo sản xuất (Andon System) dùng để quản lý sự cố trên các line sản xuất công nghiệp. Operator báo lỗi → KTV nhận sửa → Leader xác nhận → tự động thống kê.

---

## Danh sách tính năng

| # | Tính năng | Gốc/Mới |
|---|-----------|---------|
| 1 | 1-10 loại alarm tự do cấu hình qua settings.txt | Gốc |
| 2 | Grid động: Hàng = Line, Cột = Alarm Type | Gốc |
| 3 | Giao tiếp Terminal ↔ Dashboard qua file text Data/ | Gốc |
| 4 | Log alarm text file Logs/alarmlog_*.txt | Gốc |
| 5 | Đọc Workstations_terminals.txt (format gốc) | Gốc |
| 6 | Đếm thời gian lỗi trên mỗi ô (X min) | Gốc |
| 7 | Xem lịch sử alarm khi click tên Line | Gốc |
| 8 | Âm thanh cảnh báo | Gốc |
| 9 | Priority lines highlight | Gốc |
| 10 | **1 Line → nhiều Station** (Lines_stations.txt) | **Mới** |
| 11 | **Popup chọn trạm** (StationSelectForm) | **Mới** |
| 12 | **Nhập Mã NV + Họ tên** (EmployeeInputForm) | **Mới** |
| 13 | **5 trạng thái** thay vì 3 (Green/Yellow/Red/Orange/Blue) | **Mới** |
| 14 | **Luồng sự cố 7 bước** hoàn chỉnh | **Mới** |
| 15 | **SQLite Database** (bảng Tickets + DailyStats) | **Mới** |
| 16 | **Thống kê MTTR/MTBF/Availability** | **Mới** |
| 17 | **StatisticsForm** với DataGridView lọc theo ngày/line | **Mới** |
| 18 | **TicketDetailForm** xem chi tiết phiếu sự cố | **Mới** |

---

## Sơ đồ luồng 7 bước

```
[1] Operator bấm ô XANH
        │
[2] Popup CHỌN TRẠM (StationSelectForm)
        │   (chỉ hiện nếu Line có > 1 station)
        │
[3] Popup CHỌN MỨC ĐỘ (AlarmTypeForm)
        │   ● 🟡 Yellow — vẫn chạy, chờ KTV
        │   ● 🔴 Red — đã dừng, chờ KTV
        │
[4] Popup NHẬP MÃ NV + TÊN Operator (EmployeeInputForm)
        │
        ▼
    Ô đổi 🟡/🔴, bắt đầu đếm giờ, ghi DB + log txt
        │
[5] KTV bấm ô 🟡/🔴
        │
        Popup NHẬP MÃ NV + TÊN KTV (EmployeeInputForm)
        │
        ▼
    Ô đổi 🟠 (Repairing), ghi nhận response time
        │
[6] KTV bấm ô 🟠
        │
        Popup GHI CHÚ SỬA CHỮA (FixCompleteForm)
        │
        ▼
    Ô đổi 🔵 (WaitLeader — chờ Leader xác nhận)
        │
[7] Leader bấm ô 🔵
        │
        Popup NHẬP MÃ NV + TÊN Leader (EmployeeInputForm)
        │
        ▼
    Ô về 🟢 (Green), ticket đóng, cập nhật DailyStats
```

---

## Bảng 5 trạng thái

| Màu | Trạng thái | Mô tả | Hành động tiếp theo |
|-----|-----------|-------|---------------------|
| 🟢 Green | Bình thường | Máy chạy OK | Operator bấm báo lỗi |
| 🟡 Yellow | Reported | Có vấn đề, vẫn chạy, chờ KTV | KTV bấm nhận sửa |
| 🔴 Red | Reported | Đã dừng, chờ KTV | KTV bấm nhận sửa |
| 🟠 Orange | Repairing | KTV đang sửa | KTV bấm hoàn thành |
| 🔵 Blue | WaitLeader | KTV xong, chờ Leader xác nhận | Leader bấm xác nhận |

---

## Cấu trúc thư mục

> 📖 Mỗi thư mục đều có file `README.md` riêng giải thích chi tiết.

```
preAndon/
├── README.md                     ← (file này) Tổng quan
├── eAndonCSharp.sln              ← Solution file Visual Studio
│
├── SharedLib/                    ← Class Library (dùng chung bởi cả 2 app)
│   ├── README.md                 ← Giải thích Models và Services
│   ├── Models/
│   │   ├── StationInfo.cs        ← Model thông tin trạm
│   │   └── IncidentTicket.cs     ← Model phiếu sự cố + enum trạng thái
│   ├── Services/
│   │   ├── SettingsReader.cs     ← Đọc settings.txt
│   │   ├── LineStationReader.cs  ← Đọc Workstations + Lines_stations
│   │   ├── AlarmLogger.cs        ← Ghi log text file
│   │   ├── IncidentService.cs    ← CRUD ticket + SQLite
│   │   └── DailyStatsService.cs  ← Thống kê MTTR/MTBF/Availability
│   └── SharedLib.csproj
│
├── AndonTerminal/                ← WinForms App (Operator/KTV/Leader dùng)
│   ├── README.md                 ← Giải thích Terminal + hướng dẫn nhiều Terminal
│   ├── Program.cs                ← Entry point: đọc args, khởi tạo services
│   ├── Forms/
│   │   ├── TerminalMainForm.cs   ← Grid chính + logic 7 bước
│   │   ├── StationSelectForm.cs  ← Popup chọn trạm
│   │   ├── AlarmTypeForm.cs      ← Popup chọn Yellow/Red
│   │   ├── EmployeeInputForm.cs  ← Popup nhập Mã NV + Tên
│   │   └── FixCompleteForm.cs    ← Popup ghi chú sửa chữa
│   └── AndonTerminal.csproj
│
├── AndonDashboard/               ← WinForms App (Quản lý/Leader xem tổng quan)
│   ├── README.md                 ← Giải thích Dashboard + cơ chế FileSystemWatcher
│   ├── Program.cs                ← Entry point
│   ├── Forms/
│   │   ├── DashboardMainForm.cs  ← Grid tổng quan + FileSystemWatcher
│   │   ├── TicketDetailForm.cs   ← Chi tiết 1 ticket
│   │   └── StatisticsForm.cs     ← Thống kê DailyStats
│   └── AndonDashboard.csproj
│
├── Assets/                       ← Tất cả file cần thiết (có sẵn trong repo)
│   ├── README.md                 ← Giải thích từng file + nguồn icon/ảnh
│   ├── settings.txt              ← Cấu hình hệ thống
│   ├── Workstations_terminals.txt ← Phân công Lines → Terminal
│   ├── Lines_stations.txt        ← Danh sách Trạm trong mỗi Line
│   ├── app.ico                   ← Icon cửa sổ (nguồn: vitplanocka/eAndon MIT)
│   ├── Icon1-5.png               ← Icon alarm types (nguồn: vitplanocka/eAndon MIT)
│   ├── logo.png                  ← Logo công ty (nguồn: vitplanocka/eAndon MIT)
│   ├── alarm.wav                 ← Âm thanh cảnh báo (nguồn: vitplanocka/eAndon MIT)
│   └── NOTICE.txt                ← Attribution MIT License
│
├── Data/                         ← Tự tạo khi chạy (trong .gitignore)
│   ├── eandon.db                 ← SQLite database
│   ├── terminal01.txt            ← File giao tiếp Terminal01 → Dashboard
│   ├── terminal02.txt            ← File giao tiếp Terminal02 → Dashboard
│   └── terminal0N.txt            ← ... (1 file per terminal)
│
├── Logs/                         ← Tự tạo khi chạy (trong .gitignore)
│   └── alarmlog_yyyy-MM-dd.txt   ← Log alarm theo ngày
│
└── Docs/                         ← Tài liệu chi tiết
    ├── README.md                 ← Mục lục tài liệu
    ├── README_FULL.md            ← Tài liệu đầy đủ
    ├── BEGINNER_GUIDE.md         ← Hướng dẫn người mới
    ├── DATABASE.md               ← Schema database chi tiết
    ├── UI_CUSTOMIZE.md           ← Tùy chỉnh giao diện
    └── ANALYTICS.md              ← Tính năng thống kê/AI
```

---

## Chạy nhiều Terminal cùng lúc

**Ví dụ**: 3 Terminal cho 6 Lines, 1 Dashboard giám sát tất cả.

```bash
# Chạy 3 Terminal (mỗi cái 1 cửa sổ riêng)
dotnet run --project AndonTerminal -- terminal01   # Line 1, 2
dotnet run --project AndonTerminal -- terminal02   # Line 3, 4
dotnet run --project AndonTerminal -- terminal03   # Line 5, 6

# Chạy Dashboard (1 cửa sổ, giám sát tất cả)
dotnet run --project AndonDashboard
```

**Dashboard KHÔNG cần cấu hình thêm** — tự động đọc `Data/terminal01.txt`, `Data/terminal02.txt`, `Data/terminal03.txt` ngay khi chúng xuất hiện.

Xem thêm: [`AndonTerminal/README.md`](AndonTerminal/README.md)

---

## Sơ đồ 2 bảng Database

```
┌─────────────────────────────────────────────────────┐
│                      Tickets                         │
├─────────────────────┬───────────────────────────────┤
│ Id (PK)             │ INTEGER AUTOINCREMENT          │
│ TicketId (UNIQUE)   │ TEXT "TKT-20260227-001"        │
│ LineNumber          │ TEXT "010"                     │
│ LineName            │ TEXT "Line 1"                  │
│ StationId           │ TEXT "ST-010-01"               │
│ StationName         │ TEXT "Tram cat laser"          │
│ AlarmTypeIndex      │ INTEGER (1-10)                 │
│ AlarmTypeName       │ TEXT "Ho tro Bao tri"          │
│ Severity            │ TEXT "Yellow"/"Red"            │
│ ReportedAt          │ TEXT datetime                  │
│ OperatorId          │ TEXT                           │
│ OperatorName        │ TEXT                           │
│ TechCheckinAt       │ TEXT datetime (nullable)       │
│ TechnicianId        │ TEXT (nullable)                │
│ TechnicianName      │ TEXT (nullable)                │
│ TechFixedAt         │ TEXT datetime (nullable)       │
│ FixNote             │ TEXT (nullable)                │
│ LeaderConfirmedAt   │ TEXT datetime (nullable)       │
│ LeaderId            │ TEXT (nullable)                │
│ LeaderName          │ TEXT (nullable)                │
│ Status              │ INTEGER 0-5                    │
│ ReportDate          │ TEXT "yyyy-MM-dd"              │
└─────────────────────┴───────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                    DailyStats                        │
├─────────────────────┬───────────────────────────────┤
│ Id (PK)             │ INTEGER AUTOINCREMENT          │
│ StatsDate           │ TEXT "yyyy-MM-dd"              │
│ LineNumber          │ TEXT                           │
│ LineName            │ TEXT                           │
│ TotalIncidents      │ INTEGER                        │
│ TotalDowntimeSec    │ INTEGER                        │
│ AvgResponseTimeSec  │ INTEGER                        │
│ AvgRepairTimeSec    │ INTEGER                        │
│ MTTR_Minutes        │ REAL                           │
│ MTBF_Minutes        │ REAL                           │
│ Availability_Pct    │ REAL                           │
│ YellowCount         │ INTEGER                        │
│ RedCount            │ INTEGER                        │
│ UpdatedAt           │ TEXT datetime                  │
│ UNIQUE(StatsDate, LineNumber)                        │
└─────────────────────┴───────────────────────────────┘
```

---

## Hướng dẫn cài đặt Visual Studio

> 💡 Xem thêm tài liệu đầy đủ tại [`Docs/BEGINNER_GUIDE.md`](Docs/BEGINNER_GUIDE.md)

### Yêu cầu
- Visual Studio 2022 (Community hoặc cao hơn)
- **.NET 8.0 SDK** (dự án dùng net8.0-windows)
- Workload: ".NET desktop development"

### Các bước

1. **Mở Solution**: File → Open → Project/Solution → chọn `eAndonCSharp.sln`

2. **Restore NuGet packages**:
   ```
   Tools → NuGet Package Manager → Package Manager Console
   > Update-Package -reinstall
   ```
   Hoặc chuột phải vào Solution → Restore NuGet Packages

3. **Package cần cài**: `System.Data.SQLite` v1.0.118
   ```
   > Install-Package System.Data.SQLite -ProjectName SharedLib
   ```

4. **Build**: Ctrl+Shift+B hoặc Build → Build Solution

5. **Chạy Terminal**: Chuột phải `AndonTerminal` → Set as Startup Project → F5

6. **Chạy Dashboard**: Chuột phải `AndonDashboard` → Set as Startup Project → F5

### Chạy cùng lúc Terminal + Dashboard
- Debug → Start Without Debugging (Ctrl+F5) cho từng project

---

## Hướng dẫn cấu hình

### settings.txt
File nằm tại `Assets/settings.txt`. Sửa trực tiếp rồi khởi động lại ứng dụng.

Ví dụ thêm loại alarm thứ 6:
```
Number of alarm types to display : 6
Alarm label 6 : Su co An toan
Alarm image file 6 : Icon6.png
```

### Workstations_terminals.txt
```
<số lượng line>
#;Workstation nr.;Workstation name;Terminal name;
<index>;<số>;<tên>;<terminal>
```

### Lines_stations.txt
```
LINE <số>; <tên line>
  <mã trạm>; <tên trạm>
  <mã trạm>; <tên trạm>
```
Nếu không có file này, mỗi Line = 1 Station (tương thích gốc).

---

## Giải thích các file code

| File | Mô tả |
|------|-------|
| `SharedLib/Models/StationInfo.cs` | Model trạm làm việc |
| `SharedLib/Models/IncidentTicket.cs` | Model phiếu sự cố + enum trạng thái |
| `SharedLib/Services/SettingsReader.cs` | Parse settings.txt |
| `SharedLib/Services/LineStationReader.cs` | Đọc cấu hình workstation/station |
| `SharedLib/Services/AlarmLogger.cs` | Ghi log text file (format gốc eAndon) |
| `SharedLib/Services/IncidentService.cs` | CRUD ticket SQLite + 7 bước luồng |
| `SharedLib/Services/DailyStatsService.cs` | Tính MTTR/MTBF/Availability |
| `AndonTerminal/Forms/TerminalMainForm.cs` | Grid chính Terminal + logic 7 bước |
| `AndonTerminal/Forms/StationSelectForm.cs` | Popup chọn trạm |
| `AndonTerminal/Forms/AlarmTypeForm.cs` | Popup chọn Yellow/Red |
| `AndonTerminal/Forms/EmployeeInputForm.cs` | Popup nhập Mã NV + Họ tên |
| `AndonTerminal/Forms/FixCompleteForm.cs` | Popup ghi chú sửa chữa |
| `AndonDashboard/Forms/DashboardMainForm.cs` | Grid tổng quan Dashboard |
| `AndonDashboard/Forms/TicketDetailForm.cs` | Chi tiết 1 ticket |
| `AndonDashboard/Forms/StatisticsForm.cs` | Thống kê DailyStats |
