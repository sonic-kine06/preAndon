# DATABASE.md — Tài liệu chi tiết Schema Database eAndon

## Tổng quan

Hệ thống eAndon sử dụng **SQLite** để lưu trữ dữ liệu cục bộ.  
File database: `Data/eandon.db` (tự tạo khi khởi động lần đầu).

Có **2 bảng chính**:
1. `Tickets` — lưu từng phiếu sự cố
2. `DailyStats` — thống kê tổng hợp theo ngày/line

---

## Bảng Tickets

### Mục đích
Lưu trữ toàn bộ thông tin về mỗi phiếu sự cố, từ lúc Operator báo đến khi Leader đóng.

### Schema

```sql
CREATE TABLE IF NOT EXISTS Tickets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TicketId TEXT UNIQUE,
    LineNumber TEXT,
    LineName TEXT,
    StationId TEXT,
    StationName TEXT,
    AlarmTypeIndex INTEGER,
    AlarmTypeName TEXT,
    Severity TEXT,
    ReportedAt TEXT,
    OperatorId TEXT,
    OperatorName TEXT,
    TechCheckinAt TEXT,
    TechnicianId TEXT,
    TechnicianName TEXT,
    TechFixedAt TEXT,
    FixNote TEXT,
    LeaderConfirmedAt TEXT,
    LeaderId TEXT,
    LeaderName TEXT,
    Status INTEGER DEFAULT 1,
    ReportDate TEXT
);
```

### Mô tả cột

| Cột | Kiểu | Mô tả |
|-----|------|-------|
| `Id` | INTEGER PK | Khóa chính tự tăng |
| `TicketId` | TEXT UNIQUE | Mã phiếu duy nhất, dạng "TKT-20260227-142233-456" |
| `LineNumber` | TEXT | Mã line, ví dụ "010" |
| `LineName` | TEXT | Tên line, ví dụ "Line 1" |
| `StationId` | TEXT | Mã trạm, ví dụ "ST-010-01" |
| `StationName` | TEXT | Tên trạm, ví dụ "Tram cat laser" |
| `AlarmTypeIndex` | INTEGER | Loại alarm (1-10) theo settings.txt |
| `AlarmTypeName` | TEXT | Tên loại alarm, ví dụ "Ho tro Bao tri" |
| `Severity` | TEXT | Mức độ: "Yellow" hoặc "Red" |
| `ReportedAt` | TEXT | Datetime báo lỗi "yyyy-MM-dd HH:mm:ss" |
| `OperatorId` | TEXT | Mã NV Operator |
| `OperatorName` | TEXT | Họ tên Operator |
| `TechCheckinAt` | TEXT | Datetime KTV check-in (nullable) |
| `TechnicianId` | TEXT | Mã NV KTV (nullable) |
| `TechnicianName` | TEXT | Họ tên KTV (nullable) |
| `TechFixedAt` | TEXT | Datetime KTV hoàn thành (nullable) |
| `FixNote` | TEXT | Ghi chú sửa chữa (nullable) |
| `LeaderConfirmedAt` | TEXT | Datetime Leader xác nhận (nullable) |
| `LeaderId` | TEXT | Mã NV Leader (nullable) |
| `LeaderName` | TEXT | Họ tên Leader (nullable) |
| `Status` | INTEGER | Trạng thái: 0=Green, 1=Yellow, 2=Red, 3=Repairing, 4=WaitLeader, 5=Closed |
| `ReportDate` | TEXT | Ngày báo lỗi "yyyy-MM-dd" (dùng để join DailyStats) |

### Vòng đời Status

```
Tạo mới → Status = 1 (Yellow) hoặc 2 (Red)
        → AssignTechnician → Status = 3 (Repairing)
        → MarkFixed → Status = 4 (WaitLeader) [hoặc 5 nếu không cần Leader]
        → LeaderConfirm → Status = 5 (Closed)
```

### Ví dụ dữ liệu

```sql
INSERT INTO Tickets VALUES (
    1,
    'TKT-20260227-142233-456',
    '010', 'Line 1',
    'ST-010-02', 'Tram han diem',
    2, 'Ho tro Bao tri', 'Red',
    '2026-02-27 14:22:33', 'NV001', 'Nguyen Van A',
    '2026-02-27 14:35:10', 'KTV005', 'Tran Thi B',
    '2026-02-27 15:10:45', 'Thay motor drive, test OK',
    '2026-02-27 15:20:00', 'LDR002', 'Le Van C',
    5,
    '2026-02-27'
);
```

---

## Bảng DailyStats

### Mục đích
Tổng hợp thống kê theo từng Line trong từng ngày để hiển thị trên StatisticsForm.  
Tự động được cập nhật (UPSERT) mỗi khi một ticket được đóng.

### Schema

