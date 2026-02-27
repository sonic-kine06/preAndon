# AndonTerminal — Terminal sản xuất

Đây là ứng dụng **WinForms** mà Operator/KTV/Leader dùng tại mỗi máy tính trên sàn sản xuất.

---

## Mỗi Terminal hiển thị gì?

Mỗi instance Terminal chỉ hiển thị **Line được phân công** cho nó trong `Assets/Workstations_terminals.txt`.

```
Ví dụ Workstations_terminals.txt (1 terminal = 1 line):
  0;010;Line 1;terminal01   ← terminal01 phụ trách Line 010
  1;020;Line 2;terminal02   ← terminal02 phụ trách Line 020
  2;030;Line 3;terminal03   ← terminal03 phụ trách Line 030
  3;040;Line 4;terminal04   ← terminal04 phụ trách Line 040
  4;050;Line 5;terminal05   ← terminal05 phụ trách Line 050
  5;060;Line 6;terminal06   ← terminal06 phụ trách Line 060
```

Khi chạy `AndonTerminal.exe terminal01`, form chỉ hiển thị Line 1 (1 hàng duy nhất).  
*(So sánh không phân biệt chữ hoa/thường — `terminal01` và `Terminal01` đều khớp.)*

> 💡 **Nếu cần 1 terminal phụ trách nhiều line** (ví dụ giám sát 2 line từ 1 màn hình),
> chỉ cần gán cùng tên terminal cho nhiều dòng trong file:
> ```
> 0;010;Line 1;terminal01
> 1;020;Line 2;terminal01   ← cùng terminal01 → hiển thị cả 2 hàng
> ```

---

## Phần A: Program.cs — Entry Point

`Program.cs` là file đầu tiên chạy khi bạn bấm F5 hoặc chạy `.exe`. Hãy đọc từng dòng:

```csharp
[STAThread]
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
```

**`[STAThread]`** — Attribute bắt buộc với WinForms. "STA" = Single Thread Apartment. COM components trong Windows (gồm cả UI) yêu cầu thread này. Bạn không cần hiểu sâu — chỉ cần biết: thiếu dòng này → WinForms không chạy được.  
**`Application.EnableVisualStyles()`** — Kích hoạt theme Windows hiện đại (nút bóng, gradient). Thiếu dòng này → UI trông như Windows 95.  
**`Application.SetCompatibleTextRenderingDefault(false)`** — Dùng `GDI+` render text thay vì GDI cũ. Text sắc nét hơn.

```csharp
    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    string assetsDir = Path.Combine(baseDir, "..", "..", "..", "..", "Assets");
```

**`AppDomain.CurrentDomain.BaseDirectory`** — Thư mục chứa file `.exe` đang chạy. Ví dụ: `C:\...\AndonTerminal\bin\Debug\net8.0-windows\`.  
**`Path.Combine(baseDir, "..", "..", "..", "..", "Assets")`** — Lùi 4 cấp từ `bin\Debug\net8.0-windows\` để lên thư mục gốc project, rồi vào `Assets\`. Tương tự VB.NET `Path.Combine(baseDir, "..\..\..\..\Assets")` — nhưng viết kiểu mảng để dễ đọc.

```csharp
    var settings = new SettingsReader(settingsPath);
    var alarmLogger = new AlarmLogger(logsDir);
    var incidentService = new IncidentService(dbPath, alarmLogger);
    var lineStationReader = new LineStationReader(...);
```

Đây là **Dependency Injection** (tiêm phụ thuộc) thủ công: tạo các service ở `Main()` rồi truyền vào Form. Lợi ích: Form không cần biết file settings.txt ở đâu — chỉ nhận `settings` object đã sẵn sàng.

```csharp
    string terminalName = "terminal01";
    var args = Environment.GetCommandLineArgs();
    if (args.Length > 1) terminalName = args[1];

    Application.Run(new TerminalMainForm(...));
