# Tài liệu Analytics — eAndon C#

## Tổng quan

Module Analytics trong eAndon gồm 4 model phân tích dữ liệu lịch sử sự cố, được điều phối bởi **AnalyticsManager**. Tất cả model đọc dữ liệu từ cùng một file SQLite (`Data/eandon.db`).

---

## 1. DowntimeEstimator — Dự đoán Downtime (EWMA)

### Thuật toán: Exponentially Weighted Moving Average (EWMA)

EWMA là thuật toán dự báo chuỗi thời gian, ưu tiên dữ liệu gần đây hơn dữ liệu cũ.

### Công thức

```
estimate(t) = ALPHA × actual(t) + (1 - ALPHA) × estimate(t-1)
```

| Tham số | Giá trị | Ý nghĩa |
|---------|---------|---------|
| ALPHA | 0.3 | 30% trọng số cho dữ liệu mới nhất |
| MIN_SAMPLES | 3 | Cần ít nhất 3 mẫu để dự đoán |
| FULL_CONFIDENCE | 20 | 20 mẫu = 100% độ tin cậy |

### Độ tin cậy

```
confidence = min(sample_count / 20, 1.0)
```

| Khoảng | Nhãn |
|--------|------|
| 0.00 – 0.33 | Thấp |
| 0.33 – 0.66 | Trung bình |
| 0.66 – 1.00 | Cao |

### Ví dụ

Lịch sử downtime Line 010: `[30, 45, 20, 35, 25]` phút

```
ewma(1) = 30
ewma(2) = 0.3 × 45 + 0.7 × 30 = 13.5 + 21.0 = 34.5
ewma(3) = 0.3 × 20 + 0.7 × 34.5 = 6.0 + 24.15 = 30.15
ewma(4) = 0.3 × 35 + 0.7 × 30.15 = 10.5 + 21.1 = 31.6
ewma(5) = 0.3 × 25 + 0.7 × 31.6 = 7.5 + 22.12 = 29.6 phút ← dự đoán
```

confidence = min(5/20, 1.0) = 0.25 → **Thấp**

### Các phương thức

| Phương thức | Mô tả |
|-------------|-------|
| `PredictDowntime(lineNumber)` | Dự đoán theo line, dùng DailyStats |
| `PredictDowntime(lineNumber, alarmTypeIndex)` | Dự đoán theo line + loại alarm |
| `PredictForStation(stationId)` | Dự đoán theo station |
| `PredictAll()` | Dự đoán tất cả line, sắp xếp downtime cao nhất trước |

### Hiển thị trên UI

- **StatisticsForm**: Bảng "Dự đoán Downtime" sau phần DailyStats
- **TerminalMainForm**: Popup sau khi tạo ticket mới (`GetSuggestionForNewTicket`)

---

## 2. AnomalyDetector — Phát hiện Bất thường (Z-score)

### Thuật toán: Z-score so với Rolling 4 tuần

So sánh downtime tuần hiện tại với baseline 4 tuần trước.

### Công thức

```
mean  = trung bình downtime hàng tuần trong 4 tuần trước
std   = độ lệch chuẩn
Z     = (downtime_tuần_hiện_tại - mean) / std
```

| Tham số | Giá trị | Ý nghĩa |
|---------|---------|---------|
| Z_THRESHOLD | 2.0 | Z ≥ 2.0 → bất thường |
| ROLLING_WEEKS | 4 | Số tuần lịch sử để tính baseline |

### Ví dụ

4 tuần trước: `[120, 135, 115, 130]` phút/tuần

```
mean = (120 + 135 + 115 + 130) / 4 = 125 phút
std  = sqrt(((120-125)² + (135-125)² + (115-125)² + (130-125)²) / 4)
     = sqrt((25 + 100 + 100 + 25) / 4)
     = sqrt(62.5) ≈ 7.9 phút

Tuần hiện tại: 200 phút
Z = (200 - 125) / 7.9 = 75 / 7.9 ≈ 9.5 → BẤT THƯỜNG ✓
```

