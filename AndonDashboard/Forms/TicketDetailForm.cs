// File: AndonDashboard/Forms/TicketDetailForm.cs
// Mô tả: Form hiển thị chi tiết đầy đủ 1 ticket sự cố.
// Thông tin gồm: ai báo, ai sửa, ai duyệt, thời gian từng bước, ghi chú.

using System;
using System.Drawing;
using System.Windows.Forms;
using SharedLib.Models;

namespace AndonDashboard.Forms
{
    /// <summary>
    /// Form hiển thị thông tin chi tiết của một ticket sự cố.
    /// Chỉ đọc (read-only), không cho phép chỉnh sửa.
    /// </summary>
    public class TicketDetailForm : Form
    {
        private static readonly Color BackgroundColor = Color.FromArgb(44, 62, 80);
        private static readonly Color SectionColor = Color.FromArgb(52, 73, 94);
        private static readonly Color TextColor = Color.White;
        private static readonly Color LabelColor = Color.FromArgb(189, 195, 199);

        public TicketDetailForm(IncidentTicket ticket)
        {
            InitializeUI(ticket);
        }

        private void InitializeUI(IncidentTicket ticket)
        {
            this.Text = $"Chi tiết Phiếu — {ticket?.TicketId ?? "N/A"}";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BackgroundColor;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Size = new Size(520, 620);

            if (ticket == null)
            {
                this.Controls.Add(new Label { Text = "Không tìm thấy ticket", ForeColor = Color.Red,
                    Bounds = new Rectangle(20, 20, 300, 30) });
                return;
            }

            // Panel có scroll
            var panel = new Panel
            {
                AutoScroll = true,
                Bounds = new Rectangle(0, 0, 520, 580),
                BackColor = BackgroundColor
            };
            this.Controls.Add(panel);

            int y = 10, w = 480;

            // ── Tiêu đề ──
            AddLabel(panel, ref y, $"🎫 {ticket.TicketId}", Color.FromArgb(46, 204, 113), 16f, FontStyle.Bold, w);
            y += 5;

            // ── Trạng thái ──
            Color statusColor = GetStatusColor(ticket.CurrentStatus);
            AddLabel(panel, ref y, $"Trạng thái: {ticket.CurrentStatus}", statusColor, 13f, FontStyle.Bold, w);
            y += 10;

            // ── Thông tin Line/Trạm ──
            AddSection(panel, ref y, "📍 Địa điểm", w);
            AddRow(panel, ref y, "Line:", $"{ticket.LineNumber} — {ticket.LineName}", w);
            AddRow(panel, ref y, "Trạm:", $"{ticket.StationId} — {ticket.StationName}", w);
            AddRow(panel, ref y, "Loại alarm:", $"[{ticket.AlarmTypeIndex}] {ticket.AlarmTypeName}", w);
            AddRow(panel, ref y, "Mức độ:", ticket.Severity ?? "—", w);
            y += 10;

            // ── Bước 1-4: Operator ──
            AddSection(panel, ref y, "👷 Bước 1-4: Operator báo lỗi", w);
            AddRow(panel, ref y, "Thời gian:", ticket.ReportedAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "—", w);
            AddRow(panel, ref y, "Mã NV:", ticket.OperatorId ?? "—", w);
            AddRow(panel, ref y, "Họ tên:", ticket.OperatorName ?? "—", w);
            y += 10;

            // ── Bước 5: KTV nhận sửa ──
            AddSection(panel, ref y, "🔧 Bước 5: KTV nhận sửa", w);
            AddRow(panel, ref y, "Check-in:", ticket.TechCheckinAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Chưa nhận", w);
            AddRow(panel, ref y, "Mã KTV:", ticket.TechnicianId ?? "—", w);
            AddRow(panel, ref y, "Tên KTV:", ticket.TechnicianName ?? "—", w);
            if (ticket.ResponseTimeSeconds.HasValue)
                AddRow(panel, ref y, "Response time:", $"{ticket.ResponseTimeSeconds.Value / 60:F1} phút", w);
            y += 10;

            // ── Bước 6: KTV hoàn thành ──
            AddSection(panel, ref y, "✅ Bước 6: KTV hoàn thành sửa", w);
            AddRow(panel, ref y, "Hoàn thành:", ticket.TechFixedAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Chưa sửa", w);
            AddRow(panel, ref y, "Ghi chú:", ticket.FixNote ?? "—", w);
            if (ticket.RepairTimeSeconds.HasValue)
                AddRow(panel, ref y, "Repair time:", $"{ticket.RepairTimeSeconds.Value / 60:F1} phút", w);
            y += 10;

            // ── Bước 7: Leader ──
            AddSection(panel, ref y, "👔 Bước 7: Leader xác nhận", w);
            AddRow(panel, ref y, "Xác nhận:", ticket.LeaderConfirmedAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Chưa xác nhận", w);
            AddRow(panel, ref y, "Mã Leader:", ticket.LeaderId ?? "—", w);
            AddRow(panel, ref y, "Tên Leader:", ticket.LeaderName ?? "—", w);
            y += 10;

            // ── Tổng kết ──
            if (ticket.DowntimeSeconds.HasValue)
            {
                AddSection(panel, ref y, "📊 Tổng kết", w);
                var ts = TimeSpan.FromSeconds(ticket.DowntimeSeconds.Value);
                AddRow(panel, ref y, "Tổng downtime:", $"{(int)ts.TotalMinutes} phút {ts.Seconds} giây", w);
            }

            // Điều chỉnh chiều cao panel
            panel.AutoScrollMinSize = new Size(w + 20, y + 20);

            // Nút đóng
            var btnClose = new Button
            {
                Text = "Đóng",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(149, 165, 166),
                FlatStyle = FlatStyle.Flat,
                Bounds = new Rectangle(15, 580, 100, 35),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void AddSection(Panel panel, ref int y, string title, int w)
        {
            var lbl = new Label
            {
                Text = title,
                ForeColor = Color.FromArgb(52, 152, 219),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = false,
                Bounds = new Rectangle(10, y, w, 22),
                BackColor = Color.FromArgb(36, 50, 64)
            };
            panel.Controls.Add(lbl);
            y += 25;
        }

        private void AddRow(Panel panel, ref int y, string labelText, string value, int w)
        {
            var lbl = new Label
            {
                Text = labelText,
                ForeColor = LabelColor,
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Bounds = new Rectangle(15, y, 120, 20)
            };
            panel.Controls.Add(lbl);

            var val = new Label
            {
                Text = value,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Bounds = new Rectangle(140, y, w - 130, 20),
            };
            panel.Controls.Add(val);
            y += 22;
        }

        private void AddLabel(Panel panel, ref int y, string text, Color color, float size, FontStyle style, int w)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = color,
                Font = new Font("Segoe UI", size, style),
                AutoSize = false,
                Bounds = new Rectangle(10, y, w, (int)(size * 2.5f))
            };
            panel.Controls.Add(lbl);
            y += (int)(size * 2.5f) + 5;
        }

        private Color GetStatusColor(TicketStatus status)
        {
            switch (status)
            {
                case TicketStatus.Green: return Color.FromArgb(46, 204, 113);
                case TicketStatus.Yellow: return Color.FromArgb(241, 196, 15);
                case TicketStatus.Red: return Color.FromArgb(192, 57, 43);
                case TicketStatus.Repairing: return Color.FromArgb(230, 126, 34);
                case TicketStatus.WaitLeader: return Color.FromArgb(52, 152, 219);
                default: return Color.White;
            }
        }
    }
}
