# 🎨 HƯỚNG DẪN TÙY CHỈNH GIAO DIỆN (UI) — eAndon C# WinForms

> **Mục tiêu**: Giải thích chi tiết từng Form, từng Control, và hướng dẫn cách
> thiết kế lại giao diện theo ý muốn của bạn.

---

## MỤC LỤC

- [1. Tổng quan các Form](#1-tổng-quan-các-form)
- [2. TerminalMainForm — Màn hình Grid chính](#2-terminalmainform--màn-hình-grid-chính)
- [3. DashboardMainForm — Màn hình Dashboard](#3-dashboardmainform--màn-hình-dashboard)
- [4. AlarmTypeForm — Popup chọn Yellow/Red](#4-alarmtypeform--popup-chọn-yellowred)
- [5. EmployeeInputForm — Popup nhập Mã NV](#5-employeeinputform--popup-nhập-mã-nv)
- [6. StationSelectForm — Popup chọn Trạm](#6-stationselectform--popup-chọn-trạm)
- [7. FixCompleteForm — Popup KTV nhập ghi chú](#7-fixcompleteform--popup-ktv-nhập-ghi-chú)
- [8. StatisticsForm — Màn hình thống kê](#8-statisticsform--màn-hình-thống-kê)
- [9. TicketDetailForm — Chi tiết phiếu sự cố](#9-ticketdetailform--chi-tiết-phiếu-sự-cố)
- [10. Công thức thiết kế UI mới hoàn toàn](#10-công-thức-thiết-kế-ui-mới-hoàn-toàn)

---

## 1. Tổng quan các Form

### Sơ đồ quan hệ các Form

```
AndonTerminal                          AndonDashboard
─────────────────────────────          ─────────────────────────────
TerminalMainForm (Form chính)          DashboardMainForm (Form chính)
  │                                      │
  ├─► StationSelectForm (popup)          ├─► TicketDetailForm (popup)
  ├─► AlarmTypeForm     (popup)          └─► StatisticsForm   (popup)
  ├─► EmployeeInputForm (popup, x3)
  └─► FixCompleteForm   (popup)
```

### Kích thước mặc định

| Form | Kích thước | Loại | Ghi chú |
|------|-----------|------|---------|
| TerminalMainForm | Tự động theo số Line/Alarm | Sizable | Tính từ dữ liệu cấu hình |
| DashboardMainForm | Tự động theo số Line/Alarm | Sizable | Tính từ dữ liệu cấu hình |
| AlarmTypeForm | 700 × 380 | FixedDialog | Không thay đổi kích thước |
| EmployeeInputForm | 420 × 340 | FixedDialog | Không thay đổi kích thước |
| StationSelectForm | Tự động theo số Station | FixedDialog | Tính từ số lượng Station |
| FixCompleteForm | 460 × 380 | FixedDialog | Không thay đổi kích thước |
| StatisticsForm | 950 × 600 | Sizable | Có thể kéo to nhỏ |
| TicketDetailForm | 520 × 620 | FixedDialog | Scroll nội dung bên trong |

---

## 2. TerminalMainForm — Màn hình Grid chính

### 2.1 Cấu trúc Layout

```
┌──────────────────────────────────────────────────────────────────┐
│ panelHeader (DockStyle.Top, Height=60)                           │
│  [lblTitle: "🏭 eAndon Terminal | terminal01"]  [lblTime: HH:mm] │
├──────────────────────────────────────────────────────────────────┤
│ panelGrid (DockStyle.Fill, AutoScroll=true)                      │
│                                                                  │
│         │ Alarm 1  │ Alarm 2  │ Alarm 3  │ Alarm 4  │ Alarm 5  │
│ ────────┼──────────┼──────────┼──────────┼──────────┼──────────┤
│ Line 1  │ [Button] │ [Button] │ [Button] │ [Button] │ [Button] │
│ Line 2  │ [Button] │ [Button] │ [Button] │ [Button] │ [Button] │
│ ...     │          │          │          │          │          │
└──────────────────────────────────────────────────────────────────┘
```

### 2.2 Các thành phần UI quan trọng

**panelHeader** — dải tiêu đề trên cùng:
```csharp
// Vị trí: DockStyle.Top → tự dãn hết chiều ngang, chiều cao cố định 60px
var panelHeader = new Panel
{
    BackColor = ColorHeader,        // Màu nền tối hơn body
    Dock = DockStyle.Top,           // Dán vào cạnh trên form
    // Không cần đặt Bounds vì đã Dock
};
```

**lblTime** — đồng hồ góc phải:
```csharp
// Quan trọng: Anchor = Top|Right → khi resize form, label luôn ở góc phải
var lblTime = new Label
{
    Name = "lblTime",               // Đặt tên để tìm lại sau (trong RefreshGridAndWriteData)
    Anchor = AnchorStyles.Top | AnchorStyles.Right,  // Dán vào góc trên phải
    Bounds = new Rectangle(this.Width - 280, 15, 260, 30),
    TextAlign = ContentAlignment.MiddleRight,
};
```

**panelGrid** — vùng grid có thể scroll:
```csharp
var panelGrid = new Panel
{
    AutoScroll = true,              // Tự xuất hiện scrollbar khi nội dung tràn
    Dock = DockStyle.Fill,          // Chiếm phần còn lại sau header
};
```

**GridCell** — mỗi ô alarm là 1 Button:
```csharp
// Button dạng phẳng (FlatStyle.Flat) trông giống ô màu
var btn = new Button
{
    FlatStyle = FlatStyle.Flat,
    Font = new Font("Segoe UI", 18f, FontStyle.Bold),  // Font lớn cho ký hiệu ✓
    Bounds = new Rectangle(xPos, yPos, cellWidth, cellHeight),
};
btn.FlatAppearance.BorderSize = 1;       // Viền mỏng
btn.FlatAppearance.BorderColor = Color.FromArgb(0, 0, 0, 50);  // Viền đen 50% trong suốt
```

### 2.3 Cách thay đổi TerminalMainForm

**Thêm dòng thông tin phụ dưới header:**
```csharp
// Trong InitializeUI(), sau khi thêm lblTime vào panelHeader:
var lblSubInfo = new Label
{
    Text = $"🏭 Nhà máy ABC — Phân xưởng Cơ khí",
    ForeColor = Color.FromArgb(149, 165, 166),  // xám nhạt
    Font = new Font("Segoe UI", 9f),
    AutoSize = true,
    Location = new Point(15, 42),  // dưới lblTitle
};
panelHeader.Controls.Add(lblSubInfo);
// Đồng thời tăng chiều cao header: Height = 80 thay vì 60
```

**Đổi ký hiệu ✓ thành văn bản "OK":**
```csharp
// Trong UpdateCellUI(), tìm dòng:
btn.Text = "✓";
// Đổi thành:
btn.Text = "OK";
```

**Thêm tooltip khi hover lên ô:**
```csharp
// Thêm 1 lần ở đầu InitializeUI():
var toolTip = new ToolTip();
toolTip.AutoPopDelay = 5000;
toolTip.InitialDelay = 500;

// Khi tạo mỗi button, thêm tooltip:
toolTip.SetToolTip(btn, $"Line {ws.Number} — {_settings.GetAlarmLabel(col)}\nClick để báo sự cố");
```

---

## 3. DashboardMainForm — Màn hình Dashboard

### 3.1 Cấu trúc Layout

```
┌──────────────────────────────────────────────────────────────────┐
│ panelTop (DockStyle.Top, Height=65)                              │
│  [lblTitle: "🏭 eAndon Dashboard"]  [lblTime]  [btnStats]        │
├──────────────────────────────────────────────────────────────────┤
│ panelLegend (DockStyle.Top, Height=35)                           │
│  🟢 Bình thường  🟡 Yellow  🔴 Red  🟠 Repairing  🔵 WaitLeader │
├──────────────────────────────────────────────────────────────────┤
│ _panelGrid (DockStyle.Fill, AutoScroll=true)                     │
│  [Grid giống Terminal nhưng dùng Label thay vì Button]           │
├──────────────────────────────────────────────────────────────────┤
│ _lblStatus (DockStyle.Bottom, Height=22)                         │
│  "  Cập nhật từ file lúc 08:30:15"                               │
└──────────────────────────────────────────────────────────────────┘
```

### 3.2 Tại sao Dashboard dùng Label thay Button?

- Dashboard chỉ dùng để **xem**, không cần tương tác nhiều như Terminal.
- `Label` render nhanh hơn `Button` khi có nhiều ô.
- Click vào Label vẫn hoạt động (gán `Click` event).

### 3.3 Cách thay đổi DashboardMainForm

**Thêm panel tóm tắt số liệu bên dưới legend:**
```csharp
// Trong InitializeUI(), sau khi thêm panelLegend, thêm 1 panel mới:
var panelSummary = new Panel
{
    BackColor = Color.FromArgb(40, 56, 70),
    Dock = DockStyle.Top,
    Height = 40
};
this.Controls.Add(panelSummary);

// Thêm labels tóm tắt:
var lblActiveCount = new Label
{
    Name = "lblActiveAlarms",
    Text = "⚡ Đang alarm: 0",
    ForeColor = Color.FromArgb(241, 196, 15),
    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
    AutoSize = true,
    Location = new Point(10, 10)
};
panelSummary.Controls.Add(lblActiveCount);

// Cập nhật trong RefreshFromFiles() hoặc _refreshTimer:
// var lblArr = this.Controls.Find("lblActiveAlarms", true);
// if (lblArr.Length > 0 && lblArr[0] is Label lbl)
//     lbl.Text = $"⚡ Đang alarm: {_cells.Values.Count(c => c.Status != TicketStatus.Green)}";
```

**Đổi màu legend label:**
```csharp
// Trong AddLegendItem(), thêm background cho label:
private void AddLegendItem(Panel panel, int x, string text, Color color)
{
    var lbl = new Label
    {
        Text = $" {text} ",         // khoảng trắng hai bên
        ForeColor = Color.White,    // đổi sang trắng
        BackColor = color,          // nền màu trạng thái
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(x, 5),
        Padding = new Padding(4, 2, 4, 2),  // padding trong
        BorderStyle = BorderStyle.FixedSingle
    };
    panel.Controls.Add(lbl);
}
```

---

## 4. AlarmTypeForm — Popup chọn Yellow/Red

### 4.1 Cấu trúc Layout

```
┌────────────────────────────────────────────────────────────────────┐
│ Form (700×380, nền tối)                                            │
│                                                                    │
│  ┌──────────────────────┐    ┌──────────────────────┐             │
│  │     panelYellow      │    │       panelRed        │             │
│  │  (290×260, vàng)     │    │   (290×260, đỏ)      │             │
│  │                      │    │                       │             │
│  │   ⚠ (icon)           │    │   🛑 (icon)           │             │
│  │   "VÀNG"             │    │   "ĐỎ"               │             │
│  │   (mô tả từ setting) │    │   (mô tả từ setting)  │             │
│  └──────────────────────┘    └──────────────────────┘             │
│                                                                    │
│                    [    Hủy    ]                                   │
└────────────────────────────────────────────────────────────────────┘
```

### 4.2 Cách thêm mức độ thứ 3 (ví dụ: Orange = Khẩn cấp cực cao)

```csharp
// 1. Trong AlarmTypeForm.cs, thêm panel thứ 3:
var panelOrange = new Panel
{
    BackColor = Color.FromArgb(230, 126, 34),  // cam
    Cursor = Cursors.Hand,
    Bounds = new Rectangle(20, 290, 660, 60)   // bên dưới 2 panel kia
};

var lblOrangeText = new Label
{
    Text = "🆘 KHẨN CẤP — Dừng toàn bộ dây chuyền",
    Font = new Font("Segoe UI", 13f, FontStyle.Bold),
    ForeColor = Color.White,
    AutoSize = false,
    TextAlign = ContentAlignment.MiddleCenter,
    Bounds = new Rectangle(0, 0, 660, 60)
};
panelOrange.Controls.Add(lblOrangeText);

EventHandler orangeClick = (s, e) =>
{
    SelectedSeverity = "Orange";
    this.DialogResult = DialogResult.OK;
    this.Close();
};
panelOrange.Click += orangeClick;
lblOrangeText.Click += orangeClick;
this.Controls.Add(panelOrange);

// 2. Cũng cần tăng chiều cao form:
this.Size = new Size(700, 460);  // 380 → 460

// 3. Trong TerminalMainForm.HandleNewAlarm():
//    severity sẽ có thêm giá trị "Orange"
//    Thêm case xử lý tương ứng
```

### 4.3 Đổi từ 2 panel ngang sang 2 panel dọc

```csharp
// Thay Bounds của 2 panel:
// Trước (ngang):
panelYellow.Bounds = new Rectangle(20, 20, 290, 260);
panelRed.Bounds    = new Rectangle(390, 20, 290, 260);

// Sau (dọc):
panelYellow.Bounds = new Rectangle(20, 20, 660, 110);
panelRed.Bounds    = new Rectangle(20, 145, 660, 110);

// Tăng chiều cao form và điều chỉnh lblYellowIcon/lblRedIcon
```

---

## 5. EmployeeInputForm — Popup nhập Mã NV

### 5.1 Cấu trúc Layout

```
┌────────────────────────────────────┐
│ Form (420×340, nền tối)            │
│                                    │
│  [Instruction text]                │ ← lblInstruction
│                                    │
│  Mã nhân viên *                    │ ← lblId
│  [________________________]        │ ← _txtEmployeeId
│                                    │
│  Họ và tên *                       │ ← lblName
│  [________________________]        │ ← _txtEmployeeName
│                                    │
│  ⚠ (validation error)             │ ← _lblError
│                                    │
│  [✓ Xác nhận]  [✕ Hủy]            │ ← btnOK, btnCancel
└────────────────────────────────────┘
```

### 5.2 Thêm trường "Chức vụ"

```csharp
// 1. Thêm property:
public string EmployeeRole { get; private set; }

// 2. Thêm ComboBox:
private ComboBox _cboRole;

// 3. Trong InitializeUI(), sau _txtEmployeeName:
var lblRole = new Label
{
    Text = "Chức vụ *",
    ForeColor = TextColor,
    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
    AutoSize = false,
    Bounds = new Rectangle(x, 205, w, 22)
};
this.Controls.Add(lblRole);

_cboRole = new ComboBox
{
    BackColor = InputBackColor,
    ForeColor = TextColor,
    Font = new Font("Segoe UI", 11f),
    Bounds = new Rectangle(x, 230, w, 32),
    DropDownStyle = ComboBoxStyle.DropDownList  // chỉ chọn, không tự nhập
};
_cboRole.Items.AddRange(new[] { "Operator", "KTV", "Tổ trưởng/Leader", "Kỹ sư" });
_cboRole.SelectedIndex = 0;
this.Controls.Add(_cboRole);

// 4. Tăng chiều cao form: this.Size = new Size(420, 410);

// 5. Đẩy btnOK và btnCancel xuống thấp hơn (Bounds y + 70)

// 6. Trong BtnOK_Click():
EmployeeRole = _cboRole.SelectedItem?.ToString();
```

### 5.3 Thêm hỗ trợ quét mã vạch (barcode scanner)

Máy quét mã vạch hoạt động giống như bàn phím, gõ mã và nhấn Enter.
Chỉ cần đặt focus vào TextBox mã NV và nhấn Enter tự động:

```csharp
// Trong InitializeUI():
// AcceptButton đã được set ở cuối (this.AcceptButton = btnOK)
// → Nhấn Enter ở bất kỳ đâu = nhấn nút Xác nhận

// Thêm xử lý KeyPress cho _txtEmployeeId để tự nhảy sang _txtEmployeeName:
_txtEmployeeId.KeyPress += (s, e) =>
{
    if (e.KeyChar == '\r')  // Enter
    {
        e.Handled = true;
        _txtEmployeeName.Focus();
    }
};
```

---

## 6. StationSelectForm — Popup chọn Trạm

### 6.1 Cấu trúc Layout

```
┌──────────────────────────────────┐
│ Form (tự động, nền tối)          │
│                                  │
│  "Line: Line 1"                  │ ← lblTitle
│  "Chọn trạm bị lỗi:"            │ ← lblInstruction
│                                  │
│  ┌─────────────────────────────┐ │
│  │ ST-010-01 - Trạm cắt laser │ │ ← Button cho mỗi station
│  └─────────────────────────────┘ │
│  ┌─────────────────────────────┐ │
│  │ ST-010-02 - Trạm hàn điểm  │ │
│  └─────────────────────────────┘ │
│  ...                             │
│  [           Hủy              ]  │ ← btnCancel
└──────────────────────────────────┘
```

### 6.2 Đổi từ layout dọc sang layout lưới (2 cột)

```csharp
// Thay thế vòng lặp tạo nút trong InitializeUI():
int col = 0;
int row = 0;
int btnW = 200, btnH = 70, padding = 10;

foreach (var station in _stations)
{
    var capturedStation = station;
    var btn = new Button { /* ... */ };

    // Tính vị trí theo lưới 2 cột
    int xPos = padding + col * (btnW + padding);
    int yPos = 95 + row * (btnH + padding);
    btn.Bounds = new Rectangle(xPos, yPos, btnW, btnH);

    this.Controls.Add(btn);

    col++;
    if (col >= 2) { col = 0; row++; }  // sang hàng mới sau 2 cột
}

// Cập nhật kích thước form:
int totalRows = (int)Math.Ceiling(_stations.Count / 2.0);
int formW = padding * 3 + btnW * 2;
int formH = 95 + totalRows * (btnH + padding) + 65;
this.Size = new Size(formW, formH);
```

---

## 7. FixCompleteForm — Popup KTV nhập ghi chú

### 7.1 Cấu trúc Layout

```
┌──────────────────────────────────────────────────────────┐
│ Form (460×380, nền tối)                                   │
│                                                           │
│  ✓ Hoàn thành sửa chữa                                   │ ← lblTitle
│  Line: xxx | Trạm: xxx | Loại: xxx                       │ ← lblInfo
│                                                           │
│  Ghi chú sửa chữa (không bắt buộc):                     │ ← lblNote
│  ┌──────────────────────────────────────────────────┐    │
│  │ TextBox multiline (130px cao)                    │    │ ← _txtNote
│  │ Scroll dọc                                       │    │
│  └──────────────────────────────────────────────────┘    │
│                                                           │
│  [✓  Đã sửa xong — Chờ Leader xác nhận              ]   │ ← btnDone
│  [Hủy]                                                   │ ← btnCancel
└──────────────────────────────────────────────────────────┘
```

### 7.2 Thêm lựa chọn nguyên nhân lỗi (Dropdown)

```csharp
// Thêm vào FixCompleteForm, sau lblNote:
var lblCause = new Label
{
    Text = "Nguyên nhân:",
    ForeColor = TextColor,
    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
    AutoSize = false,
    Bounds = new Rectangle(x, 100, w, 25)
};
this.Controls.Add(lblCause);

var cboCause = new ComboBox
{
    BackColor = InputBackColor,
    ForeColor = TextColor,
    Font = new Font("Segoe UI", 11f),
    DropDownStyle = ComboBoxStyle.DropDownList,
    Bounds = new Rectangle(x, 128, w, 32)
};
cboCause.Items.AddRange(new[]
{
    "Hỏng cơ khí",
    "Lỗi điện",
    "Lỗi phần mềm PLC",
    "Thiếu vật tư/linh kiện",
    "Lỗi vận hành",
    "Khác"
});
cboCause.SelectedIndex = 0;
this.Controls.Add(cboCause);

// Đẩy _txtNote xuống thấp hơn (Bounds y + 62)
// Thêm property: public string CauseOfFailure { get; private set; }
// Trong btnDone.Click: CauseOfFailure = cboCause.SelectedItem?.ToString();
```

---

## 8. StatisticsForm — Màn hình thống kê

### 8.1 Cấu trúc Layout

```
┌────────────────────────────────────────────────────────────────────────┐
│ panelFilter (DockStyle.Top, Height=55)                                 │
│  [Từ ngày: DatePicker] [Đến ngày: DatePicker] [Line: TextBox]         │
│  [🔍 Lọc] [📉 Top Downtime]                                            │
├────────────────────────────────────────────────────────────────────────┤
│ _grid DataGridView (DockStyle.Fill)                                    │
│  Ngày | Mã Line | Tên Line | Sự cố | Downtime | 🟡 | 🔴 | MTTR | ...  │
├────────────────────────────────────────────────────────────────────────┤
│ _lblSummary (DockStyle.Bottom, Height=25)                              │
│  "  Tổng cộng: 15 bản ghi | Từ 2026-02-20 đến 2026-02-27"            │
└────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Thêm cột Biểu đồ thanh mini (Sparkline)

```csharp
// Thêm cột custom render:
private DataGridViewImageColumn AddSparklineColumn()
{
    var col = new DataGridViewImageColumn
    {
        Name = "Sparkline",
        HeaderText = "Downtime trend",
        Width = 120
    };
    // Tự vẽ bitmap nhỏ cho mỗi dòng
    return col;
}

// Hoặc đơn giản hơn: thêm cột text thanh ASCII:
private string GetAsciiBar(double value, double maxValue, int barWidth = 10)
{
    if (maxValue <= 0) return new string('░', barWidth);
    int filled = (int)(value / maxValue * barWidth);
    filled = Math.Max(0, Math.Min(barWidth, filled));
    return new string('█', filled) + new string('░', barWidth - filled);
}
// Sử dụng: GetAsciiBar(r.TotalDowntimeSec, maxDowntime)
// → ██████░░░░
```

### 8.3 Xuất ra Excel

Cần thêm NuGet package `ClosedXML`:
1. Trong Visual Studio, click phải vào project **AndonDashboard**
2. Chọn **"Manage NuGet Packages"**
3. Tìm và cài `ClosedXML`

```csharp
// Thêm nút xuất Excel vào panelFilter, sau btnTop:
var btnExcel = new Button
{
    Text = "📊 Xuất Excel",
    /* ... */
    Bounds = new Rectangle(815, 10, 120, 35)
};
btnExcel.Click += (s, e) => ExportToExcel();

// Hàm xuất:
private void ExportToExcel()
{
    using (var saveDialog = new SaveFileDialog())
    {
        saveDialog.Filter = "Excel|*.xlsx";
        saveDialog.FileName = $"DailyStats_{DateTime.Today:yyyyMMdd}.xlsx";
        if (saveDialog.ShowDialog() != DialogResult.OK) return;

        using (var wb = new ClosedXML.Excel.XLWorkbook())
        {
            var ws = wb.Worksheets.Add("DailyStats");
            // Copy data từ _grid:
            for (int col = 0; col < _grid.Columns.Count; col++)
                ws.Cell(1, col + 1).Value = _grid.Columns[col].HeaderText;

            for (int row = 0; row < _grid.Rows.Count; row++)
                for (int col = 0; col < _grid.Columns.Count; col++)
                    ws.Cell(row + 2, col + 1).Value =
                        _grid.Rows[row].Cells[col].Value?.ToString();

            wb.SaveAs(saveDialog.FileName);
        }
        MessageBox.Show("Xuất thành công!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
```

---

## 9. TicketDetailForm — Chi tiết phiếu sự cố

### 9.1 Cấu trúc Layout

```
┌────────────────────────────────────────┐
│ Form (520×620, nền tối)                │
│                                        │
│  Panel (AutoScroll)                    │
│   🎫 TKT-20260227-123          [xanh] │ ← TicketId
│   Trạng thái: Closed          [màu]   │ ← Status
│                                        │
│  📍 Địa điểm          [header xanh]   │
│    Line:     010 — Line 1              │
│    Trạm:     ST-010-01                 │
│    Loại:     Hỗ trợ Bảo trì           │
│    Mức độ:   Yellow                    │
│                                        │
│  👷 Bước 1-4: Operator                 │
│  🔧 Bước 5: KTV nhận sửa              │
│  ✅ Bước 6: KTV hoàn thành            │
│  👔 Bước 7: Leader xác nhận           │
│  📊 Tổng kết                           │
│                                        │
│  [Đóng]                                │
└────────────────────────────────────────┘
```

### 9.2 Thêm nút "In phiếu" / "Copy thông tin"

```csharp
// Thêm vào dưới btnClose trong InitializeUI():
var btnCopy = new Button
{
    Text = "📋 Copy",
    ForeColor = Color.White,
    BackColor = Color.FromArgb(52, 152, 219),
    FlatStyle = FlatStyle.Flat,
    Bounds = new Rectangle(125, 580, 100, 35),
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left
};
btnCopy.FlatAppearance.BorderSize = 0;
btnCopy.Click += (s, e) =>
{
    string info = FormatTicketAsText(ticket);
    Clipboard.SetText(info);
    MessageBox.Show("Đã copy vào clipboard!", "OK",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
};
this.Controls.Add(btnCopy);

// Hàm format text:
private string FormatTicketAsText(IncidentTicket t)
{
    return $"""
        PHIẾU SỰ CỐ: {t.TicketId}
        Line: {t.LineName} | Trạm: {t.StationName}
        Loại: {t.AlarmTypeName} | Mức: {t.Severity}
        Operator: {t.OperatorName} ({t.ReportedAt:dd/MM HH:mm})
        KTV: {t.TechnicianName ?? "Chưa"} ({t.TechCheckinAt:HH:mm} - {t.TechFixedAt:HH:mm})
        Ghi chú: {t.FixNote ?? "(không có)"}
        Leader: {t.LeaderName ?? "Chưa"} ({t.LeaderConfirmedAt:dd/MM HH:mm})
        """;
}
```

---

## 10. Công thức thiết kế UI mới hoàn toàn

### 10.1 Tạo Form mới từ đầu

1. Click phải vào project (AndonDashboard hoặc AndonTerminal)
2. **Add → New Item → Windows Form** (KHÔNG dùng Designer, viết tay)
3. Xóa hết code mặc định, dùng template sau:

```csharp
// File: AndonDashboard/Forms/MyNewForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AndonDashboard.Forms
{
    public class MyNewForm : Form
    {
        // ── Màu sắc (sửa tại đây) ──
        private static readonly Color ColorBg     = Color.FromArgb(44, 62, 80);
        private static readonly Color ColorHeader = Color.FromArgb(36, 50, 64);
        private static readonly Color ColorAccent = Color.FromArgb(52, 152, 219);
        private static readonly Color ColorText   = Color.White;

        public MyNewForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // ── Cài đặt Form ──
            this.Text = "Form Mới";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = ColorBg;

            // ── Header ──
            var panelHeader = new Panel
            {
                BackColor = ColorHeader,
                Dock = DockStyle.Top,
                Height = 60
            };
            this.Controls.Add(panelHeader);

            var lblTitle = new Label
            {
                Text = "🔧 Form Mới Của Tôi",
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                AutoSize = false,
                Bounds = new Rectangle(15, 15, 500, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelHeader.Controls.Add(lblTitle);

            // ── Body ──
            var panelBody = new Panel
            {
                BackColor = ColorBg,
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };
            this.Controls.Add(panelBody);

            // ── [TODO] Thêm controls vào đây ──

            // ── Footer (nút đóng) ──
            var panelFooter = new Panel
            {
                BackColor = ColorHeader,
                Dock = DockStyle.Bottom,
                Height = 55
            };
            this.Controls.Add(panelFooter);

            var btnClose = new Button
            {
                Text = "Đóng",
                ForeColor = ColorText,
                BackColor = Color.FromArgb(149, 165, 166),
                FlatStyle = FlatStyle.Flat,
                Bounds = new Rectangle(15, 10, 100, 35),
                DialogResult = DialogResult.Cancel
            };
            btnClose.FlatAppearance.BorderSize = 0;
            panelFooter.Controls.Add(btnClose);
            this.CancelButton = btnClose;
        }
    }
}
```

### 10.2 Mở Form mới từ DashboardMainForm

```csharp
// Trong DashboardMainForm, gán cho 1 nút bấm:
var btnMyNew = new Button { Text = "Mở Form Mới", /* ... */ };
btnMyNew.Click += (s, e) =>
{
    using (var form = new MyNewForm())
        form.ShowDialog(this);   // ShowDialog = modal (chặn form cha)
        // hoặc: form.Show(this);  // Show = non-modal (không chặn)
};
```

### 10.3 Nguyên tắc thiết kế UI nhất quán

Toàn bộ hệ thống sử dụng bảng màu tối (dark theme) nhất quán:

```
Màu nền chính:   #2C3E50  → Color.FromArgb(44, 62, 80)
Màu header:      #243244  → Color.FromArgb(36, 50, 64)
Màu input bg:    #34495E  → Color.FromArgb(52, 73, 94)
Màu text chính:  #FFFFFF  → Color.White
Màu text phụ:    #BDC3C7  → Color.FromArgb(189, 195, 199)
Màu accent xanh: #3498DB  → Color.FromArgb(52, 152, 219)
Màu OK (xanh lá):  #2ECC71  → Color.FromArgb(46, 204, 113)
Màu Cancel (xám):  #95A5A6  → Color.FromArgb(149, 165, 166)
```

### 10.4 Checklist UI nhất quán

Khi tạo Form hoặc Control mới:
- [ ] Dùng `Font("Segoe UI", ...)` cho tất cả text
- [ ] Nút OK màu `ColorOkButton = (46, 204, 113)`
- [ ] Nút Hủy màu `(149, 165, 166)`
- [ ] Input box `BackColor = (52, 73, 94)`, `ForeColor = White`
- [ ] Không dùng `BorderStyle.Fixed3D` (trông cũ) — dùng `BorderStyle.FixedSingle`
- [ ] `FlatStyle.Flat` cho tất cả Button
- [ ] `FlatAppearance.BorderSize = 0` cho nút không cần viền
- [ ] `Cursor = Cursors.Hand` cho tất cả control có thể click

---

> 📌 Xem thêm: [`BEGINNER_GUIDE.md`](./BEGINNER_GUIDE.md) để hướng dẫn từ bước cài đặt môi trường.
