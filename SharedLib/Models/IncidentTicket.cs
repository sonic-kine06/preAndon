// File: SharedLib/Models/IncidentTicket.cs
// Mô tả: Model đại diện cho một phiếu sự cố (incident ticket) trong hệ thống eAndon.
// Mỗi ticket được tạo ra khi operator báo lỗi và đi qua 7 bước luồng sự cố.
// Sau khi Leader xác nhận, ticket được đóng và thống kê DailyStats được cập nhật.

using System;

namespace SharedLib.Models
{
    /// <summary>
    /// Trạng thái của một ticket sự cố.
    /// Tương ứng với 5 màu hiển thị trên màn hình Dashboard/Terminal.
    /// </summary>
    public enum TicketStatus
    {
        /// <summary>Bình thường, máy chạy OK — màu Xanh lá</summary>
        Green = 0,
        /// <summary>Đã báo lỗi, vẫn chạy, chờ KTV — màu Vàng</summary>
        Yellow = 1,
        /// <summary>Đã dừng, chờ KTV — màu Đỏ</summary>
        Red = 2,
        /// <summary>KTV đang sửa — màu Cam</summary>
        Repairing = 3,
        /// <summary>KTV xong, chờ Leader xác nhận — màu Xanh dương</summary>
        WaitLeader = 4,
        /// <summary>Đã đóng — ticket kết thúc</summary>
        Closed = 5
    }

    /// <summary>
    /// Mức độ nghiêm trọng của sự cố báo bởi Operator.
    /// </summary>
    public enum AlarmSeverity
    {
        /// <summary>Vấn đề nhưng máy vẫn chạy — màu Vàng</summary>
        Yellow = 1,
        /// <summary>Máy đã dừng — màu Đỏ</summary>
        Red = 2
    }

    /// <summary>
    /// Phiếu sự cố đầy đủ, ánh xạ trực tiếp vào bảng Tickets trong SQLite.
    /// Đi qua 7 bước từ lúc Operator báo đến khi Leader đóng phiếu.
    /// </summary>
    public class IncidentTicket
    {
        /// <summary>Khóa chính tự tăng trong SQLite</summary>
        public int Id { get; set; }

        /// <summary>Mã phiếu duy nhất, dạng "TKT-{timestamp}-{random}", ví dụ: "TKT-20260227-001"</summary>
        public string TicketId { get; set; }

        // --- Thông tin Line / Trạm ---

        /// <summary>Mã Line, ví dụ: "010"</summary>
        public string LineNumber { get; set; }

        /// <summary>Tên Line hiển thị, ví dụ: "Line 1"</summary>
        public string LineName { get; set; }

        /// <summary>Mã trạm, ví dụ: "ST-010-01" (nếu line chỉ có 1 trạm thì = LineNumber)</summary>
        public string StationId { get; set; }

        /// <summary>Tên trạm hiển thị</summary>
        public string StationName { get; set; }

        // --- Thông tin Alarm Type ---

        /// <summary>Chỉ số loại alarm (1-10) theo cấu hình settings.txt</summary>
        public int AlarmTypeIndex { get; set; }

        /// <summary>Nhãn loại alarm từ settings.txt, ví dụ: "Hỗ trợ Bảo trì"</summary>
        public string AlarmTypeName { get; set; }

        /// <summary>Mức độ: "Yellow" hoặc "Red"</summary>
        public string Severity { get; set; }

        // --- Bước 1-4: Operator báo lỗi ---

        /// <summary>Thời điểm Operator bấm báo lỗi (UTC)</summary>
        public DateTime? ReportedAt { get; set; }

        /// <summary>Mã nhân viên Operator</summary>
        public string OperatorId { get; set; }

        /// <summary>Họ tên Operator</summary>
        public string OperatorName { get; set; }

        // --- Bước 5: KTV check-in ---

        /// <summary>Thời điểm KTV bấm nhận sửa</summary>
        public DateTime? TechCheckinAt { get; set; }

        /// <summary>Mã nhân viên KTV</summary>
        public string TechnicianId { get; set; }

        /// <summary>Họ tên KTV</summary>
        public string TechnicianName { get; set; }

        // --- Bước 6: KTV hoàn thành sửa ---

        /// <summary>Thời điểm KTV bấm hoàn thành sửa</summary>
        public DateTime? TechFixedAt { get; set; }

        /// <summary>Ghi chú sửa chữa từ KTV (FixCompleteForm)</summary>
        public string FixNote { get; set; }

        // --- Bước 7: Leader xác nhận ---

        /// <summary>Thời điểm Leader xác nhận đóng phiếu</summary>
        public DateTime? LeaderConfirmedAt { get; set; }

        /// <summary>Mã nhân viên Leader</summary>
        public string LeaderId { get; set; }

        /// <summary>Họ tên Leader</summary>
        public string LeaderName { get; set; }

        // --- Trạng thái ---

        /// <summary>Trạng thái hiện tại của ticket (0=Green,1=Yellow,...,5=Closed)</summary>
        public int Status { get; set; }

        /// <summary>Ngày báo lỗi dạng "yyyy-MM-dd" để query DailyStats</summary>
        public string ReportDate { get; set; }

        // --- Thuộc tính tính toán (không lưu DB) ---

        /// <summary>Tổng thời gian downtime (giây) = LeaderConfirmedAt - ReportedAt (chỉ khi Closed)</summary>
        public double? DowntimeSeconds
        {
            get
            {
                if (ReportedAt.HasValue && LeaderConfirmedAt.HasValue)
                    return (LeaderConfirmedAt.Value - ReportedAt.Value).TotalSeconds;
                return null;
            }
        }

        /// <summary>Thời gian response của KTV (giây) = TechCheckinAt - ReportedAt</summary>
        public double? ResponseTimeSeconds
        {
            get
            {
                if (ReportedAt.HasValue && TechCheckinAt.HasValue)
                    return (TechCheckinAt.Value - ReportedAt.Value).TotalSeconds;
                return null;
            }
        }

        /// <summary>Thời gian sửa chữa (giây) = TechFixedAt - TechCheckinAt</summary>
        public double? RepairTimeSeconds
        {
            get
            {
                if (TechCheckinAt.HasValue && TechFixedAt.HasValue)
                    return (TechFixedAt.Value - TechCheckinAt.Value).TotalSeconds;
                return null;
            }
        }

        /// <summary>Trạng thái enum tiện dụng</summary>
        public TicketStatus CurrentStatus
        {
            get => (TicketStatus)Status;
            set => Status = (int)value;
        }
    }
}
