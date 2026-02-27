// File: AndonDashboard/Forms/StatisticsForm.cs
// Mô tả: Form hiển thị thống kê DailyStats.
// DataGridView lọc theo ngày + line, hiển thị MTTR/MTBF/Availability.
//
// GIAO DIỆN THỰC TẾ (950×600, Sizable):
// ╔═══════════════════════════════════════════════════════════════════╗
// ║  📊 Thống kê Daily Stats — eAndon                                ║
// ╠═══════════════════════════════════════════════════════════════════╣
// ║  panelFilter (DockStyle.Top, Height=55)                          ║
// ║  ┌──────────┐ ┌──────────┐  ┌────┐ ┌────┐ ┌───────┐ ┌────────┐  ║
// ║  │Từ ngày   │ │Đến ngày  │  │Line│ │[🔍]│ │[📉Top]│ │        │  ║
// ║  │[DatePick]│ │[DatePick]│  │[  ]│ │ Lọc│ │Downtime│ │        │  ║
// ║  └──────────┘ └──────────┘  └────┘ └────┘ └───────┘ └────────┘  ║
// ╠═══════════════════════════════════════════════════════════════════╣
// ║  _grid DataGridView (DockStyle.Fill)                              ║
// ║  ┌────────┬────────┬─────────┬────────┬─────────┬──────┬───────┐ ║
// ║  │ Ngày   │Mã Line │Tên Line │Sự cố  │Downtime │Avail.│ MTTR  │ ║
// ║  ├────────┼────────┼─────────┼────────┼─────────┼──────┼───────┤ ║
// ║  │27/02   │  010   │ Line 1  │   3   │01:15:30 │97.5% │ 15.2  │ ║  ← xanh nếu ≥95%
// ║  │26/02   │  010   │ Line 1  │   5   │02:30:00 │94.8% │ 18.0  │ ║  ← vàng nếu 90-95%
// ║  │25/02   │  020   │ Line 2  │  12   │04:00:00 │85.1% │ 20.0  │ ║  ← đỏ nếu <90%
// ║  └────────┴────────┴─────────┴────────┴─────────┴──────┴───────┘ ║
// ╠═══════════════════════════════════════════════════════════════════╣
// ║  _lblSummary (DockStyle.Bottom, Height=25)                        ║
// ║  "  Tổng cộng: 15 bản ghi | Từ 2026-02-20 đến 2026-02-27"       ║
// ╚═══════════════════════════════════════════════════════════════════╝
//
// CÁC CỘT DATAGRIDVIEW (theo thứ tự):
//   StatsDate | LineNumber | LineName | TotalIncidents | TotalDowntime
//   YellowCount | RedCount | AvgResponse(ph) | AvgRepair(ph) | MTTR(ph) | MTBF(ph) | Availability%
//
// MÀU DÒNG TỰ ĐỘNG (tô theo Availability):
//   ≥ 95%  → màu mặc định (không tô)
//   90-95% → nền vàng nhạt (Alpha=100)
//   < 90%  → nền đỏ nhạt (Alpha=100)
//
// ĐỂ SỬA GIAO DIỆN:
//   - Thêm cột mới: gọi _grid.Columns.Add(...) trong SetupGridColumns()
//   - Thêm xuất Excel: xem hướng dẫn Docs/UI_CUSTOMIZE.md#8

using System;
using System.Drawing;
using System.Windows.Forms;
using SharedLib.Services;

namespace AndonDashboard.Forms
{
    /// <summary>
    /// Form thống kê hiển thị DailyStats: MTTR, MTBF, Availability, v.v.
    /// Lọc được theo khoảng ngày và theo Line.
    /// </summary>
    public class StatisticsForm : Form
    {
        private readonly DailyStatsService _statsService;

        // Controls
        private DateTimePicker _dtpFrom;
        private DateTimePicker _dtpTo;
        private TextBox _txtLineFilter;
        private DataGridView _grid;
        private Label _lblSummary;

        private static readonly Color BackgroundColor = Color.FromArgb(44, 62, 80);
        private static readonly Color HeaderColor = Color.FromArgb(36, 50, 64);
        private static readonly Color TextColor = Color.White;

        public StatisticsForm(DailyStatsService statsService)
        {
            _statsService = statsService;
            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Text = "📊 Thống kê Daily Stats — eAndon";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BackgroundColor;
            this.Size = new Size(950, 600);
            this.MinimumSize = new Size(700, 400);

            // ── Panel bộ lọc (trên cùng) ──
            var panelFilter = new Panel
            {
                BackColor = HeaderColor,
                Dock = DockStyle.Top,
                Height = 55
            };
            this.Controls.Add(panelFilter);

            // Label "Từ ngày"
            panelFilter.Controls.Add(new Label
            {
                Text = "Từ ngày:",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10f),
                Bounds = new Rectangle(10, 15, 70, 25),
                AutoSize = false
            });

