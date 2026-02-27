# 📚 GIÁO TRÌNH TỰ HỌC — Xây dựng eAndon từ Đầu

> **Mục tiêu**: Học C# WinForms bằng cách tự tay xây dựng lại hệ thống eAndon từ con số 0.  
> **Phong cách**: Mỗi bài = 1 tính năng thực tế → thêm vào project đang chạy.  
> **Phù hợp với**: Người biết lập trình cơ bản (biến, hàm, vòng lặp) nhưng chưa làm C# WinForms.

---

## Bản đồ học tập

```
BÀI 1 ──────► BÀI 2 ──────► BÀI 3 ──────► BÀI 4 ──────► BÀI 5
 (⭐Dễ)         (⭐Dễ)         (⭐⭐TB)       (⭐⭐TB)       (⭐⭐TB)
 Tạo Form     Nút + Màu     Đọc File      SQLite CRUD   Grid Động
 đầu tiên     + Timer       Cấu hình      cơ bản        N hàng × M cột
    │
    ▼
BÀI 6 ──────► BÀI 7 ──────► BÀI 8 ──────► BÀI 9 ──────► BÀI 10
 (⭐⭐⭐Khó)    (⭐⭐⭐Khó)    (⭐⭐⭐Khó)    (⭐⭐⭐⭐Nâng)  (⭐⭐⭐⭐Nâng)
 Luồng        Services      Dashboard     Analytics     Email
 7 Bước       & DI          Realtime      & Thống kê    Automation
```

---

## BÀI 1 — Tạo Form WinForms Đầu Tiên ⭐

### Bạn học được gì
- Tạo project WinForms từ command line
- Hiểu cấu trúc `Form`, `Application.Run()`
- Thêm control (`Label`, `Button`) bằng code

### Lý thuyết

**WinForms** là framework UI của .NET cho Windows. Mỗi cửa sổ = 1 lớp kế thừa `Form`.

```
Form (cửa sổ)
 └── Controls (các thành phần giao diện)
      ├── Label     — hiển thị chữ
      ├── Button    — nút bấm
      ├── TextBox   — ô nhập liệu
      ├── Panel     — vùng chứa
      └── DataGridView — bảng dữ liệu
```

### Code mẫu — Form xin chào

```csharp
// Bước 1: Tạo project
// dotnet new winforms -n HelloAndon
// cd HelloAndon

// Bước 2: Sửa file Program.cs
using System;
using System.Drawing;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

// Bước 3: Tạo file MainForm.cs
public class MainForm : Form
{
    public MainForm()
    {
        // Cấu hình cửa sổ
        this.Text = "eAndon Demo";
        this.Size = new Size(400, 300);
        this.BackColor = Color.White;

        // Thêm Label
        var label = new Label
        {
            Text = "🏭 Chào mừng đến eAndon!",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Location = new Point(50, 80),
            AutoSize = true,
            ForeColor = Color.DarkBlue
        };
        this.Controls.Add(label);

        // Thêm Button
        var btn = new Button
        {
            Text = "Bấm vào đây",
            Location = new Point(120, 150),
            Size = new Size(150, 40),
            BackColor = Color.FromArgb(46, 204, 113),  // màu xanh lá
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btn.Click += (s, e) => MessageBox.Show("Bạn đã bấm nút!");
        this.Controls.Add(btn);
    }
}
```

### Xem trong project thực tế
Mở `AndonTerminal/Forms/TerminalMainForm.cs` → tìm constructor:
```csharp
public TerminalMainForm(SettingsReader settings, ...) : base()
{
    // ... khởi tạo dữ liệu
    InitializeUI();  // ← hàm này tạo toàn bộ giao diện
}
```

### 🏋️ Bài tập thực hành

**BT1.1** — Tạo form với nền màu `#1a237e` (xanh navy), có Label "eAndon Terminal" màu trắng to 24pt.

**BT1.2** — Thêm 3 Button: "Xanh", "Vàng", "Đỏ". Khi bấm mỗi nút, nền Form đổi màu tương ứng.

**BT1.3** *(Thử thách)* — Tạo màn hình login đơn giản: 2 TextBox (username, password) + nút OK. Nếu nhập đúng "admin/1234" thì hiện MessageBox "Đăng nhập thành công!".

### 💡 Ví dụ biến thể trong thực tế
- **Màn hình POS** (điểm bán hàng): Form với nút số 0-9 + hiển thị tổng tiền
- **Máy check-in nhà máy**: Form scan thẻ RFID → hiển thị tên nhân viên
- **Bảng điều khiển máy CNC**: Form với nút Start/Stop + đèn trạng thái

---

## BÀI 2 — Nút, Màu Sắc và Timer ⭐

### Bạn học được gì
- Bảng màu 5 trạng thái của eAndon
- `System.Windows.Forms.Timer` — thực thi code định kỳ
- `DateTime` — xử lý thời gian, đếm ngược

### Lý thuyết: 5 màu của eAndon

| Màu | Tên | RGB | Ý nghĩa |
|-----|-----|-----|---------|
| 🟢 Xanh lá | Green | (46, 204, 113) | Bình thường |
| 🟡 Vàng | Yellow | (241, 196, 15) | Có lỗi, vẫn chạy |
| 🔴 Đỏ | Red | (231, 76, 60) | Đã dừng |
| 🟠 Cam | Repairing | (230, 126, 34) | Đang sửa |
| 🔵 Xanh dương | WaitLeader | (52, 152, 219) | Chờ xác nhận |

### Code mẫu — Button đổi màu + đếm thời gian

