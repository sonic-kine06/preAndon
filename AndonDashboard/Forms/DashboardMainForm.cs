// File: AndonDashboard/Forms/DashboardMainForm.cs
// Mô tả: Form chính của AndonDashboard.
// Hiển thị grid tổng quan tất cả lines với 5 màu trạng thái.
// Dùng FileSystemWatcher để đọc file Data/terminalXX.txt khi có thay đổi.
// Click vào ô → xem TicketDetailForm.
// Có nút thống kê để mở StatisticsForm.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SharedLib.Models;
using SharedLib.Services;

namespace AndonDashboard.Forms
{
    /// <summary>
    /// Form Dashboard tổng quan hiển thị trạng thái tất cả Lines.
    /// Tự động cập nhật qua FileSystemWatcher khi Terminal ghi file.
    /// </summary>
    public class DashboardMainForm : Form
    {
        // ─────────────── Dependencies ───────────────
        private readonly SettingsReader _settings;
        private readonly LineStationReader _lineStationReader;
        private readonly IncidentService _incidentService;
        private readonly DailyStatsService _statsService;
        private readonly string _dataDirectory;

        // ─────────────── FileSystemWatcher ───────────────
        // Theo dõi thư mục Data/ để cập nhật khi Terminal ghi file
        private FileSystemWatcher _watcher;

        // ─────────────── Timer ───────────────
        // Timer cập nhật đồng hồ và đếm thời gian alarm mỗi giây
        private System.Windows.Forms.Timer _refreshTimer;

        // ─────────────── Grid UI ───────────────
        private class DashCell
        {
            public Label CellLabel { get; set; }
            public TicketStatus Status { get; set; } = TicketStatus.Green;
            public DateTime? AlarmStartTime { get; set; }
            public string ActiveTicketId { get; set; }
            public string LineNumber { get; set; }
            public int AlarmTypeIndex { get; set; }
        }

        private Dictionary<string, DashCell> _cells = new Dictionary<string, DashCell>();
        private List<WorkstationEntry> _workstations;

        // ─────────────── Màu 5 trạng thái ───────────────
        // Đồng bộ với TerminalMainForm để 2 ứng dụng hiển thị nhất quán.
        // Sửa tại đây nếu muốn Dashboard có bảng màu khác Terminal.
        private static readonly Color ColorGreen      = Color.FromArgb(46, 204, 113);
        private static readonly Color ColorYellow     = Color.FromArgb(241, 196, 15);
        private static readonly Color ColorRed        = Color.FromArgb(192, 57, 43);
        private static readonly Color ColorOrange     = Color.FromArgb(230, 126, 34);
        private static readonly Color ColorBlue       = Color.FromArgb(52, 152, 219);
        private static readonly Color ColorBackground = Color.FromArgb(44, 62, 80);
        private static readonly Color ColorHeader     = Color.FromArgb(36, 50, 64);

        // Panel chứa grid (để dễ dàng cập nhật)
        private Panel _panelGrid;
        private Label _lblTime;
        private Label _lblStatus;

        public DashboardMainForm(SettingsReader settings, LineStationReader lineStationReader,
                                  IncidentService incidentService, DailyStatsService statsService,
                                  string dataDirectory)
        {
            _settings = settings;
            _lineStationReader = lineStationReader;
            _incidentService = incidentService;
            _statsService = statsService;
            _dataDirectory = dataDirectory;

            _workstations = _lineStationReader.GetWorkstations();

            InitializeUI();
            LoadStateFromDB();
            StartWatcher();
            StartTimer();
        }