### Trường hợp không tính được

- Ít hơn 2 tuần dữ liệu lịch sử → trả về `null` (không đủ baseline)
- std = 0 (tất cả tuần giống nhau) → Z = 0 (không bất thường)

### Các phương thức

| Phương thức | Mô tả |
|-------------|-------|
| `DetectAll()` | Phát hiện bất thường tất cả line, trả về danh sách IsAnomaly = true |
| `Detect(lineNumber)` | Phát hiện bất thường cho một line cụ thể |

### Hiển thị trên UI

- **DashboardMainForm**: Banner cam ở đầu form khi có anomaly
- **StatisticsForm**: Phần "Cảnh báo bất thường" trong tab Analytics

---

## 3. TechnicianTracker — Theo dõi Hiệu suất KTV

### Thuật toán: AVG thời gian sửa chữa theo nhóm

Nhóm theo `(TechnicianId, AlarmTypeIndex)`, tính trung bình thời gian sửa.

### Công thức điểm xếp hạng

```
Score = (RepairCount / AvgRepairTimeMinutes) × 10
```

KTV tốt nhất = nhiều lần sửa (kinh nghiệm) + thời gian nhanh = Score cao.

### Ví dụ

| KTV | Alarm | Số lần | Avg Time (phút) | Score |
|-----|-------|--------|-----------------|-------|
| NV001 - Nguyễn A | AlarmType#2 | 15 | 12.5 | 12.0 |
| NV002 - Trần B | AlarmType#2 | 8 | 8.0 | 10.0 |
| NV003 - Lê C | AlarmType#2 | 3 | 25.0 | 1.2 |

→ Gợi ý: **NV001** (ít thời gian nhất: AvgRepairTime sắp xếp ASC)

> **Lưu ý**: Hiện tại sort theo `AvgRepairTime ASC` (nhanh nhất trước), Score chỉ dùng tham khảo.

### Các phương thức

| Phương thức | Mô tả |
|-------------|-------|
| `SuggestBest(alarmTypeIndex)` | KTV tốt nhất cho loại alarm (toàn hệ thống) |
| `SuggestBest(alarmTypeIndex, lineNumber)` | KTV tốt nhất cho alarm + line cụ thể |
| `GetStatsByAlarmType(alarmTypeIndex)` | Thống kê KTV theo loại alarm |
| `GetOverallRanking()` | Xếp hạng tổng thể tất cả KTV |

### Hiển thị trên UI

- **TerminalMainForm**: Popup "Gợi ý KTV" sau khi Operator tạo ticket mới
- **StatisticsForm**: Bảng "Xếp hạng KTV" trong tab Analytics

---

## 4. TimePatternDetector — Phát hiện Mẫu Thời gian

### Thuật toán: Tần suất theo (Station, AlarmType, DayOfWeek, Hour)

Đếm số lần xảy ra sự cố tại từng tổ hợp (trạm, loại alarm, thứ trong tuần, giờ).

### Công thức xác suất

```
Probability = OccurrenceCount / TotalDaysObserved
```

| Tham số | Giá trị | Ý nghĩa |
|---------|---------|---------|
| HIGH_RISK_THRESHOLD | 0.20 | ≥ 20% → rủi ro cao |

### Ví dụ

Tổng số ngày quan sát: 30 ngày

| Station | Alarm | Thứ | Giờ | Số lần | Xác suất |
|---------|-------|-----|-----|--------|----------|
| ST-010-01 | AlarmType#3 | Thứ 2 | 8 | 8 | 8/30 = 26.7% → ⚠ RỦI RO CAO |
| ST-020-02 | AlarmType#1 | Thứ 6 | 14 | 5 | 5/30 = 16.7% → Bình thường |
| ST-030-01 | AlarmType#2 | Thứ 3 | 10 | 7 | 7/30 = 23.3% → ⚠ RỦI RO CAO |

### Bảng TimePatterns trong SQLite

