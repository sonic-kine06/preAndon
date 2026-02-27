// File: SharedLib/Services/Email/WeeklyReportBuilder.cs
// Mô tả: Xây dựng nội dung HTML cho email báo cáo tuần gửi sếp lớn.
// Bao gồm: thống kê thiết bị theo line, hiệu suất KTV, dữ liệu Analytics,
// so sánh KPI MTTR, Availability, số sự cố với tuần trước.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharedLib.Services.Analytics;

namespace SharedLib.Services.Email
{
    /// <summary>
    /// Xây dựng nội dung HTML email báo cáo tuần cho Ban Giám Đốc.
    /// Định dạng bảng có màu sắc (xanh/vàng/đỏ) theo tình trạng thiết bị.
    /// </summary>
    public class WeeklyReportBuilder
    {
        private readonly DailyStatsService _statsService;
        private readonly AnalyticsManager _analytics;

        /// <summary>
        /// Khởi tạo builder với DailyStatsService và AnalyticsManager.
        /// </summary>
        public WeeklyReportBuilder(DailyStatsService statsService, AnalyticsManager analytics)
        {
            _statsService = statsService;
            _analytics = analytics;
        }

        /// <summary>
        /// Tạo nội dung HTML báo cáo cho tuần kết thúc vào ngày <paramref name="weekEndDate"/>.
        /// Mặc định là tuần hiện tại nếu không truyền tham số.
        /// </summary>
        public string BuildHtml(DateTime? weekEndDate = null)
        {
            DateTime endDate = weekEndDate ?? DateTime.Today;
            DateTime startDate = endDate.AddDays(-6);
            DateTime prevEnd = startDate.AddDays(-1);
            DateTime prevStart = prevEnd.AddDays(-6);

            string fromStr = startDate.ToString("yyyy-MM-dd");
            string toStr = endDate.ToString("yyyy-MM-dd");
            string prevFromStr = prevStart.ToString("yyyy-MM-dd");
            string prevToStr = prevEnd.ToString("yyyy-MM-dd");

            // Lấy thống kê 2 tuần
            var thisWeekStats = _statsService.GetSummary(fromStr, toStr);
            var prevWeekStats = _statsService.GetSummary(prevFromStr, prevToStr);

            // Lấy analytics
            var analyticsSummary = _analytics.GetDashboardSummary();

            var sb = new StringBuilder();
            sb.Append(BuildHeader());
            sb.Append(BuildIntroSection(startDate, endDate));
            sb.Append(BuildLineStatsSection(thisWeekStats, prevWeekStats));
            sb.Append(BuildKpiSection(thisWeekStats, prevWeekStats));
            sb.Append(BuildTechnicianSection(analyticsSummary.TechnicianRanking));
            sb.Append(BuildAnalyticsSection(analyticsSummary));
            sb.Append(BuildFooter());

            return sb.ToString();
        }

        // ─────────────── Các phần HTML ───────────────