```csharp
public class StatusButtonForm : Form
{
    private Button _btnStatus;
    private Label _lblTimer;
    private System.Windows.Forms.Timer _timer;
    private DateTime? _alarmStartTime;

    // Định nghĩa màu (giống hệt TerminalMainForm.cs trong project)
    private static readonly Color ColorGreen    = Color.FromArgb(46, 204, 113);
    private static readonly Color ColorYellow   = Color.FromArgb(241, 196, 15);
    private static readonly Color ColorRed      = Color.FromArgb(231, 76, 60);
    private static readonly Color ColorRepairing= Color.FromArgb(230, 126, 34);

    public StatusButtonForm()
    {
        this.Size = new Size(300, 200);
        this.Text = "Status Demo";

        // Button trạng thái
        _btnStatus = new Button
        {
            Size = new Size(200, 80),
            Location = new Point(50, 30),
            BackColor = ColorGreen,
            Text = "✓  Bình thường",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        _btnStatus.Click += OnButtonClick;
        this.Controls.Add(_btnStatus);

        // Label hiển thị thời gian
        _lblTimer = new Label
        {
            Location = new Point(50, 130),
            AutoSize = true,
            Font = new Font("Segoe UI", 10f)
        };
        this.Controls.Add(_lblTimer);

        // Timer cập nhật mỗi giây
        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private int _clickCount = 0;
    private readonly Color[] _colors = {
        Color.FromArgb(46, 204, 113),   // Green
        Color.FromArgb(241, 196, 15),   // Yellow
        Color.FromArgb(231, 76, 60),    // Red
        Color.FromArgb(230, 126, 34),   // Repairing
        Color.FromArgb(52, 152, 219)    // WaitLeader
    };
    private readonly string[] _labels = { "✓ Bình thường", "⚠ Cảnh báo", "🛑 Dừng", "🔧 Đang sửa", "⏳ Chờ" };

    private void OnButtonClick(object sender, EventArgs e)
    {
        _clickCount = (_clickCount + 1) % _colors.Length;
        _btnStatus.BackColor = _colors[_clickCount];
        _btnStatus.Text = _labels[_clickCount];
        _alarmStartTime = _clickCount == 0 ? (DateTime?)null : DateTime.Now;
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        if (_alarmStartTime.HasValue)
        {
            var elapsed = DateTime.Now - _alarmStartTime.Value;
            _lblTimer.Text = $"⏱ Đang chờ: {(int)elapsed.TotalMinutes}m {elapsed.Seconds:D2}s";
            _lblTimer.ForeColor = Color.DarkRed;
        }
        else
        {
            _lblTimer.Text = $"🕐 {DateTime.Now:HH:mm:ss}";
            _lblTimer.ForeColor = Color.DarkGreen;
        }
    }
}
```

### Xem trong project thực tế
File `AndonTerminal/Forms/TerminalMainForm.cs` — tìm hàm `OnUpdateTimer()`:
```csharp
private void OnUpdateTimer(object sender, EventArgs e)
{
    // Cập nhật thời gian trên mỗi ô
    foreach (var kv in _gridCells)
    {
        var cell = kv.Value;
        if (cell.Status != TicketStatus.Green && cell.AlarmStartTime.HasValue)
        {
            var elapsed = DateTime.Now - cell.AlarmStartTime.Value;
            // Hiển thị "05m30s" hoặc "1h15m" tùy thời gian
            cell.Button.Text = FormatElapsedTime(elapsed);
        }
    }
}
```

### 🏋️ Bài tập thực hành

**BT2.1** — Tạo đồng hồ kỹ thuật số: Label hiển thị giờ:phút:giây, cập nhật mỗi giây.

**BT2.2** — Tạo đèn tín hiệu giao thông: 3 Panel (đỏ/vàng/xanh) tự chuyển màu sau mỗi 3/1/4 giây. Dùng `Timer` + biến trạng thái.

**BT2.3** *(Thử thách)* — Tạo màn hình "đếm ngược máy đang nghỉ": khi bấm "Bắt đầu dừng", đếm ngược từ 30 phút xuống 0. Khi hết giờ, nền đỏ + âm thanh `SystemSounds.Beep`.

### 💡 Ví dụ biến thể
- **Đồng hồ năng suất**: đếm số sản phẩm/giờ, đổi màu khi dưới target
- **Bộ đếm thời gian gia nhiệt lò**: đếm ngược kèm thanh progress
- **Đèn báo máy chạy/dừng**: Panel màu với nhấp nháy khi cảnh báo

---

## BÀI 3 — Đọc File Cấu Hình ⭐⭐

### Bạn học được gì
- Đọc file text `settings.txt` theo format key : value
- Tách logic đọc config thành class riêng (`SettingsReader`)
- Áp dụng cấu hình vào form khi khởi động

### Lý thuyết: Tại sao cần file cấu hình?

**Hardcoded** (không tốt):
```csharp
int numberOfLines = 6;              // ← phải sửa code rồi build lại khi đổi
string smtpServer = "smtp.abc.com"; // ← ai biết đâu mà sửa?
```

**Config file** (tốt):
```
# settings.txt
Number of lines : 6
Email SMTP Server : smtp.abc.com
```
→ Kỹ sư nhà máy tự sửa file → không cần lập trình viên can thiệp.

### Format settings.txt của eAndon
```
# Dòng comment bắt đầu bằng #
Key có dấu cách : Giá trị  ← dấu " : " (có khoảng trắng) là separator
```

### Code mẫu — SettingsReader đơn giản

```csharp
using System;
using System.Collections.Generic;
using System.IO;

public class SettingsReader
{
    // Dictionary lưu key → value
    private readonly Dictionary<string, string> _data =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public SettingsReader(string filePath)
    {
        if (!File.Exists(filePath)) return;

        foreach (var line in File.ReadAllLines(filePath))
        {
            // Bỏ qua dòng trống và comment
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            // Tách tại " : " đầu tiên
            int sep = line.IndexOf(" : ");
            if (sep < 0) continue;

            string key = line.Substring(0, sep).Trim();
            string val = line.Substring(sep + 3).Trim();
            _data[key] = val;
        }
    }

    // Lấy giá trị string
    public string Get(string key, string defaultValue = "")
        => _data.TryGetValue(key, out var v) ? v : defaultValue;

    // Lấy giá trị int
    public int GetInt(string key, int defaultValue = 0)
        => _data.TryGetValue(key, out var v) && int.TryParse(v, out int i) ? i : defaultValue;

    // Lấy giá trị bool
    public bool GetBool(string key, bool defaultValue = false)
        => _data.TryGetValue(key, out var v)
           ? v.Equals("true", StringComparison.OrdinalIgnoreCase)
           : defaultValue;

    // Thuộc tính tiện lợi
    public int NumberOfLines => GetInt("Number of lines", 1);
    public string CompanyName => Get("Company name", "My Company");
}
```

