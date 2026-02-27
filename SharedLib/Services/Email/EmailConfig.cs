// File: SharedLib/Services/Email/EmailConfig.cs
// Mô tả: Model chứa cấu hình SMTP và danh sách người nhận email.
// Được tạo từ SettingsReader, truyền cho EmailSender và EmailScheduler.

using System;

namespace SharedLib.Services.Email
{
    /// <summary>
    /// Cấu hình SMTP và danh sách người nhận email cho hệ thống eAndon.
    /// </summary>
    public class EmailConfig
    {
        // ── Cấu hình SMTP ──

        /// <summary>Địa chỉ SMTP server nội bộ</summary>
        public string SmtpServer { get; set; }

        /// <summary>Cổng SMTP (thường 587 cho TLS, 465 cho SSL, 25 cho không mã hóa)</summary>
        public int SmtpPort { get; set; }

        /// <summary>Sử dụng SSL/TLS hay không</summary>
        public bool UseSsl { get; set; }

        /// <summary>Địa chỉ email người gửi (andon@congty.com)</summary>
        public string SenderAddress { get; set; }

        /// <summary>Mật khẩu email người gửi</summary>
        public string SenderPassword { get; set; }

        // ── Danh sách người nhận ──

        /// <summary>Danh sách email nhận báo cáo tuần (giám đốc, trưởng phòng)</summary>
        public string[] BossRecipients { get; set; } = Array.Empty<string>();

        /// <summary>Danh sách email nhận cảnh báo real-time (quản lý KTV, trưởng ca)</summary>
        public string[] ManagerRecipients { get; set; } = Array.Empty<string>();

        // ── Cấu hình lịch gửi báo cáo tuần ──

        /// <summary>Ngày trong tuần gửi báo cáo (mặc định Sunday)</summary>
        public DayOfWeek WeeklyReportDay { get; set; } = DayOfWeek.Sunday;

        /// <summary>Giờ gửi báo cáo tuần (0-23, mặc định 20h)</summary>
        public int WeeklyReportHour { get; set; } = 20;

        // ── Cấu hình ngưỡng cảnh báo ──

        /// <summary>Số phút không có KTV nhận sự cố trước khi gửi cảnh báo</summary>
        public int AlertNoTechMinutes { get; set; } = 10;

        /// <summary>Số phút KTV sửa quá lâu trước khi gửi cảnh báo</summary>
        public int AlertLongRepairMinutes { get; set; } = 30;

        /// <summary>Bật/tắt cảnh báo downtime bất thường (Z-score)</summary>
        public bool AlertAnomalyEnabled { get; set; } = true;

        /// <summary>Bật/tắt nhắc nhở bảo dưỡng phòng ngừa từ TimePatternDetector</summary>
        public bool MaintenanceReminderEnabled { get; set; } = true;

        /// <summary>
        /// Tạo EmailConfig từ SettingsReader.
        /// </summary>
        public static EmailConfig FromSettings(SettingsReader settings)
        {
            return new EmailConfig
            {
                SmtpServer = settings.EmailSmtpServer,
                SmtpPort = settings.EmailSmtpPort,
                UseSsl = settings.EmailUseSsl,
                SenderAddress = settings.EmailSenderAddress,
                SenderPassword = settings.EmailSenderPassword,
                BossRecipients = settings.EmailBossRecipients,
                ManagerRecipients = settings.EmailManagerRecipients,
                WeeklyReportDay = settings.EmailWeeklyReportDay,
                WeeklyReportHour = settings.EmailWeeklyReportHour,
                AlertNoTechMinutes = settings.EmailAlertNoTechMinutes,
                AlertLongRepairMinutes = settings.EmailAlertLongRepairMinutes,
                AlertAnomalyEnabled = settings.EmailAlertAnomalyEnabled,
                MaintenanceReminderEnabled = settings.EmailMaintenanceReminderEnabled
            };
        }

        /// <summary>Kiểm tra cấu hình SMTP hợp lệ tối thiểu (có server và sender)</summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SmtpServer)
                && !string.IsNullOrWhiteSpace(SenderAddress);
        }
    }
}