        private string BuildHeader()
        {
            return @"<!DOCTYPE html>
<html lang=""vi"">
<head>
<meta charset=""UTF-8"">
<style>
  body { font-family: Arial, sans-serif; font-size: 14px; color: #333; background: #f5f5f5; margin: 0; padding: 20px; }
  .container { max-width: 900px; margin: 0 auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
  .header { background: linear-gradient(135deg, #1a237e, #283593); color: #fff; padding: 24px 32px; }
  .header h1 { margin: 0 0 4px; font-size: 22px; }
  .header p { margin: 0; opacity: 0.85; font-size: 13px; }
  .section { padding: 20px 32px; border-bottom: 1px solid #e0e0e0; }
  .section h2 { font-size: 16px; color: #1a237e; margin: 0 0 12px; padding-bottom: 6px; border-bottom: 2px solid #e8eaf6; }
  table { width: 100%; border-collapse: collapse; margin-top: 8px; }
  th { background: #e8eaf6; color: #1a237e; padding: 8px 12px; text-align: left; font-size: 13px; }
  td { padding: 7px 12px; font-size: 13px; border-bottom: 1px solid #f0f0f0; }
  tr:hover td { background: #fafafa; }
  .badge-green  { background: #e8f5e9; color: #2e7d32; padding: 2px 8px; border-radius: 12px; font-size: 12px; }
  .badge-yellow { background: #fff8e1; color: #f57f17; padding: 2px 8px; border-radius: 12px; font-size: 12px; }
  .badge-red    { background: #ffebee; color: #c62828; padding: 2px 8px; border-radius: 12px; font-size: 12px; }
  .kpi-grid { display: flex; gap: 16px; flex-wrap: wrap; margin-top: 8px; }
  .kpi-card { flex: 1; min-width: 160px; background: #f5f5f5; border-radius: 8px; padding: 14px 16px; text-align: center; }
  .kpi-card .value { font-size: 26px; font-weight: bold; color: #1a237e; }
  .kpi-card .label { font-size: 12px; color: #666; margin-top: 4px; }
  .kpi-card .delta-up   { color: #c62828; font-size: 12px; }
  .kpi-card .delta-down { color: #2e7d32; font-size: 12px; }
  .kpi-card .delta-same { color: #888; font-size: 12px; }
  .alert-box { background: #fff3e0; border-left: 4px solid #fb8c00; padding: 10px 14px; border-radius: 4px; margin-bottom: 8px; font-size: 13px; }
  .footer { background: #f5f5f5; padding: 14px 32px; font-size: 12px; color: #999; text-align: center; }
</style>
</head>
<body><div class=""container"">";
        }

        private string BuildIntroSection(DateTime startDate, DateTime endDate)
        {
            return $@"
<div class=""header"">
  <h1>📊 Báo Cáo Tuần — Hệ Thống eAndon</h1>
  <p>Tuần: {startDate:dd/MM/yyyy} – {endDate:dd/MM/yyyy} &nbsp;|&nbsp; Gửi lúc: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
</div>";
        }

        private string BuildLineStatsSection(List<DailyStatsRecord> thisWeek, List<DailyStatsRecord> prevWeek)
        {
            // Gom nhóm theo LineNumber (tổng cộng 7 ngày)
            var thisMap = GroupByLine(thisWeek);
            var prevMap = GroupByLine(prevWeek);

            var sb = new StringBuilder();
            sb.Append(@"<div class=""section""><h2>🏭 Tình Trạng Thiết Bị Theo Line</h2>");
            sb.Append(@"<table><tr>
              <th>Line</th>
              <th>Số sự cố</th>
              <th>Tổng downtime</th>
              <th>Downtime tuần trước</th>
              <th>MTTR (phút)</th>
              <th>Availability</th>
            </tr>");

            if (thisMap.Count == 0)
            {
                sb.Append(@"<tr><td colspan=""6"" style=""text-align:center;color:#999"">Không có dữ liệu tuần này</td></tr>");
            }
            else
            {
                foreach (var kv in thisMap.OrderBy(k => k.Key))
                {
                    var cur = kv.Value;
                    prevMap.TryGetValue(kv.Key, out var prev);

                    int prevDowntimeSec = prev?.TotalDowntimeSec ?? 0;
                    double prevDowntimeMin = prevDowntimeSec / 60.0;
                    double curDowntimeMin = cur.TotalDowntimeSec / 60.0;

                    string trend = curDowntimeMin > prevDowntimeMin * 1.2
                        ? "<span class=\"badge-red\">↑ Tăng</span>"
                        : curDowntimeMin < prevDowntimeMin * 0.8
                            ? "<span class=\"badge-green\">↓ Giảm</span>"
                            : "<span class=\"badge-yellow\">~ Bằng</span>";

                    string avail = cur.Availability_Pct >= 95
                        ? $"<span class=\"badge-green\">{cur.Availability_Pct:F1}%</span>"
                        : cur.Availability_Pct >= 80
                            ? $"<span class=\"badge-yellow\">{cur.Availability_Pct:F1}%</span>"
                            : $"<span class=\"badge-red\">{cur.Availability_Pct:F1}%</span>";

                    sb.AppendFormat(@"<tr>
                      <td><strong>{0}</strong></td>
                      <td>{1}</td>
                      <td>{2:F0} phút {3}</td>
                      <td>{4:F0} phút</td>
                      <td>{5:F1}</td>
                      <td>{6}</td>
                    </tr>",
                        HtmlEncode(cur.LineName ?? cur.LineNumber),
                        cur.TotalIncidents,
                        curDowntimeMin, trend,
                        prevDowntimeMin,
                        cur.MTTR_Minutes,
                        avail);
                }
            }

            sb.Append("</table></div>");
            return sb.ToString();
        }

        private string BuildKpiSection(List<DailyStatsRecord> thisWeek, List<DailyStatsRecord> prevWeek)
        {
            double totalIncidents = thisWeek.Sum(r => r.TotalIncidents);
            double totalDowntime = thisWeek.Sum(r => r.TotalDowntimeSec) / 60.0;
            double avgMttr = thisWeek.Count > 0 ? thisWeek.Average(r => r.MTTR_Minutes) : 0;
            double avgAvail = thisWeek.Count > 0 ? thisWeek.Average(r => r.Availability_Pct) : 0;

            double prevIncidents = prevWeek.Sum(r => r.TotalIncidents);
            double prevDowntime = prevWeek.Sum(r => r.TotalDowntimeSec) / 60.0;
            double prevMttr = prevWeek.Count > 0 ? prevWeek.Average(r => r.MTTR_Minutes) : 0;
            double prevAvail = prevWeek.Count > 0 ? prevWeek.Average(r => r.Availability_Pct) : 0;

            var sb = new StringBuilder();
            sb.Append(@"<div class=""section""><h2>📈 KPI Tổng Hợp</h2><div class=""kpi-grid"">");
            sb.Append(KpiCard("Tổng sự cố", totalIncidents.ToString("F0"), prevIncidents, totalIncidents, higherIsBad: true));
            sb.Append(KpiCard("Tổng downtime (phút)", totalDowntime.ToString("F0"), prevDowntime, totalDowntime, higherIsBad: true));
            sb.Append(KpiCard("MTTR TB (phút)", avgMttr.ToString("F1"), prevMttr, avgMttr, higherIsBad: true));
            sb.Append(KpiCard("Availability TB", avgAvail.ToString("F1") + "%", prevAvail, avgAvail, higherIsBad: false));
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private string KpiCard(string label, string value, double prev, double cur, bool higherIsBad)
        {
            string delta = "";
            if (prev > 0)
            {
                double diff = cur - prev;
                double pct = diff / prev * 100;
                bool worse = higherIsBad ? diff > 0 : diff < 0;
                string cls = worse ? "delta-up" : (diff == 0 ? "delta-same" : "delta-down");
                string arrow = diff > 0 ? "▲" : (diff < 0 ? "▼" : "=");
                delta = $@"<div class=""{cls}"">{arrow} {Math.Abs(pct):F0}% so tuần trước</div>";
            }
            return $@"<div class=""kpi-card""><div class=""value"">{HtmlEncode(value)}</div><div class=""label"">{HtmlEncode(label)}</div>{delta}</div>";
        }

        private string BuildTechnicianSection(List<TechnicianStat> ranking)
        {
            var sb = new StringBuilder();
            sb.Append(@"<div class=""section""><h2>👷 Hiệu Suất Kỹ Thuật Viên</h2>");

            if (ranking == null || ranking.Count == 0)
            {
                sb.Append(@"<p style=""color:#999"">Chưa có dữ liệu KTV tuần này.</p>");
            }
            else
            {
                sb.Append(@"<table><tr>
                  <th>#</th><th>Mã NV</th><th>Họ Tên</th>
                  <th>Avg sửa (phút)</th><th>Số lần</th><th>Điểm</th>
                </tr>");
                int rank = 1;
                foreach (var t in ranking.Take(10))
                {
                    string medal = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : $"{rank}.";
                    sb.AppendFormat(@"<tr>
                      <td>{0}</td>
                      <td>{1}</td>
                      <td><strong>{2}</strong></td>
                      <td>{3:F1}</td>
                      <td>{4}</td>
                      <td>{5:F1}</td>
                    </tr>",
                        medal,
                        HtmlEncode(t.TechnicianId ?? "-"),
                        HtmlEncode(t.TechnicianName ?? "-"),
                        t.AvgRepairTimeMinutes,
                        t.RepairCount,
                        t.Score);
                    rank++;
                }
                sb.Append("</table>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private string BuildAnalyticsSection(DashboardAnalyticsSummary summary)
        {
            var sb = new StringBuilder();
            sb.Append(@"<div class=""section""><h2>🤖 Phân Tích & Dự Báo</h2>");

            // Dự đoán downtime
            if (summary.DowntimePredictions != null && summary.DowntimePredictions.Count > 0)
            {
                sb.Append("<p><strong>Dự đoán downtime tuần tới:</strong></p><ul>");
                foreach (var p in summary.DowntimePredictions.Take(5))
                    sb.AppendFormat(@"<li>Line {0}: ~<strong>{1:F0} phút</strong> (độ tin cậy: {2})</li>",
                        HtmlEncode(p.Name ?? p.Key),
                        p.PredictedDowntimeMinutes,
                        HtmlEncode(p.ConfidenceLabel));
                sb.Append("</ul>");
            }

            // Bất thường
            if (summary.Anomalies != null && summary.Anomalies.Count > 0)
            {
                sb.Append(@"<div class=""alert-box"">⚠ <strong>Phát hiện bất thường downtime:</strong><ul style=""margin:4px 0"">");
                foreach (var a in summary.Anomalies)
                    sb.AppendFormat("<li>{0}</li>", HtmlEncode(a.Description));
                sb.Append("</ul></div>");
            }

            // Mẫu thời gian rủi ro cao
            if (summary.HighRiskTimePatterns != null && summary.HighRiskTimePatterns.Count > 0)
            {
                sb.Append("<p><strong>Khuyến nghị bảo dưỡng phòng ngừa:</strong></p><ul>");
                foreach (var pat in summary.HighRiskTimePatterns.Take(5))
                    sb.AppendFormat("<li>{0}</li>", HtmlEncode(pat.Description));
                sb.Append("</ul>");
            }

            if ((summary.DowntimePredictions == null || summary.DowntimePredictions.Count == 0)
                && (summary.Anomalies == null || summary.Anomalies.Count == 0)
                && (summary.HighRiskTimePatterns == null || summary.HighRiskTimePatterns.Count == 0))
            {
                sb.Append(@"<p style=""color:#999"">Chưa đủ dữ liệu lịch sử để phân tích tuần này.</p>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private string BuildFooter()
        {
            return $@"
<div class=""footer"">
  Email tự động từ hệ thống eAndon &nbsp;|&nbsp; {DateTime.Now:dd/MM/yyyy HH:mm} &nbsp;|&nbsp; Vui lòng không reply email này.
</div>
</div></body></html>";
        }

        // ─────────────── Tiện ích ───────────────

        /// <summary>Gom nhóm stats theo LineNumber, cộng dồn các cột số</summary>
        private Dictionary<string, DailyStatsRecord> GroupByLine(List<DailyStatsRecord> records)
        {
            var map = new Dictionary<string, DailyStatsRecord>(StringComparer.OrdinalIgnoreCase);
            if (records == null) return map;

            foreach (var r in records)
            {
                if (!map.TryGetValue(r.LineNumber, out var acc))
                {
                    acc = new DailyStatsRecord
                    {
                        LineNumber = r.LineNumber,
                        LineName = r.LineName
                    };
                    map[r.LineNumber] = acc;
                }
                acc.TotalIncidents += r.TotalIncidents;
                acc.TotalDowntimeSec += r.TotalDowntimeSec;
                // MTTR và Availability: lấy trung bình — tích lũy rồi chia sau
                acc.MTTR_Minutes += r.MTTR_Minutes;
                acc.Availability_Pct += r.Availability_Pct;
            }

            // Chia trung bình MTTR và Availability
            foreach (var kv in map)
            {
                int cnt = records.Count(r => r.LineNumber == kv.Key);
                if (cnt > 1)
                {
                    kv.Value.MTTR_Minutes = kv.Value.MTTR_Minutes / cnt;
                    kv.Value.Availability_Pct = kv.Value.Availability_Pct / cnt;
                }
            }

            return map;
        }

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
