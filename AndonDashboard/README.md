# AndonDashboard — Màn hình giám sát tổng quan

Đây là ứng dụng **WinForms** dành cho **Quản lý/Leader** xem tổng quan toàn bộ sàn sản xuất trên 1 màn hình duy nhất.

---

## Dashboard hoạt động thế nào?

Dashboard **KHÔNG kết nối trực tiếp với Terminal**. Thay vào đó:

```
Terminal01  ghi  →  Data/terminal01.txt
Terminal02  ghi  →  Data/terminal02.txt
Terminal03  ghi  →  Data/terminal03.txt

                       ▲
          FileSystemWatcher theo dõi toàn bộ Data/*.txt
                       │
               AndonDashboard.exe
           (tự cập nhật ngay khi có file thay đổi)
```

**Ưu điểm**: Thêm bao nhiêu Terminal tùy ý, Dashboard tự nhận diện — không cần cấu hình thêm gì.

---

## Phần A: FileSystemWatcher — Cơ chế theo dõi file

### FileSystemWatcher là gì?

`FileSystemWatcher` là class của .NET dùng để theo dõi thư mục — nhận thông báo ngay khi file được tạo, sửa, hay xóa. **Không cần polling** (không cần vòng lặp hỏi liên tục "file thay đổi chưa?").

```csharp
var watcher = new FileSystemWatcher(_dataDir)
{
    Filter = "*.txt",                    // Chỉ theo dõi file .txt
    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
    EnableRaisingEvents = true           // Bắt đầu theo dõi
};

watcher.Changed += Watcher_Changed;
watcher.Created += Watcher_Changed;

void Watcher_Changed(object sender, FileSystemEventArgs e)
{
    // Chạy trên background thread! Phải dùng Invoke để update UI
    this.Invoke((Action)RefreshDashboard);
}
```

**Tại sao chọn cách này thay vì network?**
- **Đơn giản**: Không cần cấu hình port, firewall, network permissions
- **Đáng tin cậy**: File system OS cấp, không bị drop packet như network
- **Offline-friendly**: Hoạt động dù không có internet hoặc LAN

**Giới hạn**: Chỉ hoạt động trên cùng máy hoặc cùng network share (mapped drive). Đủ cho nhà máy có mạng LAN nội bộ.

### Tại sao cần `Invoke`?

WinForms có quy tắc: **chỉ thread tạo ra UI mới được phép update UI**. `FileSystemWatcher` callback chạy trên **background thread** (không phải UI thread). Nếu gọi trực tiếp `label.Text = "..."` từ background thread → `InvalidOperationException`.

```csharp
// SAI: gọi trực tiếp từ background thread
void Watcher_Changed(object sender, FileSystemEventArgs e)
{
    lblStatus.Text = "Cập nhật...";  // ← CRASH!
}

// ĐÚNG: dùng Invoke để chuyển về UI thread
void Watcher_Changed(object sender, FileSystemEventArgs e)
{
    this.Invoke((Action)(() => {
        lblStatus.Text = "Cập nhật...";  // ← OK
        RefreshDashboard();
    }));
}
```

**So sánh C# vs VB.NET — Invoke**:

| C# | VB.NET |
|----|--------|
| `this.Invoke((Action)RefreshDashboard)` | `Me.Invoke(Sub() RefreshDashboard())` |
| `this.InvokeRequired` | `Me.InvokeRequired` |

---

## Phần B: Timer — Tại sao cần Timer ngoài FileSystemWatcher?

FileSystemWatcher phản ứng ngay khi file thay đổi. Nhưng có những việc cần làm **định kỳ bất kể có thay đổi không**:

1. **Đồng hồ** — cập nhật giờ hiện tại mỗi giây
2. **Bộ đếm thời gian** — hiển thị "Line 1 đã lỗi X phút"
3. **Analytics** — kiểm tra anomaly mỗi 15 giây
4. **Email** — kiểm tra đến giờ gửi chưa mỗi phút

```csharp
// Timer cập nhật UI mỗi giây
var uiTimer = new Timer { Interval = 1000 };
uiTimer.Tick += (s, e) => {
    lblClock.Text = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
    UpdateAllTimerCells();
};
uiTimer.Start();

// Timer kiểm tra analytics mỗi 15 giây
var analyticsTimer = new Timer { Interval = 15000 };
analyticsTimer.Tick += async (s, e) => {
    var summary = await Task.Run(() => _analytics.GetDashboardSummary());
    this.Invoke((Action)(() => UpdateAnalyticsBanner(summary)));
};
analyticsTimer.Start();
```

