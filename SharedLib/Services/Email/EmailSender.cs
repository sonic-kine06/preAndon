// File: SharedLib/Services/Email/EmailSender.cs
// Mô tả: Gửi email qua SMTP nội bộ sử dụng System.Net.Mail.
// Hỗ trợ nội dung HTML với inline CSS.
// Không cần NuGet bổ sung — System.Net.Mail có sẵn trong .NET.

using System;
using System.Net;
using System.Net.Mail;

namespace SharedLib.Services.Email
{
    /// <summary>
    /// Gửi email HTML qua SMTP nội bộ.
    /// Thread-safe: mỗi lần gọi SendEmail() tạo SmtpClient mới.
    /// </summary>
    public class EmailSender
    {
        private readonly EmailConfig _config;
        private readonly AlarmLogger _logger;

        /// <summary>
        /// Khởi tạo EmailSender với cấu hình SMTP và logger tùy chọn.
        /// </summary>
        public EmailSender(EmailConfig config, AlarmLogger logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
        }

        /// <summary>
        /// Gửi một email HTML đến danh sách người nhận.
        /// Trả về true nếu gửi thành công, false nếu lỗi.
        /// </summary>
        /// <param name="recipients">Danh sách địa chỉ email người nhận</param>
        /// <param name="subject">Tiêu đề email</param>
        /// <param name="htmlBody">Nội dung email dạng HTML</param>
        public bool SendEmail(string[] recipients, string subject, string htmlBody)
        {
            if (recipients == null || recipients.Length == 0)
            {
                _logger?.Log("EmailSender: Không có người nhận — bỏ qua gửi email.");
                return false;
            }

            if (!_config.IsValid())
            {
                _logger?.Log("EmailSender: Cấu hình SMTP chưa đầy đủ — bỏ qua gửi email.");
                return false;
            }

            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_config.SenderAddress, "eAndon System");
                    foreach (var recipient in recipients)
                    {
                        if (!string.IsNullOrWhiteSpace(recipient))
                            message.To.Add(new MailAddress(recipient.Trim()));
                    }
                    message.Subject = subject;
                    message.Body = htmlBody;
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient(_config.SmtpServer, _config.SmtpPort))
                    {
                        smtp.EnableSsl = _config.UseSsl;
                        if (!string.IsNullOrWhiteSpace(_config.SenderPassword))
                            smtp.Credentials = new NetworkCredential(_config.SenderAddress, _config.SenderPassword);
                        smtp.Timeout = 15000; // 15 giây timeout

                        smtp.Send(message);
                    }
                }

                _logger?.Log($"EmailSender: Đã gửi email '{subject}' đến {recipients.Length} người nhận.");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Log($"EmailSender: Lỗi gửi email '{subject}': {ex.Message}");
                return false;
            }
        }
    }
}