        private void InitializeUI()
        {
            int alarmCount = _settings.NumberOfAlarmTypes;  // số cột = số loại alarm
            int rowCount = _workstations.Count;              // số hàng = số line

            // ── Kích thước ô Dashboard (hơi lớn hơn Terminal để dễ đọc từ xa) ──
            // Thay đổi để phù hợp với màn hình TV/monitor của bạn
            int cellW     = 130;  // rộng ô (px) — Dashboard thường rộng hơn Terminal
            int cellH     = 85;   // cao ô (px)
            int headerH   = 55;   // cao hàng tiêu đề (tên alarm)
            int rowHeaderW = 160; // rộng cột tên line
            int padding   = 4;    // khoảng cách giữa các ô

            // +120 để chừa chỗ cho 2 panel phía trên (panelTop + panelLegend)
            int formWidth  = rowHeaderW + alarmCount * (cellW + padding) + padding * 3 + 40;
            int formHeight = 120 + headerH + rowCount * (cellH + padding) + padding * 2 + 50;

            this.Text = "eAndon Dashboard — Tổng quan hệ thống";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorBackground;
            this.Size = new Size(Math.Max(900, formWidth), Math.Max(600, formHeight));  // tối thiểu 900×600

            // ── Panel header trên cùng (chứa tiêu đề + đồng hồ + nút thống kê) ──
            var panelTop = new Panel
            {
                BackColor = ColorHeader,
                Dock = DockStyle.Top,   // dán vào cạnh trên
                Height = 65
            };
            this.Controls.Add(panelTop);

            var lblTitle = new Label
            {
                Text = "🏭 eAndon Dashboard",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize = false,
                Bounds = new Rectangle(15, 12, 350, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelTop.Controls.Add(lblTitle);

            // Đồng hồ số ở giữa header
            // _lblTime là field (có _ ở đầu) vì cần cập nhật trong _refreshTimer
            _lblTime = new Label
            {
                Text = DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy"),
                ForeColor = Color.FromArgb(189, 195, 199),
                Font = new Font("Segoe UI", 12f),
                AutoSize = false,
                Bounds = new Rectangle(400, 12, 300, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelTop.Controls.Add(_lblTime);

            // Nút "📊 Thống kê" ở góc phải — mở StatisticsForm
            var btnStats = new Button
            {
                Text = "📊 Thống kê",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 152, 219),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(this.Width - 200, 15, 130, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnStats.FlatAppearance.BorderSize = 0;
            btnStats.Click += (s, e) =>
            {
                using (var statsForm = new StatisticsForm(_statsService))
                    statsForm.ShowDialog(this);
            };
            panelTop.Controls.Add(btnStats);

            // ── Panel legend màu ──
            var panelLegend = new Panel
            {
                BackColor = Color.FromArgb(40, 56, 70),
                Dock = DockStyle.Top,
                Height = 35
            };
            this.Controls.Add(panelLegend);
            AddLegendItem(panelLegend, 10, "🟢 Bình thường", ColorGreen);
            AddLegendItem(panelLegend, 165, "🟡 " + _settings.YellowStatusName, ColorYellow);
            AddLegendItem(panelLegend, 310, "🔴 " + _settings.RedStatusName, ColorRed);
            AddLegendItem(panelLegend, 455, "🟠 " + _settings.OrangeStatusName, ColorOrange);
            AddLegendItem(panelLegend, 600, "🔵 " + _settings.BlueStatusName, ColorBlue);

            // ── Status bar ──
            _lblStatus = new Label
            {
                Text = "  Đang khởi động...",
                ForeColor = Color.FromArgb(189, 195, 199),
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Bottom,
                Height = 22,
                BackColor = ColorHeader,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(_lblStatus);

            // ── Panel Grid ──
            _panelGrid = new Panel
            {
                AutoScroll = true,
                BackColor = ColorBackground,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(_panelGrid);

            BuildGrid(alarmCount, cellW, cellH, headerH, rowHeaderW, padding);
        }

        private void AddLegendItem(Panel panel, int x, string text, Color color)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = color,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(x, 8)
            };
            panel.Controls.Add(lbl);
        }

        private void BuildGrid(int alarmCount, int cellW, int cellH, int headerH, int rowHeaderW, int padding)
        {
            _panelGrid.Controls.Clear();
            _cells.Clear();

            int startX = 10, startY = 8;

            // Header cột (Alarm Types)
            for (int i = 1; i <= alarmCount; i++)
            {
                int xPos = startX + rowHeaderW + (i - 1) * (cellW + padding);
                var lblCol = new Label
                {
                    Text = _settings.GetAlarmLabel(i),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    BackColor = ColorHeader,
                    AutoSize = false,
                    Bounds = new Rectangle(xPos, startY, cellW, headerH - 5),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle
                };
                _panelGrid.Controls.Add(lblCol);
            }

            // Các hàng (Lines)
            for (int row = 0; row < _workstations.Count; row++)
            {
                var ws = _workstations[row];
                int yPos = startY + headerH + row * (cellH + padding);

                // Label tên Line
                var lblLine = new Label
                {
                    Text = $"{ws.Number}\n{ws.Name}",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    BackColor = ColorHeader,
                    AutoSize = false,
                    Bounds = new Rectangle(startX, yPos, rowHeaderW - 3, cellH),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BorderStyle = BorderStyle.FixedSingle
                };

                // Click vào tên Line → xem danh sách ticket
                string capturedLine = ws.Number;
                string capturedName = ws.Name;
                lblLine.Click += (s, e) => ShowLineTickets(capturedLine, capturedName);
                _panelGrid.Controls.Add(lblLine);

                // Các ô alarm
                for (int col = 1; col <= alarmCount; col++)
                {
                    int xPos = startX + rowHeaderW + (col - 1) * (cellW + padding);
                    string cellKey = $"{ws.Number}_{col}";
                    int capturedCol = col;

                    var lbl = new Label
                    {
                        Name = $"dashcell_{ws.Number}_{col}",
                        Text = "✓",
                        ForeColor = Color.FromArgb(44, 62, 80),
                        BackColor = ColorGreen,
                        Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                        AutoSize = false,
                        Bounds = new Rectangle(xPos, yPos, cellW, cellH),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Cursor = Cursors.Hand,
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    var cell = new DashCell
                    {
                        CellLabel = lbl,
                        LineNumber = ws.Number,
                        AlarmTypeIndex = col
                    };
                    _cells[cellKey] = cell;

                    // Click ô → xem chi tiết ticket
                    lbl.Click += (s, e) => ShowCellDetail(cell);
                    _panelGrid.Controls.Add(lbl);
                }
            }
        }

        // ─────────────── Load state từ DB ───────────────

        private void LoadStateFromDB()
        {
            var openTickets = _incidentService.GetAllOpen();
            foreach (var ticket in openTickets)
            {
                string key = $"{ticket.LineNumber}_{ticket.AlarmTypeIndex}";
                if (_cells.TryGetValue(key, out var cell))
                {
                    cell.Status = ticket.CurrentStatus;
                    cell.AlarmStartTime = ticket.ReportedAt;
                    cell.ActiveTicketId = ticket.TicketId;
                    UpdateCellUI(cell);
                }
            }
            _lblStatus.Text = $"  Cập nhật từ DB lúc {DateTime.Now:HH:mm:ss}";

            // ══════════════════════════════════════════════════════════════════════
            // ► [TODO] HOOK SAU KHI LOAD TRẠNG THÁI — Viết thêm logic tại đây
            // ──────────────────────────────────────────────────────────────────────
            // Ví dụ: hiển thị banner cảnh báo từ AnomalyDetector,
            //        kiểm tra rủi ro thời gian hiện tại (TimePatternDetector),
            //        load cấu hình bổ sung từ server, v.v.
            //
            // var analytics = new Analytics.AnalyticsManager("Data/eandon.db");
            // var summary = analytics.GetDashboardSummary();
            // if (summary.HasAnomalies)
            //     ShowAnomalyBanner(summary.Anomalies[0].Description);
            // ══════════════════════════════════════════════════════════════════════
        }

        // ─────────────── FileSystemWatcher ───────────────

        private void StartWatcher()
        {
            if (!Directory.Exists(_dataDirectory)) return;

            _watcher = new FileSystemWatcher(_dataDirectory, "*.txt")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnDataFileChanged;
            _watcher.Created += OnDataFileChanged;
        }

        private void OnDataFileChanged(object sender, FileSystemEventArgs e)
        {
            // Chạy trên thread UI
            if (this.IsHandleCreated)
                this.BeginInvoke(new Action(() => RefreshFromFiles()));
        }

        /// <summary>
        /// Đọc lại trạng thái từ tất cả file Data/terminal*.txt.
        /// Format mỗi dòng: lineNumber;alarmTypeIndex;status;ticketId;startTime
        /// </summary>
        private void RefreshFromFiles()
        {
            try
            {
                // Reset tất cả ô về Green trước
                foreach (var cell in _cells.Values)
                {
                    cell.Status = TicketStatus.Green;
                    cell.AlarmStartTime = null;
                    cell.ActiveTicketId = null;
                }

                // Đọc từng file terminal
                foreach (string filePath in Directory.GetFiles(_dataDirectory, "*.txt"))
                {
                    if (!filePath.EndsWith(".txt")) continue;
                    try
                    {
                        foreach (string rawLine in File.ReadAllLines(filePath))
                        {
                            if (rawLine.StartsWith("#") || string.IsNullOrWhiteSpace(rawLine)) continue;
                            var parts = rawLine.Split(';');
                            if (parts.Length < 3) continue;

                            string lineNumber = parts[0].Trim();
                            if (!int.TryParse(parts[1].Trim(), out int alarmIdx)) continue;
                            if (!int.TryParse(parts[2].Trim(), out int statusInt)) continue;

                            string cellKey = $"{lineNumber}_{alarmIdx}";
                            if (!_cells.TryGetValue(cellKey, out var cell)) continue;

                            cell.Status = (TicketStatus)statusInt;
                            cell.ActiveTicketId = parts.Length > 3 ? parts[3].Trim() : null;
                            if (parts.Length > 4 && DateTime.TryParse(parts[4].Trim(), out DateTime dt))
                                cell.AlarmStartTime = dt;
                        }
                    }
                    catch { /* Bỏ qua lỗi đọc file đơn lẻ */ }
                }

                // Cập nhật UI
                foreach (var cell in _cells.Values)
                    UpdateCellUI(cell);

                _lblStatus.Text = $"  Cập nhật từ file lúc {DateTime.Now:HH:mm:ss}";
            }
            catch { }
        }

        // ─────────────── Timer cập nhật thời gian ───────────────

        private void StartTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 }; // 1 giây
            _refreshTimer.Tick += (s, e) =>
            {
                _lblTime.Text = DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy");
                // Cập nhật thời gian đếm trên ô đang alarm
                foreach (var cell in _cells.Values)
                    if (cell.Status != TicketStatus.Green && cell.Status != TicketStatus.WaitLeader)
                        UpdateCellUI(cell);
            };
            _refreshTimer.Start();
        }

        // ─────────────── Cập nhật UI ô ───────────────

        private void UpdateCellUI(DashCell cell)
        {
            var lbl = cell.CellLabel;
            switch (cell.Status)
            {
                case TicketStatus.Green:
                    lbl.BackColor = ColorGreen;
                    lbl.ForeColor = Color.FromArgb(44, 62, 80);
                    lbl.Text = "✓";
                    break;
                case TicketStatus.Yellow:
                    lbl.BackColor = ColorYellow;
                    lbl.ForeColor = Color.FromArgb(44, 62, 80);
                    lbl.Text = FormatElapsed(cell.AlarmStartTime);
                    break;
                case TicketStatus.Red:
                    lbl.BackColor = ColorRed;
                    lbl.ForeColor = Color.White;
                    lbl.Text = FormatElapsed(cell.AlarmStartTime);
                    break;
                case TicketStatus.Repairing:
                    lbl.BackColor = ColorOrange;
                    lbl.ForeColor = Color.White;
                    lbl.Text = FormatElapsed(cell.AlarmStartTime);
                    break;
                case TicketStatus.WaitLeader:
                    lbl.BackColor = ColorBlue;
                    lbl.ForeColor = Color.White;
                    lbl.Text = "⏳";
                    break;
            }
        }

        private string FormatElapsed(DateTime? startTime)
        {
            if (!startTime.HasValue) return "●";
            var e = DateTime.Now - startTime.Value;
            if (e.TotalHours >= 1)
                return $"{(int)e.TotalHours}h\n{e.Minutes:D2}m";
            return $"{(int)e.TotalMinutes}m\n{e.Seconds:D2}s";
        }

        // ─────────────── Click ô → xem chi tiết ───────────────

        private void ShowCellDetail(DashCell cell)
        {
            if (string.IsNullOrEmpty(cell.ActiveTicketId))
            {
                // Không có ticket active → xem lịch sử
                ShowLineTickets(cell.LineNumber, cell.LineNumber);
                return;
            }

            var ticket = _incidentService.GetTicket(cell.ActiveTicketId);
            if (ticket == null) return;

            using (var detailForm = new TicketDetailForm(ticket))
                detailForm.ShowDialog(this);
        }

        private void ShowLineTickets(string lineNumber, string lineName)
        {
            var tickets = _incidentService.GetHistory(lineNumber, null);
            if (tickets.Count == 0)
            {
                MessageBox.Show($"Không có lịch sử alarm cho {lineName}",
                    "Lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hiện ticket mới nhất còn active
            var activeTicket = tickets.Find(t => t.Status != (int)TicketStatus.Green
                                                  && t.Status != (int)TicketStatus.Closed);
            if (activeTicket != null)
            {
                using (var detailForm = new TicketDetailForm(activeTicket))
                    detailForm.ShowDialog(this);
            }
            else
            {
                // Không có active → hiện lịch sử
                var msg = new System.Text.StringBuilder();
                msg.AppendLine($"Lịch sử alarm — {lineName}");
                foreach (var t in tickets)
                    msg.AppendLine($"[{t.ReportedAt:dd/MM HH:mm}] {t.AlarmTypeName} | {(TicketStatus)t.Status}");

                MessageBox.Show(msg.ToString(), "Lịch sử Alarm");
            }
        }

        // ─────────────── Đóng Form ───────────────

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _refreshTimer?.Stop();
            _watcher?.Dispose();
        }

        // ══════════════════════════════════════════════════════════════════════
        // ► [TODO] MỞ RỘNG DASHBOARD — Thêm methods tùy chỉnh của bạn tại đây
        // ──────────────────────────────────────────────────────────────────────
        // Ví dụ các chức năng có thể mở rộng:
        //   - Hiển thị banner cảnh báo bất thường (AnomalyDetector)
        //   - Vẽ biểu đồ xu hướng downtime (DowntimeEstimator)
        //   - Kết nối màn hình lớn (TV/Monitor) qua Secondary Screen
        //   - Gửi báo cáo tự động cuối ca / cuối ngày
        //   - Tích hợp bản đồ nhà máy (factory map overlay)
        // ══════════════════════════════════════════════════════════════════════

    }
}
