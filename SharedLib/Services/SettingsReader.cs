// File: SharedLib/Services/SettingsReader.cs
// Mô tả: Đọc và parse file cấu hình settings.txt theo format gốc eAndon.
// Format gốc: "Key : Value" (có dấu cách quanh dấu hai chấm).
// Hỗ trợ tất cả các key gốc + key mới cho 5 trạng thái.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharedLib.Services
{
    /// <summary>
    /// Đọc cấu hình từ file settings.txt và cung cấp các thuộc tính tiện dụng.
    /// Tất cả key đều case-insensitive khi tra cứu.
    /// </summary>
    public class SettingsReader
    {
        // Dictionary lưu toàn bộ key-value từ file settings.txt
        private readonly Dictionary<string, string> _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Đường dẫn file settings.txt đang được đọc</summary>
        public string FilePath { get; }

        /// <summary>
        /// Khởi tạo và đọc ngay file settings.txt tại đường dẫn chỉ định.
        /// </summary>
        /// <param name="filePath">Đường dẫn đầy đủ tới file settings.txt</param>
        public SettingsReader(string filePath)
        {
            FilePath = filePath;
            Load();
        }

        /// <summary>
        /// Đọc lại file settings.txt (dùng khi file thay đổi trong runtime).
        /// </summary>
        public void Load()
        {
            _settings.Clear();
            if (!File.Exists(FilePath)) return;

            foreach (var line in File.ReadAllLines(FilePath))
            {
                // Bỏ qua dòng trống hoặc dòng comment (bắt đầu bằng #)
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                // Format: "Key : Value" — tách tại dấu " : " đầu tiên
                int separatorIndex = line.IndexOf(" : ", StringComparison.Ordinal);
                if (separatorIndex < 0) continue;

                string key = line.Substring(0, separatorIndex).Trim();
                string value = line.Substring(separatorIndex + 3).Trim();
                _settings[key] = value;
            }
        }

        /// <summary>Lấy giá trị string theo key, trả về defaultValue nếu không tìm thấy</summary>
        public string Get(string key, string defaultValue = "")
        {
            return _settings.TryGetValue(key, out string val) ? val : defaultValue;
        }

        /// <summary>Lấy giá trị int theo key</summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (_settings.TryGetValue(key, out string val) && int.TryParse(val, out int result))
                return result;
            return defaultValue;
        }

        /// <summary>Lấy giá trị bool theo key (so sánh với "true")</summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_settings.TryGetValue(key, out string val))
                return val.Equals("true", StringComparison.OrdinalIgnoreCase);
            return defaultValue;
        }

        // ─────────────── Thuộc tính cụ thể ───────────────

        /// <summary>Số lượng loại alarm cấu hình (1-10)</summary>
        public int NumberOfAlarmTypes => GetInt("Number of alarm types to display", 3);

        /// <summary>Tên trạng thái Xanh lá</summary>
        public string GreenStatusName => Get("Green status name", "Green");

        /// <summary>Tên trạng thái Vàng</summary>
        public string YellowStatusName => Get("Yellow status name", "Yellow");

        /// <summary>Tên trạng thái Đỏ</summary>
        public string RedStatusName => Get("Red status name", "Red");

        /// <summary>Tên trạng thái Cam (KTV đang sửa)</summary>
        public string OrangeStatusName => Get("Orange status name", "Repairing");

        /// <summary>Tên trạng thái Xanh dương (chờ Leader)</summary>
        public string BlueStatusName => Get("Blue status name", "WaitLeader");

        /// <summary>Có yêu cầu Leader xác nhận không</summary>
        public bool RequireLeaderConfirmation => GetBool("Require Leader Confirmation", true);

        /// <summary>Tên file logo công ty</summary>
        public string LogoFile => Get("Image file for company logo", "logo.png");

        /// <summary>Tên file âm thanh cảnh báo</summary>
        public string AlarmSoundFile => Get("Alarm sound file", "alarm.wav");

        /// <summary>Hướng dẫn hiển thị (dùng | để xuống dòng)</summary>
        public string Instructions => Get("Instructions (use | for new line)", "");

        /// <summary>Tiêu đề cửa sổ chọn loại alarm</summary>
        public string AlarmWindowTitle => Get("Label for alarm type window - Title", "Chọn mức độ sự cố");

        /// <summary>Nhãn nút Yellow trong AlarmTypeForm</summary>
        public string AlarmWindowYellowLabel => Get("Label for alarm type window - Yellow", "Trạm có vấn đề nhưng vẫn chạy");

        /// <summary>Nhãn nút Red trong AlarmTypeForm</summary>
        public string AlarmWindowRedLabel => Get("Label for alarm type window - Red", "Trạm đã dừng hoàn toàn");

        /// <summary>Nhãn nút Hủy trong AlarmTypeForm</summary>
        public string AlarmWindowCancelLabel => Get("Label for alarm type window - Cancel", "Hủy");

        /// <summary>
        /// Lấy nhãn của alarm type theo chỉ số (1-based).
        /// Ví dụ: GetAlarmLabel(1) → "Hỗ trợ Teamleader"
        /// </summary>
        public string GetAlarmLabel(int index) => Get($"Alarm label {index}", $"Alarm {index}");

        /// <summary>
        /// Lấy tên file icon của alarm type theo chỉ số (1-based).
        /// Ví dụ: GetAlarmImageFile(1) → "Icon1.png"
        /// </summary>
        public string GetAlarmImageFile(int index) => Get($"Alarm image file {index}", $"Icon{index}.png");

        /// <summary>
        /// Lấy danh sách tất cả alarm labels theo số lượng cấu hình.
        /// </summary>
        public List<string> GetAllAlarmLabels()
        {
            var labels = new List<string>();
            for (int i = 1; i <= NumberOfAlarmTypes; i++)
                labels.Add(GetAlarmLabel(i));
            return labels;
        }

        // ─────────────── Cấu hình Email ───────────────

        /// <summary>SMTP server nội bộ</summary>
        public string EmailSmtpServer => Get("Email SMTP Server", "localhost");

        /// <summary>Cổng SMTP</summary>
        public int EmailSmtpPort => GetInt("Email SMTP Port", 587);

        /// <summary>Sử dụng SSL/TLS</summary>
        public bool EmailUseSsl => GetBool("Email Use SSL", false);

        /// <summary>Địa chỉ email người gửi</summary>
        public string EmailSenderAddress => Get("Email Sender Address", "");

        /// <summary>Mật khẩu email người gửi</summary>
        public string EmailSenderPassword => Get("Email Sender Password", "");

        /// <summary>Danh sách email nhận báo cáo tuần (sếp lớn), phân cách bằng |</summary>
        public string[] EmailBossRecipients => Get("Email Boss Recipients", "").Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);

        /// <summary>Danh sách email nhận cảnh báo real-time (quản lý KTV), phân cách bằng |</summary>
        public string[] EmailManagerRecipients => Get("Email Manager Recipients", "").Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);

        /// <summary>Ngày trong tuần gửi báo cáo tuần (mặc định Sunday)</summary>
        public DayOfWeek EmailWeeklyReportDay
        {
            get
            {
                string val = Get("Email Weekly Report Day", "Sunday");
                if (Enum.TryParse(val, true, out DayOfWeek day)) return day;
                return DayOfWeek.Sunday;
            }
        }

        /// <summary>Giờ gửi báo cáo tuần (0-23, mặc định 20)</summary>
        public int EmailWeeklyReportHour => GetInt("Email Weekly Report Hour", 20);

        /// <summary>Số phút chưa có KTV nhận sự cố để gửi cảnh báo</summary>
        public int EmailAlertNoTechMinutes => GetInt("Email Alert No Tech Minutes", 10);

        /// <summary>Số phút KTV sửa quá lâu để gửi cảnh báo</summary>
        public int EmailAlertLongRepairMinutes => GetInt("Email Alert Long Repair Minutes", 30);

        /// <summary>Bật/tắt cảnh báo downtime bất thường</summary>
        public bool EmailAlertAnomalyEnabled => GetBool("Email Alert Anomaly Enabled", true);

        /// <summary>Bật/tắt nhắc nhở bảo dưỡng phòng ngừa</summary>
        public bool EmailMaintenanceReminderEnabled => GetBool("Email Maintenance Reminder Enabled", true);
    }
}
