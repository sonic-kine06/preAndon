// File: AndonTerminal/Forms/TerminalMainForm.cs
// Mô tả: Form chính của AndonTerminal.
// Hiển thị grid động: Hàng = Line, Cột = Alarm Type.
// Mỗi ô có màu tương ứng 5 trạng thái + đếm thời gian.
// Click ô → 7 bước luồng sự cố (Station → AlarmType → Employee → ...).
// Timer cập nhật thời gian và ghi file Data/terminalXX.txt.
// FileSystemWatcher không dùng ở Terminal (Terminal ghi, Dashboard đọc).
//
// GIAO DIỆN THỰC TẾ (kích thước tự động theo số Line × Alarm):
// ╔══════════════════════════════════════════════════════════════════╗
// ║  panelHeader (DockStyle.Top, Height=60, BackColor=ColorHeader)  ║
// ║  ┌─────────────────────────────┐  ┌──────────────────────────┐  ║
// ║  │ 🏭 eAndon Terminal |term01  │  │  08:30:15  27/02/2026    │  ║
// ║  └─────────────────────────────┘  └──────────────────────────┘  ║
// ║  lblTitle (14pt Bold)               lblTime (11pt, Anchor=Right) ║
// ╠══════════════════════════════════════════════════════════════════╣
// ║  panelGrid (DockStyle.Fill, AutoScroll=true)                    ║
// ║                                                                  ║
// ║  [startX,startY]                                                 ║
// ║         ┌────────────┬────────────┬────────────┬─────────────┐  ║
// ║         │ Hỗ trợ TL  │  Bảo trì  │ Chất lượng│  Thiếu VL  │  ║
// ║         │(9pt Bold)  │           │            │             │  ║
// ║  ┌──────┼────────────┼────────────┼────────────┼─────────────┤  ║
// ║  │010   │ [🟢 ✓    ] │[🟡05m30s ] │ [🟢 ✓    ] │ [🟢 ✓    ] │  ║
// ║  │Line1 │ Button     │ Button     │ Button     │ Button     │  ║
// ║  │(10pt)│ 120×80px  │            │            │            │  ║
// ║  ├──────┼────────────┼────────────┼────────────┼─────────────┤  ║
// ║  │020   │ [🔴09m   ] │ [🟢 ✓    ] │ [🟠Đang S] │ [🟢 ✓    ] │  ║
// ║  │Line2 │            │            │            │            │  ║
// ║  └──────┴────────────┴────────────┴────────────┴─────────────┘  ║
// ╚══════════════════════════════════════════════════════════════════╝
//
// CẤU TRÚC DỮ LIỆU:
//   _cells: Dictionary<string, GridCell>
//   Key = "lineNumber_alarmIndex" ví dụ: "010_1", "020_3"
//   Mỗi GridCell: { Button, Status, Ticket, AlarmTypeIndex, LineNumber... }
//
// TIMER (_refreshTimer, 5000ms):
//   → RefreshGridAndWriteData() → cập nhật màu + đếm giờ + ghi file terminalXX.txt
//
// ĐỂ SỬA GIAO DIỆN:
//   - Kích thước ô: sửa cellWidth/cellHeight trong InitializeUI()
//   - Màu ô: sửa ColorGreen/Yellow/Red/Orange/Blue (const ở đầu class)
//   - Thêm thông tin header: xem hướng dẫn Docs/UI_CUSTOMIZE.md#2

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using SharedLib.Models;
using SharedLib.Services;

namespace AndonTerminal.Forms
{
    /// <summary>
    /// Form chính của Terminal Andon.
    /// Operator dùng form này để báo lỗi và theo dõi tiến trình sửa.
    /// </summary>
    public class TerminalMainForm : Form
    {
        // ─────────────── Dependencies ───────────────
        private readonly SettingsReader _settings;
        private readonly LineStationReader _lineStationReader;
        private readonly IncidentService _incidentService;
        private readonly AlarmLogger _alarmLogger;

        // ─────────────── Dữ liệu cấu hình ───────────────
        private List<WorkstationEntry> _workstations;
        private string _terminalName;   // Tên terminal, ví dụ: "terminal01"
        private string _dataDirectory;  // Thư mục ghi file Data/
        private string _assetsDirectory; // Thư mục chứa assets (âm thanh, cấu hình)

        // ─────────────── Tài nguyên cần dispose ───────────────
        private readonly List<IDisposable> _ownedResources = new List<IDisposable>();

