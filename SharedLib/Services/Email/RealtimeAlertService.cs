// File: SharedLib/Services/Email/RealtimeAlertService.cs
// Mô tả: Theo dõi và gửi cảnh báo email real-time khi xảy ra sự kiện quan trọng:
//   1. Ticket chưa có KTV nhận quá ngưỡng (mặc định 10 phút)
//   2. KTV đang sửa quá lâu so với trung bình (mặc định 30 phút)
//   3. AnomalyDetector phát hiện downtime bất thường (Z-score)
//   4. TimePatternDetector phát hiện pattern bảo dưỡng cho ngày mai

using System;
using System.Collections.Generic;
using System.Text;
using SharedLib.Models;
using SharedLib.Services.Analytics;

namespace SharedLib.Services.Email
{
    /// <summary>
    /// Dịch vụ gửi cảnh báo email real-time.
    /// Được gọi định kỳ từ EmailScheduler mỗi 60 giây.
    /// </summary>
    public class RealtimeAlertService
    {
        private readonly IncidentService _incidentService;
        private readonly AnomalyDetector _anomalyDetector;
        private readonly TimePatternDetector _timePatternDetector;
        private readonly EmailSender _emailSender;
        private readonly EmailConfig _config;
        private readonly AlarmLogger _logger;

        // Theo dõi để không gửi trùng cảnh báo cho cùng một ticket/sự kiện
        private readonly HashSet<string> _alertedNoTechTickets = new HashSet<string>();
        private readonly HashSet<string> _alertedLongRepairTickets = new HashSet<string>();
        private DateTime _lastAnomalyAlertTime = DateTime.MinValue;
        private DateTime _lastMaintenanceReminderTime = DateTime.MinValue;

        // Thời gian tối thiểu giữa 2 lần gửi cảnh báo anomaly (tránh spam)
        private static readonly TimeSpan AnomalyAlertCooldown = TimeSpan.FromHours(4);
        // Thời gian tối thiểu giữa 2 lần gửi nhắc nhở bảo dưỡng (1 lần/ngày)
        private static readonly TimeSpan MaintenanceReminderCooldown = TimeSpan.FromHours(22);

