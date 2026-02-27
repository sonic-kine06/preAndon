// File: SharedLib/Services/Analytics/DowntimeEstimator.cs
// Mô tả: Dự đoán thời gian downtime tương lai bằng thuật toán EWMA
// (Exponentially Weighted Moving Average).
// Nguyên lý: giá trị gần đây được ưu tiên hơn giá trị cũ (trọng số mũ).
// Công thức: estimate = ALPHA * latest + (1 - ALPHA) * previous_estimate
// Độ tin cậy tăng dần từ MIN_SAMPLES đến FULL_CONFIDENCE mẫu.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace SharedLib.Services.Analytics
{
    /// <summary>
    /// Kết quả dự đoán downtime cho một line hoặc station.
    /// </summary>
    public class DowntimePrediction
    {
        /// <summary>Mã line hoặc station ID</summary>
        public string Key { get; set; }

        /// <summary>Tên hiển thị</summary>
        public string Name { get; set; }

        /// <summary>Downtime dự đoán (phút)</summary>
        public double PredictedDowntimeMinutes { get; set; }

        /// <summary>Độ tin cậy từ 0.0 đến 1.0 (dựa trên số mẫu)</summary>
        public double Confidence { get; set; }

        /// <summary>Số mẫu lịch sử đã dùng</summary>
        public int SampleCount { get; set; }

        /// <summary>Chuỗi mô tả độ tin cậy (Thấp/Trung bình/Cao)</summary>
        public string ConfidenceLabel
        {
            get
            {
                if (Confidence < 0.33) return "Thấp";
                if (Confidence < 0.66) return "Trung bình";
                return "Cao";
            }
        }
    }

    /// <summary>
    /// Dự đoán downtime bằng EWMA (Exponentially Weighted Moving Average).
    /// Truy vấn lịch sử từ bảng DailyStats và Tickets trong SQLite.
    /// </summary>
    public class DowntimeEstimator
    {
        // Hệ số làm mịn EWMA: 0.3 là giá trị phổ biến trong tài liệu kỹ thuật
        // (Gardner 1985, Holt-Winters). Giá trị nhỏ (0.1-0.2) → ổn định hơn nhưng
        // phản ứng chậm; giá trị lớn (0.5+) → nhạy hơn nhưng dao động.
        // 0.3 cân bằng tốt cho môi trường sản xuất có chu kỳ ổn định.
        private const double ALPHA = 0.3;

        // Cần ít nhất 3 mẫu để EWMA có ý nghĩa thống kê.
        // Với ít hơn 3 điểm, thuật toán chưa "hội tụ" và kết quả không đáng tin cậy.
        private const int MIN_SAMPLES = 3;

        // 20 mẫu (tương đương ~1 tháng nếu đo theo ngày) được coi là đủ
        // để EWMA đạt độ tin cậy cao trong bối cảnh sản xuất 5-6 ngày/tuần.
        private const int FULL_CONFIDENCE = 20;

        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo DowntimeEstimator với đường dẫn file SQLite.
        /// </summary>
        public DowntimeEstimator(string dbFilePath)
        {
            _connectionString = $"Data Source={dbFilePath};Version=3;";
        }

        /// <summary>
        /// Dự đoán downtime cho một line dựa trên toàn bộ lịch sử DailyStats.
        /// </summary>
        public DowntimePrediction PredictDowntime(string lineNumber)
        {
            var samples = GetDailyDowntimeSamples(lineNumber, null);
            return ComputeEwma(lineNumber, lineNumber, samples);
        }

        /// <summary>
        /// Dự đoán downtime cho một line + loại alarm cụ thể.
        /// Dùng bảng Tickets để lọc theo AlarmTypeIndex.
        /// </summary>
        public DowntimePrediction PredictDowntime(string lineNumber, int alarmTypeIndex)
        {
            var samples = GetTicketDowntimeSamples(lineNumber, alarmTypeIndex);
            string key = $"{lineNumber}|{alarmTypeIndex}";
            return ComputeEwma(key, $"{lineNumber} AlarmType#{alarmTypeIndex}", samples);
        }

        /// <summary>
        /// Dự đoán downtime cho một station cụ thể (stationId).
        /// </summary>
        public DowntimePrediction PredictForStation(string stationId)
        {
            var samples = GetStationDowntimeSamples(stationId);
            return ComputeEwma(stationId, stationId, samples);
        }

        /// <summary>
        /// Dự đoán downtime cho tất cả các line có dữ liệu.
        /// </summary>
        public List<DowntimePrediction> PredictAll()
        {
            var result = new List<DowntimePrediction>();
            var lineNumbers = GetAllLineNumbers();
            foreach (string ln in lineNumbers)
            {
                var pred = PredictDowntime(ln);
                if (pred.SampleCount >= MIN_SAMPLES)
                    result.Add(pred);
            }
            // Sắp xếp: downtime dự đoán cao nhất trước
            result.Sort((a, b) => b.PredictedDowntimeMinutes.CompareTo(a.PredictedDowntimeMinutes));
            return result;
        }

        // ─────────────── EWMA core ───────────────

        /// <summary>
        /// Tính EWMA từ danh sách mẫu downtime (phút) theo thứ tự thời gian cũ → mới.
        /// </summary>
        private DowntimePrediction ComputeEwma(string key, string name, List<double> samples)
        {
            if (samples.Count < MIN_SAMPLES)
                return new DowntimePrediction
                {
                    Key = key,
                    Name = name,
                    PredictedDowntimeMinutes = samples.Count > 0 ? samples[samples.Count - 1] : 0,
                    Confidence = 0,
                    SampleCount = samples.Count
                };

            // Khởi tạo EWMA bằng giá trị đầu tiên
            double ewma = samples[0];
            for (int i = 1; i < samples.Count; i++)
                ewma = ALPHA * samples[i] + (1 - ALPHA) * ewma;

            double confidence = Math.Min((double)samples.Count / FULL_CONFIDENCE, 1.0);

            return new DowntimePrediction
            {
                Key = key,
                Name = name,
                PredictedDowntimeMinutes = Math.Round(ewma, 1),
                Confidence = Math.Round(confidence, 2),
                SampleCount = samples.Count
            };
        }

        // ─────────────── Data queries ───────────────

        /// <summary>
        /// Lấy danh sách TotalDowntimeSec (đổi sang phút) từ DailyStats theo line.
        /// Sắp xếp theo ngày tăng dần (cũ → mới) để EWMA tính đúng.
        /// </summary>
        private List<double> GetDailyDowntimeSamples(string lineNumber, string fromDate)
        {
            var result = new List<double>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    string where = "WHERE LineNumber = @LineNumber";
                    if (!string.IsNullOrEmpty(fromDate)) where += " AND StatsDate >= @FromDate";
                    cmd.CommandText = $"SELECT TotalDowntimeSec FROM DailyStats {where} ORDER BY StatsDate ASC";
                    cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                    if (!string.IsNullOrEmpty(fromDate))
                        cmd.Parameters.AddWithValue("@FromDate", fromDate);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            result.Add(Convert.ToDouble(reader["TotalDowntimeSec"]) / 60.0);
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy downtime (phút) từ bảng Tickets, nhóm theo ngày cho line + alarmType.
        /// </summary>
        private List<double> GetTicketDowntimeSamples(string lineNumber, int alarmTypeIndex)
        {
            var result = new List<double>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT ReportDate,
                               SUM(CAST((julianday(COALESCE(LeaderConfirmedAt, TechFixedAt, datetime('now')))
                                       - julianday(ReportedAt)) * 1440 AS REAL)) AS DowntimeMin
                        FROM Tickets
                        WHERE LineNumber = @LineNumber
                          AND AlarmTypeIndex = @AlarmTypeIndex
                          AND Status = 5
                        GROUP BY ReportDate
                        ORDER BY ReportDate ASC";
                    cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                    cmd.Parameters.AddWithValue("@AlarmTypeIndex", alarmTypeIndex);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            double val = reader["DowntimeMin"] == DBNull.Value ? 0 : Convert.ToDouble(reader["DowntimeMin"]);
                            result.Add(val);
                        }
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy downtime (phút) từ Tickets theo station, nhóm theo ngày.
        /// </summary>
        private List<double> GetStationDowntimeSamples(string stationId)
        {
            var result = new List<double>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT ReportDate,
                               SUM(CAST((julianday(COALESCE(LeaderConfirmedAt, TechFixedAt, datetime('now')))
                                       - julianday(ReportedAt)) * 1440 AS REAL)) AS DowntimeMin
                        FROM Tickets
                        WHERE StationId = @StationId
                          AND Status = 5
                        GROUP BY ReportDate
                        ORDER BY ReportDate ASC";
                    cmd.Parameters.AddWithValue("@StationId", stationId);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            double val = reader["DowntimeMin"] == DBNull.Value ? 0 : Convert.ToDouble(reader["DowntimeMin"]);
                            result.Add(val);
                        }
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy danh sách tất cả LineNumber có trong DailyStats.
        /// </summary>
        private List<string> GetAllLineNumbers()
        {
            var result = new List<string>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT LineNumber FROM DailyStats ORDER BY LineNumber";
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            result.Add(reader["LineNumber"].ToString());
                }
            }
            return result;
        }
    }
}