---

## Phần C: 3 Forms — Mục đích và cách hoạt động

### `DashboardMainForm` — Form chính

**Mục đích**: Hiển thị grid tất cả Lines × Alarm Types, cập nhật real-time qua FileSystemWatcher và Timer.

**Cách đọc data**: Mỗi `terminal01.txt` có format:
```
LINE;010;Line 1;AlarmType1;STATUS;MINUTES;AlarmType2;STATUS;MINUTES;...
```
DashboardMainForm đọc tất cả file `.txt` trong `Data/`, parse và hiển thị.

**Click vào ô** → mở `TicketDetailForm` để xem chi tiết ticket đang active.  
**Click "📊 Thống kê"** → mở `StatisticsForm`.

### `TicketDetailForm` — Chi tiết ticket

**Mục đích**: Hiển thị toàn bộ thông tin 1 phiếu sự cố đang mở.

```
╔══════════════════════════════════════╗
║  Chi tiết phiếu sự cố                ║
║  TKT-20260227-142233-456             ║
╠══════════════════════════════════════╣
║  Line: Line 1 / Trạm: Tram cat laser ║
║  Loại: Hỗ trợ Bảo trì / Mức: Red    ║
║                                      ║
║  ✅ Operator: NV001 - Nguyen Van A   ║
║     Báo lúc: 14:22:33                ║
║  ✅ KTV: KTV005 - Tran Thi B         ║
║     Nhận lúc: 14:35:10               ║
║  ⏳ Đang sửa...                      ║
╚══════════════════════════════════════╝
```

**Nhận `ticketId`** từ DashboardMainForm, gọi `_incidentService.GetById(ticketId)` để load data.

### `StatisticsForm` — Thống kê

**Mục đích**: Hiển thị MTTR/MTBF/Availability và Analytics theo ngày/line.

**DataGridView với lọc**:
```csharp
// Lọc theo ngày
var stats = _dailyStatsService.GetStats(selectedDate);
dataGridView.DataSource = stats;  // Tự động hiển thị bảng

// Export CSV
var sb = new StringBuilder();
foreach (DataGridViewRow row in dataGridView.Rows)
    sb.AppendLine(string.Join(",", row.Cells.Cast<DataGridViewCell>().Select(c => c.Value)));
File.WriteAllText("export.csv", sb.ToString());
```

---

## Khi copy ra nhiều Terminal — Dashboard có thay đổi không?

**KHÔNG.** Dashboard đọc `Data/*.txt` (tất cả file `.txt` trong thư mục Data).  
Mỗi Terminal mới chỉ cần:
1. Thêm entries vào `Assets/Workstations_terminals.txt`
2. Chạy `AndonTerminal.exe <tên_terminal_mới>`

Dashboard tự động hiển thị Lines mới ngay khi khởi động.

---

## Chạy Dashboard

```bash
# dotnet run
dotnet run --project AndonDashboard

# Hoặc exe trực tiếp
cd AndonDashboard/bin/Debug/net8.0-windows/
AndonDashboard.exe
```

---

## Giao diện

```
╔══════════════════════════════════════════════════════════════════╗
║  🏭 eAndon Dashboard        08:30:15  27/02/2026   📊 Thống kê ║
╠══════════════════════════════════════════════════════════════════╣
║  🟢 Bình thường  🟡 Yellow  🔴 Red  🟠 Repairing  🔵 WaitLeader║
╠══════════════════════════════════════════════════════════════════╣
║        │ Hỗ trợ TL │  Bảo trì  │ Chất lượng │ Thiếu VL │      ║
║  Line1 │ [🟢  ✓  ] │ [🟡 5m  ] │ [🟢  ✓  ] │ [🟢  ✓ ] │      ║
║  Line2 │ [🔴  9m ] │ [🟢  ✓  ] │ [🟠 Sửa ] │ [🟢  ✓ ] │      ║
║  Line3 │ [🟢  ✓  ] │ [🟢  ✓  ] │ [🟢  ✓  ] │ [🟢  ✓ ] │      ║
╠══════════════════════════════════════════════════════════════════╣
║  Cập nhật từ file lúc 08:30:20                                  ║
╚══════════════════════════════════════════════════════════════════╝
```

- Click vào ô → `TicketDetailForm` xem chi tiết phiếu sự cố
- Click "📊 Thống kê" → `StatisticsForm` xem MTTR/MTBF/Availability