```sql
CREATE TABLE TimePatterns (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StationId TEXT,
    StationName TEXT,
    AlarmTypeIndex INTEGER,
    AlarmTypeName TEXT,
    DayOfWeek INTEGER,    -- 0=CN, 1=T2, ..., 6=T7
    Hour INTEGER,         -- 0-23
    OccurrenceCount INTEGER DEFAULT 0,
    Probability REAL DEFAULT 0,
    UpdatedAt TEXT,
    UNIQUE(StationId, AlarmTypeIndex, DayOfWeek, Hour)
);
```

### Các phương thức

| Phương thức | Mô tả |
|-------------|-------|
| `RefreshPatterns()` | Đọc lại toàn bộ Tickets và cập nhật TimePatterns |
| `GetHighRiskPatterns()` | Lấy tất cả pattern có Probability ≥ 20% |
| `GetCurrentRisks()` | Lấy pattern rủi ro đang khớp với thời điểm hiện tại |

### Hiển thị trên UI

- **DashboardMainForm**: Banner cảnh báo "Nguy cơ sự cố sắp xảy ra tại..."
- **StatisticsForm**: Bảng "Mẫu thời gian rủi ro cao"

---

## 5. AnalyticsManager — Điều phối Tổng hợp

### Mục đích

`AnalyticsManager` là điểm truy cập duy nhất cho tất cả analytics. Nó khởi tạo và giữ instance của cả 4 model.

### Khởi tạo

```csharp
var analytics = new AnalyticsManager("Data/eandon.db");
```

### GetDashboardSummary()

Trả về `DashboardAnalyticsSummary` chứa:

| Trường | Loại | Mô tả |
|--------|------|-------|
| `DowntimePredictions` | `List<DowntimePrediction>` | EWMA cho tất cả line |
| `Anomalies` | `List<AnomalyResult>` | Line có Z-score bất thường |
| `TechnicianRanking` | `List<TechnicianStat>` | Xếp hạng KTV tổng thể |
| `HighRiskTimePatterns` | `List<TimePattern>` | Mẫu xác suất ≥ 20% |
| `CurrentRisks` | `List<TimePattern>` | Rủi ro đang khớp giờ hiện tại |
| `HasAnomalies` | `bool` | True nếu có bất thường → hiện banner |
| `HasCurrentRisks` | `bool` | True nếu có rủi ro hiện tại |

### GetSuggestionForNewTicket(lineNumber, alarmTypeIndex)

Trả về `TicketSuggestion` sau khi tạo ticket mới:

```csharp
// Ví dụ sử dụng trong TerminalMainForm sau OpenIncident():
var suggestion = analytics.GetSuggestionForNewTicket("010", 2);
if (suggestion.HasSuggestion)
    MessageBox.Show(suggestion.SummaryText, "💡 Gợi ý");
```

Ví dụ output `SummaryText`:
```
👷 KTV đề xuất: Nguyễn Văn A (NV001) — Avg: 12.5 phút (15 lần)
⏱ Dự đoán downtime: ~30 phút (độ tin cậy: Cao)
⚠ Cảnh báo mẫu thời gian: ST-010-01 - Hỗ trợ Bảo trì: 27% vào Thứ 2 08:00
```

---

## Luồng dữ liệu tổng thể

```
Ticket đóng
    ↓
IncidentService.LeaderConfirm()
    ↓
DailyStatsService.UpdateForLine()  → bảng DailyStats
    ↓
AnalyticsManager.GetDashboardSummary()
    ├── DowntimeEstimator ← DailyStats + Tickets
    ├── AnomalyDetector   ← DailyStats
    ├── TechnicianTracker ← Tickets
    └── TimePatternDetector ← Tickets → bảng TimePatterns
```

---

## Khi nào trigger Analytics

| Sự kiện | Action |
|---------|--------|
| Dashboard mở | `GetDashboardSummary()` một lần |
| Timer 15 giây (Dashboard) | Kiểm tra `DetectAll()` + `GetCurrentRisks()` |
| Terminal tạo ticket mới | `GetSuggestionForNewTicket()` |
| StatisticsForm mở | `GetDashboardSummary()` đầy đủ |