```

**`Environment.GetCommandLineArgs()`** — Đọc tham số dòng lệnh. `args[0]` = tên chương trình, `args[1]` = tham số đầu tiên (tên terminal).  
**`Application.Run(...)`** — Khởi chạy form và vòng lặp xử lý message Windows. Dòng này **không return** cho đến khi người dùng đóng cửa sổ.

**So sánh C# vs VB.NET — Main**:

| C# | VB.NET |
|----|--------|
| `[STAThread]` + `static void Main()` | Form chính được cấu hình trong `My.Application` |
| `Application.Run(new MainForm())` | `Application.Run(New MainForm())` (gần giống) |
| `Environment.GetCommandLineArgs()` | `My.Application.CommandLineArgs` |

---

## Phần B: WinForms cơ bản — Các khái niệm qua ví dụ trong project

### Form là gì?

Mỗi cửa sổ trong WinForms là 1 **Form** — class kế thừa từ `System.Windows.Forms.Form`.

```csharp
public partial class TerminalMainForm : Form
{
    public TerminalMainForm(SettingsReader settings, ...) : base()
    {
        InitializeComponent();   // Tạo controls
        InitializeUI();          // Layout tùy chỉnh
    }
}
```

Trong eAndon, `InitializeUI()` tự tạo toàn bộ giao diện bằng code (không dùng Designer `.resx`). Cách này tốt hơn cho hệ thống phải tạo UI động (số hàng/cột thay đổi theo file cấu hình).

**So sánh**: Trong VB.NET Form cũng tương tự — có `InitializeComponent()` và các control. Cú pháp gần như giống hệt.

### Button, Label, Panel, Timer

| Control | Dùng trong project | Ví dụ |
|---------|-------------------|-------|
| `Button` | Mỗi ô màu trong grid là 1 Button | `var btn = new Button { BackColor = Color.Green }` |
| `Label` | Tên Line, đồng hồ, trạng thái | `var lbl = new Label { Text = "Line 1" }` |
| `Panel` | Container chứa nhiều control | `var panel = new Panel { Dock = DockStyle.Fill }` |
| `Timer` | Đếm thời gian sự cố, cập nhật đồng hồ | `var timer = new Timer { Interval = 1000 }` |
| `DataGridView` | Bảng thống kê trong StatisticsForm | `var grid = new DataGridView()` |

**Timer trong eAndon**:
```csharp
var timer = new Timer();
timer.Interval = 1000;     // Mỗi 1000ms = 1 giây
timer.Tick += Timer_Tick;  // Đăng ký event handler
timer.Start();

void Timer_Tick(object sender, EventArgs e)
{
    lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
    UpdateAllCellTimers();  // Cập nhật bộ đếm giờ cho mỗi ô đang lỗi
}
```

### Event Handling — Tại sao WinForms dùng event

WinForms dùng **event-driven programming**: code KHÔNG chạy liên tục, chỉ chạy khi có sự kiện (click, timer tick, form load, v.v.).

```csharp
// Đăng ký: "khi btn bị click, gọi hàm Btn_Click"
btn.Click += Btn_Click;

// Handler: chạy khi sự kiện xảy ra
void Btn_Click(object sender, EventArgs e)
{
    var clickedBtn = (Button)sender;  // sender = control nào phát sinh sự kiện
    HandleCellClick(clickedBtn);
}
```

**So sánh C# vs VB.NET — Events**:

| C# | VB.NET |
|----|--------|
| `btn.Click += Btn_Click;` | `AddHandler btn.Click, AddressOf Btn_Click` |
| `void Btn_Click(object sender, EventArgs e)` | `Sub Btn_Click(sender As Object, e As EventArgs)` |
| `var btn = (Button)sender;` | `Dim btn = DirectCast(sender, Button)` |

**Sự kiện Load**: Chạy khi Form hiển thị lần đầu:
```csharp
this.Load += Form_Load;

