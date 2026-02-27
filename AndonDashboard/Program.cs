// File: AndonDashboard/Program.cs
// Mô tả: Điểm khởi đầu của ứng dụng AndonDashboard.
// Đọc cấu hình và khởi chạy DashboardMainForm.

using System;
using System.IO;
using System.Windows.Forms;
using SharedLib.Services;
using AndonDashboard.Forms;

namespace AndonDashboard
{
    static class Program
    {
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

            // ── Khởi chạy Dashboard ──
            Application.Run(new DashboardMainForm(
                settings, lineStationReader, incidentService, statsService, dataDir, assetsDir));
        }
    }
}