        // ─────────────── Grid UI ───────────────
        // Mỗi ô trong grid lưu: Button control + trạng thái hiện tại
        private class GridCell
        {
            public Button Button { get; set; }
            public TicketStatus Status { get; set; } = TicketStatus.Green;
            public DateTime? AlarmStartTime { get; set; }
            public string ActiveTicketId { get; set; }
            public string LineNumber { get; set; }
            public int AlarmTypeIndex { get; set; }
        }

        // Map từ (lineNumber, alarmTypeIndex) → GridCell
        private Dictionary<string, GridCell> _gridCells = new Dictionary<string, GridCell>();

        // ─────────────── Timer ───────────────
        // Timer cập nhật hiển thị thời gian và ghi file mỗi 5 giây
        private System.Windows.Forms.Timer _updateTimer;

        // ─────────────── Màu 5 trạng thái ───────────────
        // Thay đổi các giá trị Color.FromArgb(R, G, B) để đổi màu giao diện.
        // Công cụ chọn màu: https://colorpicker.me/ — chọn màu → lấy R,G,B
        private static readonly Color ColorGreen      = Color.FromArgb(46, 204, 113);   // xanh lá tươi
        private static readonly Color ColorYellow     = Color.FromArgb(241, 196, 15);   // vàng
        private static readonly Color ColorRed        = Color.FromArgb(192, 57, 43);    // đỏ đậm
        private static readonly Color ColorOrange     = Color.FromArgb(230, 126, 34);   // cam (đang sửa)
        private static readonly Color ColorBlue       = Color.FromArgb(52, 152, 219);   // xanh dương (chờ Leader)
        private static readonly Color ColorBackground = Color.FromArgb(44, 62, 80);     // nền tối chính
        private static readonly Color ColorHeader     = Color.FromArgb(36, 50, 64);     // nền header tối hơn
        private static readonly Color ColorTextDark   = Color.FromArgb(44, 62, 80);     // chữ tối (trên nền vàng)