void Form_Load(object sender, EventArgs e)
{
    // Khởi tạo grid, load dữ liệu ban đầu
    RefreshGrid();
}
```

---

## Phần C: Luồng 7 bước — Chi tiết từng bước có code

### Tổng quan

```
[Bước 1] Operator click ô XANH
    → [Bước 2] StationSelectForm (chọn trạm — chỉ hiện nếu Line có > 1 trạm)
    → [Bước 3] AlarmTypeForm (chọn Yellow hoặc Red)
    → [Bước 4] EmployeeInputForm (nhập Mã + Tên Operator)
    → Ô đổi màu 🟡/🔴, bắt đầu đếm giờ, ghi SQLite + log file

[Bước 5] KTV click ô 🟡/🔴
    → EmployeeInputForm (nhập Mã + Tên KTV)
    → Ô đổi màu 🟠 (Repairing)

[Bước 6] KTV click ô 🟠
    → FixCompleteForm (ghi chú sửa chữa)
    → Ô đổi màu 🔵 (WaitLeader)

[Bước 7] Leader click ô 🔵
    → EmployeeInputForm (nhập Mã + Tên Leader)
    → Ô về 🟢, ticket đóng, cập nhật DailyStats
```

### Bước 1: Operator click ô XANH

```csharp
void CellButton_Click(object sender, EventArgs e)
{
    var btn = (Button)sender;
    var cellData = (CellData)btn.Tag;  // Tag lưu dữ liệu gắn với ô này
    var ticket = cellData.ActiveTicket;

    switch (ticket?.CurrentStatus ?? TicketStatus.Green)
    {
        case TicketStatus.Green:
            HandleGreenClick(cellData);  // Bước 1-4
            break;
        case TicketStatus.Yellow:
        case TicketStatus.Red:
            HandleReportedClick(ticket); // Bước 5
            break;
        case TicketStatus.Repairing:
            HandleRepairingClick(ticket); // Bước 6
            break;
        case TicketStatus.WaitLeader:
            HandleWaitLeaderClick(ticket); // Bước 7
            break;
    }
}
```

**`btn.Tag`**: Mỗi control WinForms có property `Tag` kiểu `object` — dùng để lưu dữ liệu tuỳ ý. Ở đây dùng để biết ô nào ứng với Line/Alarm nào.

### Bước 2: Chọn trạm (nếu có nhiều trạm)

```csharp
void HandleGreenClick(CellData cellData)
{
    var stations = _lineStationReader.GetStationsForLine(cellData.LineNumber);

    StationInfo selectedStation;
    if (stations.Count <= 1)
    {
        // Chỉ có 1 trạm → bỏ qua popup, dùng trạm duy nhất
        selectedStation = stations.FirstOrDefault() ?? DefaultStation(cellData);
    }
    else
    {
        // Nhiều trạm → hiện popup
        using var form = new StationSelectForm(stations, cellData.LineName);
        if (form.ShowDialog() != DialogResult.OK) return;  // Người dùng Cancel
        selectedStation = form.SelectedStation;
    }
    // Tiếp tục bước 3...
}
```

**`form.ShowDialog()`**: Mở form dạng **modal** (phải đóng trước khi tiếp tục). Trả về `DialogResult.OK` (người dùng nhấn OK/Confirm) hoặc `DialogResult.Cancel` (nhấn Hủy). Trong VB.NET tương tự: `form.ShowDialog()`.

**Trường hợp**: 
- Line chỉ có 1 station → bỏ qua StationSelectForm, tiếp tục ngay
- Operator nhấn Cancel → `return` → không làm gì thêm, ô vẫn xanh

### Bước 3-4: Chọn mức độ + Nhập thông tin

```csharp
// Bước 3: Chọn Yellow hoặc Red
using var alarmForm = new AlarmTypeForm(_settings);
if (alarmForm.ShowDialog() != DialogResult.OK) return;
string severity = alarmForm.SelectedSeverity;  // "Yellow" hoặc "Red"

