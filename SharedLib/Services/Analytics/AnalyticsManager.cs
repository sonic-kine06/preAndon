// File: SharedLib/Services/Analytics/AnalyticsManager.cs
// Mô tả: Điều phối tất cả 4 model analytics (DowntimeEstimator, AnomalyDetector,
// TechnicianTracker, TimePatternDetector) để cung cấp dữ liệu cho Dashboard
// và đề xuất khi tạo ticket mới trên Terminal.
//
// Hai phương thức chính:
//   GetDashboardSummary()        → tất cả analytics cho StatisticsForm/Dashboard
//   GetSuggestionForNewTicket()  → gợi ý KTV + cảnh báo khi tạo ticket mới

using System.Collections.Generic;

namespace SharedLib.Services.Analytics
{
    /// <summary>
    /// Kết quả tổng hợp analytics cho Dashboard.
    /// Chứa đầy đủ dữ liệu từ cả 4 model.
    /// </summary>
    public class DashboardAnalyticsSummary
    {
        /// <summary>Danh sách dự đoán downtime cho tất cả line (EWMA)</summary>
        public List<DowntimePrediction> DowntimePredictions { get; set; } = new List<DowntimePrediction>();

        /// <summary>Danh sách line đang có downtime bất thường (Z-score)</summary>
        public List<AnomalyResult> Anomalies { get; set; } = new List<AnomalyResult>();

        /// <summary>Xếp hạng KTV tổng thể (nhanh nhất + nhiều lần nhất)</summary>
        public List<TechnicianStat> TechnicianRanking { get; set; } = new List<TechnicianStat>();

        /// <summary>Các mẫu thời gian rủi ro cao (xác suất >= 20%)</summary>
        public List<TimePattern> HighRiskTimePatterns { get; set; } = new List<TimePattern>();

        /// <summary>Các rủi ro đang khớp với thời điểm hiện tại</summary>
        public List<TimePattern> CurrentRisks { get; set; } = new List<TimePattern>();

        /// <summary>Tổng số line bất thường</summary>
        public int AnomalyCount => Anomalies?.Count ?? 0;

        /// <summary>Có bất thường nào không (để hiện banner cảnh báo)</summary>
        public bool HasAnomalies => AnomalyCount > 0;

        /// <summary>Có rủi ro thời điểm hiện tại không</summary>
        public bool HasCurrentRisks => (CurrentRisks?.Count ?? 0) > 0;
    }

    /// <summary>
    /// Gợi ý hiển thị khi Terminal tạo ticket mới.
    /// Chứa thông tin KTV đề xuất và cảnh báo liên quan.
    /// </summary>
    public class TicketSuggestion
    {
        /// <summary>Có dữ liệu để đề xuất không</summary>
        public bool HasSuggestion { get; set; }

        /// <summary>KTV đề xuất tốt nhất cho loại alarm + line này</summary>
        public TechnicianStat SuggestedTechnician { get; set; }

        /// <summary>Dự đoán downtime cho line (phút)</summary>
        public DowntimePrediction DowntimePrediction { get; set; }

        /// <summary>Cảnh báo mẫu thời gian (nếu đúng thời điểm rủi ro)</summary>
        public List<TimePattern> TimePatternWarnings { get; set; } = new List<TimePattern>();

        /// <summary>Tóm tắt văn bản để hiển thị popup gợi ý</summary>
        public string SummaryText
        {
            get
            {
                if (!HasSuggestion) return "Chưa có đủ dữ liệu để đưa ra gợi ý.";

                var lines = new System.Text.StringBuilder();
                if (SuggestedTechnician != null)
                    lines.AppendLine($"👷 KTV đề xuất: {SuggestedTechnician.TechnicianName} ({SuggestedTechnician.TechnicianId})" +
                                     $" — Avg: {SuggestedTechnician.AvgRepairTimeMinutes:F1} phút ({SuggestedTechnician.RepairCount} lần)");

                if (DowntimePrediction != null && DowntimePrediction.Confidence > 0)
                    lines.AppendLine($"⏱ Dự đoán downtime: ~{DowntimePrediction.PredictedDowntimeMinutes:F0} phút" +
                                     $" (độ tin cậy: {DowntimePrediction.ConfidenceLabel})");

                if (TimePatternWarnings?.Count > 0)
                    lines.AppendLine($"⚠ Cảnh báo mẫu thời gian: {TimePatternWarnings[0].Description}");

                return lines.ToString().TrimEnd();
            }
        }
    }

