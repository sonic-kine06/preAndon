# SharedLib — Thư viện dùng chung

Class Library được **cả AndonTerminal và AndonDashboard** tham chiếu (`<ProjectReference>`).  
Chứa toàn bộ Models, Services, và business logic để tránh lặp code.

---

## Phần A: SharedLib là gì? Tại sao tách ra?

### Class Library là gì?

Hãy tưởng tượng bạn có 2 ứng dụng: Terminal (cho Operator/KTV) và Dashboard (cho Leader/Quản lý). Cả hai đều cần:
- Đọc cùng file cấu hình `settings.txt`
- Làm việc với cùng kiểu dữ liệu `IncidentTicket`
- Truy vấn cùng database SQLite

Nếu viết riêng cho từng app → code lặp, sửa 1 chỗ phải sửa 2 chỗ. → **Giải pháp**: tách thành thư viện chung `SharedLib`.

**SharedLib** là project kiểu **Class Library** — không có giao diện, không chạy độc lập, chỉ cung cấp "công cụ" cho các project khác dùng.

### So sánh với VB.NET Module

Nếu bạn quen VB.NET, bạn hay dùng `Module` để chứa hàm dùng chung. Trong C#, tương đương là `static class`, nhưng cách tổ chức tốt hơn là dùng **Class Library project**.

| Khái niệm | VB.NET | C# |
|-----------|--------|----|
| Code dùng chung trong cùng project | `Module MyModule` | `static class MyHelper` |
| Code dùng chung ở project riêng | Thêm vào project VB.NET khác | **Class Library project** |
| Tham chiếu giữa projects | `Add Reference` | `<ProjectReference>` trong `.csproj` |

### `.csproj` là gì? `<ProjectReference>` là gì?

File `.csproj` là file cấu hình project — tương tự `.vbproj` trong VB.NET. Mở `AndonTerminal.csproj` bạn sẽ thấy:

```xml
<ProjectReference Include="..\SharedLib\SharedLib.csproj" />
```

Dòng này nói: "AndonTerminal cần dùng code từ SharedLib". Khi build, Visual Studio tự động build SharedLib trước, rồi build AndonTerminal sau. **Không cần copy file DLL thủ công.**

---

## Cấu trúc thư mục

```
SharedLib/
├── SharedLib.csproj         ← Project file (.NET 8 Class Library)
│
├── Models/
│   ├── StationInfo.cs       ← Model: thông tin 1 trạm làm việc
│   └── IncidentTicket.cs    ← Model: phiếu sự cố + enum TicketStatus
│
├── Services/
│   ├── SettingsReader.cs    ← Đọc Assets/settings.txt
│   ├── LineStationReader.cs ← Đọc Workstations + Lines_stations
│   ├── AlarmLogger.cs       ← Ghi log text file Logs/alarmlog_*.txt
│   ├── IncidentService.cs   ← CRUD ticket SQLite + logic 7 bước
│   ├── DailyStatsService.cs ← Tính và lưu MTTR/MTBF/Availability
│   ├── Analytics/
│   │   ├── AnalyticsManager.cs
│   │   ├── DowntimeEstimator.cs
│   │   ├── AnomalyDetector.cs
│   │   ├── TechnicianTracker.cs
│   │   └── TimePatternDetector.cs
│   └── Email/
│       ├── EmailConfig.cs
│       ├── EmailSender.cs
│       ├── WeeklyReportBuilder.cs
│       ├── RealtimeAlertService.cs
│       └── EmailScheduler.cs
│
└── README.md                ← (file này)
```

---

## Phần B: Models — Giải thích từng file

### `StationInfo.cs` — Model thông tin trạm

**Mục đích**: Lưu trữ thông tin của MỘT trạm làm việc.  
**Ai dùng**: `LineStationReader` tạo ra danh sách `StationInfo`, `TerminalMainForm` dùng để hiển thị danh sách trạm.

**Code thực tế**:
```csharp
public class StationInfo
{
    public string StationId { get; set; }    // "ST-010-01"
    public string StationName { get; set; }  // "Trạm cắt laser"
    public string LineNumber { get; set; }   // "010"
    public string LineName { get; set; }     // "Line 1"

    public StationInfo(string stationId, string stationName, string lineNumber, string lineName)
    {
        StationId = stationId;
        // ...
    }

    public override string ToString()
    {
        return $"{StationId} - {StationName}";  // "ST-010-01 - Trạm cắt laser"
    }
}
```