**Dùng trong form:**
```csharp
var settings = new SettingsReader("settings.txt");
this.Text = settings.CompanyName;
int lines = settings.NumberOfLines;
```

### Xem trong project thực tế
- File: `SharedLib/Services/SettingsReader.cs`
- Sử dụng tại: `AndonTerminal/Program.cs` → `AndonDashboard/Program.cs`

### 🏋️ Bài tập thực hành

**BT3.1** — Tạo file `myapp.txt`:
```
App title : Quản lý máy
Background color : DarkBlue
Font size : 16
```
Tạo `AppConfig.cs` đọc file này, áp dụng vào Form (đổi Title, BackColor, cỡ chữ Label).

**BT3.2** — Mở rộng: Thêm key `Alarm labels : Lỗi máy|Thiếu vật liệu|Chất lượng` — đọc rồi tạo 3 Button tương ứng từ danh sách phân cách `|`.

**BT3.3** *(Thử thách)* — Tạo "Live reload": dùng `FileSystemWatcher` theo dõi file config. Khi file thay đổi, tự động reload mà không cần restart app.

### 💡 Ví dụ cấu hình thực tế
```
# Cấu hình dây chuyền số 3
Line name : Dây chuyền Hàn
Number of stations : 8
Supervisor name : Nguyễn Văn B
Target output per hour : 120
Warning threshold minutes : 5
Critical threshold minutes : 15
```

---

## BÀI 4 — SQLite Database Cơ Bản ⭐⭐

### Bạn học được gì
- Tạo và kết nối SQLite database
- Tạo bảng, INSERT, SELECT, UPDATE cơ bản
- Đọc kết quả từ `SQLiteDataReader`
- Tránh SQL Injection bằng parameter

### Lý thuyết: Tại sao SQLite?

| | SQLite | SQL Server/MySQL |
|--|--------|-----------------|
| Cài đặt | ❌ Không cần | ✅ Cần server riêng |
| File | 1 file `.db` | Nhiều file hệ thống |
| Phù hợp | App nhỏ, desktop | Web app, multi-user lớn |
| eAndon dùng | ✅ Đúng rồi | Quá nặng |

### Thêm NuGet vào project
```bash
dotnet add package System.Data.SQLite
```

### Code mẫu — CRUD phiếu sự cố

```csharp
using System;
using System.Data.SQLite;
using System.Collections.Generic;

public class TicketDatabase
{
    private readonly string _connStr;

    public TicketDatabase(string dbFilePath)
    {
        _connStr = $"Data Source={dbFilePath};Version=3;";
        CreateTableIfNotExists();
    }

    // ── Tạo bảng nếu chưa có ──
    private void CreateTableIfNotExists()
    {
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Tickets (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                LineNumber  TEXT NOT NULL,
                AlarmType   TEXT NOT NULL,
                Status      TEXT DEFAULT 'Open',
                ReportedAt  TEXT,
                ClosedAt    TEXT
            )";
        cmd.ExecuteNonQuery();
    }

    // ── INSERT phiếu mới ──
    public int CreateTicket(string lineNumber, string alarmType)
    {
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();

        // ⚠ LUÔN dùng @parameter để tránh SQL Injection
        cmd.CommandText = @"
            INSERT INTO Tickets (LineNumber, AlarmType, ReportedAt)
            VALUES (@Line, @Type, @ReportedAt);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("@Line", lineNumber);
        cmd.Parameters.AddWithValue("@Type", alarmType);
        cmd.Parameters.AddWithValue("@ReportedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // ── SELECT lấy danh sách ──
    public List<(int id, string line, string alarm, string status)> GetOpenTickets()
    {
        var result = new List<(int, string, string, string)>();
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, LineNumber, AlarmType, Status FROM Tickets WHERE Status = 'Open'";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add((
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)
            ));
        }
        return result;
    }

    // ── UPDATE đóng phiếu ──
    public void CloseTicket(int id)
    {
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Tickets
            SET Status = 'Closed', ClosedAt = @ClosedAt
            WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@ClosedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }
}
```

**Dùng trong form:**
```csharp
var db = new TicketDatabase("Data/eandon.db");
int id = db.CreateTicket("010", "Bảo trì");   // tạo phiếu
var list = db.GetOpenTickets();                // lấy danh sách
db.CloseTicket(id);                            // đóng phiếu
```

### Xem trong project thực tế
- Schema đầy đủ: `Docs/DATABASE.md`
- Implementation: `SharedLib/Services/IncidentService.cs`

### 🏋️ Bài tập thực hành

**BT4.1** — Tạo bảng `Employees (Id, Code TEXT, Name TEXT, Department TEXT)`. Viết hàm thêm nhân viên + hiển thị danh sách lên `DataGridView`.

**BT4.2** — Thêm tìm kiếm: TextBox nhập mã NV → SELECT … WHERE Code LIKE @Search → cập nhật DataGridView.

**BT4.3** *(Thử thách)* — Tạo bảng `MachineLog (Id, MachineId, StartTime, EndTime, Status)`. Tính thời gian uptime/downtime tổng cộng mỗi máy bằng SQL aggregate.

### ⚠️ Lỗi thường gặp
```
Lỗi: "no such table: Tickets"
→ Quên gọi CreateTableIfNotExists() trước khi dùng

Lỗi: "database is locked"
→ Còn 1 connection đang mở. Dùng using { } để tự đóng
```

---

## BÀI 5 — Grid Động N×M ⭐⭐

### Bạn học được gì
- Tạo grid Button tự động từ dữ liệu (không hard-code vị trí)
- `Dictionary<string, Button>` để tra cứu nhanh
- Tính toán tọa độ `x = startX + col * (width + gap)`

### Lý thuyết: Grid eAndon

```
Grid = N hàng × M cột
  Hàng = Line sản xuất (đọc từ Workstations_terminals.txt)
  Cột  = Loại alarm (đọc từ settings.txt)
  Ô    = Button màu thay đổi theo trạng thái
```