        /// <summary>
        /// Khởi tạo RealtimeAlertService với các dependency.
        /// </summary>
        public RealtimeAlertService(
            IncidentService incidentService,
            AnomalyDetector anomalyDetector,
            TimePatternDetector timePatternDetector,
            EmailSender emailSender,
            EmailConfig config,
            AlarmLogger logger = null)
        {
            _incidentService = incidentService;
            _anomalyDetector = anomalyDetector;
            _timePatternDetector = timePatternDetector;
            _emailSender = emailSender;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra tất cả điều kiện trigger và gửi email cảnh báo nếu cần.
        /// Được gọi mỗi 60 giây từ EmailScheduler.
        /// </summary>
        public void CheckAndSendAlerts()
        {
            try
            {
                CheckNoTechAssigned();
                CheckLongRepair();
                CheckAnomalies();
                CheckMaintenanceReminders();
            }
            catch (Exception ex)
            {
                _logger?.Log($"RealtimeAlertService: Lỗi khi kiểm tra cảnh báo: {ex.Message}");
            }
        }

        // ─────────────── Trigger 1: Chưa có KTV nhận sự cố ───────────────

        /// <summary>
        /// Kiểm tra các ticket đang mở mà chưa có KTV nhận quá ngưỡng phút.
        /// </summary>
        private void CheckNoTechAssigned()
        {
            if (_config.ManagerRecipients == null || _config.ManagerRecipients.Length == 0) return;

            var openTickets = _incidentService.GetAllOpen();
            DateTime now = DateTime.Now;

            foreach (var ticket in openTickets)
            {
                // Chỉ xét ticket đang ở trạng thái Yellow hoặc Red (chưa có KTV)
                if (ticket.CurrentStatus != TicketStatus.Yellow && ticket.CurrentStatus != TicketStatus.Red)
                    continue;
                if (ticket.TechnicianId != null) continue;
                if (!ticket.ReportedAt.HasValue) continue;

                double waitMinutes = (now - ticket.ReportedAt.Value).TotalMinutes;
                if (waitMinutes < _config.AlertNoTechMinutes) continue;

                // Tránh gửi trùng
                if (_alertedNoTechTickets.Contains(ticket.TicketId)) continue;

                _alertedNoTechTickets.Add(ticket.TicketId);
                SendNoTechAlert(ticket, waitMinutes);
            }
        }

        private void SendNoTechAlert(IncidentTicket ticket, double waitMinutes)
        {
            string subject = $"[eAndon] ⚠ Sự cố chưa có KTV nhận — {ticket.LineName} / {ticket.StationName}";
            string html = BuildNoTechAlertHtml(ticket, waitMinutes);
            _emailSender.SendEmail(_config.ManagerRecipients, subject, html);
            _logger?.Log($"RealtimeAlertService: Đã gửi cảnh báo chưa có KTV cho ticket {ticket.TicketId}");
        }

        private string BuildNoTechAlertHtml(IncidentTicket ticket, double waitMinutes)
        {
            return $@"<!DOCTYPE html><html lang=""vi""><head><meta charset=""UTF-8"">
<style>
  body {{ font-family: Arial, sans-serif; font-size:14px; color:#333; padding:20px; }}
  .box {{ max-width:600px; margin:0 auto; border:2px solid #f57f17; border-radius:8px; overflow:hidden; }}
  .hdr {{ background:#f57f17; color:#fff; padding:16px 20px; }}
  .hdr h2 {{ margin:0; font-size:18px; }}
  .body {{ padding:20px; }}
  table {{ width:100%; border-collapse:collapse; }}
  td {{ padding:8px 12px; border-bottom:1px solid #f0f0f0; font-size:13px; }}
  td:first-child {{ color:#666; width:40%; }}
  .wait {{ font-size:20px; font-weight:bold; color:#c62828; }}
  .footer {{ background:#fafafa; padding:10px 20px; font-size:12px; color:#999; text-align:center; }}
</style></head><body>
<div class=""box"">
  <div class=""hdr""><h2>⚠ Cảnh Báo: Sự Cố Chưa Có KTV Nhận</h2></div>
  <div class=""body"">
    <p>Một sự cố đã chờ <span class=""wait"">{waitMinutes:F0} phút</span> mà chưa có KTV nhận xử lý!</p>
    <table>
      <tr><td>Mã phiếu</td><td><strong>{HtmlEncode(ticket.TicketId)}</strong></td></tr>
      <tr><td>Line</td><td>{HtmlEncode(ticket.LineName ?? ticket.LineNumber)}</td></tr>
      <tr><td>Trạm</td><td>{HtmlEncode(ticket.StationName ?? ticket.StationId)}</td></tr>
      <tr><td>Loại sự cố</td><td>{HtmlEncode(ticket.AlarmTypeName)}</td></tr>
      <tr><td>Mức độ</td><td>{HtmlEncode(ticket.Severity)}</td></tr>
      <tr><td>Thời gian báo</td><td>{ticket.ReportedAt?.ToString("dd/MM/yyyy HH:mm") ?? "-"}</td></tr>
      <tr><td>Thời gian chờ</td><td><strong>{waitMinutes:F0} phút</strong></td></tr>
    </table>
    <p style=""margin-top:12px"">Vui lòng điều phối KTV xử lý ngay.</p>
  </div>
  <div class=""footer"">eAndon System — {DateTime.Now:dd/MM/yyyy HH:mm}</div>
</div>
</body></html>";
        }

        // ─────────────── Trigger 2: KTV sửa quá lâu ───────────────

        /// <summary>
        /// Kiểm tra các ticket đang sửa mà KTV đã làm việc quá ngưỡng phút.
        /// </summary>
        private void CheckLongRepair()
        {
            if (_config.ManagerRecipients == null || _config.ManagerRecipients.Length == 0) return;

            var openTickets = _incidentService.GetAllOpen();
            DateTime now = DateTime.Now;

            foreach (var ticket in openTickets)
            {
                if (ticket.CurrentStatus != TicketStatus.Repairing) continue;
                if (!ticket.TechCheckinAt.HasValue) continue;

                double repairMinutes = (now - ticket.TechCheckinAt.Value).TotalMinutes;
                if (repairMinutes < _config.AlertLongRepairMinutes) continue;

                if (_alertedLongRepairTickets.Contains(ticket.TicketId)) continue;

                _alertedLongRepairTickets.Add(ticket.TicketId);
                SendLongRepairAlert(ticket, repairMinutes);
            }
        }

        private void SendLongRepairAlert(IncidentTicket ticket, double repairMinutes)
        {
            string subject = $"[eAndon] ⏱ KTV sửa quá lâu — {ticket.LineName} ({ticket.TechnicianName})";
            string html = BuildLongRepairAlertHtml(ticket, repairMinutes);
            _emailSender.SendEmail(_config.ManagerRecipients, subject, html);
            _logger?.Log($"RealtimeAlertService: Đã gửi cảnh báo sửa lâu cho ticket {ticket.TicketId}");
        }

        private string BuildLongRepairAlertHtml(IncidentTicket ticket, double repairMinutes)
        {
            return $@"<!DOCTYPE html><html lang=""vi""><head><meta charset=""UTF-8"">
<style>
  body {{ font-family: Arial, sans-serif; font-size:14px; color:#333; padding:20px; }}
  .box {{ max-width:600px; margin:0 auto; border:2px solid #1565c0; border-radius:8px; overflow:hidden; }}
  .hdr {{ background:#1565c0; color:#fff; padding:16px 20px; }}
  .hdr h2 {{ margin:0; font-size:18px; }}
  .body {{ padding:20px; }}
  table {{ width:100%; border-collapse:collapse; }}
  td {{ padding:8px 12px; border-bottom:1px solid #f0f0f0; font-size:13px; }}
  td:first-child {{ color:#666; width:40%; }}
  .time {{ font-size:20px; font-weight:bold; color:#1565c0; }}
  .footer {{ background:#fafafa; padding:10px 20px; font-size:12px; color:#999; text-align:center; }}
</style></head><body>
<div class=""box"">
  <div class=""hdr""><h2>⏱ Cảnh Báo: KTV Sửa Quá Lâu</h2></div>
  <div class=""body"">
    <p>KTV đang sửa <span class=""time"">{repairMinutes:F0} phút</span> — cần kiểm tra hoặc hỗ trợ thêm.</p>
    <table>
      <tr><td>Mã phiếu</td><td><strong>{HtmlEncode(ticket.TicketId)}</strong></td></tr>
      <tr><td>Line</td><td>{HtmlEncode(ticket.LineName ?? ticket.LineNumber)}</td></tr>
      <tr><td>Trạm</td><td>{HtmlEncode(ticket.StationName ?? ticket.StationId)}</td></tr>
      <tr><td>Loại sự cố</td><td>{HtmlEncode(ticket.AlarmTypeName)}</td></tr>
      <tr><td>KTV phụ trách</td><td><strong>{HtmlEncode(ticket.TechnicianName)} ({HtmlEncode(ticket.TechnicianId)})</strong></td></tr>
      <tr><td>Check-in lúc</td><td>{ticket.TechCheckinAt?.ToString("dd/MM/yyyy HH:mm") ?? "-"}</td></tr>
      <tr><td>Thời gian sửa</td><td><strong>{repairMinutes:F0} phút</strong></td></tr>
    </table>
    <p style=""margin-top:12px"">Gợi ý: Điều chuyển KTV hỗ trợ hoặc báo cáo lên trưởng ca.</p>
  </div>
  <div class=""footer"">eAndon System — {DateTime.Now:dd/MM/yyyy HH:mm}</div>
</div>
</body></html>";
        }

        // ─────────────── Trigger 3: Downtime bất thường ───────────────

        /// <summary>
        /// Kiểm tra AnomalyDetector và gửi cảnh báo nếu có line bất thường.
        /// Cooldown 4 giờ để tránh spam.
        /// </summary>
        private void CheckAnomalies()
        {
            if (!_config.AlertAnomalyEnabled) return;
            if (_config.ManagerRecipients == null || _config.ManagerRecipients.Length == 0) return;
            if (DateTime.Now - _lastAnomalyAlertTime < AnomalyAlertCooldown) return;

            var anomalies = _anomalyDetector.DetectAll();
            if (anomalies == null || anomalies.Count == 0) return;

            _lastAnomalyAlertTime = DateTime.Now;
            SendAnomalyAlert(anomalies);
        }

        private void SendAnomalyAlert(List<AnomalyResult> anomalies)
        {
            string subject = $"[eAndon] 🔴 Cảnh báo downtime bất thường — {anomalies.Count} line";
            string html = BuildAnomalyAlertHtml(anomalies);
            _emailSender.SendEmail(_config.ManagerRecipients, subject, html);
            _logger?.Log($"RealtimeAlertService: Đã gửi cảnh báo anomaly cho {anomalies.Count} line");
        }

        private string BuildAnomalyAlertHtml(List<AnomalyResult> anomalies)
        {
            var rows = new StringBuilder();
            foreach (var a in anomalies)
            {
                string zColor = a.ZScore >= 3.0 ? "#c62828" : "#f57f17";
                rows.AppendFormat(@"<tr>
                  <td>{0}</td>
                  <td style=""color:{1};font-weight:bold"">{2:F2}</td>
                  <td>{3:F0} phút</td>
                  <td>{4:F0} phút</td>
                </tr>",
                    HtmlEncode(a.LineName ?? a.LineNumber),
                    zColor, a.ZScore,
                    a.CurrentWeekDowntimeMinutes,
                    a.HistoricalMeanMinutes);
            }

            return $@"<!DOCTYPE html><html lang=""vi""><head><meta charset=""UTF-8"">
<style>
  body {{ font-family: Arial, sans-serif; font-size:14px; color:#333; padding:20px; }}
  .box {{ max-width:650px; margin:0 auto; border:2px solid #c62828; border-radius:8px; overflow:hidden; }}
  .hdr {{ background:#c62828; color:#fff; padding:16px 20px; }}
  .hdr h2 {{ margin:0; font-size:18px; }}
  .body {{ padding:20px; }}
  table {{ width:100%; border-collapse:collapse; margin-top:8px; }}
  th {{ background:#ffebee; color:#c62828; padding:8px 12px; text-align:left; font-size:13px; }}
  td {{ padding:7px 12px; border-bottom:1px solid #f0f0f0; font-size:13px; }}
  .footer {{ background:#fafafa; padding:10px 20px; font-size:12px; color:#999; text-align:center; }}
</style></head><body>
<div class=""box"">
  <div class=""hdr""><h2>🔴 Cảnh Báo: Downtime Bất Thường</h2></div>
  <div class=""body"">
    <p>Phát hiện <strong>{anomalies.Count} line</strong> có downtime vượt ngưỡng bất thường (Z-score ≥ 2.0):</p>
    <table>
      <tr><th>Line</th><th>Z-Score</th><th>Downtime tuần này</th><th>Trung bình lịch sử</th></tr>
      {rows}
    </table>
    <p style=""margin-top:12px"">Vui lòng kiểm tra thiết bị và lên kế hoạch bảo trì.</p>
  </div>
  <div class=""footer"">eAndon System — {DateTime.Now:dd/MM/yyyy HH:mm}</div>
</div>
</body></html>";
        }

        // ─────────────── Trigger 4: Nhắc bảo dưỡng phòng ngừa ───────────────

        /// <summary>
        /// Kiểm tra TimePatternDetector và gửi nhắc nhở bảo dưỡng nếu có pattern rủi ro ngày mai.
        /// Cooldown 22 giờ để chỉ gửi 1 lần/ngày.
        /// </summary>
        private void CheckMaintenanceReminders()
        {
            if (!_config.MaintenanceReminderEnabled) return;
            if (_config.ManagerRecipients == null || _config.ManagerRecipients.Length == 0) return;
            if (DateTime.Now - _lastMaintenanceReminderTime < MaintenanceReminderCooldown) return;

            // Lấy ngày mai
            int tomorrowDow = (int)DateTime.Today.AddDays(1).DayOfWeek;
            var allPatterns = _timePatternDetector.GetHighRiskPatterns();
            var tomorrowPatterns = new List<TimePattern>();
            foreach (var p in allPatterns)
            {
                if (p.DayOfWeek == tomorrowDow)
                    tomorrowPatterns.Add(p);
            }

            if (tomorrowPatterns.Count == 0) return;

            _lastMaintenanceReminderTime = DateTime.Now;
            SendMaintenanceReminder(tomorrowPatterns);
        }

        private void SendMaintenanceReminder(List<TimePattern> patterns)
        {
            string subject = $"[eAndon] 🔧 Nhắc bảo dưỡng phòng ngừa ngày {DateTime.Today.AddDays(1):dd/MM/yyyy}";
            string html = BuildMaintenanceReminderHtml(patterns);
            _emailSender.SendEmail(_config.ManagerRecipients, subject, html);
            _logger?.Log($"RealtimeAlertService: Đã gửi nhắc bảo dưỡng cho {patterns.Count} pattern");
        }

        private string BuildMaintenanceReminderHtml(List<TimePattern> patterns)
        {
            string tomorrow = DateTime.Today.AddDays(1).ToString("dd/MM/yyyy (dddd)");
            var rows = new StringBuilder();
            foreach (var p in patterns)
            {
                rows.AppendFormat(@"<tr>
                  <td>{0}</td>
                  <td>{1}</td>
                  <td>{2:D2}:00</td>
                  <td style=""color:#c62828;font-weight:bold"">{3:F0}%</td>
                  <td>{4}</td>
                </tr>",
                    HtmlEncode(p.StationName ?? p.StationId),
                    HtmlEncode(p.AlarmTypeName),
                    p.Hour,
                    p.Probability * 100,
                    p.OccurrenceCount);
            }

            return $@"<!DOCTYPE html><html lang=""vi""><head><meta charset=""UTF-8"">
<style>
  body {{ font-family: Arial, sans-serif; font-size:14px; color:#333; padding:20px; }}
  .box {{ max-width:700px; margin:0 auto; border:2px solid #388e3c; border-radius:8px; overflow:hidden; }}
  .hdr {{ background:#388e3c; color:#fff; padding:16px 20px; }}
  .hdr h2 {{ margin:0; font-size:18px; }}
  .body {{ padding:20px; }}
  table {{ width:100%; border-collapse:collapse; margin-top:8px; }}
  th {{ background:#e8f5e9; color:#2e7d32; padding:8px 12px; text-align:left; font-size:13px; }}
  td {{ padding:7px 12px; border-bottom:1px solid #f0f0f0; font-size:13px; }}
  .footer {{ background:#fafafa; padding:10px 20px; font-size:12px; color:#999; text-align:center; }}
</style></head><body>
<div class=""box"">
  <div class=""hdr""><h2>🔧 Nhắc Bảo Dưỡng Phòng Ngừa</h2></div>
  <div class=""body"">
    <p>Ngày mai <strong>{HtmlEncode(tomorrow)}</strong> có {patterns.Count} trạm được dự báo rủi ro cao dựa trên lịch sử:</p>
    <table>
      <tr><th>Trạm</th><th>Loại sự cố</th><th>Giờ rủi ro</th><th>Xác suất</th><th>Số lần lịch sử</th></tr>
      {rows}
    </table>
    <p style=""margin-top:12px"">Khuyến nghị: Kiểm tra và bảo dưỡng phòng ngừa trước giờ rủi ro.</p>
  </div>
  <div class=""footer"">eAndon System — {DateTime.Now:dd/MM/yyyy HH:mm}</div>
</div>
</body></html>";
        }

        // ─────────────── Tiện ích ───────────────

        private static string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
