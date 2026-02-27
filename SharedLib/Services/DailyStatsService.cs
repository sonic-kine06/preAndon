// File: SharedLib/Services/DailyStatsService.cs
// Mô tả: Tính toán và lưu thống kê hàng ngày theo Line vào bảng DailyStats.
// Các chỉ số: MTTR, MTBF, Availability, TotalIncidents, TotalDowntime, v.v.
//
// Công thức:
//   MTTR (Mean Time To Repair) = TổngDowntime / SốSự cố (phút)
//   MTBF (Mean Time Between Failures) = (1440 - TổngDowntime_phút) / SốSựCố (phút)
//   Availability = MTBF / (MTBF + MTTR) * 100 (%)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace SharedLib.Services
{
    /// <summary>
    /// Bản ghi thống kê DailyStats cho một line trong một ngày.
    /// Ánh xạ trực tiếp vào bảng DailyStats.
    /// </summary>
    public class DailyStatsRecord
    {
        public int Id { get; set; }
        public string StatsDate { get; set; }
        public string LineNumber { get; set; }
        public string LineName { get; set; }
        public int TotalIncidents { get; set; }
        public int TotalDowntimeSec { get; set; }
        public int AvgResponseTimeSec { get; set; }
        public int AvgRepairTimeSec { get; set; }
        public double MTTR_Minutes { get; set; }
        public double MTBF_Minutes { get; set; }
        public double Availability_Pct { get; set; }
        public int YellowCount { get; set; }
        public int RedCount { get; set; }
        public string UpdatedAt { get; set; }

        /// <summary>Tổng downtime dạng chuỗi "HH:mm:ss" để hiển thị</summary>
        public string TotalDowntimeFormatted
        {
            get
            {
                var ts = TimeSpan.FromSeconds(TotalDowntimeSec);
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
        }
    }

    /// <summary>
    /// Service tính toán và quản lý thống kê DailyStats trong SQLite.
    /// Được gọi tự động mỗi khi IncidentService đóng một ticket.
    /// </summary>
    public class DailyStatsService
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo với connection string hoặc đường dẫn file SQLite.
        /// </summary>
        public DailyStatsService(string dbFilePath)
        {
            _connectionString = $"Data Source={dbFilePath};Version=3;";
        }

        /// <summary>
        /// Lấy thống kê DailyStats của một line theo ngày.
        /// </summary>
        public DailyStatsRecord GetForLine(string lineNumber, string date)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT * FROM DailyStats
                        WHERE LineNumber = @LineNumber AND StatsDate = @Date";
                    cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                    cmd.Parameters.AddWithValue("@Date", date);

                    using (var reader = cmd.ExecuteReader())
                        if (reader.Read()) return MapRecord(reader);
                }
            }
            return null;
        }

        /// <summary>
        /// Lấy thống kê tổng hợp nhiều ngày (dùng cho StatisticsForm).
        /// </summary>
        /// <param name="fromDate">Từ ngày (yyyy-MM-dd)</param>
        /// <param name="toDate">Đến ngày (yyyy-MM-dd)</param>
        /// <param name="lineNumber">Lọc theo line (null = tất cả)</param>
        public List<DailyStatsRecord> GetSummary(string fromDate, string toDate, string lineNumber = null)
        {
            var result = new List<DailyStatsRecord>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    string whereClause = "WHERE StatsDate >= @FromDate AND StatsDate <= @ToDate";
                    if (!string.IsNullOrEmpty(lineNumber))
                        whereClause += " AND LineNumber = @LineNumber";

                    cmd.CommandText = $"SELECT * FROM DailyStats {whereClause} ORDER BY StatsDate DESC, LineNumber";
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    if (!string.IsNullOrEmpty(lineNumber))
                        cmd.Parameters.AddWithValue("@LineNumber", lineNumber);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) result.Add(MapRecord(reader));
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy top N line có downtime nhiều nhất trong khoảng ngày.
        /// </summary>
        public List<DailyStatsRecord> GetTopDowntime(string fromDate, string toDate, int topN = 5)
        {
            var result = new List<DailyStatsRecord>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT LineNumber, LineName,
                               SUM(TotalIncidents) AS TotalIncidents,
                               SUM(TotalDowntimeSec) AS TotalDowntimeSec,
                               AVG(MTTR_Minutes) AS MTTR_Minutes,
                               AVG(MTBF_Minutes) AS MTBF_Minutes,
                               AVG(Availability_Pct) AS Availability_Pct,
                               SUM(YellowCount) AS YellowCount,
                               SUM(RedCount) AS RedCount,
                               MAX(StatsDate) AS StatsDate, MAX(UpdatedAt) AS UpdatedAt
                        FROM DailyStats
                        WHERE StatsDate >= @FromDate AND StatsDate <= @ToDate
                        GROUP BY LineNumber, LineName
                        ORDER BY TotalDowntimeSec DESC
                        LIMIT @TopN";

                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    cmd.Parameters.AddWithValue("@TopN", topN);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) result.Add(MapRecord(reader));
                }
            }
            return result;
        }

        /// <summary>
        /// Tính toán lại và UPSERT DailyStats cho một line trong một ngày.
        /// Được gọi tự động sau khi mỗi ticket đóng.
        /// </summary>
        /// <param name="lineNumber">Mã line</param>
        /// <param name="lineName">Tên line</param>
        /// <param name="date">Ngày (yyyy-MM-dd)</param>
        /// <param name="conn">Kết nối SQLite đang mở (để tránh mở connection mới)</param>
        public void UpdateForLine(string lineNumber, string lineName, string date, SQLiteConnection conn = null)
        {
            bool ownConnection = conn == null;
            if (ownConnection)
            {
                conn = new SQLiteConnection(_connectionString);
                conn.Open();
            }

            try
            {
                // Truy vấn tất cả ticket đã đóng của line trong ngày
                int totalIncidents = 0, totalDowntimeSec = 0;
                int totalResponseSec = 0, totalRepairSec = 0;
                int yellowCount = 0, redCount = 0;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Severity, ReportedAt, TechCheckinAt, TechFixedAt, LeaderConfirmedAt
                        FROM Tickets
                        WHERE LineNumber = @LineNumber AND ReportDate = @Date
                          AND Status = 5"; // Status 5 = Closed

                    cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                    cmd.Parameters.AddWithValue("@Date", date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            totalIncidents++;

                            string severity = reader["Severity"]?.ToString();
                            if (severity == "Yellow") yellowCount++;
                            else if (severity == "Red") redCount++;

                            // Downtime = ReportedAt → LeaderConfirmedAt
                            if (DateTime.TryParse(reader["ReportedAt"]?.ToString(), out DateTime reportedAt) &&
                                DateTime.TryParse(reader["LeaderConfirmedAt"]?.ToString(), out DateTime closedAt))
                            {
                                totalDowntimeSec += (int)(closedAt - reportedAt).TotalSeconds;
                            }

                            // Response time = ReportedAt → TechCheckinAt
                            if (DateTime.TryParse(reader["ReportedAt"]?.ToString(), out DateTime rAt) &&
                                DateTime.TryParse(reader["TechCheckinAt"]?.ToString(), out DateTime cAt))
                            {
                                totalResponseSec += (int)(cAt - rAt).TotalSeconds;
                            }

                            // Repair time = TechCheckinAt → TechFixedAt
                            if (DateTime.TryParse(reader["TechCheckinAt"]?.ToString(), out DateTime tcAt) &&
                                DateTime.TryParse(reader["TechFixedAt"]?.ToString(), out DateTime tfAt))
                            {
                                totalRepairSec += (int)(tfAt - tcAt).TotalSeconds;
                            }
                        }
                    }
                }

                // Tính MTTR, MTBF, Availability
                double mttrMin = 0, mtbfMin = 0, availPct = 100;
                int avgResponseSec = 0, avgRepairSec = 0;

                if (totalIncidents > 0)
                {
                    double totalDowntimeMin = totalDowntimeSec / 60.0;
                    double workingHoursMin = 24 * 60; // 1440 phút/ngày

                    mttrMin = totalDowntimeMin / totalIncidents;
                    mtbfMin = (workingHoursMin - totalDowntimeMin) / totalIncidents;
                    availPct = mtbfMin > 0 ? (mtbfMin / (mtbfMin + mttrMin)) * 100 : 0;
                    availPct = Math.Max(0, Math.Min(100, availPct));

                    avgResponseSec = totalResponseSec / totalIncidents;
                    avgRepairSec = totalRepairSec / totalIncidents;
                }

                // UPSERT vào DailyStats
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO DailyStats
                            (StatsDate, LineNumber, LineName, TotalIncidents, TotalDowntimeSec,
                             AvgResponseTimeSec, AvgRepairTimeSec, MTTR_Minutes, MTBF_Minutes,
                             Availability_Pct, YellowCount, RedCount, UpdatedAt)
                        VALUES
                            (@StatsDate, @LineNumber, @LineName, @TotalIncidents, @TotalDowntimeSec,
                             @AvgResponseTimeSec, @AvgRepairTimeSec, @MTTR_Minutes, @MTBF_Minutes,
                             @Availability_Pct, @YellowCount, @RedCount, @UpdatedAt)
                        ON CONFLICT(StatsDate, LineNumber) DO UPDATE SET
                            LineName = excluded.LineName,
                            TotalIncidents = excluded.TotalIncidents,
                            TotalDowntimeSec = excluded.TotalDowntimeSec,
                            AvgResponseTimeSec = excluded.AvgResponseTimeSec,
                            AvgRepairTimeSec = excluded.AvgRepairTimeSec,
                            MTTR_Minutes = excluded.MTTR_Minutes,
                            MTBF_Minutes = excluded.MTBF_Minutes,
                            Availability_Pct = excluded.Availability_Pct,
                            YellowCount = excluded.YellowCount,
                            RedCount = excluded.RedCount,
                            UpdatedAt = excluded.UpdatedAt";

                    cmd.Parameters.AddWithValue("@StatsDate", date);
                    cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                    cmd.Parameters.AddWithValue("@LineName", lineName ?? lineNumber);
                    cmd.Parameters.AddWithValue("@TotalIncidents", totalIncidents);
                    cmd.Parameters.AddWithValue("@TotalDowntimeSec", totalDowntimeSec);
                    cmd.Parameters.AddWithValue("@AvgResponseTimeSec", avgResponseSec);
                    cmd.Parameters.AddWithValue("@AvgRepairTimeSec", avgRepairSec);
                    cmd.Parameters.AddWithValue("@MTTR_Minutes", Math.Round(mttrMin, 2));
                    cmd.Parameters.AddWithValue("@MTBF_Minutes", Math.Round(mtbfMin, 2));
                    cmd.Parameters.AddWithValue("@Availability_Pct", Math.Round(availPct, 2));
                    cmd.Parameters.AddWithValue("@YellowCount", yellowCount);
                    cmd.Parameters.AddWithValue("@RedCount", redCount);
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (ownConnection) conn.Dispose();
            }
        }

        // ─────────────── Map SQLite row → DailyStatsRecord ───────────────
        private DailyStatsRecord MapRecord(IDataReader reader)
        {
            return new DailyStatsRecord
            {
                Id = reader.GetOrdinal("Id") >= 0 ? Convert.ToInt32(reader["Id"]) : 0,
                StatsDate = reader["StatsDate"]?.ToString(),
                LineNumber = reader["LineNumber"]?.ToString(),
                LineName = reader["LineName"]?.ToString(),
                TotalIncidents = Convert.ToInt32(reader["TotalIncidents"]),
                TotalDowntimeSec = Convert.ToInt32(reader["TotalDowntimeSec"]),
                AvgResponseTimeSec = Convert.ToInt32(reader["AvgResponseTimeSec"]),
                AvgRepairTimeSec = Convert.ToInt32(reader["AvgRepairTimeSec"]),
                MTTR_Minutes = Convert.ToDouble(reader["MTTR_Minutes"]),
                MTBF_Minutes = Convert.ToDouble(reader["MTBF_Minutes"]),
                Availability_Pct = Convert.ToDouble(reader["Availability_Pct"]),
                YellowCount = Convert.ToInt32(reader["YellowCount"]),
                RedCount = Convert.ToInt32(reader["RedCount"]),
                UpdatedAt = reader["UpdatedAt"]?.ToString()
            };
        }
    }
}