### Code mẫu — Grid tạo tự động

```csharp
public class GridForm : Form
{
    // Lưu tất cả button: key = "row_col", value = Button
    private Dictionary<string, Button> _buttons = new Dictionary<string, Button>();

    // Tên các hàng và cột (đọc từ config hoặc DB)
    private string[] _rows = { "Line 1", "Line 2", "Line 3" };
    private string[] _cols = { "Bảo trì", "Chất lượng", "Thiếu VL" };

    // Kích thước ô
    private const int CellW = 130, CellH = 80, Gap = 4;
    private const int RowHeaderW = 90, ColHeaderH = 50;
    private const int StartX = 20, StartY = 20;

    public GridForm()
    {
        this.Text = "eAndon Grid Demo";
        this.AutoScroll = true;
        this.Size = new Size(800, 400);
        BuildGrid();
    }

    private void BuildGrid()
    {
        // ── Vẽ tiêu đề cột ──
        for (int col = 0; col < _cols.Length; col++)
        {
            int x = StartX + RowHeaderW + col * (CellW + Gap);
            var header = new Label
            {
                Text = _cols[col],
                Bounds = new Rectangle(x, StartY, CellW, ColHeaderH),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 73, 94),  // xanh đậm
                ForeColor = Color.White
            };
            this.Controls.Add(header);
        }

        // ── Vẽ hàng + ô ──
        for (int row = 0; row < _rows.Length; row++)
        {
            int y = StartY + ColHeaderH + row * (CellH + Gap);

            // Label tên hàng (tên Line)
            var rowLabel = new Label
            {
                Text = _rows[row],
                Bounds = new Rectangle(StartX, y, RowHeaderW, CellH),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White
            };
            this.Controls.Add(rowLabel);

            // Button cho mỗi ô
            for (int col = 0; col < _cols.Length; col++)
            {
                int x = StartX + RowHeaderW + col * (CellW + Gap);
                string key = $"{row}_{col}";

                var btn = new Button
                {
                    Text = "✓",
                    Bounds = new Rectangle(x, y, CellW, CellH),
                    BackColor = Color.FromArgb(46, 204, 113),  // xanh = bình thường
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Tag = key  // lưu key để biết ô nào được bấm
                };
                btn.Click += OnCellClick;
                _buttons[key] = btn;
                this.Controls.Add(btn);
            }
        }
    }

    private void OnCellClick(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        string key = btn.Tag.ToString();
        // Đổi màu khi click (demo)
        btn.BackColor = btn.BackColor == Color.FromArgb(46, 204, 113)
            ? Color.FromArgb(241, 196, 15)  // đổi sang vàng
            : Color.FromArgb(46, 204, 113); // về xanh
    }
}
```

### Xem trong project thực tế
`AndonTerminal/Forms/TerminalMainForm.cs` → hàm `InitializeUI()` và `BuildGrid()`:
```csharp
// Tạo header cột với icon
for (int col = 0; col < _alarmCount; col++)
{
    int x = startX + rowHeaderWidth + col * (cellWidth + gap);
    // ... tạo Panel header với PictureBox icon ...
}
// Tạo hàng + button
for (int row = 0; row < _workstations.Count; row++)
{
    var ws = _workstations[row];
    // ... tạo Label tên line ...
    for (int col = 0; col < _alarmCount; col++)
    {
        // ... tạo Button + lưu vào _gridCells ...
    }
}
```

### 🏋️ Bài tập thực hành

**BT5.1** — Tạo grid 4×5 Button màu xanh. Click bất kỳ ô → đổi màu random.

**BT5.2** — Đọc `Lines_stations.txt` → tạo grid từ dữ liệu thực (số hàng = số line, số cột = cố định 3).

**BT5.3** *(Thử thách)* — Thêm Label header cho cột (tên từ settings.txt) + Label cho hàng (tên Line). Khi scroll ngang, header cột vẫn cố định.

---

## BÀI 6 — Luồng Sự Cố 7 Bước ⭐⭐⭐

### Bạn học được gì
- Thiết kế luồng xử lý nhiều bước (`enum` trạng thái)
- Mở popup Form và nhận kết quả về (`ShowDialog()`)
- Ghi nhận thông tin từng bước vào model

### Lý thuyết: State Machine (Máy trạng thái)

```
Green → (bấm) → Yellow/Red → (KTV nhận) → Repairing → (KTV xong) → WaitLeader → (Leader OK) → Green
```

Đây là **State Machine**: mỗi lần tương tác → chuyển sang trạng thái tiếp theo.

### Thiết kế Model (Ticket)

```csharp
public class Ticket
{
    public string Id { get; set; }
    public string LineNumber { get; set; }
    public string AlarmType { get; set; }
    public int Status { get; set; }  // 0=Green, 1=Yellow, 2=Red, 3=Repairing, 4=WaitLeader, 5=Closed

    // Bước 1-4
    public DateTime? ReportedAt { get; set; }
    public string OperatorId { get; set; }
    public string OperatorName { get; set; }

    // Bước 5
    public DateTime? TechCheckinAt { get; set; }
    public string TechnicianId { get; set; }
    public string TechnicianName { get; set; }

    // Bước 6
    public DateTime? FixedAt { get; set; }
    public string FixNote { get; set; }

    // Bước 7
    public DateTime? LeaderConfirmedAt { get; set; }
    public string LeaderId { get; set; }

    // Tính toán
    public double? DowntimeMinutes =>
        ReportedAt.HasValue && LeaderConfirmedAt.HasValue
        ? (LeaderConfirmedAt.Value - ReportedAt.Value).TotalMinutes
        : (double?)null;
}
```

### Code mẫu — Popup nhập thông tin nhân viên

