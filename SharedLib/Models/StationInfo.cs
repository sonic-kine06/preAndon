// File: SharedLib/Models/StationInfo.cs
// Mô tả: Model đại diện cho thông tin một trạm (station) trong một line sản xuất.
// Một Line có thể có nhiều Station. Nếu file Lines_stations.txt không tồn tại
// thì mỗi Line sẽ tự động được coi là có đúng 1 Station.

namespace SharedLib.Models
{
    /// <summary>
    /// Đại diện cho một trạm làm việc cụ thể trong một Line sản xuất.
    /// </summary>
    public class StationInfo
    {
        /// <summary>
        /// Mã định danh của trạm, ví dụ: "ST-010-01"
        /// </summary>
        public string StationId { get; set; }

        /// <summary>
        /// Tên trạm hiển thị, ví dụ: "Trạm cắt laser"
        /// </summary>
        public string StationName { get; set; }

        /// <summary>
        /// Mã line mà trạm này thuộc về, ví dụ: "010"
        /// </summary>
        public string LineNumber { get; set; }

        /// <summary>
        /// Tên line hiển thị, ví dụ: "Line 1"
        /// </summary>
        public string LineName { get; set; }

        public StationInfo() { }

        public StationInfo(string stationId, string stationName, string lineNumber, string lineName)
        {
            StationId = stationId;
            StationName = stationName;
            LineNumber = lineNumber;
            LineName = lineName;
        }

        public override string ToString()
        {
            return $"{StationId} - {StationName}";
        }
    }
}
