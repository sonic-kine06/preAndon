// File: SharedLib/Services/Analytics/AnomalyDetector.cs
// Mô tả: Phát hiện bất thường trong dữ liệu DailyStats bằng Z-score.
// So sánh downtime tuần hiện tại với trung bình 4 tuần trước.
// Nếu Z-score vượt ngưỡng Z_THRESHOLD = 2.0 → cảnh báo bất thường.
//
// Công thức Z-score:
//   mean  = trung bình downtime 4 tuần trước
//   std   = độ lệch chuẩn downtime 4 tuần trước
//   Z     = (current - mean) / std
// Khi Z >= 2.0: line đang có downtime bất thường cao.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace SharedLib.Services.Analytics
{
    /// <summary>
    /// Kết quả phát hiện bất thường cho một line.
    /// </summary>
    public class AnomalyResult
    {
        /// <summary>Mã line</summary>
        public string LineNumber { get; set; }

        /// <summary>Tên line</summary>
        public string LineName { get; set; }

        /// <summary>Downtime tuần hiện tại (phút)</summary>
        public double CurrentWeekDowntimeMinutes { get; set; }

        /// <summary>Trung bình downtime 4 tuần trước (phút)</summary>
        public double HistoricalMeanMinutes { get; set; }

        /// <summary>Độ lệch chuẩn 4 tuần trước</summary>
        public double HistoricalStdDev { get; set; }

        /// <summary>Giá trị Z-score</summary>
        public double ZScore { get; set; }

        /// <summary>Có bất thường hay không (Z >= Z_THRESHOLD)</summary>
        public bool IsAnomaly { get; set; }

        /// <summary>Mô tả ngắn để hiển thị trên banner cảnh báo</summary>
        public string Description =>
            IsAnomaly
                ? $"{LineName}: Downtime tăng bất thường (Z={ZScore:F1}, {CurrentWeekDowntimeMinutes:F0} phút vs trung bình {HistoricalMeanMinutes:F0} phút)"
                : $"{LineName}: Bình thường (Z={ZScore:F1})";
    }

    /// <summary>
    /// Phát hiện bất thường downtime bằng Z-score so với rolling 4 tuần.
    /// Hiển thị banner cảnh báo màu cam trên DashboardMainForm.
    /// </summary>
    public class AnomalyDetector
    {
        // Z_THRESHOLD = 2.0 tương ứng ~95th percentile trong phân phối chuẩn,
        // nghĩa là chỉ ~5% trường hợp bình thường sẽ vượt ngưỡng này.
        // Chọn 2.0 thay vì 1.5 (90%) để giảm cảnh báo giả; thay vì 3.0 (99.7%)
        // để không bỏ sót các sự cố thực sự bất thường trong môi trường sản xuất.
        private const double Z_THRESHOLD = 2.0;

        // 4 tuần lịch sử = baseline đủ dài để phát hiện xu hướng,
        // đủ ngắn để phản ứng với thay đổi thiết bị/quy trình mới.
        // Tương đương 1 chu kỳ sản xuất tháng. Có thể điều chỉnh
        // nếu chu kỳ sản xuất thực tế khác (ví dụ: 2 tuần hay 8 tuần).
        private const int ROLLING_WEEKS = 4;

        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo AnomalyDetector với đường dẫn file SQLite.
        /// </summary>
        public AnomalyDetector(string dbFilePath)
        {
            _connectionString = $"Data Source={dbFilePath};Version=3;";
        }

        /// <summary>
        /// Phát hiện bất thường cho tất cả các line.
        /// Chỉ trả về những line có IsAnomaly = true.
        /// </summary>
        public List<AnomalyResult> DetectAll()
        {
            var result = new List<AnomalyResult>();
            var lineNumbers = GetAllLines();
            foreach (var (lineNumber, lineName) in lineNumbers)
            {
                var r = Detect(lineNumber, lineName);
                if (r != null && r.IsAnomaly)
                    result.Add(r);
            }
            // Sắp xếp: Z-score cao nhất trước
            result.Sort((a, b) => b.ZScore.CompareTo(a.ZScore));
            return result;
        }

        /// <summary>
        /// Phát hiện bất thường cho một line cụ thể.
        /// Trả về null nếu không đủ dữ liệu lịch sử (ít hơn 2 tuần).
        /// </summary>
        public AnomalyResult Detect(string lineNumber, string lineName = null)
        {
            // Lấy downtime tuần hiện tại (7 ngày gần nhất)
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            string weekStart = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd");
            double currentWeekDowntime = GetTotalDowntimeMinutes(lineNumber, weekStart, today);

            // Lấy downtime từng tuần trong 4 tuần trước
            var historicalWeekly = new List<double>();
            for (int w = 1; w <= ROLLING_WEEKS; w++)
            {
                string hEnd = DateTime.Today.AddDays(-7 * w).ToString("yyyy-MM-dd");
                string hStart = DateTime.Today.AddDays(-7 * w - 6).ToString("yyyy-MM-dd");
                double weekDowntime = GetTotalDowntimeMinutes(lineNumber, hStart, hEnd);
                // Chỉ thêm nếu có dữ liệu
                if (weekDowntime > 0 || HasAnyData(lineNumber, hStart, hEnd))
                    historicalWeekly.Add(weekDowntime);
            }

            // Cần ít nhất 2 tuần lịch sử để tính Z-score
            if (historicalWeekly.Count < 2)
                return null;

            double mean = CalculateMean(historicalWeekly);
            double stdDev = CalculateStdDev(historicalWeekly, mean);

            // Nếu std = 0 (tất cả giống nhau) → không thể tính Z-score
            double zScore = stdDev > 0 ? (currentWeekDowntime - mean) / stdDev : 0;

            return new AnomalyResult
            {
                LineNumber = lineNumber,
                LineName = lineName ?? lineNumber,
                CurrentWeekDowntimeMinutes = Math.Round(currentWeekDowntime, 1),
                HistoricalMeanMinutes = Math.Round(mean, 1),
                HistoricalStdDev = Math.Round(stdDev, 1),
                ZScore = Math.Round(zScore, 2),
                IsAnomaly = zScore >= Z_THRESHOLD
            };
        }

        // ─────────────── Tính toán thống kê ───────────────

        private double CalculateMean(List<double> values)
        {
            double sum = 0;
            foreach (double v in values) sum += v;
            return sum / values.Count;
        }

        private double CalculateStdDev(List<double> values, double mean)
        {
            double sumSq = 0;
            foreach (double v in values) sumSq += (v - mean) * (v - mean);
            return Math.Sqrt(sumSq / values.Count);
        }

        // ─────────────── Data queries ───────────────

        /// <summary>
        /// Tổng downtime (phút) của một line trong khoảng ngày.
        /// </summary>
        private double GetTotalDowntimeMinutes(string lineNumber, string fromDate, string toDate)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COALESCE(SUM(TotalDowntimeSec), 0) AS TotalSec
                        FROM DailyStats
                        WHERE LineNumber = @LineNumber
                          AND StatsDate >= @FromDate
                          AND StatsDate <= @ToDate";
                    cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    object val = cmd.ExecuteScalar();
                    return val == DBNull.Value ? 0 : Convert.ToDouble(val) / 60.0;
                }
            }
        }

        /// <summary>
        /// Kiểm tra có bất kỳ dữ liệu nào của line trong khoảng ngày không.
        /// </summary>
        private bool HasAnyData(string lineNumber, string fromDate, string toDate)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*) FROM DailyStats
                        WHERE LineNumber = @LineNumber
                          AND StatsDate >= @FromDate
                          AND StatsDate <= @ToDate";
                    cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả (LineNumber, LineName) từ DailyStats.
        /// </summary>
        private List<(string lineNumber, string lineName)> GetAllLines()
        {
            var result = new List<(string, string)>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT LineNumber, LineName FROM DailyStats ORDER BY LineNumber";
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            result.Add((reader["LineNumber"].ToString(), reader["LineName"]?.ToString() ?? reader["LineNumber"].ToString()));
                }
            }
            return result;
        }
    }
}