```csharp
// Form popup (dialog)
public class EmployeeInputDialog : Form
{
    public string EmployeeId { get; private set; }
    public string EmployeeName { get; private set; }

    public EmployeeInputDialog(string title)
    {
        this.Text = title;
        this.Size = new Size(320, 200);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;

        // TextBox mã NV
        var lblId = new Label { Text = "Mã NV:", Location = new Point(20, 30), AutoSize = true };
        var txtId = new TextBox { Location = new Point(100, 27), Width = 180 };

        // TextBox họ tên
        var lblName = new Label { Text = "Họ tên:", Location = new Point(20, 65), AutoSize = true };
        var txtName = new TextBox { Location = new Point(100, 62), Width = 180 };

        // Nút OK — KHÔNG đặt DialogResult trên Button,
        // để Click handler tự validate trước rồi mới đóng
        var btnOk = new Button
        {
            Text = "✓ Xác nhận",
            Location = new Point(60, 110),
            Size = new Size(100, 35)
        };
        btnOk.Click += (s, e) => {
            EmployeeId = txtId.Text.Trim();
            EmployeeName = txtName.Text.Trim();
            // Validate trước khi đóng
            if (string.IsNullOrEmpty(EmployeeId)) {
                MessageBox.Show("Vui lòng nhập Mã NV.");
                return;
            }
            // Đặt DialogResult → WinForms tự đóng form
            this.DialogResult = DialogResult.OK;
        };

        // Nút Hủy
        var btnCancel = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Location = new Point(175, 110),
            Size = new Size(70, 35)
        };

        this.Controls.AddRange(new Control[] { lblId, txtId, lblName, txtName, btnOk, btnCancel });
    }
}

// Dùng trong TerminalMainForm.cs khi click ô 🟡/🔴:
private void HandleKTVCheckin(Ticket ticket)
{
    using var dialog = new EmployeeInputDialog("KTV nhận sửa");
    if (dialog.ShowDialog(this) == DialogResult.OK)
    {
        ticket.TechnicianId = dialog.EmployeeId;
        ticket.TechnicianName = dialog.EmployeeName;
        ticket.TechCheckinAt = DateTime.Now;
        ticket.Status = 3; // Repairing
        // Cập nhật màu ô sang cam...
    }
}
```

### Xem trong project thực tế
- `AndonTerminal/Forms/EmployeeInputForm.cs` — popup nhập mã NV
- `AndonTerminal/Forms/FixCompleteForm.cs` — popup ghi chú sửa chữa
- `AndonTerminal/Forms/AlarmTypeForm.cs` — popup chọn Vàng/Đỏ
- `SharedLib/Services/IncidentService.cs` → `OpenIncident()`, `AssignTechnician()`, `MarkFixed()`, `LeaderConfirm()`

### 🏋️ Bài tập thực hành

**BT6.1** — Tạo form "Luồng 3 bước đơn giản": Báo lỗi → Nhận xử lý → Hoàn thành. Mỗi bước mở 1 dialog nhập tên người phụ trách. Log kết quả vào ListBox.

**BT6.2** — Thêm validation: Bước 2 chỉ cho phép nếu Bước 1 đã xong (disable button Nhận xử lý nếu chưa Báo lỗi).

**BT6.3** *(Thử thách)* — Lưu lịch sử tất cả luồng đã hoàn thành vào `List<Ticket>`, hiển thị lên `DataGridView` với cột "Thời gian xử lý (phút)".

---

## BÀI 7 — Services và Dependency Injection ⭐⭐⭐

### Bạn học được gì
- Tách logic vào class Service riêng (Single Responsibility)
- Truyền dependency qua constructor (Constructor Injection)
- Tại sao KHÔNG nên gọi `new Database()` ngay trong Form

### Lý thuyết: Vấn đề khi nhét hết vào Form

```csharp
// ❌ KHÔNG TỐT — Form ôm hết
public class BadForm : Form
{
    private void OnButtonClick() {
        var conn = new SQLiteConnection("...");  // logic DB trong form
        conn.Open();
        // ... 50 dòng SQL ...
    }
}

// ✅ TỐT — Tách thành Service
public class IncidentService  // class riêng, không kế thừa Form
{
    private readonly string _connStr;
    public IncidentService(string dbPath) { _connStr = $"Data Source={dbPath}"; }

    public Ticket OpenIncident(string line, string alarm) { /* SQL ở đây */ }
}

public class GoodForm : Form
{
    private readonly IncidentService _service;
    public GoodForm(IncidentService service)
    {
        _service = service;  // nhận qua constructor (DI)
    }
    private void OnButtonClick()
    {
        _service.OpenIncident("010", "Bảo trì");  // gọn, dễ test
    }
}
```

### Kiến trúc 3 lớp của eAndon

```
┌──────────────────────────────────────────────────────────┐
│  PRESENTATION LAYER (Forms)                              │
│  AndonTerminal/Forms/  và  AndonDashboard/Forms/        │
│  → Chỉ xử lý UI, gọi service, không có logic nghiệp vụ  │
├──────────────────────────────────────────────────────────┤
│  SERVICE LAYER (Services)                                │
│  SharedLib/Services/IncidentService.cs                   │
│  SharedLib/Services/DailyStatsService.cs                 │
│  SharedLib/Services/SettingsReader.cs                    │
│  → Logic nghiệp vụ, CRUD, tính toán                     │
├──────────────────────────────────────────────────────────┤
│  DATA LAYER (Database)                                   │
│  SQLite: Data/eandon.db                                  │
│  Files: Data/terminal*.txt, Logs/alarmlog_*.txt         │
│  → Lưu trữ thuần túy                                    │
└──────────────────────────────────────────────────────────┘
```

### Code mẫu — Tổ chức Service + DI

```csharp
// 1. Service tách biệt
public class MachineService
{
    private readonly string _connStr;
    private readonly ILogger _logger;  // nhận logger qua DI

    public MachineService(string dbPath, ILogger logger = null)
    {
        _connStr = $"Data Source={dbPath}";
        _logger = logger;
    }

    public int ReportBreakdown(string machineId, string reason)
    {
        // Logic thuần: lưu DB, ghi log
        _logger?.Log($"Máy {machineId} báo lỗi: {reason}");
        // ... INSERT into Breakdowns ...
        return newId;
    }
}

// 2. Wiring trong Program.cs (không phải trong Form)
static void Main()
{
    var logger = new FileLogger("Logs/");
    var machineService = new MachineService("Data/factory.db", logger);

    // Truyền service vào Form
    Application.Run(new MainForm(machineService));
}

// 3. Form chỉ nhận service qua constructor
public class MainForm : Form
{
    private readonly MachineService _machineService;

    public MainForm(MachineService service)
    {
        _machineService = service;
        InitializeUI();
    }

    private void OnReportClick(object sender, EventArgs e)
    {
        // Form chỉ gọi service, không biết SQL
        int id = _machineService.ReportBreakdown("M-001", "Motor hỏng");
        MessageBox.Show($"Đã tạo phiếu #{id}");
    }
}
```