---

## Cấu trúc thư mục

```
AndonDashboard/
├── Program.cs                  ← Entry point
├── AndonDashboard.csproj       ← Project file (.NET 8 WinForms)
├── Forms/
│   ├── DashboardMainForm.cs    ← Form chính: grid + FileSystemWatcher + Timer
│   ├── TicketDetailForm.cs     ← Chi tiết 1 ticket khi click ô
│   └── StatisticsForm.cs       ← Thống kê MTTR/MTBF/Availability
└── README.md                   ← (file này)
```

---

## Nguồn icon/ảnh

| File | Nguồn | License |
|------|-------|---------|
| `Assets/app.ico` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |

Xem `Assets/NOTICE.txt` để biết đầy đủ nội dung MIT License.

Đây là ứng dụng **WinForms** dành cho **Quản lý/Leader** xem tổng quan toàn bộ sàn sản xuất trên 1 màn hình duy nhất.

---

## Dashboard hoạt động thế nào?

Dashboard **KHÔNG kết nối trực tiếp với Terminal**. Thay vào đó:

```
Terminal01  ghi  →  Data/terminal01.txt
Terminal02  ghi  →  Data/terminal02.txt
Terminal03  ghi  →  Data/terminal03.txt

                       ▲
          FileSystemWatcher theo dõi toàn bộ Data/*.txt
                       │
               AndonDashboard.exe
           (tự cập nhật ngay khi có file thay đổi)
```

**Ưu điểm**: Thêm bao nhiêu Terminal tùy ý, Dashboard tự nhận diện — không cần cấu hình thêm gì.

---

## Chạy Dashboard

```bash
# dotnet run
dotnet run --project AndonDashboard

# Hoặc exe trực tiếp
cd AndonDashboard/bin/Debug/net8.0-windows/
AndonDashboard.exe
```

---

## Khi copy ra nhiều Terminal — Dashboard có thay đổi không?

**KHÔNG.** Dashboard đọc `Data/*.txt` (tất cả file `.txt` trong thư mục Data).  
Mỗi Terminal mới chỉ cần:
1. Thêm entries vào `Assets/Workstations_terminals.txt`
2. Chạy `AndonTerminal.exe <tên_terminal_mới>`

Dashboard tự động hiển thị Lines mới ngay khi khởi động.

---

## Giao diện

```
╔══════════════════════════════════════════════════════════════════╗
║  🏭 eAndon Dashboard        08:30:15  27/02/2026   📊 Thống kê ║
╠══════════════════════════════════════════════════════════════════╣
║  🟢 Bình thường  🟡 Yellow  🔴 Red  🟠 Repairing  🔵 WaitLeader║
╠══════════════════════════════════════════════════════════════════╣
║        │ Hỗ trợ TL │  Bảo trì  │ Chất lượng │ Thiếu VL │      ║
║  Line1 │ [🟢  ✓  ] │ [🟡 5m  ] │ [🟢  ✓  ] │ [🟢  ✓ ] │      ║
║  Line2 │ [🔴  9m ] │ [🟢  ✓  ] │ [🟠 Sửa ] │ [🟢  ✓ ] │      ║
║  Line3 │ [🟢  ✓  ] │ [🟢  ✓  ] │ [🟢  ✓  ] │ [🟢  ✓ ] │      ║
╠══════════════════════════════════════════════════════════════════╣
║  Cập nhật từ file lúc 08:30:20                                  ║
╚══════════════════════════════════════════════════════════════════╝
```

- Click vào ô → `TicketDetailForm` xem chi tiết phiếu sự cố
- Click "📊 Thống kê" → `StatisticsForm` xem MTTR/MTBF/Availability

---

## Cấu trúc thư mục

```
AndonDashboard/
├── Program.cs                  ← Entry point
├── AndonDashboard.csproj       ← Project file (.NET 8 WinForms)
├── Forms/
│   ├── DashboardMainForm.cs    ← Form chính: grid + FileSystemWatcher + Timer
│   ├── TicketDetailForm.cs     ← Chi tiết 1 ticket khi click ô
│   └── StatisticsForm.cs       ← Thống kê MTTR/MTBF/Availability
└── README.md                   ← (file này)
```

---

## Nguồn icon/ảnh

| File | Nguồn | License |
|------|-------|---------|
| `Assets/app.ico` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |

Xem `Assets/NOTICE.txt` để biết đầy đủ nội dung MIT License.
