// File: SharedLib/Services/Analytics/TechnicianTracker.cs
// Mô tả: Theo dõi hiệu suất KTV dựa trên thời gian sửa chữa trung bình.
// Nhóm theo (TechnicianId, AlarmTypeIndex) để biết KTV nào giỏi loại alarm nào.
// Phương thức SuggestBest() gợi ý KTV tốt nhất cho loại alarm đang xảy ra.
// Dùng để hiển thị gợi ý trên Terminal khi tạo ticket mới.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace SharedLib.Services.Analytics
{
    /// <summary>
    /// Thống kê hiệu suất của một KTV cho một loại alarm.
    /// </summary>
    public class TechnicianStat
    {
        /// <summary>Mã nhân viên KTV</summary>
        public string TechnicianId { get; set; }

        /// <summary>Họ tên KTV</summary>
        public string TechnicianName { get; set; }

        /// <summary>Chỉ số loại alarm</summary>
        public int AlarmTypeIndex { get; set; }

        /// <summary>Thời gian sửa trung bình (phút)</summary>
        public double AvgRepairTimeMinutes { get; set; }

        /// <summary>Số lần đã sửa loại alarm này</summary>
        public int RepairCount { get; set; }

        /// <summary>Thời gian sửa nhanh nhất (phút)</summary>
        public double MinRepairTimeMinutes { get; set; }

        /// <summary>Điểm xếp hạng tổng hợp (dựa trên số lần + thời gian)</summary>
        public double Score { get; set; }
    }

    /// <summary>
    /// Theo dõi và xếp hạng KTV theo hiệu suất sửa chữa.
    /// Gợi ý KTV phù hợp nhất khi có sự cố mới.
    /// </summary>
    public class TechnicianTracker
    {
        // Hệ số nhân Score = 10: chuyển đổi tỷ lệ RepairCount/AvgRepairTime
        // sang khoảng giá trị dễ đọc (0-100 thay vì 0-10).
        // Ví dụ: 15 lần / 12.5 phút × 10 = 12.0 điểm.
        // Đây là hệ số trình bày (presentation multiplier), không ảnh hưởng
        // đến thứ tự xếp hạng vì được áp dụng đồng đều cho tất cả KTV.
        private const double SCORE_MULTIPLIER = 10.0;

        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo TechnicianTracker với đường dẫn file SQLite.
        /// </summary>
        public TechnicianTracker(string dbFilePath)
        {
            _connectionString = $"Data Source={dbFilePath};Version=3;";
        }

        /// <summary>
        /// Gợi ý KTV tốt nhất cho một loại alarm (không lọc theo line).
        /// Ưu tiên KTV có AvgRepairTime thấp nhất và số lần nhiều nhất.
        /// Trả về null nếu không có dữ liệu.
        /// </summary>
        public TechnicianStat SuggestBest(int alarmTypeIndex)
        {
            var stats = GetStatsByAlarmType(alarmTypeIndex);
            return stats.Count > 0 ? stats[0] : null;
        }

        /// <summary>
        /// Gợi ý KTV tốt nhất cho loại alarm + line cụ thể.
        /// Ưu tiên KTV đã xử lý nhiều nhất ở line đó.
        /// </summary>
        public TechnicianStat SuggestBest(int alarmTypeIndex, string lineNumber)
        {
            var stats = GetStatsByAlarmTypeAndLine(alarmTypeIndex, lineNumber);
            if (stats.Count > 0) return stats[0];
            // Fallback: tìm KTV giỏi loại alarm này trên bất kỳ line nào
            return SuggestBest(alarmTypeIndex);
        }

        /// <summary>
        /// Lấy danh sách thống kê KTV theo loại alarm, sắp xếp theo hiệu suất.
        /// Hạng 1 = KTV tốt nhất (AvgRepairTime thấp, số lần nhiều).
        /// </summary>
        public List<TechnicianStat> GetStatsByAlarmType(int alarmTypeIndex = -1)
        {
            var result = new List<TechnicianStat>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    string whereClause = alarmTypeIndex >= 0
                        ? "WHERE AlarmTypeIndex = @AlarmTypeIndex AND Status = 5"
                        : "WHERE Status = 5";

                    cmd.CommandText = $@"
                        SELECT TechnicianId, TechnicianName, AlarmTypeIndex,
                               COUNT(*) AS RepairCount,
                               AVG(CAST((julianday(TechFixedAt) - julianday(TechCheckinAt)) * 1440 AS REAL)) AS AvgRepairMin,
                               MIN(CAST((julianday(TechFixedAt) - julianday(TechCheckinAt)) * 1440 AS REAL)) AS MinRepairMin
                        FROM Tickets
                        {whereClause}
                          AND TechnicianId IS NOT NULL
                          AND TechnicianId != ''
                          AND TechCheckinAt IS NOT NULL
                          AND TechFixedAt IS NOT NULL
                        GROUP BY TechnicianId, TechnicianName, AlarmTypeIndex
                        ORDER BY AvgRepairMin ASC, RepairCount DESC";

                    if (alarmTypeIndex >= 0)
                        cmd.Parameters.AddWithValue("@AlarmTypeIndex", alarmTypeIndex);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            double avgMin = reader["AvgRepairMin"] == DBNull.Value ? 0 : Convert.ToDouble(reader["AvgRepairMin"]);
                            double minMin = reader["MinRepairMin"] == DBNull.Value ? 0 : Convert.ToDouble(reader["MinRepairMin"]);
                            int count = Convert.ToInt32(reader["RepairCount"]);

                            result.Add(new TechnicianStat
                            {
                                TechnicianId = reader["TechnicianId"].ToString(),
                                TechnicianName = reader["TechnicianName"]?.ToString() ?? "",
                                AlarmTypeIndex = Convert.ToInt32(reader["AlarmTypeIndex"]),
                                AvgRepairTimeMinutes = Math.Round(avgMin, 1),
                                MinRepairTimeMinutes = Math.Round(minMin, 1),
                                RepairCount = count,
                                // Điểm: số lần / thời gian trung bình (nhiều + nhanh = điểm cao)
                                Score = avgMin > 0 ? Math.Round(count / avgMin * SCORE_MULTIPLIER, 2) : 0
                            });
                        }
                }
            }
            return result;
        }

        /// <summary>
        /// Xếp hạng KTV tổng thể (tính trên tất cả loại alarm).
        /// Mỗi KTV có điểm tổng = tổng Score trên tất cả alarm type đã làm.
        /// </summary>
        public List<TechnicianStat> GetOverallRanking()
        {
            var result = new List<TechnicianStat>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT TechnicianId, TechnicianName,
                               -1 AS AlarmTypeIndex,
                               COUNT(*) AS RepairCount,
                               AVG(CAST((julianday(TechFixedAt) - julianday(TechCheckinAt)) * 1440 AS REAL)) AS AvgRepairMin,
                               MIN(CAST((julianday(TechFixedAt) - julianday(TechCheckinAt)) * 1440 AS REAL)) AS MinRepairMin
                        FROM Tickets
                        WHERE Status = 5
                          AND TechnicianId IS NOT NULL
                          AND TechnicianId != ''
                          AND TechCheckinAt IS NOT NULL
                          AND TechFixedAt IS NOT NULL
                        GROUP BY TechnicianId, TechnicianName
                        ORDER BY AvgRepairMin ASC, RepairCount DESC";

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            double avgMin = reader["AvgRepairMin"] == DBNull.Value ? 0 : Convert.ToDouble(reader["AvgRepairMin"]);
                            double minMin = reader["MinRepairMin"] == DBNull.Value ? 0 : Convert.ToDouble(reader["MinRepairMin"]);
                            int count = Convert.ToInt32(reader["RepairCount"]);

                            result.Add(new TechnicianStat
                            {
                                TechnicianId = reader["TechnicianId"].ToString(),
                                TechnicianName = reader["TechnicianName"]?.ToString() ?? "",
                                AlarmTypeIndex = -1, // tổng thể
                                AvgRepairTimeMinutes = Math.Round(avgMin, 1),
                                MinRepairTimeMinutes = Math.Round(minMin, 1),
                                RepairCount = count,
                                Score = avgMin > 0 ? Math.Round(count / avgMin * SCORE_MULTIPLIER, 2) : 0
                            });
                        }
                }
            }
            return result;
        }

        // ─────────────── Private helpers ───────────────

        /// <summary>
        /// Lấy thống kê KTV cho loại alarm + line cụ thể.
        /// </summary>
        private List<TechnicianStat> GetStatsByAlarmTypeAndLine(int alarmTypeIndex, string lineNumber)
        {
            var result = new List<TechnicianStat>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT TechnicianId, TechnicianName, AlarmTypeIndex,
                               COUNT(*) AS RepairCount,
                               AVG(CAST((julianday(TechFixedAt) - julianday(TechCheckinAt)) * 1440 AS REAL)) AS AvgRepairMin,
                               MIN(CAST((julianday(TechFixedAt) - julianday(TechCheckinAt)) * 1440 AS REAL)) AS MinRepairMin
                        FROM Tickets
                        WHERE AlarmTypeIndex = @AlarmTypeIndex
                          AND LineNumber = @LineNumber
                          AND Status = 5
                          AND TechnicianId IS NOT NULL
                          AND TechnicianId != ''
                          AND TechCheckinAt IS NOT NULL
                          AND TechFixedAt IS NOT NULL
                        GROUP BY TechnicianId, TechnicianName, AlarmTypeIndex
                        ORDER BY AvgRepairMin ASC, RepairCount DESC";

                    cmd.Parameters.AddWithValue("@AlarmTypeIndex", alarmTypeIndex);
                    cmd.Parameters.AddWithValue("@LineNumber", lineNumber);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            double avgMin = reader["AvgRepairMin"] == DBNull.Value ? 0 : Convert.ToDouble(reader["AvgRepairMin"]);
                            double minMin = reader["MinRepairMin"] == DBNull.Value ? 0 : Convert.ToDouble(reader["MinRepairMin"]);
                            int count = Convert.ToInt32(reader["RepairCount"]);

                            result.Add(new TechnicianStat
                            {
                                TechnicianId = reader["TechnicianId"].ToString(),
                                TechnicianName = reader["TechnicianName"]?.ToString() ?? "",
                                AlarmTypeIndex = Convert.ToInt32(reader["AlarmTypeIndex"]),
                                AvgRepairTimeMinutes = Math.Round(avgMin, 1),
                                MinRepairTimeMinutes = Math.Round(minMin, 1),
                                RepairCount = count,
                                Score = avgMin > 0 ? Math.Round(count / avgMin * SCORE_MULTIPLIER, 2) : 0
                            });
                        }
                }
            }
            return result;
        }
    }
}
