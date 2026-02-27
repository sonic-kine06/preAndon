# SharedLib — Thư viện dùng chung

Class Library được **cả AndonTerminal và AndonDashboard** tham chiếu (`<ProjectReference>`).  
Chứa toàn bộ Models, Services, và business logic để tránh lặp code.

---

## Cấu trúc thư mục

```
SharedLib/
├── SharedLib.csproj         ← Project file (.NET 8 Class Library)
│
├── Models/
│   ├── StationInfo.cs       ← Model thông tin 1 trạm làm việc
│   └── IncidentTicket.cs    ← Model phiếu sự cố + enum TicketStatus
│
├── Services/
│   ├── SettingsReader.cs    ← Đọc Assets/settings.txt
│   ├── LineStationReader.cs ← Đọc Workstations + Lines_stations
│   ├── AlarmLogger.cs       ← Ghi log text file Logs/alarmlog_*.txt
│   ├── IncidentService.cs   ← CRUD ticket SQLite + logic 7 bước
│   ├── DailyStatsService.cs ← Tính và lưu MTTR/MTBF/Availability
│   └── Analytics/           ← (mở rộng tương lai)
│       ├── AnalyticsManager.cs
│       ├── AnomalyDetector.cs
│       ├── DowntimeEstimator.cs
│       ├── TechnicianTracker.cs
│       └── TimePatternDetector.cs
│
└── README.md                ← (file này)
```

---

## Models

### `IncidentTicket.cs`
- `TicketStatus` enum: `Green=0, Yellow=1, Red=2, Repairing=3, WaitLeader=4`
- `IncidentTicket` class: tất cả trường của bảng `Tickets` trong SQLite

### `StationInfo.cs`
- `StationInfo(stationId, stationName, lineNumber, lineName)` — model 1 trạm

---

## Services

### `SettingsReader.cs`
Đọc `settings.txt` với format `Key : Value`. Cung cấp:
```csharp
settings.NumberOfAlarmTypes   // "Number of alarm types to display"
settings.GetAlarmLabel(i)     // "Alarm label 1" ... "Alarm label N"
settings.GetAlarmImageFile(i) // "Alarm image file 1" ... "Alarm image file N"
settings.AlarmSoundFile       // "Alarm sound file"
settings.RequireLeaderConfirmation
```

### `LineStationReader.cs`
Đọc 2 file cấu hình:
- `Workstations_terminals.txt` → `GetWorkstations()` (tất cả entries)
- `Lines_stations.txt` → `GetLineStationsMap()`, `GetStationsForLine(lineNumber)`

> ⚠️ **Lưu ý quan trọng**: `GetWorkstations()` trả về TẤT CẢ workstations.  
> `TerminalMainForm` phải tự lọc theo `terminalName`:
> ```csharp
> _workstations = reader.GetWorkstations()
>     .Where(w => w.Terminal == terminalName).ToList();
> ```

### `AlarmLogger.cs`
Ghi log dạng text vào `Logs/alarmlog_yyyy-MM-dd.txt`.  
Format gốc tương thích với eAndon VB.NET.

### `IncidentService.cs`
- Khởi tạo SQLite DB (tự tạo bảng nếu chưa có)
- `OpenTicket(...)`, `TechCheckIn(...)`, `CompleteRepair(...)`, `LeaderConfirm(...)`, `GetAllOpen()`, `GetHistory(...)`

### `DailyStatsService.cs`
- Tính MTTR (Mean Time To Repair), MTBF, Availability
- Lưu vào bảng `DailyStats` trong SQLite
- `GetStats(date)`, `GetStatsByLine(lineNumber, startDate, endDate)`

---

## NuGet Dependencies

| Package | Phiên bản | Mục đích |
|---------|-----------|---------|
| `System.Data.SQLite` | 1.0.118+ | SQLite database |

Tự động restore khi `dotnet build` hoặc `dotnet restore`.