// Bước 4: Nhập thông tin Operator
using var empForm = new EmployeeInputForm("Nhập thông tin Operator");
if (empForm.ShowDialog() != DialogResult.OK) return;

// Tạo ticket
var ticket = _incidentService.OpenTicket(
    cellData.LineNumber, cellData.LineName,
    selectedStation.StationId, selectedStation.StationName,
    cellData.AlarmTypeIndex, cellData.AlarmTypeName,
    severity, empForm.EmployeeId, empForm.EmployeeName);

// Cập nhật giao diện
UpdateCellAppearance(btn, ticket);
StartCellTimer(cellData, ticket);
```

### Bước 5-7: KTV và Leader

```csharp
// Bước 5: KTV nhận sửa
void HandleReportedClick(IncidentTicket ticket)
{
    using var empForm = new EmployeeInputForm("Nhập thông tin KTV");
    if (empForm.ShowDialog() != DialogResult.OK) return;
    _incidentService.TechCheckIn(ticket.TicketId, empForm.EmployeeId, empForm.EmployeeName);
    UpdateGrid();
}

// Bước 6: KTV hoàn thành
void HandleRepairingClick(IncidentTicket ticket)
{
    using var fixForm = new FixCompleteForm();
    if (fixForm.ShowDialog() != DialogResult.OK) return;
    _incidentService.CompleteRepair(ticket.TicketId, fixForm.FixNote);
    UpdateGrid();
}

