// File: AndonTerminal/Program.cs
// Mô tả: Điểm khởi đầu của ứng dụng AndonTerminal.
// Đọc cấu hình từ Assets/settings.txt và khởi chạy TerminalMainForm.

using System;
using System.IO;
using System.Windows.Forms;
using SharedLib.Services;
using AndonTerminal.Forms;

namespace AndonTerminal
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

            // Tạo thư mục nếu chưa có
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(logsDir);

            // ── Đọc settings ──
            string settingsPath = Path.Combine(assetsDir, "settings.txt");
            var settings = new SettingsReader(settingsPath);

            // ── Khởi tạo services ──
            var alarmLogger = new AlarmLogger(logsDir);
            var incidentService = new IncidentService(dbPath, alarmLogger);
            var lineStationReader = new LineStationReader(
                Path.Combine(assetsDir, "Workstations_terminals.txt"),
                Path.Combine(assetsDir, "Lines_stations.txt"));

            // ── Xác định terminal name (đọc từ args hoặc dùng mặc định) ──
            string terminalName = "terminal01";
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1) terminalName = args[1];

            // ── Khởi chạy main form ──
            Application.Run(new TerminalMainForm(
                settings, lineStationReader, incidentService, alarmLogger,
                terminalName, dataDir, assetsDir));
        }
    }
}
