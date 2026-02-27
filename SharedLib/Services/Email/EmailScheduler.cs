// File: SharedLib/Services/Email/EmailScheduler.cs
// Mô tả: Timer chạy nền, mỗi 60 giây kiểm tra các điều kiện gửi email:
//   - Cảnh báo real-time (chưa có KTV, sửa quá lâu, anomaly, bảo dưỡng)
//   - Báo cáo tuần (mỗi Chủ nhật lúc 20:00 hoặc theo cấu hình)
// Sử dụng System.Threading.Timer — không block UI thread.

using System;
using System.Threading;
using SharedLib.Services.Analytics;

namespace SharedLib.Services.Email
{
    /// <summary>
    /// Lên lịch và kích hoạt gửi email định kỳ.
    /// Chạy bằng System.Threading.Timer, kiểm tra mỗi 60 giây.
    /// Gọi Start() sau khi khởi tạo và Dispose() khi đóng ứng dụng.
    /// </summary>
    public class EmailScheduler : IDisposable
    {
        private readonly RealtimeAlertService _alertService;
        private readonly WeeklyReportBuilder _reportBuilder;
        private readonly EmailSender _emailSender;
        private readonly EmailConfig _config;
        private readonly AlarmLogger _logger;

        private Timer _timer;
        private DateTime _lastWeeklyReportSent = DateTime.MinValue;
        private bool _disposed;

        // Kiểm tra mỗi 60 giây
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Khởi tạo EmailScheduler với tất cả các dependency.
        /// </summary>
        public EmailScheduler(
            RealtimeAlertService alertService,
            WeeklyReportBuilder reportBuilder,
            EmailSender emailSender,
            EmailConfig config,
            AlarmLogger logger = null)
        {
            _alertService = alertService;
            _reportBuilder = reportBuilder;
            _emailSender = emailSender;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Khởi động timer nền. Gọi một lần sau khi khởi tạo.
        /// </summary>
        public void Start()
        {
            if (_disposed) return;
            // Bắt đầu ngay sau 10 giây, sau đó lặp mỗi 60 giây
            _timer = new Timer(OnTimerTick, null,
                TimeSpan.FromSeconds(10),
                CheckInterval);
            _logger?.Log("EmailScheduler: Đã khởi động. Kiểm tra mỗi 60 giây.");
        }

        /// <summary>
        /// Dừng timer và giải phóng tài nguyên.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
            _logger?.Log("EmailScheduler: Đã dừng.");
        }

        // ─────────────── Xử lý timer ───────────────

        private void OnTimerTick(object state)
        {
            try
            {
                // Kiểm tra cảnh báo real-time
                _alertService.CheckAndSendAlerts();

                // Kiểm tra lịch báo cáo tuần
                CheckWeeklyReport();
            }
            catch (Exception ex)
            {
                _logger?.Log($"EmailScheduler: Lỗi trong timer tick: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra xem có đến giờ gửi báo cáo tuần chưa.
        /// Gửi đúng một lần trong cửa sổ 5 phút của giờ cấu hình.
        /// </summary>
        private void CheckWeeklyReport()
        {
            if (_config.BossRecipients == null || _config.BossRecipients.Length == 0) return;

            DateTime now = DateTime.Now; // Local time để so sánh giờ cấu hình

            // Chỉ gửi vào đúng ngày trong tuần và đúng giờ cấu hình
            if (now.DayOfWeek != _config.WeeklyReportDay) return;
            if (now.Hour != _config.WeeklyReportHour) return;

            // Tránh gửi trùng trong cùng một giờ (cửa sổ 1 giờ)
            if (_lastWeeklyReportSent != DateTime.MinValue
                && (now - _lastWeeklyReportSent).TotalHours < 1.0)
                return;

            _lastWeeklyReportSent = now;
            SendWeeklyReport();
        }

        private void SendWeeklyReport()
        {
            try
            {
                _logger?.Log("EmailScheduler: Đang tạo báo cáo tuần...");
                string html = _reportBuilder.BuildHtml();
                string subject = $"[eAndon] 📊 Báo Cáo Tuần — {DateTime.Now:dd/MM/yyyy}";
                bool sent = _emailSender.SendEmail(_config.BossRecipients, subject, html);
                if (sent)
                    _logger?.Log("EmailScheduler: Đã gửi báo cáo tuần thành công.");
                else
                    _logger?.Log("EmailScheduler: Không gửi được báo cáo tuần.");
            }
            catch (Exception ex)
            {
                _logger?.Log($"EmailScheduler: Lỗi khi gửi báo cáo tuần: {ex.Message}");
            }
        }

        /// <summary>
        /// Gửi báo cáo tuần ngay lập tức (dùng để test hoặc gửi thủ công).
        /// </summary>
        public void SendWeeklyReportNow()
        {
            SendWeeklyReport();
        }
    }
}