// Bước 7: Leader xác nhận
void HandleWaitLeaderClick(IncidentTicket ticket)
{
    if (!_settings.RequireLeaderConfirmation)
    {
        _incidentService.LeaderConfirm(ticket.TicketId, "AUTO", "Auto");
        return;
    }
    using var empForm = new EmployeeInputForm("Nhập thông tin Leader");
    if (empForm.ShowDialog() != DialogResult.OK) return;
    _incidentService.LeaderConfirm(ticket.TicketId, empForm.EmployeeId, empForm.EmployeeName);
    UpdateGrid();
}
```

**Trường hợp đặc biệt**:
- Operator nhấn Cancel ở bất kỳ bước nào → `return` → ô không thay đổi
- KTV không xác nhận → ticket vẫn ở Yellow/Red
- Leader không xác nhận → ticket vẫn ở Blue (WaitLeader)
- `RequireLeaderConfirmation = false` → Bước 7 tự động, không cần bấm

---

## Cách chạy một Terminal

### Cách 1 — Visual Studio
```
Chuột phải AndonTerminal → Properties → Debug → Command line arguments: terminal01
F5 để chạy
```

### Cách 2 — Command line
```bash
cd AndonTerminal/bin/Debug/net8.0-windows/
AndonTerminal.exe terminal01   # Line 1
AndonTerminal.exe terminal02   # Line 2
```

### Cách 3 — dotnet run
```bash
dotnet run --project AndonTerminal -- terminal01
dotnet run --project AndonTerminal -- terminal02
```

---

## Cấu hình thêm Terminal mới

**Bước 1**: Thêm Lines vào `Assets/Workstations_terminals.txt`:
```
# Thêm terminal04 phụ trách Line 7 và Line 8
6;070;Line 7;terminal04
7;080;Line 8;terminal04
```
*(Cập nhật số đếm dòng đầu file: `6` → `8`)*

**Bước 2**: Chạy thêm 1 instance:
```bash
AndonTerminal.exe terminal04
```

**Không cần sửa code, không cần rebuild** — chỉ sửa file txt.

---

## Cấu trúc thư mục

```
AndonTerminal/
├── Program.cs               ← Entry point: đọc settings + khởi tạo services
├── AndonTerminal.csproj     ← Project file (.NET 8 WinForms)
├── Forms/
│   ├── TerminalMainForm.cs  ← Form chính: grid Lines × Alarm Types
│   ├── StationSelectForm.cs ← Popup bước 2: chọn trạm bị lỗi
│   ├── AlarmTypeForm.cs     ← Popup bước 3: chọn mức độ Yellow/Red
│   ├── EmployeeInputForm.cs ← Popup bước 4/5/7: nhập Mã NV + Tên
│   └── FixCompleteForm.cs   ← Popup bước 6: ghi chú sửa chữa
└── README.md                ← (file này)
```

---

## Copy ra nhiều Terminal — kết nối Dashboard có thay đổi không?

**KHÔNG thay đổi gì cả.** Dashboard tự động đọc **tất cả file** `Data/*.txt`.

Mỗi Terminal ghi 1 file riêng:
```
terminal01.exe → ghi Data/terminal01.txt
terminal02.exe → ghi Data/terminal02.txt
terminal03.exe → ghi Data/terminal03.txt
```

Dashboard dùng `FileSystemWatcher` theo dõi thư mục `Data/` — khi **bất kỳ** file `.txt` nào thay đổi, Dashboard tự cập nhật ngay lập tức. **Không cần cấu hình thêm gì.**

```
Sơ đồ kết nối:
  terminal01.exe  →  Data/terminal01.txt  ─┐
  terminal02.exe  →  Data/terminal02.txt  ─┼──►  AndonDashboard.exe
  terminal03.exe  →  Data/terminal03.txt  ─┘      (đọc toàn bộ Data/*.txt)
```

---

## Nguồn icon/ảnh

| File | Nguồn | License |
|------|-------|---------|
| `Assets/app.ico` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/Icon1-5.png` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/alarm.wav` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/logo.png` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |

Xem `Assets/NOTICE.txt` để biết đầy đủ nội dung MIT License.

Đây là ứng dụng **WinForms** mà Operator/KTV/Leader dùng tại mỗi máy tính trên sàn sản xuất.

---

## Mỗi Terminal hiển thị gì?

Mỗi instance Terminal chỉ hiển thị **Line được phân công** cho nó trong `Assets/Workstations_terminals.txt`.

```
Ví dụ Workstations_terminals.txt (1 terminal = 1 line):
  0;010;Line 1;terminal01   ← terminal01 phụ trách Line 010
  1;020;Line 2;terminal02   ← terminal02 phụ trách Line 020
  2;030;Line 3;terminal03   ← terminal03 phụ trách Line 030
  3;040;Line 4;terminal04   ← terminal04 phụ trách Line 040
  4;050;Line 5;terminal05   ← terminal05 phụ trách Line 050
  5;060;Line 6;terminal06   ← terminal06 phụ trách Line 060
```

Khi chạy `AndonTerminal.exe terminal01`, form chỉ hiển thị Line 1 (1 hàng duy nhất).  
*(So sánh không phân biệt chữ hoa/thường — `terminal01` và `Terminal01` đều khớp.)*

> 💡 **Nếu cần 1 terminal phụ trách nhiều line** (ví dụ giám sát 2 line từ 1 màn hình),
> chỉ cần gán cùng tên terminal cho nhiều dòng trong file:
> ```
> 0;010;Line 1;terminal01
> 1;020;Line 2;terminal01   ← cùng terminal01 → hiển thị cả 2 hàng
> ```

---

## Cách chạy một Terminal

### Cách 1 — Visual Studio
```
Chuột phải AndonTerminal → Properties → Debug → Command line arguments: terminal01
F5 để chạy
```

### Cách 2 — Command line
```bash
cd AndonTerminal/bin/Debug/net8.0-windows/
AndonTerminal.exe terminal01   # Line 1
AndonTerminal.exe terminal02   # Line 2
AndonTerminal.exe terminal03   # Line 3
# ... mở 6 cửa sổ riêng biệt cho 6 terminal
```

### Cách 3 — dotnet run
```bash
dotnet run --project AndonTerminal -- terminal01
dotnet run --project AndonTerminal -- terminal02
dotnet run --project AndonTerminal -- terminal06
```

---

## Copy ra nhiều Terminal — kết nối Dashboard có thay đổi không?

**KHÔNG thay đổi gì cả.** Dashboard tự động đọc **tất cả file** `Data/*.txt`.

Mỗi Terminal ghi 1 file riêng:
```
terminal01.exe → ghi Data/terminal01.txt
terminal02.exe → ghi Data/terminal02.txt
terminal03.exe → ghi Data/terminal03.txt
```

Dashboard dùng `FileSystemWatcher` theo dõi thư mục `Data/` — khi **bất kỳ** file `.txt` nào thay đổi, Dashboard tự cập nhật ngay lập tức. **Không cần cấu hình thêm gì.**

```
Sơ đồ kết nối:
  terminal01.exe  →  Data/terminal01.txt  ─┐
  terminal02.exe  →  Data/terminal02.txt  ─┼──►  AndonDashboard.exe
  terminal03.exe  →  Data/terminal03.txt  ─┘      (đọc toàn bộ Data/*.txt)
```

---

## Cấu hình thêm Terminal mới

**Bước 1**: Thêm Lines vào `Assets/Workstations_terminals.txt`:
```
# Thêm terminal04 phụ trách Line 7 và Line 8
6;070;Line 7;terminal04
7;080;Line 8;terminal04
```
*(Cập nhật số đếm dòng đầu file: `6` → `8`)*

**Bước 2**: Chạy thêm 1 instance:
```bash
AndonTerminal.exe terminal04
```

**Không cần sửa code, không cần rebuild** — chỉ sửa file txt.

---

## Cấu trúc thư mục

```
AndonTerminal/
├── Program.cs               ← Entry point: đọc settings + khởi tạo services
├── AndonTerminal.csproj     ← Project file (.NET 8 WinForms)
├── Forms/
│   ├── TerminalMainForm.cs  ← Form chính: grid Lines × Alarm Types
│   ├── StationSelectForm.cs ← Popup bước 2: chọn trạm bị lỗi
│   ├── AlarmTypeForm.cs     ← Popup bước 3: chọn mức độ Yellow/Red
│   ├── EmployeeInputForm.cs ← Popup bước 4/5/7: nhập Mã NV + Tên
│   └── FixCompleteForm.cs   ← Popup bước 6: ghi chú sửa chữa
└── README.md                ← (file này)
```

---

## Luồng 7 bước trong TerminalMainForm.cs

```
[Bước 1] Operator click ô XANH
    → [Bước 2] StationSelectForm (chọn trạm — chỉ hiện nếu Line có > 1 trạm)
    → [Bước 3] AlarmTypeForm (chọn Yellow hoặc Red)
    → [Bước 4] EmployeeInputForm (nhập Mã + Tên Operator)
    → Ô đổi màu 🟡/🔴, bắt đầu đếm giờ, ghi SQLite + log file

[Bước 5] KTV click ô 🟡/🔴
    → EmployeeInputForm (nhập Mã + Tên KTV)
    → Ô đổi màu 🟠 (Repairing)

[Bước 6] KTV click ô 🟠
    → FixCompleteForm (ghi chú sửa chữa)
    → Ô đổi màu 🔵 (WaitLeader)

[Bước 7] Leader click ô 🔵
    → EmployeeInputForm (nhập Mã + Tên Leader)
    → Ô về 🟢, ticket đóng, cập nhật DailyStats
```

---

## Nguồn icon/ảnh

| File | Nguồn | License |
|------|-------|---------|
| `Assets/app.ico` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/Icon1-5.png` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/alarm.wav` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/logo.png` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |

Xem `Assets/NOTICE.txt` để biết đầy đủ nội dung MIT License.