**So sánh C# vs VB.NET**:

| C# | VB.NET |
|----|--------|
| `public string StationId { get; set; }` | `Public Property StationId As String` |
| `public StationInfo(string id, ...)` | `Public Sub New(id As String, ...)` |
| `public override string ToString()` | `Public Overrides Function ToString() As String` |
| `$"{StationId} - {StationName}"` | `$"{StationId} - {StationName}"` (giống nhau!) |

**Trường hợp thực tế**: Khi `Lines_stations.txt` có dòng:
```
  ST-010-01; Tram cat laser
```
`LineStationReader` sẽ tạo:
```csharp
new StationInfo("ST-010-01", "Tram cat laser", "010", "Line 1")
```

**Bài tập**: Mở `StationInfo.cs`, thêm property `IsActive` kiểu `bool`. Build lại — nếu không có lỗi, bạn đã hiểu đúng cú pháp C# property!

---

### `IncidentTicket.cs` — Model phiếu sự cố

**Mục đích**: Lưu toàn bộ thông tin 1 phiếu sự cố từ lúc Operator báo đến khi Leader đóng phiếu. Ánh xạ trực tiếp vào bảng `Tickets` trong SQLite.  
**Ai dùng**: `IncidentService` tạo/cập nhật, `TerminalMainForm` và `DashboardMainForm` hiển thị.

**Enum TicketStatus**:
```csharp
public enum TicketStatus
{
    Green = 0,       // Bình thường
    Yellow = 1,      // Đã báo lỗi, vẫn chạy
    Red = 2,         // Đã dừng
    Repairing = 3,   // KTV đang sửa
    WaitLeader = 4,  // Chờ Leader xác nhận
    Closed = 5       // Đã đóng
}
```

**So sánh C# vs VB.NET — Enum**:

| C# | VB.NET |
|----|--------|
| `public enum TicketStatus { Green = 0, Yellow = 1 }` | `Public Enum TicketStatus : Green = 0 : Yellow = 1 : End Enum` |
| `TicketStatus.Yellow` | `TicketStatus.Yellow` (giống nhau) |
| `(TicketStatus)ticket.Status` | `CType(ticket.Status, TicketStatus)` |

**Property tính toán** (không lưu DB):
```csharp
public double? DowntimeSeconds
{
    get
    {
        if (ReportedAt.HasValue && LeaderConfirmedAt.HasValue)
            return (LeaderConfirmedAt.Value - ReportedAt.Value).TotalSeconds;
        return null;
    }
}
```

**Giải thích**: `DateTime?` là `Nullable<DateTime>` — có thể null (chưa điền). `.HasValue` kiểm tra có giá trị không. `.Value` lấy giá trị. Tương tự VB.NET: `DateTime?` = `Nullable(Of DateTime)`, `.HasValue` = `.HasValue`, `.Value` = `.Value`.

**Trường hợp thực tế**: Khi Operator báo lỗi Line 1 Trạm cắt laser lúc 14:22, hệ thống tạo 1 `IncidentTicket` với:
- `LineNumber = "010"`, `StationId = "ST-010-01"`
- `ReportedAt = DateTime.Now`
- `Status = 1` (Yellow) hoặc `2` (Red)
- `TechCheckinAt = null` (chưa có KTV nhận)

**Bài tập**: Thêm property `Priority` kiểu `int` vào `IncidentTicket`. Build lại. Sau đó mở `IncidentService.cs` xem có cần sửa thêm gì không.

---

## Phần C: Services — Giải thích từng file (từ dễ đến khó)

### `SettingsReader.cs` — Đọc cấu hình settings.txt

**Sơ đồ**: `settings.txt` → `SettingsReader` → Dictionary → các property tiện dụng → `TerminalMainForm`, `EmailScheduler`

**Pattern chính — Dictionary**:
```csharp
private readonly Dictionary<string, string> _settings
    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
```

- `Dictionary<string, string>`: cấu trúc lưu key-value, giống `Hashtable` trong VB.NET
- `StringComparer.OrdinalIgnoreCase`: key không phân biệt hoa/thường — `"Number of alarm types"` = `"number of alarm types"`

**Cách đọc file**:
```csharp
foreach (var line in File.ReadAllLines(FilePath))
{
    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
        continue;   // Bỏ qua dòng trống và comment

    int separatorIndex = line.IndexOf(" : ");
    if (separatorIndex < 0) continue;

    string key = line.Substring(0, separatorIndex).Trim();
    string value = line.Substring(separatorIndex + 3).Trim();
    _settings[key] = value;
}
```