### Xem trong project thực tế
`AndonTerminal/Program.cs`:
```csharp
// Tất cả service được tạo và "wire" ở đây
var alarmLogger = new AlarmLogger(logsDir);
var incidentService = new IncidentService(dbPath, alarmLogger);
var lineStationReader = new LineStationReader(...);

// Form nhận service qua constructor
Application.Run(new TerminalMainForm(
    settings, lineStationReader, incidentService, alarmLogger,
    terminalName, dataDir, assetsDir));
```

### 🏋️ Bài tập thực hành

**BT7.1** — Refactor BT4 của bạn: tách `TicketDatabase` thành `TicketService`, Form chỉ gọi service. Program.cs tạo và truyền service vào Form.

**BT7.2** — Tạo interface `ITicketRepository` với các method `Create`, `GetOpen`, `Close`. Implement với SQLite. Lợi ích: có thể test với `FakeRepository` không cần DB thật.

**BT7.3** *(Thử thách)* — Tạo `AlarmLogger` service ghi log: mỗi ngày 1 file `log-yyyy-MM-dd.txt`. Đảm bảo thread-safe bằng `lock`. Inject vào `TicketService`.

---

## BÀI 8 — Dashboard Realtime ⭐⭐⭐

### Bạn học được gì
- Đọc file text định kỳ (FileSystemWatcher vs Timer)
- Cập nhật UI từ background thread (`Invoke()`)
- Hiển thị dữ liệu nhiều Terminal trên 1 màn hình

### Lý thuyết: 2 cách theo dõi thay đổi

| Cách | Khi nào dùng | Ưu điểm | Nhược điểm |
|------|-------------|---------|------------|
| `Timer` (5 giây poll) | Dữ liệu ít thay đổi | Đơn giản | Chậm hơn 5s |
| `FileSystemWatcher` | File text thay đổi ngay | Real-time | Phức tạp hơn |

eAndon Dashboard dùng **cả hai**: FSW để realtime + Timer để đếm thời gian.

### Code mẫu — FileSystemWatcher đọc file terminal

```csharp
public class DashboardForm : Form
{
    private FileSystemWatcher _watcher;
    private Dictionary<string, TerminalState> _terminalData = new();

    public DashboardForm()
    {
        InitializeUI();
        StartWatching("Data/");
    }

    private void StartWatching(string dataDir)
    {
        _watcher = new FileSystemWatcher(dataDir, "*.txt")
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        // Khi file thay đổi → cập nhật UI
        _watcher.Changed += (s, e) => {
            // FSW chạy ở thread khác → phải dùng Invoke để cập nhật UI
            if (this.IsHandleCreated)
                this.Invoke((Action)(() => ReadAndUpdateFromFile(e.FullPath)));
        };
    }

    private void ReadAndUpdateFromFile(string filePath)
    {
        // Đọc file: mỗi dòng = "lineNumber;alarmType;status;ticketId;startTime"
        try
        {
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (line.StartsWith("#")) continue;
                var parts = line.Split(';');
                if (parts.Length < 3) continue;

                string lineNum = parts[0];
                int alarmIdx = int.Parse(parts[1]);
                int status = int.Parse(parts[2]);

                // Cập nhật màu button tương ứng
                UpdateCell(lineNum, alarmIdx, status);
            }
        }
        catch { /* bỏ qua nếu file đang được ghi */ }
    }

    private void UpdateCell(string lineNumber, int alarmIdx, int status)
    {
        string key = $"{lineNumber}_{alarmIdx}";
        if (_gridButtons.TryGetValue(key, out var btn))
        {
            btn.BackColor = GetStatusColor(status);
        }
    }
}
```

### Xem trong project thực tế
`AndonDashboard/Forms/DashboardMainForm.cs`:
```csharp
// FileSystemWatcher theo dõi thư mục Data/
_watcher = new FileSystemWatcher(_dataDirectory, "*.txt");
_watcher.Changed += OnDataFileChanged;
_watcher.EnableRaisingEvents = true;

private void OnDataFileChanged(object sender, FileSystemEventArgs e)
{
    // Quan trọng: cập nhật UI phải dùng Invoke (chạy trên UI thread)
    if (this.IsHandleCreated)
        this.BeginInvoke((Action)(() => RefreshFromFiles()));
}
```

### 🏋️ Bài tập thực hành

**BT8.1** — Tạo 2 form: `WriterForm` (ghi file mỗi 2 giây) và `ReaderForm` (hiển thị nội dung file realtime bằng FSW).

**BT8.2** — Mô phỏng dashboard: WriterForm có nút đổi màu 5 ô → ghi vào file. ReaderForm đọc file → hiển thị 5 ô màu tương ứng.

**BT8.3** *(Thử thách)* — Xử lý race condition: nếu file vừa được ghi xong mà bạn đọc, có thể IOException. Implement retry logic với `Thread.Sleep(100)`.

---

## BÀI 9 — Thống Kê và Analytics ⭐⭐⭐⭐

### Bạn học được gì
- Aggregate query SQL (SUM, AVG, COUNT, GROUP BY)
- Chỉ số MTTR, MTBF, Availability
- Hiển thị DataGridView với lọc + sắp xếp
- Thuật toán EWMA và Z-score cơ bản

### Lý thuyết: 3 KPI của Andon

```
MTTR (Mean Time To Repair) = Tổng thời gian sửa / Số lần sửa
  → Càng thấp càng tốt. VD: MTTR = 15 phút

MTBF (Mean Time Between Failures) = Tổng thời gian chạy / Số lần hỏng
  → Càng cao càng tốt. VD: MTBF = 8 giờ

Availability = MTBF / (MTBF + MTTR) × 100%
  → Target: ≥ 95% trong sản xuất thực tế
```