    /// <summary>
    /// Điều phối tất cả 4 analytics model.
    /// Cung cấp API đơn giản cho Dashboard và Terminal.
    /// </summary>
    public class AnalyticsManager
    {
        private readonly DowntimeEstimator _downtimeEstimator;
        private readonly AnomalyDetector _anomalyDetector;
        private readonly TechnicianTracker _technicianTracker;
        private readonly TimePatternDetector _timePatternDetector;

        /// <summary>
        /// Khởi tạo AnalyticsManager với đường dẫn file SQLite.
        /// Tất cả 4 model dùng chung cùng một file DB.
        /// </summary>
        public AnalyticsManager(string dbFilePath)
        {
            _downtimeEstimator = new DowntimeEstimator(dbFilePath);
            _anomalyDetector = new AnomalyDetector(dbFilePath);
            _technicianTracker = new TechnicianTracker(dbFilePath);
            _timePatternDetector = new TimePatternDetector(dbFilePath);
        }

        /// <summary>
        /// Lấy tổng hợp analytics đầy đủ cho Dashboard và StatisticsForm.
        /// Tự động làm mới TimePatterns trước khi tổng hợp.
        /// </summary>
        public DashboardAnalyticsSummary GetDashboardSummary()
        {
            // Làm mới dữ liệu TimePatterns từ lịch sử ticket
            _timePatternDetector.RefreshPatterns();

            return new DashboardAnalyticsSummary
            {
                DowntimePredictions = _downtimeEstimator.PredictAll(),
                Anomalies = _anomalyDetector.DetectAll(),
                TechnicianRanking = _technicianTracker.GetOverallRanking(),
                HighRiskTimePatterns = _timePatternDetector.GetHighRiskPatterns(),
                CurrentRisks = _timePatternDetector.GetCurrentRisks()
            };
        }

        /// <summary>
        /// Tạo gợi ý khi Terminal vừa tạo một ticket mới.
        /// Gọi ngay sau OpenIncident() để hiển thị popup đề xuất KTV.
        /// </summary>
        /// <param name="lineNumber">Mã line đang gặp sự cố</param>
        /// <param name="alarmTypeIndex">Chỉ số loại alarm (1-based)</param>
        public TicketSuggestion GetSuggestionForNewTicket(string lineNumber, int alarmTypeIndex)
        {
            // Gợi ý KTV tốt nhất cho line + alarmType
            var bestTech = _technicianTracker.SuggestBest(alarmTypeIndex, lineNumber);

            // Dự đoán downtime cho line + alarmType
            var downtime = _downtimeEstimator.PredictDowntime(lineNumber, alarmTypeIndex);

            // Kiểm tra mẫu thời gian rủi ro hiện tại (khớp với giờ hiện tại)
            var currentRisks = _timePatternDetector.GetCurrentRisks();

            bool hasSuggestion = bestTech != null
                || (downtime != null && downtime.Confidence > 0)
                || currentRisks.Count > 0;

            return new TicketSuggestion
            {
                HasSuggestion = hasSuggestion,
                SuggestedTechnician = bestTech,
                DowntimePrediction = downtime,
                TimePatternWarnings = currentRisks
            };
        }

        // ─────────────── Truy cập trực tiếp các model ───────────────

        /// <summary>Truy cập trực tiếp DowntimeEstimator nếu cần</summary>
        public DowntimeEstimator DowntimeEstimator => _downtimeEstimator;

        /// <summary>Truy cập trực tiếp AnomalyDetector nếu cần</summary>
        public AnomalyDetector AnomalyDetector => _anomalyDetector;

        /// <summary>Truy cập trực tiếp TechnicianTracker nếu cần</summary>
        public TechnicianTracker TechnicianTracker => _technicianTracker;

        /// <summary>Truy cập trực tiếp TimePatternDetector nếu cần</summary>
        public TimePatternDetector TimePatternDetector => _timePatternDetector;
    }
}