**Ví dụ thực tế**: Khi `settings.txt` có dòng:
```
Number of alarm types to display : 5
Alarm label 1 : Ho tro Teamleader
```
Dictionary sẽ có:
```
{ "Number of alarm types to display" → "5" }
{ "Alarm label 1" → "Ho tro Teamleader" }
```

**Property tiện dụng**:
```csharp
public int NumberOfAlarmTypes => GetInt("Number of alarm types to display", 3);
// Đọc key trên, parse thành int, mặc định 3 nếu không tìm thấy
```

**So sánh C# vs VB.NET**:

| C# | VB.NET |
|----|--------|
| `public int NumberOfAlarmTypes => GetInt(...)` | `Public ReadOnly Property NumberOfAlarmTypes As Integer` + `Get` + `Return GetInt(...)` + `End Get` + `End Property` |
| `string.IsNullOrWhiteSpace(line)` | `String.IsNullOrWhiteSpace(line)` (giống nhau) |
| `line.TrimStart().StartsWith("#")` | `line.TrimStart().StartsWith("#")` (giống nhau) |

**Trường hợp lỗi**:
- `settings.txt` không tồn tại → `Load()` gọi `File.Exists()` → return sớm, Dictionary rỗng → tất cả property trả về giá trị mặc định
- Thiếu key → `Get(key, defaultValue)` trả về `defaultValue`
- Giá trị không parse được thành int → `GetInt()` trả về `defaultValue`

---

### `LineStationReader.cs` — Đọc cấu hình workstation/station

**Sơ đồ**: `Workstations_terminals.txt` + `Lines_stations.txt` → `LineStationReader` → `List<StationInfo>`

**Hai phương thức chính**:
- `GetWorkstations()` → TẤT CẢ workstations (tất cả lines, tất cả terminals)
- `GetStationsForLine(lineNumber)` → danh sách Stations của 1 line cụ thể

> ⚠️ **Quan trọng**: `TerminalMainForm` phải TỰ LỌC theo `terminalName`:
> ```csharp
> var workstations = reader.GetWorkstations()
>     .Where(w => w.Terminal == terminalName).ToList();
> ```
> `Where(...)` là LINQ — tương tự `filter()` trong Python hoặc vòng lặp có điều kiện trong VB.NET.

**Format `Workstations_terminals.txt`**:
```
6
#;Workstation nr.;Workstation name;Terminal name;
0;010;Line 1;terminal01
1;020;Line 2;terminal01
```
Dòng đầu `6` = số lượng line. Dòng `#;...` là header (bỏ qua).

**Format `Lines_stations.txt`**:
```
LINE 010; Line 1
  ST-010-01; Tram cat laser
  ST-010-02; Tram han diem
LINE 020; Line 2
  ST-020-01; Tram may
```
Dòng bắt đầu `LINE` = bắt đầu 1 line mới. Dòng thụt đầu = station của line đó.

---

### `AlarmLogger.cs` — Ghi log text file

**Sơ đồ**: `AlarmLogger(logsDir)` → ghi `Logs/alarmlog_yyyy-MM-dd.txt`

**Mục đích**: Lưu lịch sử alarm ra file text — dễ đọc bằng Notepad, tương thích với hệ thống VB.NET gốc.

**Pattern chính — StreamWriter**:
```csharp
using (var writer = new StreamWriter(logFilePath, append: true))
{
    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {lineNumber} | {alarmLabel}");
}
```

- `using (...)` → tự động đóng file khi xong, dù có lỗi hay không — tương tự `Try...Finally...Close()` trong VB.NET
- `append: true` → ghi thêm vào cuối file, không xóa nội dung cũ

**Ví dụ log file**:
```
2026-02-27 14:22:33 | Line 1 (010) | ST-010-02 - Tram han diem | Ho tro Bao tri | RED | Reported | NV001 Nguyen Van A
2026-02-27 14:35:10 | Line 1 (010) | ST-010-02 - Tram han diem | Ho tro Bao tri | RED | TechCheckIn | KTV005 Tran Thi B
```

**So sánh C# vs VB.NET**:

| C# | VB.NET |
|----|--------|
| `using (var writer = new StreamWriter(...))` | `Dim writer As New StreamWriter(...)` + `writer.Close()` |
| `writer.WriteLine(...)` | `writer.WriteLine(...)` (giống nhau) |
| `$"{DateTime.Now:yyyy-MM-dd}"` | `DateTime.Now.ToString("yyyy-MM-dd")` |

