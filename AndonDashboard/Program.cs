// File: AndonDashboard/Program.cs
// Mô tả: Điểm khởi đầu của ứng dụng AndonDashboard.
// Đọc cấu hình và khởi chạy DashboardMainForm.

using System;
using System.IO;
using System.Windows.Forms;
using SharedLib.Services;
using SharedLib.Services.Analytics;
using SharedLib.Services.Email;
using AndonDashboard.Forms;

namespace AndonDashboard
{
    static class Program
    {
        // Giữ tham chiếu để EmailScheduler không bị GC thu hồi
        private static EmailScheduler _emailScheduler;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ── Xác định đường dẫn base ──
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string assetsDir = Path.Combine(baseDir, "..", "..", "..", "..", "Assets");
            string dataDir = Path.Combine(baseDir, "..", "..", "..", "..", "Data");
            string logsDir = Path.Combine(baseDir, "..", "..", "..", "..", "Logs");
            string dbPath = Path.Combine(dataDir, "eandon.db");

            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(logsDir);

            // ── Đọc settings ──
            string settingsPath = Path.Combine(assetsDir, "settings.txt");
            var settings = new SettingsReader(settingsPath);

            // ── Khởi tạo services ──
            var alarmLogger = new AlarmLogger(logsDir);
            var incidentService = new IncidentService(dbPath, alarmLogger);
            var statsService = new DailyStatsService(dbPath);
            var lineStationReader = new LineStationReader(
                Path.Combine(assetsDir, "Workstations_terminals.txt"),
                Path.Combine(assetsDir, "Lines_stations.txt"));

            // ── Khởi tạo Email Scheduler ──
            var emailConfig = EmailConfig.FromSettings(settings);
            var emailSender = new EmailSender(emailConfig, alarmLogger);
            var analyticsManager = new AnalyticsManager(dbPath);
            var reportBuilder = new WeeklyReportBuilder(statsService, analyticsManager);
            var alertService = new RealtimeAlertService(
                incidentService,
                analyticsManager.AnomalyDetector,
                analyticsManager.TimePatternDetector,
                emailSender,
                emailConfig,
                alarmLogger);
            _emailScheduler = new EmailScheduler(alertService, reportBuilder, emailSender, emailConfig, alarmLogger);
            _emailScheduler.Start();

            // ── Khởi chạy Dashboard ──
            Application.Run(new DashboardMainForm(
                settings, lineStationReader, incidentService, statsService, dataDir, assetsDir));

            // ── Dọn dẹp khi Dashboard đóng ──
            _emailScheduler.Dispose();
        }
    }
}
