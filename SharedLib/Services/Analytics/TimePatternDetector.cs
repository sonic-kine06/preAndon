// File: SharedLib/Services/Analytics/TimePatternDetector.cs
// Mô tả: Phát hiện mẫu thời gian của sự cố (ngày trong tuần + giờ trong ngày).
// Đếm tần suất xuất hiện theo (StationId, AlarmTypeIndex, DayOfWeek, Hour).
// Tính xác suất = count / tổng số ngày đã quan sát.
// Nếu xác suất >= 20% → "rủi ro cao" → hiển thị cảnh báo dự phòng.
// Dữ liệu được lưu vào bảng TimePatterns trong SQLite.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace SharedLib.Services.Analytics
{
    /// <summary>
    /// Một mẫu thời gian rủi ro cao: station X hay bị lỗi Y vào ngày Z lúc H giờ.
    /// </summary>
    public class TimePattern
    {
        /// <summary>Mã station</summary>
        public string StationId { get; set; }

        /// <summary>Tên station (từ Tickets)</summary>
        public string StationName { get; set; }

        /// <summary>Chỉ số loại alarm</summary>
        public int AlarmTypeIndex { get; set; }

        /// <summary>Tên loại alarm (từ Tickets)</summary>
        public string AlarmTypeName { get; set; }

        /// <summary>Thứ trong tuần (0=CN, 1=T2, ..., 6=T7)</summary>
        public int DayOfWeek { get; set; }

        /// <summary>Giờ trong ngày (0-23)</summary>
        public int Hour { get; set; }

        /// <summary>Số lần xuất hiện mẫu này trong lịch sử</summary>
        public int OccurrenceCount { get; set; }

        /// <summary>Xác suất xảy ra (0.0 - 1.0)</summary>
        public double Probability { get; set; }

        /// <summary>Ngày cập nhật gần nhất</summary>
        public string UpdatedAt { get; set; }

        /// <summary>Tên thứ trong tuần tiếng Việt</summary>
        public string DayOfWeekName
        {
            get
            {
                string[] names = { "Chủ nhật", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" };
                return DayOfWeek >= 0 && DayOfWeek < 7 ? names[DayOfWeek] : DayOfWeek.ToString();
            }
        }

        /// <summary>Mô tả ngắn để hiển thị trên UI</summary>
        public string Description =>
            $"{StationName} - {AlarmTypeName}: {(Probability * 100):F0}% vào {DayOfWeekName} {Hour:D2}:00";
    }

    /// <summary>
    /// Phát hiện và theo dõi mẫu thời gian của sự cố.
    /// Tự tạo bảng TimePatterns trong SQLite nếu chưa tồn tại.
    /// </summary>
    public class TimePatternDetector
    {
        // Ngưỡng 20%: nếu sự cố xảy ra ít nhất 1 lần trong 5 lần cơ hội
        // (20 ngày/tuần × 1 giờ), được coi là "rủi ro cao" và cần cảnh báo
        // chủ động. Ngưỡng thấp hơn (10%) gây quá nhiều cảnh báo;
        // cao hơn (30%) có thể bỏ sót vấn đề định kỳ thực sự.
        private const double HIGH_RISK_THRESHOLD = 0.20;

        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo TimePatternDetector với đường dẫn file SQLite.
        /// Tự động tạo bảng TimePatterns nếu chưa tồn tại.
        /// </summary>
        public TimePatternDetector(string dbFilePath)
        {
            _connectionString = $"Data Source={dbFilePath};Version=3;";
            EnsureTableExists();
        }

        /// <summary>
        /// Tạo bảng TimePatterns nếu chưa tồn tại.
        /// </summary>
        private void EnsureTableExists()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS TimePatterns (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            StationId TEXT,
                            StationName TEXT,
                            AlarmTypeIndex INTEGER,
                            AlarmTypeName TEXT,
                            DayOfWeek INTEGER,
                            Hour INTEGER,
                            OccurrenceCount INTEGER DEFAULT 0,
                            Probability REAL DEFAULT 0,
                            UpdatedAt TEXT,
                            UNIQUE(StationId, AlarmTypeIndex, DayOfWeek, Hour)
                        )";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Đọc toàn bộ lịch sử Tickets và cập nhật bảng TimePatterns.
        /// Gọi theo lịch (ví dụ: mỗi đêm) hoặc sau khi đóng 50 ticket.
        /// </summary>
        public void RefreshPatterns()
        {
            // Đếm tổng số ngày quan sát (từ ngày đầu tiên đến hôm nay)
            int totalDaysObserved = GetTotalDaysObserved();
            if (totalDaysObserved < 1) totalDaysObserved = 1;

            // Đếm tần suất theo (StationId, AlarmTypeIndex, DayOfWeek, Hour)
            var counts = new Dictionary<string, (string stId, string stName, int alarmIdx, string alarmName, int dow, int hr, int cnt)>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // Lấy tất cả ticket đã đóng, tính DayOfWeek và Hour từ ReportedAt
                    cmd.CommandText = @"
                        SELECT StationId, StationName, AlarmTypeIndex, AlarmTypeName, ReportedAt
                        FROM Tickets
                        WHERE Status = 5 AND ReportedAt IS NOT NULL";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string stationId = reader["StationId"]?.ToString() ?? "";
                            string stationName = reader["StationName"]?.ToString() ?? stationId;
                            int alarmIdx = Convert.ToInt32(reader["AlarmTypeIndex"]);
                            string alarmName = reader["AlarmTypeName"]?.ToString() ?? "";

                            if (!DateTime.TryParse(reader["ReportedAt"]?.ToString(), out DateTime reportedAt))
                                continue;

                            int dow = (int)reportedAt.DayOfWeek; // 0=Sunday
                            int hr = reportedAt.Hour;

                            string key = $"{stationId}|{alarmIdx}|{dow}|{hr}";
                            if (counts.ContainsKey(key))
                            {
                                var existing = counts[key];
                                counts[key] = (existing.stId, existing.stName, existing.alarmIdx, existing.alarmName, existing.dow, existing.hr, existing.cnt + 1);
                            }
                            else
                            {
                                counts[key] = (stationId, stationName, alarmIdx, alarmName, dow, hr, 1);
                            }
                        }
                    }
                }

                // UPSERT từng pattern vào bảng TimePatterns
                using (var transaction = conn.BeginTransaction())
                {
                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    foreach (var kv in counts)
                    {
                        var (stId, stName, alarmIdx, alarmName, dow, hr, cnt) = kv.Value;
                        double prob = (double)cnt / totalDaysObserved;

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = @"
                                INSERT INTO TimePatterns
                                    (StationId, StationName, AlarmTypeIndex, AlarmTypeName,
                                     DayOfWeek, Hour, OccurrenceCount, Probability, UpdatedAt)
                                VALUES
                                    (@StationId, @StationName, @AlarmTypeIndex, @AlarmTypeName,
                                     @DayOfWeek, @Hour, @OccurrenceCount, @Probability, @UpdatedAt)
                                ON CONFLICT(StationId, AlarmTypeIndex, DayOfWeek, Hour) DO UPDATE SET
                                    StationName = excluded.StationName,
                                    AlarmTypeName = excluded.AlarmTypeName,
                                    OccurrenceCount = excluded.OccurrenceCount,
                                    Probability = excluded.Probability,
                                    UpdatedAt = excluded.UpdatedAt";

                            cmd.Parameters.AddWithValue("@StationId", stId);
                            cmd.Parameters.AddWithValue("@StationName", stName);
                            cmd.Parameters.AddWithValue("@AlarmTypeIndex", alarmIdx);
                            cmd.Parameters.AddWithValue("@AlarmTypeName", alarmName);
                            cmd.Parameters.AddWithValue("@DayOfWeek", dow);
                            cmd.Parameters.AddWithValue("@Hour", hr);
                            cmd.Parameters.AddWithValue("@OccurrenceCount", cnt);
                            cmd.Parameters.AddWithValue("@Probability", Math.Round(prob, 4));
                            cmd.Parameters.AddWithValue("@UpdatedAt", now);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Lấy tất cả mẫu có xác suất >= 20% (rủi ro cao).
        /// Sắp xếp theo xác suất giảm dần.
        /// </summary>
        public List<TimePattern> GetHighRiskPatterns()
        {
            var result = new List<TimePattern>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT * FROM TimePatterns
                        WHERE Probability >= @Threshold
                        ORDER BY Probability DESC, OccurrenceCount DESC";
                    cmd.Parameters.AddWithValue("@Threshold", HIGH_RISK_THRESHOLD);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            result.Add(MapPattern(reader));
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy các mẫu rủi ro cao ĐANG KHỚP với thời điểm hiện tại
        /// (cùng DayOfWeek và cùng giờ).
        /// Dùng để hiển thị cảnh báo chủ động trên Dashboard.
        /// </summary>
        public List<TimePattern> GetCurrentRisks()
        {
            int currentDow = (int)DateTime.Now.DayOfWeek;
            int currentHour = DateTime.Now.Hour;

            var result = new List<TimePattern>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT * FROM TimePatterns
                        WHERE DayOfWeek = @DayOfWeek
                          AND Hour = @Hour
                          AND Probability >= @Threshold
                        ORDER BY Probability DESC";

                    cmd.Parameters.AddWithValue("@DayOfWeek", currentDow);
                    cmd.Parameters.AddWithValue("@Hour", currentHour);
                    cmd.Parameters.AddWithValue("@Threshold", HIGH_RISK_THRESHOLD);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            result.Add(MapPattern(reader));
                }
            }
            return result;
        }

        // ─────────────── Helpers ───────────────

        /// <summary>
        /// Đếm tổng số ngày quan sát duy nhất trong bảng Tickets.
        /// </summary>
        private int GetTotalDaysObserved()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(DISTINCT ReportDate) FROM Tickets WHERE Status = 5";
                    object val = cmd.ExecuteScalar();
                    return val == DBNull.Value ? 1 : Math.Max(1, Convert.ToInt32(val));
                }
            }
        }

        /// <summary>
        /// Map một dòng từ SQLite reader sang đối tượng TimePattern.
        /// </summary>
        private TimePattern MapPattern(IDataReader reader)
        {
            return new TimePattern
            {
                StationId = reader["StationId"]?.ToString() ?? "",
                StationName = reader["StationName"]?.ToString() ?? "",
                AlarmTypeIndex = Convert.ToInt32(reader["AlarmTypeIndex"]),
                AlarmTypeName = reader["AlarmTypeName"]?.ToString() ?? "",
                DayOfWeek = Convert.ToInt32(reader["DayOfWeek"]),
                Hour = Convert.ToInt32(reader["Hour"]),
                OccurrenceCount = Convert.ToInt32(reader["OccurrenceCount"]),
                Probability = Convert.ToDouble(reader["Probability"]),
                UpdatedAt = reader["UpdatedAt"]?.ToString() ?? ""
            };
        }
    }
}
