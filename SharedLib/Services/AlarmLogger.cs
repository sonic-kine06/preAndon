// File: SharedLib/Services/AlarmLogger.cs
// Mô tả: Ghi log alarm vào file text theo format gốc eAndon.
// Format log gốc: "Event DateTime | ...; Workstation | ...; Alarm type | ...; New Color | ...; Length of alarm (seconds) | ..."
// File log được đặt tại: Logs/alarmlog_YYYY-MM-DD.txt

using System;
using System.IO;

namespace SharedLib.Services
{
    /// <summary>
    /// Ghi log sự kiện alarm vào file text theo format gốc eAndon.
    /// Mỗi ngày tạo một file log riêng: alarmlog_yyyy-MM-dd.txt
    /// </summary>
    public class AlarmLogger
    {
        // Thư mục chứa file log
        private readonly string _logDirectory;

        // Khóa đồng bộ để tránh xung đột ghi file đồng thời
        private static readonly object _lockObj = new object();

        /// <summary>
        /// Khởi tạo AlarmLogger với thư mục lưu log.
        /// </summary>
        /// <param name="logDirectory">Đường dẫn thư mục log, sẽ tự tạo nếu chưa tồn tại</param>
        public AlarmLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// Đường dẫn file log của ngày hiện tại.
        /// </summary>
        private string TodayLogFilePath =>
            Path.Combine(_logDirectory, $"alarmlog_{DateTime.Now:yyyy-MM-dd}.txt");

        /// <summary>
        /// Ghi một sự kiện alarm vào file log theo format gốc eAndon.
        /// Format: "Event DateTime | {dt}; Workstation | {ws}; Alarm type | {type}; New Color | {color}; Length of alarm (seconds) | {sec}"
        /// </summary>
        /// <param name="workstation">Tên/số workstation hoặc Line</param>
        /// <param name="alarmTypeName">Tên loại alarm, ví dụ: "Hỗ trợ Bảo trì"</param>
        /// <param name="newColor">Màu mới sau sự kiện, ví dụ: "Yellow", "Red", "Green"</param>
        /// <param name="durationSeconds">Thời gian alarm (giây), 0 nếu vừa bắt đầu</param>
        /// <param name="eventTime">Thời điểm sự kiện (mặc định = Now)</param>
        public void LogAlarm(string workstation, string alarmTypeName, string newColor,
                             double durationSeconds = 0, DateTime? eventTime = null)
        {
            DateTime dt = eventTime ?? DateTime.Now;
            string logLine = $"Event DateTime | {dt:yyyy-MM-dd HH:mm:ss}; " +
                             $"Workstation | {workstation}; " +
                             $"Alarm type | {alarmTypeName}; " +
                             $"New Color | {newColor}; " +
                             $"Length of alarm (seconds) | {(int)durationSeconds}";

            WriteToFile(TodayLogFilePath, logLine);
        }

        /// <summary>
        /// Ghi log khi một ticket sự cố được đóng (7 bước hoàn tất).
        /// </summary>
        /// <param name="lineNumber">Mã line</param>
        /// <param name="stationName">Tên trạm</param>
        /// <param name="alarmTypeName">Tên loại alarm</param>
        /// <param name="severity">Mức độ: Yellow hoặc Red</param>
        /// <param name="reportedAt">Thời điểm báo lỗi</param>
        /// <param name="closedAt">Thời điểm đóng phiếu</param>
        public void LogTicketClosed(string lineNumber, string stationName, string alarmTypeName,
                                    string severity, DateTime reportedAt, DateTime closedAt)
        {
            double durationSec = (closedAt - reportedAt).TotalSeconds;
            LogAlarm($"{lineNumber} - {stationName}", alarmTypeName, "Green", durationSec, closedAt);
        }

        /// <summary>
        /// Ghi log tất cả alarm đang mở khi Terminal đóng (giống hành vi gốc eAndon).
        /// </summary>
        /// <param name="workstation">Tên workstation</param>
        /// <param name="alarmTypeName">Tên loại alarm</param>
        /// <param name="currentColor">Màu hiện tại</param>
        /// <param name="startTime">Thời điểm bắt đầu alarm</param>
        public void LogAlarmOnShutdown(string workstation, string alarmTypeName, string currentColor, DateTime startTime)
        {
            double durationSec = (DateTime.Now - startTime).TotalSeconds;
            LogAlarm(workstation, alarmTypeName, currentColor, durationSec);
        }

        /// <summary>
        /// Ghi một dòng vào file log, tạo file nếu chưa tồn tại.
        /// Thread-safe nhờ lock.
        /// </summary>
        private void WriteToFile(string filePath, string content)
        {
            lock (_lockObj)
            {
                try
                {
                    File.AppendAllText(filePath, content + Environment.NewLine);
                }
                catch (IOException)
                {
                    // Nếu file đang bị lock bởi process khác, thử lại sau 100ms
                    System.Threading.Thread.Sleep(100);
                    try { File.AppendAllText(filePath, content + Environment.NewLine); }
                    catch { /* Bỏ qua nếu vẫn lỗi */ }
                }
            }
        }
    }
}
