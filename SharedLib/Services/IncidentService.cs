// File: SharedLib/Services/IncidentService.cs
// Mô tả: Service quản lý vòng đời của ticket sự cố (IncidentTicket) trong SQLite.
// Cung cấp đầy đủ CRUD + các thao tác theo 7 bước luồng sự cố.
// Tự động cập nhật DailyStats mỗi khi ticket được đóng.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using SharedLib.Models;

namespace SharedLib.Services
{
    /// <summary>
    /// Quản lý toàn bộ vòng đời của ticket sự cố trong cơ sở dữ liệu SQLite.
    /// Bao gồm: tạo ticket, cập nhật từng bước, đóng ticket, truy vấn.
    /// </summary>
    public class IncidentService
    {
        private readonly string _connectionString;
        private readonly DailyStatsService _dailyStatsService;
        private readonly AlarmLogger _alarmLogger;

        /// <summary>
        /// Khởi tạo IncidentService với đường dẫn file SQLite.
        /// Tự động tạo database và bảng nếu chưa tồn tại.
        /// </summary>
        /// <param name="dbFilePath">Đường dẫn file SQLite, ví dụ: "Data/eandon.db"</param>
        /// <param name="alarmLogger">AlarmLogger để ghi log text file (có thể null)</param>
        public IncidentService(string dbFilePath, AlarmLogger alarmLogger = null)
        {
            // Tạo thư mục chứa DB nếu chưa có
            string dir = Path.GetDirectoryName(dbFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _connectionString = $"Data Source={dbFilePath};Version=3;";
            _alarmLogger = alarmLogger;
            _dailyStatsService = new DailyStatsService(dbFilePath);

            InitializeDatabase();
        }

        /// <summary>Tạo bảng Tickets và DailyStats nếu chưa tồn tại</summary>
        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // Tạo bảng Tickets
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Tickets (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            TicketId TEXT UNIQUE,
                            LineNumber TEXT, LineName TEXT,
                            StationId TEXT, StationName TEXT,
                            AlarmTypeIndex INTEGER, AlarmTypeName TEXT, Severity TEXT,
                            ReportedAt TEXT, OperatorId TEXT, OperatorName TEXT,
                            TechCheckinAt TEXT, TechnicianId TEXT, TechnicianName TEXT,
                            TechFixedAt TEXT, FixNote TEXT,
                            LeaderConfirmedAt TEXT, LeaderId TEXT, LeaderName TEXT,
                            Status INTEGER DEFAULT 1,
                            ReportDate TEXT
                        );";
                    cmd.ExecuteNonQuery();

                    // Tạo bảng DailyStats
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS DailyStats (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            StatsDate TEXT,
                            LineNumber TEXT, LineName TEXT,
                            TotalIncidents INTEGER DEFAULT 0,
                            TotalDowntimeSec INTEGER DEFAULT 0,
                            AvgResponseTimeSec INTEGER DEFAULT 0,
                            AvgRepairTimeSec INTEGER DEFAULT 0,
                            MTTR_Minutes REAL DEFAULT 0,
                            MTBF_Minutes REAL DEFAULT 0,
                            Availability_Pct REAL DEFAULT 100,
                            YellowCount INTEGER DEFAULT 0,
                            RedCount INTEGER DEFAULT 0,
                            UpdatedAt TEXT,
                            UNIQUE(StatsDate, LineNumber)
                        );";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ─────────────── Tạo kết nối ───────────────
        private SQLiteConnection CreateConnection()
        {
            var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // Dùng chung 1 instance Random để tránh trùng khi gọi nhiều lần nhanh
        private static readonly Random _random = new Random();

        // ─────────────── Tạo TicketId duy nhất ───────────────
        private string GenerateTicketId()
        {
            return $"TKT-{DateTime.Now:yyyyMMdd-HHmmss}-{_random.Next(100, 999)}";
        }

        // ─────────────── Format DateTime cho SQLite ───────────────
        private string FormatDt(DateTime? dt) => dt?.ToString("yyyy-MM-dd HH:mm:ss");

        // ─────────────── Parse DateTime từ SQLite ───────────────
        private DateTime? ParseDt(object val)
        {
            if (val == null || val == DBNull.Value) return null;
            if (DateTime.TryParse(val.ToString(), out DateTime dt)) return dt;
            return null;
        }

        /// <summary>
        /// Bước 1-4: Operator mở ticket mới (báo lỗi).
        /// Tạo ticket với trạng thái Yellow hoặc Red.
        /// </summary>
        public IncidentTicket OpenIncident(string lineNumber, string lineName,
            string stationId, string stationName,
            int alarmTypeIndex, string alarmTypeName,
            string severity, // "Yellow" hoặc "Red"
            string operatorId, string operatorName)
        {
            var ticket = new IncidentTicket
            {
                TicketId = GenerateTicketId(),
                LineNumber = lineNumber,
                LineName = lineName,
                StationId = stationId,
                StationName = stationName,
                AlarmTypeIndex = alarmTypeIndex,
                AlarmTypeName = alarmTypeName,
                Severity = severity,
                ReportedAt = DateTime.Now,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Status = severity == "Red" ? (int)TicketStatus.Red : (int)TicketStatus.Yellow,
                ReportDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            using (var conn = CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Tickets
                        (TicketId, LineNumber, LineName, StationId, StationName,
                         AlarmTypeIndex, AlarmTypeName, Severity,
                         ReportedAt, OperatorId, OperatorName, Status, ReportDate)
                    VALUES
                        (@TicketId, @LineNumber, @LineName, @StationId, @StationName,
                         @AlarmTypeIndex, @AlarmTypeName, @Severity,
                         @ReportedAt, @OperatorId, @OperatorName, @Status, @ReportDate)";

                cmd.Parameters.AddWithValue("@TicketId", ticket.TicketId);
                cmd.Parameters.AddWithValue("@LineNumber", ticket.LineNumber);
                cmd.Parameters.AddWithValue("@LineName", ticket.LineName);
                cmd.Parameters.AddWithValue("@StationId", ticket.StationId);
                cmd.Parameters.AddWithValue("@StationName", ticket.StationName);
                cmd.Parameters.AddWithValue("@AlarmTypeIndex", ticket.AlarmTypeIndex);
                cmd.Parameters.AddWithValue("@AlarmTypeName", ticket.AlarmTypeName);
                cmd.Parameters.AddWithValue("@Severity", ticket.Severity);
                cmd.Parameters.AddWithValue("@ReportedAt", FormatDt(ticket.ReportedAt));
                cmd.Parameters.AddWithValue("@OperatorId", ticket.OperatorId);
                cmd.Parameters.AddWithValue("@OperatorName", ticket.OperatorName);
                cmd.Parameters.AddWithValue("@Status", ticket.Status);
                cmd.Parameters.AddWithValue("@ReportDate", ticket.ReportDate);
                cmd.ExecuteNonQuery();

                ticket.Id = (int)conn.LastInsertRowId;
            }

            // Ghi log alarm text file
            _alarmLogger?.LogAlarm($"{lineNumber} - {stationName}", alarmTypeName, severity);

            return ticket;
        }

        /// <summary>
        /// Bước 5: KTV check-in nhận sửa.
        /// Chuyển trạng thái sang Repairing (Cam).
        /// </summary>
        public bool AssignTechnician(string ticketId, string technicianId, string technicianName)
        {
            using (var conn = CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE Tickets SET
                        TechCheckinAt = @TechCheckinAt,
                        TechnicianId = @TechnicianId,
                        TechnicianName = @TechnicianName,
                        Status = @Status
                    WHERE TicketId = @TicketId AND Status IN (1, 2)";

                cmd.Parameters.AddWithValue("@TechCheckinAt", FormatDt(DateTime.Now));
                cmd.Parameters.AddWithValue("@TechnicianId", technicianId);
                cmd.Parameters.AddWithValue("@TechnicianName", technicianName);
                cmd.Parameters.AddWithValue("@Status", (int)TicketStatus.Repairing);
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Bước 6: KTV hoàn thành sửa, nhập ghi chú.
        /// Chuyển trạng thái sang WaitLeader (Xanh dương) nếu yêu cầu Leader xác nhận,
        /// hoặc Closed ngay nếu không yêu cầu.
        /// </summary>
        public bool MarkFixed(string ticketId, string fixNote, bool requireLeaderConfirmation = true)
        {
            int newStatus = requireLeaderConfirmation
                ? (int)TicketStatus.WaitLeader
                : (int)TicketStatus.Closed;

            using (var conn = CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE Tickets SET
                        TechFixedAt = @TechFixedAt,
                        FixNote = @FixNote,
                        Status = @Status
                    WHERE TicketId = @TicketId AND Status = @OldStatus";

                cmd.Parameters.AddWithValue("@TechFixedAt", FormatDt(DateTime.Now));
                cmd.Parameters.AddWithValue("@FixNote", fixNote ?? "");
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@OldStatus", (int)TicketStatus.Repairing);
                bool updated = cmd.ExecuteNonQuery() > 0;

                if (updated && !requireLeaderConfirmation)
                    UpdateDailyStatsForTicket(ticketId, conn);

                return updated;
            }
        }

        /// <summary>
        /// Bước 7: Leader xác nhận đóng phiếu.
        /// Chuyển trạng thái sang Closed, tính DailyStats.
        /// </summary>
        public bool LeaderConfirm(string ticketId, string leaderId, string leaderName)
        {
            using (var conn = CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE Tickets SET
                        LeaderConfirmedAt = @LeaderConfirmedAt,
                        LeaderId = @LeaderId,
                        LeaderName = @LeaderName,
                        Status = @Status
                    WHERE TicketId = @TicketId AND Status = @OldStatus";

                cmd.Parameters.AddWithValue("@LeaderConfirmedAt", FormatDt(DateTime.Now));
                cmd.Parameters.AddWithValue("@LeaderId", leaderId);
                cmd.Parameters.AddWithValue("@LeaderName", leaderName);
                cmd.Parameters.AddWithValue("@Status", (int)TicketStatus.Closed);
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@OldStatus", (int)TicketStatus.WaitLeader);
                bool updated = cmd.ExecuteNonQuery() > 0;

                if (updated)
                {
                    UpdateDailyStatsForTicket(ticketId, conn);
                    // Ghi log đóng ticket
                    var ticket = GetTicketInternal(ticketId, conn);
                    if (ticket != null)
                        _alarmLogger?.LogTicketClosed(ticket.LineNumber, ticket.StationName,
                            ticket.AlarmTypeName, ticket.Severity,
                            ticket.ReportedAt ?? DateTime.Now, DateTime.Now);
                }

                return updated;
            }
        }

        /// <summary>Lấy thông tin ticket theo TicketId</summary>
        public IncidentTicket GetTicket(string ticketId)
        {
            using (var conn = CreateConnection())
                return GetTicketInternal(ticketId, conn);
        }

        private IncidentTicket GetTicketInternal(string ticketId, SQLiteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Tickets WHERE TicketId = @TicketId";
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                using (var reader = cmd.ExecuteReader())
                    if (reader.Read()) return MapTicket(reader);
            }
            return null;
        }

        /// <summary>
        /// Lấy ticket đang active (chưa đóng) cho một ô alarm (line + alarmTypeIndex).
        /// Dùng để Terminal biết trạng thái hiện tại của mỗi ô.
        /// </summary>
        public IncidentTicket GetActiveForCell(string lineNumber, int alarmTypeIndex)
        {
            using (var conn = CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT * FROM Tickets
                    WHERE LineNumber = @LineNumber
                      AND AlarmTypeIndex = @AlarmTypeIndex
                      AND Status NOT IN (0, 5)
                    ORDER BY ReportedAt DESC LIMIT 1";

                cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                cmd.Parameters.AddWithValue("@AlarmTypeIndex", alarmTypeIndex);
                using (var reader = cmd.ExecuteReader())
                    if (reader.Read()) return MapTicket(reader);
            }
            return null;
        }

        /// <summary>Lấy tất cả ticket đang mở (chưa Closed)</summary>
        public List<IncidentTicket> GetAllOpen()
        {
            return QueryTickets("WHERE Status NOT IN (0, 5) ORDER BY ReportedAt DESC");
        }

        /// <summary>Lấy lịch sử ticket của một line (tất cả trạng thái)</summary>
        public List<IncidentTicket> GetHistory(string lineNumber, string date = null)
        {
            string where = "WHERE LineNumber = @LineNumber";
            if (!string.IsNullOrEmpty(date)) where += " AND ReportDate = @Date";
            where += " ORDER BY ReportedAt DESC";

            using (var conn = CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM Tickets {where}";
                cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                if (!string.IsNullOrEmpty(date))
                    cmd.Parameters.AddWithValue("@Date", date);

                var tickets = new List<IncidentTicket>();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) tickets.Add(MapTicket(reader));
                return tickets;
            }
        }

        /// <summary>Lấy DailyStats của một line theo ngày</summary>
        public DailyStatsRecord GetDailyStats(string lineNumber, string date)
        {
            return _dailyStatsService.GetForLine(lineNumber, date);
        }

        // ─────────────── Cập nhật DailyStats sau khi ticket đóng ───────────────

        private void UpdateDailyStatsForTicket(string ticketId, SQLiteConnection conn)
        {
            var ticket = GetTicketInternal(ticketId, conn);
            if (ticket == null || string.IsNullOrEmpty(ticket.ReportDate)) return;
            _dailyStatsService.UpdateForLine(ticket.LineNumber, ticket.LineName, ticket.ReportDate, conn);
        }

        // ─────────────── Helper query ───────────────

        private List<IncidentTicket> QueryTickets(string whereClause)
        {
            var result = new List<IncidentTicket>();
            using (var conn = CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM Tickets {whereClause}";
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) result.Add(MapTicket(reader));
            }
            return result;
        }

        // ─────────────── Map SQLite row → IncidentTicket ───────────────

        private IncidentTicket MapTicket(IDataReader reader)
        {
            return new IncidentTicket
            {
                Id = Convert.ToInt32(reader["Id"]),
                TicketId = reader["TicketId"]?.ToString(),
                LineNumber = reader["LineNumber"]?.ToString(),
                LineName = reader["LineName"]?.ToString(),
                StationId = reader["StationId"]?.ToString(),
                StationName = reader["StationName"]?.ToString(),
                AlarmTypeIndex = Convert.ToInt32(reader["AlarmTypeIndex"]),
                AlarmTypeName = reader["AlarmTypeName"]?.ToString(),
                Severity = reader["Severity"]?.ToString(),
                ReportedAt = ParseDt(reader["ReportedAt"]),
                OperatorId = reader["OperatorId"]?.ToString(),
                OperatorName = reader["OperatorName"]?.ToString(),
                TechCheckinAt = ParseDt(reader["TechCheckinAt"]),
                TechnicianId = reader["TechnicianId"]?.ToString(),
                TechnicianName = reader["TechnicianName"]?.ToString(),
                TechFixedAt = ParseDt(reader["TechFixedAt"]),
                FixNote = reader["FixNote"]?.ToString(),
                LeaderConfirmedAt = ParseDt(reader["LeaderConfirmedAt"]),
                LeaderId = reader["LeaderId"]?.ToString(),
                LeaderName = reader["LeaderName"]?.ToString(),
                Status = Convert.ToInt32(reader["Status"]),
                ReportDate = reader["ReportDate"]?.ToString()
            };
        }
    }
}