### Code mẫu — Query tính MTTR

```csharp
public class StatsService
{
    private readonly string _connStr;
    public StatsService(string dbPath)
    {
        _connStr = $"Data Source={dbPath}";
    }

    public (int incidents, double mttrMinutes, double availability)
        GetDailyStats(string lineNumber, string date)
    {
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();

        // SQL tính 3 KPI trong 1 query
        cmd.CommandText = @"
            SELECT
                COUNT(*)                              AS TotalIncidents,
                AVG((julianday(ClosedAt) - julianday(ReportedAt)) * 1440) AS AvgMinutes,
                -- Availability = 1 - (TotalDowntime / TotalShiftTime)
                (1.0 - SUM((julianday(ClosedAt) - julianday(ReportedAt)) * 1440) / 480.0) * 100 AS Avail
            FROM Tickets
            WHERE LineNumber = @Line
              AND ReportDate = @Date
              AND Status = 5";   -- 5 = Closed

        cmd.Parameters.AddWithValue("@Line", lineNumber);
        cmd.Parameters.AddWithValue("@Date", date);

        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return (
                r.GetInt32(0),
                r.IsDBNull(1) ? 0 : r.GetDouble(1),
                r.IsDBNull(2) ? 100.0 : r.GetDouble(2)
            );
        }
        return (0, 0, 100.0);
    }
}
```

### Thuật toán EWMA — Dự đoán downtime

```csharp
// EWMA: Exponentially Weighted Moving Average
// Dùng để dự đoán downtime tiếp theo dựa trên lịch sử
// α = 0.3: trọng số cho ngày gần nhất (càng gần → càng quan trọng)

public double PredictNextDowntime(List<double> history)
{
    const double alpha = 0.3;
    if (history.Count == 0) return 0;

    double ewma = history[0];  // khởi đầu = giá trị đầu tiên
    for (int i = 1; i < history.Count; i++)
        ewma = alpha * history[i] + (1 - alpha) * ewma;

    return ewma;  // dự đoán cho kỳ tiếp theo
}

// Ví dụ:
// History: [30, 45, 20, 60, 35] phút
// EWMA: 30 → 34.5 → 29.2 → 38.4 → 37.4 → Dự đoán: ~37 phút
```

### Thuật toán Z-score — Phát hiện bất thường

```csharp
// Z-score: số lần độ lệch chuẩn so với trung bình
// Z >= 2.0 → bất thường (nằm ngoài 95% phân phối bình thường)

public bool IsAnomalous(double currentValue, List<double> history)
{
    if (history.Count < 2) return false;

    double mean = history.Average();
    double std = Math.Sqrt(history.Average(x => (x - mean) * (x - mean)));
    if (std == 0) return false;

    double zScore = (currentValue - mean) / std;
    return zScore >= 2.0;  // ngưỡng 2 sigma
}

// Ví dụ:
// Lịch sử downtime: [30, 35, 28, 32, 31] → mean=31.2, std=2.4
// Hiện tại: 95 phút → Z = (95-31.2)/2.4 = 26.6 → BẤT THƯỜNG!
```

### Xem trong project thực tế
- `SharedLib/Services/Analytics/DowntimeEstimator.cs` — EWMA
- `SharedLib/Services/Analytics/AnomalyDetector.cs` — Z-score
- `SharedLib/Services/Analytics/TechnicianTracker.cs` — xếp hạng KTV
- `Docs/ANALYTICS.md` — mô tả chi tiết tất cả model

### 🏋️ Bài tập thực hành

**BT9.1** — Tạo form thống kê đơn giản: DateTimePicker chọn ngày + ComboBox chọn Line → Query DB → hiển thị số sự cố, MTTR, Availability.

**BT9.2** — Vẽ biểu đồ cơ bản: dùng `Panel.Paint` + `DrawRectangle` vẽ cột (bar chart) cho downtime theo ngày.

**BT9.3** *(Thử thách)* — Implement bảng xếp hạng KTV: query theo `TechnicianId`, tính AvgRepairTime + RepairCount, hiển thị DataGridView có nút sắp xếp.

---

## BÀI 10 — Email Automation ⭐⭐⭐⭐

### Bạn học được gì
- `System.Net.Mail` gửi email SMTP (không cần NuGet)
- Xây dựng nội dung HTML email với inline CSS
- `System.Threading.Timer` chạy tác vụ nền
- Thiết kế hệ thống cảnh báo event-driven

### Lý thuyết: 2 loại email trong eAndon

| Loại | Trigger | Người nhận | Template |
|------|---------|-----------|----------|
| Báo cáo tuần | Mỗi CN 20:00 | Ban Giám Đốc | HTML bảng thống kê |
| Cảnh báo ngay | Có sự kiện | Quản lý KTV | HTML cảnh báo màu đỏ |

### Code mẫu — Gửi email HTML đơn giản

```csharp
using System.Net;
using System.Net.Mail;

public class EmailHelper
{
    public static bool SendHtml(
        string smtpServer, int port, bool useSsl,
        string from, string password,
        string[] to, string subject, string htmlBody)
    {
        try
        {
            using var msg = new MailMessage();
            msg.From = new MailAddress(from, "eAndon System");
            foreach (var addr in to)
                msg.To.Add(addr);
            msg.Subject = subject;
            msg.Body = htmlBody;
            msg.IsBodyHtml = true;

            using var smtp = new SmtpClient(smtpServer, port)
            {
                EnableSsl = useSsl,
                Credentials = new NetworkCredential(from, password),
                Timeout = 15000  // 15 giây
            };
            smtp.Send(msg);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email lỗi: {ex.Message}");
            return false;
        }
    }
}

// Dùng:
EmailHelper.SendHtml(
    "smtp.congty.com", 587, true,
    "andon@congty.com", "matkhau",
    new[] { "giamdoc@congty.com" },
    "[eAndon] Báo cáo tuần",
    "<h1>Báo cáo tuần</h1><p>Tổng sự cố: <b>12</b></p>"
);
```

### Code mẫu — Template HTML email đơn giản