```sql
CREATE TABLE IF NOT EXISTS DailyStats (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StatsDate TEXT,
    LineNumber TEXT,
    LineName TEXT,
    TotalIncidents INTEGER DEFAULT 0,
    TotalDowntimeSec INTEGER DEFAULT 0,
    AvgResponseTimeSec INTEGER DEFAULT 0,
    AvgRepairTimeSec INTEGER DEFAULT 0,
    MTTR_Minutes REAL DEFAULT 0,
    MTBF_Minutes REAL DEFAULT 0,
    Availability_Pct REAL DEFAULT 100,
    YellowCount INTEGER DEFAULT 0,
    RedCount INTEGER DEFAULT 0,
    UpdatedAt TEXT,
    UNIQUE(StatsDate, LineNumber)
);
```

### Mô tả cột

| Cột | Mô tả |
|-----|-------|
| `StatsDate` | Ngày thống kê "yyyy-MM-dd" |
| `LineNumber` | Mã line |
| `LineName` | Tên line |
| `TotalIncidents` | Tổng số sự cố đã đóng trong ngày |
| `TotalDowntimeSec` | Tổng thời gian downtime (giây) |
| `AvgResponseTimeSec` | Thời gian trung bình KTV phản hồi (giây) |
| `AvgRepairTimeSec` | Thời gian trung bình sửa chữa (giây) |
| `MTTR_Minutes` | Mean Time To Repair (phút) |
| `MTBF_Minutes` | Mean Time Between Failures (phút) |
| `Availability_Pct` | Tỷ lệ sẵn sàng hoạt động (%) |
| `YellowCount` | Số sự cố mức Yellow |
| `RedCount` | Số sự cố mức Red |
| `UpdatedAt` | Thời điểm cập nhật cuối |

---

## Công thức tính chỉ số

### Thời gian downtime của 1 ticket
```
DowntimeSec = LeaderConfirmedAt - ReportedAt  (tính bằng giây)
```

### Response Time (thời gian KTV phản hồi)
```
ResponseTimeSec = TechCheckinAt - ReportedAt
```

### Repair Time (thời gian sửa chữa)
```
RepairTimeSec = TechFixedAt - TechCheckinAt
```

### MTTR (Mean Time To Repair)
```
TotalDowntimeMin = SUM(DowntimeSec) / 60
MTTR = TotalDowntimeMin / TotalIncidents   [đơn vị: phút]
```

### MTBF (Mean Time Between Failures)
```
WorkingHours = 24 * 60 = 1440 phút/ngày
MTBF = (1440 - TotalDowntimeMin) / TotalIncidents   [đơn vị: phút]
```

### Availability (Tỷ lệ sẵn sàng)
```
Availability = MTBF / (MTBF + MTTR) * 100   [đơn vị: %]
```

### Ví dụ tính toán

Cho Line 010 trong ngày 2026-02-27 có 3 sự cố:
- Sự cố 1: downtime = 48 phút, response = 12 phút, repair = 30 phút
- Sự cố 2: downtime = 22 phút, response = 5 phút, repair = 15 phút
- Sự cố 3: downtime = 65 phút, response = 8 phút, repair = 50 phút

```
TotalIncidents = 3
TotalDowntimeMin = 48 + 22 + 65 = 135 phút

MTTR = 135 / 3 = 45.0 phút
MTBF = (1440 - 135) / 3 = 1305 / 3 = 435.0 phút
Availability = 435 / (435 + 45) * 100 = 435/480 * 100 ≈ 90.6%

AvgResponseTimeSec = (12+5+8)*60 / 3 = 500 giây ≈ 8.3 phút
AvgRepairTimeSec = (30+15+50)*60 / 3 = 1900 giây ≈ 31.7 phút
```

---

## Query thường dùng

### Lấy tất cả ticket đang active
```sql
SELECT * FROM Tickets
WHERE Status NOT IN (0, 5)
ORDER BY ReportedAt DESC;
```

### Thống kê theo ngày của một line
```sql
SELECT * FROM DailyStats
WHERE LineNumber = '010'
  AND StatsDate >= '2026-02-01'
ORDER BY StatsDate DESC;
```

### Top 5 Line có downtime nhiều nhất tháng này
```sql
SELECT LineNumber, LineName,
       SUM(TotalIncidents) AS TotalIncidents,
       SUM(TotalDowntimeSec) / 60 AS TotalDowntimeMin,
       AVG(Availability_Pct) AS AvgAvailability
FROM DailyStats
WHERE StatsDate >= '2026-02-01' AND StatsDate <= '2026-02-28'
GROUP BY LineNumber, LineName
ORDER BY TotalDowntimeMin DESC
LIMIT 5;
```

### Lịch sử ticket của một Line trong ngày
```sql
SELECT t.TicketId, t.StationName, t.AlarmTypeName, t.Severity,
       t.ReportedAt, t.OperatorName,
       t.TechnicianName, t.TechCheckinAt,
       t.LeaderName, t.LeaderConfirmedAt,
       (julianday(t.LeaderConfirmedAt) - julianday(t.ReportedAt)) * 24 * 60 AS DowntimeMin
FROM Tickets t
WHERE t.LineNumber = '010'
  AND t.ReportDate = '2026-02-27'
  AND t.Status = 5
ORDER BY t.ReportedAt;
```
