# So sánh C# và VB.NET — Dựa trên code eAndon

> **Dành cho**: Người đã biết VB.NET, muốn hiểu nhanh C# mà không cần học lại từ đầu.  
> **Nguyên tắc**: Mỗi so sánh đều có ví dụ code **thực tế từ project eAndon** — không có Hello World.

---

## 1. Khai báo biến

| C# (eAndon) | VB.NET tương đương |
|-------------|-------------------|
| `var settings = new SettingsReader(path)` | `Dim settings As New SettingsReader(path)` |
| `string terminalName = "terminal01"` | `Dim terminalName As String = "terminal01"` |
| `int count = 0` | `Dim count As Integer = 0` |
| `bool isActive = true` | `Dim isActive As Boolean = True` |
| `IncidentTicket ticket = null` | `Dim ticket As IncidentTicket = Nothing` |

**Ghi chú**: `var` trong C# = trình biên dịch tự suy ra kiểu. Giống `Dim` nhưng không cần ghi tên kiểu. Chỉ dùng được khi có giá trị khởi tạo ngay.

---

## 2. Property

**C# trong `StationInfo.cs`**:
```csharp
public class StationInfo
{
    public string StationId { get; set; }
    public string StationName { get; set; }
    public string LineNumber { get; set; }
}
```

**VB.NET tương đương**:
```vb
Public Class StationInfo
    Public Property StationId As String
    Public Property StationName As String
    Public Property LineNumber As String
End Class
```