---

### `IncidentService.cs` — CRUD ticket SQLite (service khó nhất!)

**Sơ đồ**: `IncidentService(dbPath)` → khởi tạo SQLite DB → cung cấp các method CRUD

**Khởi tạo database**:
```csharp
public IncidentService(string dbPath, AlarmLogger logger)
{
    _dbPath = dbPath;
    _logger = logger;
    InitializeDatabase();  // Tự tạo bảng nếu chưa có
}

private void InitializeDatabase()
{
    using var conn = new SQLiteConnection($"Data Source={_dbPath}");
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Tickets (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TicketId TEXT UNIQUE,
            ...
        )";
    cmd.ExecuteNonQuery();
}
```

**`using var conn`**: Mở kết nối SQLite, tự động đóng khi hết scope `{}`. Rất quan trọng — nếu không đóng, database sẽ bị **lock**.

**OpenTicket — Tạo ticket mới (Bước 1-4)**:
```csharp
public IncidentTicket OpenTicket(string lineNumber, string lineName,
    string stationId, string stationName,
    int alarmTypeIndex, string alarmTypeName,
    string severity, string operatorId, string operatorName)
{
    var ticket = new IncidentTicket
    {
        TicketId = GenerateTicketId(),
        LineNumber = lineNumber,
        // ... gán các field
        Status = (int)(severity == "Red" ? TicketStatus.Red : TicketStatus.Yellow),
        ReportedAt = DateTime.Now
    };

    // INSERT vào SQLite
    using var conn = new SQLiteConnection($"Data Source={_dbPath}");
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO Tickets (TicketId, LineNumber, ...) VALUES (@id, @line, ...)";
    cmd.Parameters.AddWithValue("@id", ticket.TicketId);
    cmd.Parameters.AddWithValue("@line", ticket.LineNumber);
    cmd.ExecuteNonQuery();

    _logger.LogAlarm(ticket, "Reported");
    return ticket;
}
```

**Tại sao dùng `@id` thay vì ghép string trực tiếp?** → **Parameterized queries** ngăn chặn SQL Injection. Luôn dùng cách này.

**Các phương thức theo từng bước**:

| Phương thức | Bước | Hành động |
|------------|------|-----------|
| `OpenTicket(...)` | 1–4 | INSERT bản ghi mới, Status=Yellow/Red |
| `TechCheckIn(ticketId, techId, techName)` | 5 | UPDATE TechCheckinAt, TechnicianId, Status=Repairing |
| `CompleteRepair(ticketId, fixNote)` | 6 | UPDATE TechFixedAt, FixNote, Status=WaitLeader |
| `LeaderConfirm(ticketId, leaderId, leaderName)` | 7 | UPDATE LeaderConfirmedAt, Status=Closed |
| `GetAllOpen()` | Dashboard | SELECT WHERE Status NOT IN (0,5) |
| `GetHistory(lineNumber, date)` | Dashboard | SELECT WHERE LineNumber=? AND ReportDate=? |

**Trường hợp lỗi thực tế**:
- Database bị lock → `SQLiteException: database is locked` → nguyên nhân thường là 2 tiến trình cùng ghi → giải pháp: đảm bảo chỉ 1 app ghi cùng lúc, hoặc dùng `WAL mode`
- File DB chưa tồn tại → `InitializeDatabase()` tự tạo file mới khi `conn.Open()`

---

### `DailyStatsService.cs` — Thống kê MTTR/MTBF/Availability

**Sơ đồ**: `DailyStatsService(dbPath)` → đọc `Tickets` đã Closed → tính toán → UPSERT vào `DailyStats`

**Được gọi khi nào?** Sau mỗi `LeaderConfirm()` → tự động cập nhật thống kê ngày hôm đó cho Line đó.

**Công thức**:
```
MTTR = TổngDowntimePhút / SốSựCố
MTBF = (1440 - TổngDowntimePhút) / SốSựCố   [1440 = 24h × 60 phút]
Availability = MTBF / (MTBF + MTTR) × 100%
```

**Ví dụ**: Line 010 ngày hôm nay có 3 sự cố, tổng downtime 135 phút:
```
MTTR = 135 / 3 = 45 phút
MTBF = (1440 - 135) / 3 = 435 phút
Availability = 435 / (435 + 45) × 100 ≈ 90.6%
```