```csharp
public string BuildAlertEmail(string lineName, double waitMinutes)
{
    return $@"<!DOCTYPE html>
<html lang=""vi"">
<head><meta charset=""UTF-8"">
<style>
  body {{ font-family: Arial; padding: 20px; background: #f5f5f5; }}
  .box {{ max-width: 500px; margin: auto; background: white;
          border: 2px solid #e74c3c; border-radius: 8px; overflow: hidden; }}
  .header {{ background: #e74c3c; color: white; padding: 16px 20px; }}
  .header h2 {{ margin: 0; }}
  .body {{ padding: 20px; }}
  .time {{ font-size: 24px; font-weight: bold; color: #e74c3c; }}
</style>
</head>
<body>
  <div class=""box"">
    <div class=""header""><h2>⚠ Cảnh Báo: Chưa Có KTV Nhận</h2></div>
    <div class=""body"">
      <p>Line <strong>{lineName}</strong> đã chờ
         <span class=""time"">{waitMinutes:F0} phút</span>
         mà chưa có KTV nhận sự cố!
      </p>
      <p>Vui lòng điều phối KTV ngay.</p>
    </div>
  </div>
</body>
</html>";
}
```

### Code mẫu — Background Timer gửi email định kỳ

```csharp
public class EmailScheduler : IDisposable
{
    private System.Threading.Timer _timer;

    public void Start()
    {
        // Kiểm tra mỗi 60 giây
        _timer = new System.Threading.Timer(
            OnTick,
            null,
            TimeSpan.FromSeconds(10),    // delay lần đầu
            TimeSpan.FromSeconds(60));   // lặp mỗi 60 giây
    }

    private void OnTick(object state)
    {
        try
        {
            CheckWeeklyReport();   // gửi báo cáo tuần nếu đến giờ
            CheckRealTimeAlerts(); // kiểm tra cảnh báo real-time
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Scheduler lỗi: {ex.Message}");
        }
    }

    private void CheckWeeklyReport()
    {
        var now = DateTime.Now;
        // Gửi mỗi Chủ nhật lúc 20:00
        if (now.DayOfWeek == DayOfWeek.Sunday && now.Hour == 20)
        {
            // ... gửi email báo cáo ...
        }
    }

    public void Dispose() => _timer?.Dispose();
}
```

### Xem trong project thực tế
- `SharedLib/Services/Email/EmailSender.cs`
- `SharedLib/Services/Email/WeeklyReportBuilder.cs`
- `SharedLib/Services/Email/RealtimeAlertService.cs`
- `SharedLib/Services/Email/EmailScheduler.cs`
- `AndonDashboard/Program.cs` — khởi tạo EmailScheduler

### 🏋️ Bài tập thực hành

**BT10.1** — Gửi email báo cáo đơn giản: nội dung HTML với 1 bảng thống kê đơn giản. Test với tài khoản Gmail + App Password.

**BT10.2** — Tạo `NotificationScheduler`: mỗi ngày lúc 17:00 gửi email tóm tắt ngày (số sự cố, line nào nhiều nhất).

**BT10.3** *(Thử thách)* — Thêm cooldown: không gửi cùng 1 loại cảnh báo trong vòng 2 giờ. Dùng `Dictionary<string, DateTime>` lưu lần gửi cuối.

---

## Tổng Kết: Roadmap Học Tập

```
TUẦN 1 ── Bài 1-2: C# WinForms cơ bản
          ● Tạo form, button, label, timer
          ● Kết quả: Màn hình demo 5 màu eAndon

TUẦN 2 ── Bài 3-4: Data & Config
          ● SettingsReader + SQLite CRUD
          ● Kết quả: Mini app ghi/đọc phiếu sự cố

TUẦN 3 ── Bài 5-6: Grid + Luồng
          ● Grid động + State Machine 7 bước
          ● Kết quả: Terminal mini hoạt động được

TUẦN 4 ── Bài 7-8: Kiến trúc + Dashboard
          ● Services + DI + FileSystemWatcher
          ● Kết quả: Terminal + Dashboard liên thông

TUẦN 5 ── Bài 9-10: Nâng cao
          ● Analytics + Email automation
          ● Kết quả: Hệ thống eAndon đầy đủ!
```

---

## Tài Liệu Tham Khảo

| Tài liệu | Nội dung |
|----------|---------|
| [`BEGINNER_GUIDE.md`](BEGINNER_GUIDE.md) | Hướng dẫn chạy ngay không cần đọc code |
| [`DATABASE.md`](DATABASE.md) | Schema SQLite đầy đủ, ví dụ query |
| [`UI_CUSTOMIZE.md`](UI_CUSTOMIZE.md) | Đổi màu, kích thước, bố cục form |
| [`ANALYTICS.md`](ANALYTICS.md) | MTTR, MTBF, EWMA, Z-score chi tiết |
| [`README_FULL.md`](README_FULL.md) | Kiến trúc hệ thống tổng thể |

---

## Câu Hỏi Thường Gặp Khi Tự Build

**Q: Bắt đầu từ đâu?**  
A: Bài 1 → build 1 form chạy được → thêm dần. **Đừng đọc hết rồi mới code.**

**Q: Có cần biết Visual Studio Designer không?**  
A: **Không cần.** Tất cả UI trong eAndon được viết bằng code C# thuần, không dùng `.Designer.cs`.

**Q: Tại sao dùng `this.Invoke()` trong Dashboard?**  
A: FileSystemWatcher và Timer chạy trên thread khác. WinForms **không cho phép** cập nhật UI từ thread khác → phải dùng `Invoke()` để chuyển về UI thread.

**Q: Tại sao Terminal01 chỉ hiện 1 line?**  
A: File `Workstations_terminals.txt` gán mỗi terminal cho 1 line. Xem cột cuối:
```
0;010;Line 1;terminal01   ← chỉ terminal01 mới hiển thị line này
1;020;Line 2;terminal02   ← chỉ terminal02 mới hiển thị line này
```

**Q: Làm sao test email mà không có SMTP thật?**  
A: Dùng [Mailtrap.io](https://mailtrap.io) (free sandbox) hoặc Gmail với App Password.