**Property chỉ đọc (C# trong `IncidentTicket.cs`)**:
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

**VB.NET**:
```vb
Public ReadOnly Property DowntimeSeconds As Double?
    Get
        If ReportedAt.HasValue AndAlso LeaderConfirmedAt.HasValue Then
            Return (LeaderConfirmedAt.Value - ReportedAt.Value).TotalSeconds
        End If
        Return Nothing
    End Get
End Property
```

**Arrow property (C#) — ngắn gọn hơn**:
```csharp
// C# — SettingsReader.cs
public int NumberOfAlarmTypes => GetInt("Number of alarm types to display", 3);
```

```vb
' VB.NET — không có cú pháp tương đương ngắn gọn như vậy
Public ReadOnly Property NumberOfAlarmTypes As Integer
    Get
        Return GetInt("Number of alarm types to display", 3)
    End Get
End Property
```

---

## 3. Method (Hàm/Thủ tục)

| C# (eAndon) | VB.NET |
|-------------|--------|
| `public void Load()` | `Public Sub Load()` |
| `public string Get(string key)` | `Public Function Get(key As String) As String` |
| `public int GetInt(string key, int def)` | `Public Function GetInt(key As String, def As Integer) As Integer` |
| `private void InitializeDatabase()` | `Private Sub InitializeDatabase()` |
| `static void Main()` | `Sub Main()` |

**C# — không phân biệt `Sub` và `Function`**: dùng `void` cho hàm không trả về.

---

## 4. If / Else

**C# trong `SettingsReader.cs`**:
```csharp
if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
    continue;
```

**VB.NET**:
```vb
If String.IsNullOrWhiteSpace(line) OrElse line.TrimStart().StartsWith("#") Then
    Continue For
End If
```

| C# | VB.NET |
|----|--------|
| `\|\|` (OR ngắn mạch) | `OrElse` |
| `&&` (AND ngắn mạch) | `AndAlso` |
| `!condition` | `Not condition` |
| `!=` | `<>` |
| `continue` | `Continue For` / `Continue While` |

---

## 5. For / Foreach

**C# trong `SettingsReader.cs`**:
```csharp
foreach (var line in File.ReadAllLines(FilePath))
{
    // xử lý từng dòng
}
```

**VB.NET**:
```vb
For Each line As String In File.ReadAllLines(FilePath)
    ' xử lý từng dòng
Next
```

**C# for thường**:
```csharp
// SettingsReader.GetAllAlarmLabels()
for (int i = 1; i <= NumberOfAlarmTypes; i++)
    labels.Add(GetAlarmLabel(i));
```

**VB.NET**:
```vb
For i As Integer = 1 To NumberOfAlarmTypes
    labels.Add(GetAlarmLabel(i))
Next
```

---

## 6. Try / Catch

**C# trong `IncidentService.cs`**:
```csharp
try
{
    using var conn = new SQLiteConnection($"Data Source={_dbPath}");
    conn.Open();
    // ...
}
catch (SQLiteException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
    throw;
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
finally
{
    // Luôn chạy dù có lỗi hay không
}
```

**VB.NET**:
```vb
Try
    Dim conn As New SQLiteConnection($"Data Source={_dbPath}")
    conn.Open()
    ' ...
Catch ex As SQLiteException
    Console.WriteLine($"Database error: {ex.Message}")
    Throw
Catch ex As Exception
    Console.WriteLine($"Unexpected error: {ex.Message}")
Finally
    ' Luôn chạy dù có lỗi hay không
End Try
```

---

## 7. Event Handling

**C# trong `TerminalMainForm.cs`**:
```csharp
// Đăng ký event
btn.Click += CellButton_Click;
timer.Tick += Timer_Tick;
watcher.Changed += Watcher_Changed;

// Handler
private void CellButton_Click(object sender, EventArgs e)
{
    var btn = (Button)sender;
    // ...
}

private void Timer_Tick(object sender, EventArgs e)
{
    lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
}
```

**VB.NET**:
```vb
' Đăng ký event
AddHandler btn.Click, AddressOf CellButton_Click
AddHandler timer.Tick, AddressOf Timer_Tick

' Handler
Private Sub CellButton_Click(sender As Object, e As EventArgs)
    Dim btn As Button = DirectCast(sender, Button)
    ' ...
End Sub
```

**Lambda event handler — C# ngắn gọn hơn**:
```csharp
// C#
btn.Click += (s, e) => HandleCellClick((Button)s);
```
```vb
' VB.NET
AddHandler btn.Click, Sub(s, e) HandleCellClick(DirectCast(s, Button))
```

---

## 8. String Interpolation

| C# | VB.NET |
|----|--------|
| `$"TKT-{date:yyyyMMdd}-{random}"` | `$"TKT-{date.ToString("yyyyMMdd")}-{random}"` |
| `$"Data Source={_dbPath}"` | `$"Data Source={_dbPath}"` (giống nhau!) |
| `$"{StationId} - {StationName}"` | `$"{StationId} - {StationName}"` (giống nhau!) |

**C# có thể format trực tiếp**:
```csharp
// C# — có thể ghi format trực tiếp trong { }
$"{DateTime.Now:yyyy-MM-dd HH:mm:ss}"
$"{value:F2}"   // 2 chữ số thập phân
```

```vb
' VB.NET — phải gọi .ToString()
$"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}"
$"{value.ToString("F2")}"
```

---

## 9. Dictionary và List

**C# trong `SettingsReader.cs`**:
```csharp
// Khai báo
private readonly Dictionary<string, string> _settings
    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

// Thêm/cập nhật
_settings[key] = value;

// Đọc an toàn
if (_settings.TryGetValue(key, out string val))
    return val;
return defaultValue;

// Kiểm tra có key không
if (_settings.ContainsKey("Number of alarm types"))
    // ...
```

**VB.NET**:
```vb
' Khai báo
Private ReadOnly _settings As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

' Thêm/cập nhật
_settings(key) = value

' Đọc an toàn
Dim val As String = ""
If _settings.TryGetValue(key, val) Then
    Return val
End If
Return defaultValue
```

**List trong `SharedLib`**:
```csharp
// C#
var labels = new List<string>();
labels.Add(GetAlarmLabel(i));
return labels;
```

```vb
' VB.NET
Dim labels As New List(Of String)()
labels.Add(GetAlarmLabel(i))
Return labels
```

---

## 10. LINQ

**C# trong `TerminalMainForm.cs`**:
```csharp
// Lọc workstations theo terminal
var myWorkstations = reader.GetWorkstations()
    .Where(w => w.Terminal.Equals(terminalName, StringComparison.OrdinalIgnoreCase))
    .ToList();

// Lấy station đầu tiên
var first = stations.FirstOrDefault();

// Kiểm tra có bao nhiêu station
int count = stations.Count(s => s.LineNumber == "010");
```

**VB.NET tương đương**:
```vb
' Lọc
Dim myWorkstations = reader.GetWorkstations() _
    .Where(Function(w) w.Terminal.Equals(terminalName, StringComparison.OrdinalIgnoreCase)) _
    .ToList()

' Lấy đầu tiên
Dim first = stations.FirstOrDefault()

' Đếm
Dim count = stations.Count(Function(s) s.LineNumber = "010")
```

---

## 11. using / Imports và namespace

| Khái niệm | C# | VB.NET |
|-----------|----|----|
| Import namespace | `using System.IO;` | `Imports System.IO` |
| Khai báo namespace | `namespace SharedLib.Services { }` | `Namespace SharedLib.Services` ... `End Namespace` |
| using resource (tự đóng) | `using var conn = new SQLiteConnection(...)` | `Using conn As New SQLiteConnection(...)` ... `End Using` |

**`using` tự đóng resource — C# trong `IncidentService.cs`**:
```csharp
using var conn = new SQLiteConnection($"Data Source={_dbPath}");
conn.Open();
// ... dùng conn ...
// conn.Dispose() tự gọi khi ra khỏi scope
```

**VB.NET**:
```vb
Using conn As New SQLiteConnection($"Data Source={_dbPath}")
    conn.Open()
    ' ... dùng conn ...
End Using  ' conn.Dispose() tự gọi ở đây
```

---

## 12. Nullable types

**C# trong `IncidentTicket.cs`**:
```csharp
public DateTime? ReportedAt { get; set; }     // Nullable DateTime
public string TechnicianId { get; set; }       // string mặc định nullable trong C#
public double? DowntimeSeconds { get; }        // Nullable double

// Kiểm tra null
if (ReportedAt.HasValue)
    var dt = ReportedAt.Value;

// Null-coalescing
string name = ticket.OperatorName ?? "Chưa có";

// Null-conditional
int len = ticket.FixNote?.Length ?? 0;
```

**VB.NET**:
```vb
Public Property ReportedAt As DateTime?       ' Nullable DateTime
Public Property TechnicianId As String        ' string mặc định nullable

' Kiểm tra null
If ReportedAt.HasValue Then
    Dim dt = ReportedAt.Value
End If

' Null-coalescing
Dim name = If(ticket.OperatorName, "Chưa có")

' Null-conditional
Dim len = If(ticket.FixNote?.Length, 0)
```

---

## 13. Enum

**C# trong `IncidentTicket.cs`**:
```csharp
public enum TicketStatus
{
    Green = 0,
    Yellow = 1,
    Red = 2,
    Repairing = 3,
    WaitLeader = 4,
    Closed = 5
}

// Dùng
var status = TicketStatus.Yellow;
int statusInt = (int)status;              // Cast về int: 1
TicketStatus s = (TicketStatus)1;         // Cast từ int: Yellow
```

**VB.NET**:
```vb
Public Enum TicketStatus
    Green = 0
    Yellow = 1
    Red = 2
    Repairing = 3
    WaitLeader = 4
    Closed = 5
End Enum

' Dùng
Dim status = TicketStatus.Yellow
Dim statusInt As Integer = CInt(status)
Dim s As TicketStatus = CType(1, TicketStatus)
```

---

## 14. Class kế thừa và Form

**C# trong `TerminalMainForm.cs`**:
```csharp
public partial class TerminalMainForm : Form
{
    private readonly SettingsReader _settings;

    public TerminalMainForm(SettingsReader settings, ...) : base()
    {
        _settings = settings;
        InitializeComponent();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        // cleanup
    }
}
```

**VB.NET**:
```vb
Public Class TerminalMainForm
    Inherits Form

    Private ReadOnly _settings As SettingsReader

    Public Sub New(settings As SettingsReader, ...)
        MyBase.New()
        _settings = settings
        InitializeComponent()
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        ' cleanup
    End Sub
End Class
```

---

## 15. Từ khóa hay gặp — bảng tra nhanh

| Khái niệm | C# | VB.NET |
|-----------|----|----|
| Class không kế thừa được | `sealed class` | `NotInheritable Class` |
| Method không override được | `sealed override void X()` | `NotOverridable Overrides Sub X()` |
| Method abstract | `abstract void X()` | `MustOverride Sub X()` |
| Method virtual | `virtual void X()` | `Overridable Sub X()` |
| This (tham chiếu bản thân) | `this` | `Me` |
| Base class | `base.Method()` | `MyBase.Method()` |
| Kiểm tra kiểu | `obj is Button` | `TypeOf obj Is Button` |
| Cast kiểu | `(Button)obj` | `DirectCast(obj, Button)` |
| Cast an toàn | `obj as Button` | `TryCast(obj, Button)` |
| In ra console | `Console.WriteLine(...)` | `Console.WriteLine(...)` (giống nhau) |
| Comment 1 dòng | `// comment` | `' comment` |
| Comment nhiều dòng | `/* comment */` | Không có, dùng nhiều dòng `'` |