---

## Phần D: Analytics — Giải thích các class AI/ML

> 📖 Xem chi tiết đầy đủ tại [`Docs/ANALYTICS.md`](../../Docs/ANALYTICS.md)

### Tổng quan 4 class

| Class | Bài toán | Thuật toán |
|-------|---------|-----------|
| `DowntimeEstimator` | Downtime tuần tới sẽ bao nhiêu? | EWMA (trung bình trọng số hàm mũ) |
| `AnomalyDetector` | Tuần này có bất thường không? | Z-score (so sánh với baseline 4 tuần) |
| `TechnicianTracker` | KTV nào giỏi nhất loại alarm X? | AVG thời gian sửa theo nhóm |
| `TimePatternDetector` | Lúc nào hay xảy ra sự cố? | Tần suất theo (station, alarm, thứ, giờ) |

### `DowntimeEstimator` — Dự đoán đơn giản nhất để hiểu

**Giải thích bằng tiếng Việt**: "Lấy trung bình các ngày trước, nhưng ưu tiên ngày gần hơn". Nếu tuần trước downtime 30 phút, tuần kia 45 phút, tuần này 20 phút → dự đoán ≈ 26 phút (không phải (30+45+20)/3=31.7, mà ngày gần nhất được tính nhiều hơn).

**Hằng số có thể điều chỉnh**:
```csharp
private const double ALPHA = 0.3;         // Trọng số ngày gần nhất (0.1–0.5)
private const int MIN_SAMPLES = 3;        // Cần ít nhất 3 ngày dữ liệu
private const int FULL_CONFIDENCE = 20;   // 20 ngày = độ tin cậy 100%
```

Muốn dự đoán bám sát dữ liệu mới hơn → tăng `ALPHA`. Muốn dự đoán ổn định hơn → giảm `ALPHA`.

### `AnalyticsManager` — Dùng tất cả cùng lúc

```csharp
var analytics = new AnalyticsManager("Data/eandon.db");
var summary = analytics.GetDashboardSummary();
// summary.HasAnomalies → hiện banner cảnh báo?
// summary.DowntimePredictions → bảng dự đoán
// summary.TechnicianRanking → bảng xếp hạng KTV
```

---

## Phần E: Email Services

> 📖 Xem chi tiết đầy đủ tại [`SharedLib/Services/Email/README.md`](Services/Email/README.md)

### Tổng quan

Hệ thống gửi 2 loại email:
1. **Báo cáo tuần** (mỗi Chủ Nhật tối): gửi cho sếp/giám đốc — tóm tắt MTTR/MTBF/Availability
2. **Cảnh báo real-time**: gửi cho quản lý KTV — khi sự cố chờ quá lâu

### SMTP là gì?

**SMTP** (Simple Mail Transfer Protocol) = giao thức gửi email. Khi bạn bấm "Gửi" trong Outlook → Outlook dùng SMTP để chuyển email đến server. Code eAndon cũng làm vậy — thay vì gõ tay, code tự động bấm "Gửi".

### Cấu hình trong `settings.txt`

```
Email SMTP Server : mail.congty.com
Email SMTP Port : 587
Email Use SSL : true
Email Sender Address : andon@congty.com
Email Sender Password : matkhau123
Email Boss Recipients : giamdoc@congty.com|truongphong@congty.com
Email Manager Recipients : quanly.ktv@congty.com
Email Weekly Report Day : Sunday
Email Weekly Report Hour : 20
Email Alert No Tech Minutes : 10
Email Alert Long Repair Minutes : 30
```

### Các class

| Class | Vai trò |
|-------|---------|
| `EmailConfig` | Model: lưu cấu hình SMTP và danh sách người nhận |
| `EmailSender` | Gửi email thực tế qua `System.Net.Mail.SmtpClient` |
| `WeeklyReportBuilder` | Tạo nội dung HTML cho báo cáo tuần |
| `RealtimeAlertService` | Kiểm tra ticket nào chờ quá lâu → gửi cảnh báo |
| `EmailScheduler` | Timer kiểm tra định kỳ: đến giờ chưa? Có cần gửi không? |

---

## NuGet Dependencies

| Package | Phiên bản | Mục đích |
|---------|-----------|---------|
| `System.Data.SQLite` | 1.0.118+ | SQLite database |

Tự động restore khi `dotnet build` hoặc `dotnet restore`.