        public TerminalMainForm(SettingsReader settings, LineStationReader lineStationReader,
                                 IncidentService incidentService, AlarmLogger alarmLogger,
                                 string terminalName, string dataDirectory, string assetsDirectory = null)
        {
            _settings = settings;
            _lineStationReader = lineStationReader;
            _incidentService = incidentService;
            _alarmLogger = alarmLogger;
            _terminalName = terminalName;
            _dataDirectory = dataDirectory;
            _assetsDirectory = assetsDirectory;

            if (!Directory.Exists(_dataDirectory))
                Directory.CreateDirectory(_dataDirectory);

            // ── Icon cửa sổ ──
            // Nguồn: Assets/app.ico — lấy từ https://github.com/vitplanocka/eAndon (MIT License)
            // Xem attribution chi tiết tại Assets/NOTICE.txt
            string iconPath = _assetsDirectory != null
                ? Path.Combine(_assetsDirectory, "app.ico")
                : null;
            if (iconPath != null && File.Exists(iconPath))
            {
                var appIcon = new Icon(iconPath);
                _ownedResources.Add(appIcon);
                this.Icon = appIcon;
            }

            // BUG FIX: phải lọc theo _terminalName để mỗi Terminal chỉ hiển thị
            // các Lines được phân công cho nó trong Workstations_terminals.txt.
            // Nếu KHÔNG lọc → terminal01 sẽ hiển thị tất cả 6 Lines thay vì chỉ Lines 1-2.
            _workstations = _lineStationReader.GetWorkstations()
                .Where(w => string.Equals(w.Terminal, _terminalName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            InitializeUI();
            LoadGridState();
            StartUpdateTimer();
        }

        private void InitializeUI()
        {
            // Đọc số cột (= số loại alarm) và số hàng (= số line) từ cấu hình
            int alarmCount = _settings.NumberOfAlarmTypes;  // settings.txt: "Number of alarm types to display"
            int rowCount = _workstations.Count;             // số dòng trong Workstations_terminals.txt

            // ── Kích thước mỗi ô trong grid ──
            // Thay đổi các giá trị dưới đây để điều chỉnh kích thước grid
            int cellWidth  = 120;  // chiều rộng ô (px) — tăng nếu cần hiển thị text dài hơn
            int cellHeight = 80;   // chiều cao ô (px) — tăng nếu cần hiển thị 2 dòng text
            int headerH    = 50;   // chiều cao hàng tiêu đề cột (tên Alarm)
            int rowHeaderW = 150;  // chiều rộng cột tiêu đề hàng (tên Line) — tăng nếu tên dài
            int padding    = 5;    // khoảng cách giữa các ô (px)

            // Tự động tính kích thước form theo số hàng/cột
            int formWidth  = rowHeaderW + alarmCount * (cellWidth + padding) + padding * 2 + 20;
            int formHeight = headerH + rowCount * (cellHeight + padding) + padding * 2 + 80;

            this.Text = $"eAndon Terminal — {_terminalName}";
            this.FormBorderStyle = FormBorderStyle.Sizable;    // cho phép kéo to nhỏ
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorBackground;
            this.Size = new Size(Math.Max(800, formWidth), Math.Max(500, formHeight));  // tối thiểu 800×500
            this.MinimumSize = new Size(600, 400);

            // ── Panel tiêu đề (dải ngang trên cùng) ──
            // DockStyle.Top = tự dãn hết chiều ngang, bám vào cạnh trên
            var panelHeader = new Panel
            {
                BackColor = ColorHeader,                       // màu tối hơn nền chính
                Bounds = new Rectangle(0, 0, this.Width, 60), // không dùng vì đã Dock
                Dock = DockStyle.Top                           // dán vào cạnh trên form
            };

            var lblTitle = new Label
            {
                Text = $"🏭 eAndon Terminal  |  {_terminalName}",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                AutoSize = false,
                Bounds = new Rectangle(15, 10, 500, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelHeader.Controls.Add(lblTitle);

            // Đồng hồ số ở góc phải header
            // Name = "lblTime" để tìm lại trong RefreshGridAndWriteData()
            // Anchor = Top|Right → khi resize form, label luôn ở góc trên phải
            var lblTime = new Label
            {
                Name = "lblTime",
                Text = DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy"),
                ForeColor = Color.FromArgb(189, 195, 199),  // xám nhạt
                Font = new Font("Segoe UI", 11f),
                AutoSize = false,
                Bounds = new Rectangle(this.Width - 280, 15, 260, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right  // quan trọng: bám góc phải
            };
            panelHeader.Controls.Add(lblTime);
            this.Controls.Add(panelHeader);  // thêm panelHeader vào form

            // ── Panel Grid có thể scroll ──
            // DockStyle.Fill = chiếm toàn bộ vùng còn lại sau header
            // AutoScroll = true → tự hiện thanh cuộn khi grid vượt kích thước cửa sổ
            var panelGrid = new Panel
            {
                AutoScroll = true,
                BackColor = ColorBackground,
                Bounds = new Rectangle(0, 60, this.Width, this.Height - 60), // không dùng vì đã Dock
                Dock = DockStyle.Fill  // chiếm phần còn lại
            };
            this.Controls.Add(panelGrid);

            int startX = 10, startY = 10;  // điểm bắt đầu vẽ grid (offset từ góc trái trên của panelGrid)

            // ── Header cột: tên các loại Alarm (có icon ảnh nếu file tồn tại) ──
            // Vòng lặp từ 1 đến alarmCount (1-based theo settings.txt)
            for (int i = 1; i <= alarmCount; i++)
            {
                string label = _settings.GetAlarmLabel(i);  // đọc từ "Alarm label 1", "Alarm label 2"...
                // xPos tính từ: startX + cột header bên trái + (chỉ số cột - 1) * (rộng ô + padding)
                int xPos = startX + rowHeaderW + (i - 1) * (cellWidth + padding);

                // ── Icon alarm column header ──
                // Nguồn: Assets/Icon1-5.png — lấy từ https://github.com/vitplanocka/eAndon (MIT License)
                // Xem attribution chi tiết tại Assets/NOTICE.txt
                string imgFile = _assetsDirectory != null
                    ? Path.Combine(_assetsDirectory, _settings.GetAlarmImageFile(i))
                    : null;
                if (imgFile != null && File.Exists(imgFile))
                {
                    // Hiển thị ảnh icon phía trên + text label phía dưới
                    var img = Image.FromFile(imgFile);
                    _ownedResources.Add(img);  // đảm bảo được dispose khi form đóng
                    var pb = new PictureBox
                    {
                        Image = img,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = ColorHeader,
                        Bounds = new Rectangle(xPos + (cellWidth - 32) / 2, startY, 32, 32),
                    };
                    panelGrid.Controls.Add(pb);
                    var lblCol = new Label
                    {
                        Text = label,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                        BackColor = ColorHeader,
                        AutoSize = false,
                        Bounds = new Rectangle(xPos, startY + 32, cellWidth, headerH - 32 - 2),
                        TextAlign = ContentAlignment.MiddleCenter,
                    };
                    panelGrid.Controls.Add(lblCol);
                }
                else
                {
                    // Fallback: chỉ hiển thị text khi không có file ảnh
                    var lblCol = new Label
                    {
                        Text = label,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                        BackColor = ColorHeader,
                        AutoSize = false,
                        Bounds = new Rectangle(xPos, startY, cellWidth, headerH - 10),
                        TextAlign = ContentAlignment.MiddleCenter,
                    };
                    panelGrid.Controls.Add(lblCol);
                }
            }

            // ── Các hàng: một hàng = một Line sản xuất ──
            for (int row = 0; row < _workstations.Count; row++)
            {
                var ws = _workstations[row];
                // yPos: bắt đầu từ header + (hàng hiện tại * (cao ô + padding))
                int yPos = startY + headerH + row * (cellHeight + padding);

                // Label tên Line ở cột đầu tiên (bên trái)
                // Hiển thị: "010\nLine 1" (số mã và tên)
                var lblLine = new Label
                {
                    Text = $"{ws.Number}\n{ws.Name}",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    BackColor = ColorHeader,
                    AutoSize = false,
                    Bounds = new Rectangle(startX, yPos, rowHeaderW - 5, cellHeight),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,  // con trỏ tay khi hover → gợi ý có thể click
                };

                // Click vào tên Line → mở lịch sử alarm của line đó
                // Dùng biến captured để tránh "closure bug" trong vòng lặp
                string capturedLine = ws.Number;
                string capturedLineName = ws.Name;
                lblLine.Click += (s, e) => ShowLineHistory(capturedLine, capturedLineName);
                panelGrid.Controls.Add(lblLine);

                // ── Tạo các ô alarm (Button) cho từng cột trong hàng này ──
                for (int col = 1; col <= alarmCount; col++)
                {
                    // xPos: bắt đầu từ cột header bên trái + (chỉ số cột - 1) * (rộng + padding)
                    int xPos = startX + rowHeaderW + (col - 1) * (cellWidth + padding);
                    // Key duy nhất để tra cứu cell: "010_1", "010_2", "020_1"...
                    string cellKey = $"{ws.Number}_{col}";
                    int capturedCol = col;

                    // Mỗi ô là 1 Button với FlatStyle để trông như ô màu phẳng
                    var btn = new Button
                    {
                        Name = $"cell_{ws.Number}_{col}",
                        Text = "✓",                    // ký hiệu mặc định khi trạng thái Green
                        ForeColor = ColorTextDark,
                        BackColor = ColorGreen,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        Bounds = new Rectangle(xPos, yPos, cellWidth, cellHeight)
                    };
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(0, 0, 0, 50);

                    var cell = new GridCell
                    {
                        Button = btn,
                        LineNumber = ws.Number,
                        AlarmTypeIndex = col
                    };
                    _gridCells[cellKey] = cell;

                    btn.Click += (s, e) => HandleCellClick(cell, capturedLineName);
                    panelGrid.Controls.Add(btn);
                }
            }
        }

        // ─────────────── Xử lý click ô alarm ───────────────

        private void HandleCellClick(GridCell cell, string lineName)
        {
            switch (cell.Status)
            {
                case TicketStatus.Green:
                    // Bước 1-4: Operator báo lỗi
                    HandleNewAlarm(cell, lineName);
                    break;

                case TicketStatus.Yellow:
                case TicketStatus.Red:
                    // Bước 5: KTV nhận sửa
                    HandleTechCheckin(cell);
                    break;

                case TicketStatus.Repairing:
                    // Bước 6: KTV hoàn thành sửa
                    HandleFixComplete(cell, lineName);
                    break;

                case TicketStatus.WaitLeader:
                    // Bước 7: Leader xác nhận
                    HandleLeaderConfirm(cell);
                    break;
            }
        }

        /// <summary>Bước 1-4: Operator báo lỗi mới</summary>
        private void HandleNewAlarm(GridCell cell, string lineName)
        {
            // Bước 2: Chọn trạm nếu Line có nhiều station
            StationInfo selectedStation = null;
            if (_lineStationReader.HasMultipleStations(cell.LineNumber))
            {
                var stations = _lineStationReader.GetStationsForLine(cell.LineNumber);
                using (var stationForm = new StationSelectForm(stations, lineName))
                {
                    if (stationForm.ShowDialog(this) != DialogResult.OK) return;
                    selectedStation = stationForm.SelectedStation;
                }
            }
            else
            {
                var stations = _lineStationReader.GetStationsForLine(cell.LineNumber);
                selectedStation = stations.Count > 0 ? stations[0] : new StationInfo(cell.LineNumber, lineName, cell.LineNumber, lineName);
            }

            // Bước 3: Chọn Yellow/Red
            string severity;
            using (var alarmForm = new AlarmTypeForm(_settings))
            {
                if (alarmForm.ShowDialog(this) != DialogResult.OK) return;
                severity = alarmForm.SelectedSeverity;
            }

            // Bước 4: Nhập thông tin Operator
            using (var empForm = new EmployeeInputForm("Nhập thông tin Operator", "Bạn vui lòng nhập mã NV và họ tên:"))
            {
                if (empForm.ShowDialog(this) != DialogResult.OK) return;

                string alarmTypeName = _settings.GetAlarmLabel(cell.AlarmTypeIndex);

                // Tạo ticket
                var ticket = _incidentService.OpenIncident(
                    cell.LineNumber, lineName,
                    selectedStation.StationId, selectedStation.StationName,
                    cell.AlarmTypeIndex, alarmTypeName,
                    severity,
                    empForm.EmployeeId, empForm.EmployeeName);

                // Cập nhật ô Grid
                cell.Status = severity == "Red" ? TicketStatus.Red : TicketStatus.Yellow;
                cell.AlarmStartTime = DateTime.Now;
                cell.ActiveTicketId = ticket.TicketId;
                UpdateCellUI(cell);

                // ══════════════════════════════════════════════════════════════
                // ► [TODO] HOOK SAU KHI TẠO TICKET — Viết thêm logic tại đây
                // Ví dụ: hiển thị gợi ý KTV từ AnalyticsManager,
                //        gửi thông báo ra ngoài, gọi API, v.v.
                //
                // var analytics = new Analytics.AnalyticsManager("Data/eandon.db");
                // var gợiÝ = analytics.GetSuggestionForNewTicket(
                //                 cell.LineNumber, cell.AlarmTypeIndex);
                // if (gợiÝ.HasSuggestion)
                //     MessageBox.Show(gợiÝ.SummaryText, "💡 Gợi ý",
                //         MessageBoxButtons.OK, MessageBoxIcon.Information);
                // ══════════════════════════════════════════════════════════════

                // Phát âm thanh cảnh báo
                PlayAlarmSound();
            }
        }

        /// <summary>Bước 5: KTV nhận sửa</summary>
        private void HandleTechCheckin(GridCell cell)
        {
            if (string.IsNullOrEmpty(cell.ActiveTicketId)) return;

            using (var empForm = new EmployeeInputForm("Nhập thông tin KTV", "KTV vui lòng nhập mã NV và họ tên để nhận sửa:"))
            {
                if (empForm.ShowDialog(this) != DialogResult.OK) return;

                bool ok = _incidentService.AssignTechnician(cell.ActiveTicketId, empForm.EmployeeId, empForm.EmployeeName);
                if (ok)
                {
                    cell.Status = TicketStatus.Repairing;
                    UpdateCellUI(cell);
                }
            }
        }

        /// <summary>Bước 6: KTV hoàn thành sửa</summary>
        private void HandleFixComplete(GridCell cell, string lineName)
        {
            if (string.IsNullOrEmpty(cell.ActiveTicketId)) return;

            var ticket = _incidentService.GetTicket(cell.ActiveTicketId);
            string stationName = ticket?.StationName ?? lineName;
            string alarmTypeName = _settings.GetAlarmLabel(cell.AlarmTypeIndex);

            using (var fixForm = new FixCompleteForm(lineName, stationName, alarmTypeName))
            {
                if (fixForm.ShowDialog(this) != DialogResult.OK) return;

                bool requireLeader = _settings.RequireLeaderConfirmation;
                bool ok = _incidentService.MarkFixed(cell.ActiveTicketId, fixForm.FixNote, requireLeader);
                if (ok)
                {
                    if (requireLeader)
                    {
                        cell.Status = TicketStatus.WaitLeader;
                    }
                    else
                    {
                        cell.Status = TicketStatus.Green;
                        cell.AlarmStartTime = null;
                        cell.ActiveTicketId = null;
                    }
                    UpdateCellUI(cell);
                }
            }
        }

        /// <summary>Bước 7: Leader xác nhận đóng phiếu</summary>
        private void HandleLeaderConfirm(GridCell cell)
        {
            if (string.IsNullOrEmpty(cell.ActiveTicketId)) return;

            using (var empForm = new EmployeeInputForm("Xác nhận của Leader", "Leader vui lòng nhập mã NV và họ tên để đóng phiếu:"))
            {
                if (empForm.ShowDialog(this) != DialogResult.OK) return;

                bool ok = _incidentService.LeaderConfirm(cell.ActiveTicketId, empForm.EmployeeId, empForm.EmployeeName);
                if (ok)
                {
                    cell.Status = TicketStatus.Green;
                    cell.AlarmStartTime = null;
                    cell.ActiveTicketId = null;
                    UpdateCellUI(cell);
                }
            }
        }

        // ─────────────── Cập nhật UI ───────────────

        private void UpdateCellUI(GridCell cell)
        {
            var btn = cell.Button;
            switch (cell.Status)
            {
                case TicketStatus.Green:
                    btn.BackColor = ColorGreen;
                    btn.ForeColor = ColorTextDark;
                    btn.Text = "✓";
                    break;
                case TicketStatus.Yellow:
                    btn.BackColor = ColorYellow;
                    btn.ForeColor = ColorTextDark;
                    btn.Text = FormatElapsedTime(cell.AlarmStartTime);
                    break;
                case TicketStatus.Red:
                    btn.BackColor = ColorRed;
                    btn.ForeColor = Color.White;
                    btn.Text = FormatElapsedTime(cell.AlarmStartTime);
                    break;
                case TicketStatus.Repairing:
                    btn.BackColor = ColorOrange;
                    btn.ForeColor = Color.White;
                    btn.Text = FormatElapsedTime(cell.AlarmStartTime);
                    break;
                case TicketStatus.WaitLeader:
                    btn.BackColor = ColorBlue;
                    btn.ForeColor = Color.White;
                    btn.Text = "⏳";
                    break;
            }
        }

        private string FormatElapsedTime(DateTime? startTime)
        {
            if (!startTime.HasValue) return "?";
            var elapsed = DateTime.Now - startTime.Value;
            if (elapsed.TotalHours >= 1)
                return $"{(int)elapsed.TotalHours}h{elapsed.Minutes:D2}m";
            return $"{(int)elapsed.TotalMinutes}m{elapsed.Seconds:D2}s";
        }

        // ─────────────── Timer cập nhật ───────────────

        private void StartUpdateTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer { Interval = 5000 }; // 5 giây
            _updateTimer.Tick += (s, e) => RefreshGridAndWriteData();
            _updateTimer.Start();
        }

        private void RefreshGridAndWriteData()
        {
            // Cập nhật thời gian hiển thị
            var lblTime = this.Controls.Find("lblTime", true);
            if (lblTime.Length > 0)
                ((Label)lblTime[0]).Text = DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy");

            // Cập nhật text ô đang alarm
            foreach (var cell in _gridCells.Values)
            {
                if (cell.Status != TicketStatus.Green && cell.Status != TicketStatus.WaitLeader)
                    UpdateCellUI(cell);
            }

            // Ghi file Data/terminalXX.txt
            WriteTerminalDataFile();
        }

        /// <summary>
        /// Ghi trạng thái grid vào file Data/terminalXX.txt (format gốc eAndon).
        /// Dashboard đọc file này qua FileSystemWatcher.
        /// Format: mỗi dòng = "lineNumber;alarmTypeIndex;status;ticketId;startTime"
        /// </summary>
        private void WriteTerminalDataFile()
        {
            try
            {
                string filePath = Path.Combine(_dataDirectory, $"{_terminalName}.txt");
                var lines = new System.Text.StringBuilder();
                lines.AppendLine($"# eAndon Terminal Data — {_terminalName} — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                foreach (var kvp in _gridCells)
                {
                    var cell = kvp.Value;
                    string startTime = cell.AlarmStartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    string ticketId = cell.ActiveTicketId ?? "";
                    lines.AppendLine($"{cell.LineNumber};{cell.AlarmTypeIndex};{(int)cell.Status};{ticketId};{startTime}");
                }

                File.WriteAllText(filePath, lines.ToString());
            }
            catch { /* Bỏ qua lỗi ghi file tạm thời */ }
        }

        // ─────────────── Load trạng thái khi khởi động ───────────────

        private void LoadGridState()
        {
            // Khôi phục trạng thái từ DB (các ticket đang active)
            var openTickets = _incidentService.GetAllOpen();
            foreach (var ticket in openTickets)
            {
                string key = $"{ticket.LineNumber}_{ticket.AlarmTypeIndex}";
                if (_gridCells.TryGetValue(key, out var cell))
                {
                    cell.Status = ticket.CurrentStatus;
                    cell.ActiveTicketId = ticket.TicketId;
                    cell.AlarmStartTime = ticket.ReportedAt;
                    UpdateCellUI(cell);
                }
            }
        }

        // ─────────────── Xem lịch sử alarm của Line ───────────────

        private void ShowLineHistory(string lineNumber, string lineName)
        {
            var tickets = _incidentService.GetHistory(lineNumber);
            if (tickets.Count == 0)
            {
                MessageBox.Show($"Không có lịch sử alarm cho {lineName}",
                    "Lịch sử Alarm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Lịch sử alarm — {lineName} ({lineNumber})");
            sb.AppendLine(new string('─', 60));
            foreach (var t in tickets)
            {
                sb.AppendLine($"[{t.ReportedAt:dd/MM HH:mm}] {t.AlarmTypeName} | {t.Severity} | {t.StationName}");
                sb.AppendLine($"  Operator: {t.OperatorName} | Status: {(TicketStatus)t.Status}");
                if (t.TechnicianName != null)
                    sb.AppendLine($"  KTV: {t.TechnicianName} | Sửa lúc: {t.TechCheckinAt:HH:mm}");
                if (t.LeaderName != null)
                    sb.AppendLine($"  Leader: {t.LeaderName} | Đóng lúc: {t.LeaderConfirmedAt:HH:mm}");
                sb.AppendLine();
            }

            MessageBox.Show(sb.ToString(), $"Lịch sử — {lineName}",
                MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        // ─────────────── Âm thanh cảnh báo ───────────────

        private void PlayAlarmSound()
        {
            try
            {
                // ── Âm thanh alarm ──
                // Nguồn: Assets/alarm.wav — lấy từ https://github.com/vitplanocka/eAndon (MIT License)
                // Xem attribution chi tiết tại Assets/NOTICE.txt
                // Nếu file không tồn tại → dùng âm thanh hệ thống thay thế (không crash)
                string assetBase = _assetsDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
                string soundFile = Path.Combine(assetBase, _settings.AlarmSoundFile);
                if (File.Exists(soundFile))
                {
                    var player = new SoundPlayer(soundFile);
                    player.Play();
                }
                else
                {
                    SystemSounds.Exclamation.Play();
                }
            }
            catch { /* Bỏ qua lỗi âm thanh */ }
        }

        // ─────────────── Đóng Form: log tất cả alarm đang mở ───────────────

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _updateTimer?.Stop();

            // Ghi log tất cả alarm đang mở (giống hành vi gốc eAndon)
            foreach (var cell in _gridCells.Values)
            {
                if (cell.Status != TicketStatus.Green && cell.AlarmStartTime.HasValue)
                {
                    string alarmTypeName = _settings.GetAlarmLabel(cell.AlarmTypeIndex);
                    _alarmLogger?.LogAlarmOnShutdown(
                        cell.LineNumber,
                        alarmTypeName,
                        cell.Status.ToString(),
                        cell.AlarmStartTime.Value);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var res in _ownedResources)
                    res?.Dispose();
                _ownedResources.Clear();
            }
            base.Dispose(disposing);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ► [TODO] MỞ RỘNG TERMINAL — Thêm methods tùy chỉnh của bạn tại đây
        // ──────────────────────────────────────────────────────────────────────
        // Ví dụ các chức năng có thể mở rộng:
        //   - Gửi SMS / email khi alarm phát sinh
        //   - Kết nối API ngoài (ERP, MES) để đồng bộ ticket
        //   - Tùy chỉnh logic màu sắc / âm thanh
        //   - Thêm bước xác nhận trung gian (bước 4.5)
        //   - Xuất báo cáo ca làm việc
        // ══════════════════════════════════════════════════════════════════════

    }
}