            // DateTimePicker Từ ngày
            _dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-7),
                Bounds = new Rectangle(82, 12, 110, 30),
                CalendarForeColor = Color.Black
            };
            panelFilter.Controls.Add(_dtpFrom);

            // Label "Đến ngày"
            panelFilter.Controls.Add(new Label
            {
                Text = "Đến ngày:",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10f),
                Bounds = new Rectangle(205, 15, 75, 25),
                AutoSize = false
            });

            // DateTimePicker Đến ngày
            _dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Bounds = new Rectangle(283, 12, 110, 30)
            };
            panelFilter.Controls.Add(_dtpTo);

            // Label "Line"
            panelFilter.Controls.Add(new Label
            {
                Text = "Line:",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10f),
                Bounds = new Rectangle(410, 15, 40, 25),
                AutoSize = false
            });

            // TextBox lọc Line
            _txtLineFilter = new TextBox
            {
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10f),
                Bounds = new Rectangle(453, 12, 80, 30),
                PlaceholderText = "010"
            };
            panelFilter.Controls.Add(_txtLineFilter);

            // Nút Lọc
            var btnFilter = new Button
            {
                Text = "🔍 Lọc",
                ForeColor = TextColor,
                BackColor = Color.FromArgb(52, 152, 219),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Bounds = new Rectangle(545, 10, 100, 35),
                Cursor = Cursors.Hand
            };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadData();
            panelFilter.Controls.Add(btnFilter);

            // Nút Top Downtime
            var btnTop = new Button
            {
                Text = "📉 Top Downtime",
                ForeColor = TextColor,
                BackColor = Color.FromArgb(192, 57, 43),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Bounds = new Rectangle(655, 10, 150, 35),
                Cursor = Cursors.Hand
            };
            btnTop.FlatAppearance.BorderSize = 0;
            btnTop.Click += (s, e) => LoadTopDowntime();
            panelFilter.Controls.Add(btnTop);

            // ── DataGridView ──
            _grid = new DataGridView
            {
                BackgroundColor = BackgroundColor,
                ForeColor = TextColor,
                GridColor = Color.FromArgb(52, 73, 94),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = HeaderColor,
                    ForeColor = TextColor,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(52, 73, 94),
                    ForeColor = TextColor,
                    SelectionBackColor = Color.FromArgb(52, 152, 219),
                    SelectionForeColor = Color.White
                },
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None
            };
            _grid.CellDoubleClick += Grid_CellDoubleClick;
            this.Controls.Add(_grid);

            // ── Label tóm tắt (dưới cùng) ──
            _lblSummary = new Label
            {
                ForeColor = Color.FromArgb(189, 195, 199),
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0),
                BackColor = HeaderColor
            };
            this.Controls.Add(_lblSummary);

            SetupGridColumns();
        }

        // Ngưỡng Availability để tô màu dòng trong DataGridView
        private const double AvailabilityRedThreshold = 90.0;
        private const double AvailabilityYellowThreshold = 95.0;

        private void SetupGridColumns()
        {
            _grid.Columns.Clear();
            _grid.Columns.Add("StatsDate", "Ngày");
            _grid.Columns.Add("LineNumber", "Mã Line");
            _grid.Columns.Add("LineName", "Tên Line");
            _grid.Columns.Add("TotalIncidents", "Sự cố");
            _grid.Columns.Add("TotalDowntime", "Downtime");
            _grid.Columns.Add("YellowCount", "🟡 Vàng");
            _grid.Columns.Add("RedCount", "🔴 Đỏ");
            _grid.Columns.Add("AvgResponse", "Avg Response (phút)");
            _grid.Columns.Add("AvgRepair", "Avg Repair (phút)");
            _grid.Columns.Add("MTTR", "MTTR (phút)");
            _grid.Columns.Add("MTBF", "MTBF (phút)");
            _grid.Columns.Add("Availability", "Availability %");
        }

        private void LoadData()
        {
            string fromDate = _dtpFrom.Value.ToString("yyyy-MM-dd");
            string toDate = _dtpTo.Value.ToString("yyyy-MM-dd");
            string lineFilter = _txtLineFilter.Text.Trim();
            if (string.IsNullOrWhiteSpace(lineFilter)) lineFilter = null;

            var records = _statsService.GetSummary(fromDate, toDate, lineFilter);

            _grid.Rows.Clear();
            foreach (var r in records)
            {
                _grid.Rows.Add(
                    r.StatsDate,
                    r.LineNumber,
                    r.LineName,
                    r.TotalIncidents,
                    r.TotalDowntimeFormatted,
                    r.YellowCount,
                    r.RedCount,
                    (r.AvgResponseTimeSec / 60.0).ToString("F1"),
                    (r.AvgRepairTimeSec / 60.0).ToString("F1"),
                    r.MTTR_Minutes.ToString("F1"),
                    r.MTBF_Minutes.ToString("F1"),
                    r.Availability_Pct.ToString("F1") + "%"
                );

                // Tô màu dòng theo Availability
                var row = _grid.Rows[_grid.Rows.Count - 1];
                if (r.Availability_Pct < AvailabilityRedThreshold)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(100, 192, 57, 43);
                else if (r.Availability_Pct < AvailabilityYellowThreshold)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(100, 241, 196, 15);
            }

            _lblSummary.Text = $"  Tổng cộng: {records.Count} bản ghi | Từ {fromDate} đến {toDate}";
        }

        private void LoadTopDowntime()
        {
            string fromDate = _dtpFrom.Value.ToString("yyyy-MM-dd");
            string toDate = _dtpTo.Value.ToString("yyyy-MM-dd");

            var records = _statsService.GetTopDowntime(fromDate, toDate, 10);
            _grid.Rows.Clear();
            foreach (var r in records)
            {
                _grid.Rows.Add(
                    r.StatsDate,
                    r.LineNumber,
                    r.LineName,
                    r.TotalIncidents,
                    r.TotalDowntimeFormatted,
                    r.YellowCount,
                    r.RedCount,
                    (r.AvgResponseTimeSec / 60.0).ToString("F1"),
                    (r.AvgRepairTimeSec / 60.0).ToString("F1"),
                    r.MTTR_Minutes.ToString("F1"),
                    r.MTBF_Minutes.ToString("F1"),
                    r.Availability_Pct.ToString("F1") + "%"
                );
            }
            _lblSummary.Text = $"  Top 10 Line có Downtime nhiều nhất | Từ {fromDate} đến {toDate}";
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Có thể mở drill-down chi tiết nếu cần
        }
    }
}
