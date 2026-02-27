// File: SharedLib/Services/LineStationReader.cs
// Mô tả: Đọc 2 file cấu hình workstation/line:
//   1. Workstations_terminals.txt — format gốc eAndon (giữ nguyên tương thích)
//   2. Lines_stations.txt — format mới, mỗi line có nhiều station
//
// Nếu Lines_stations.txt không tồn tại → mỗi Line = 1 Station (tương thích gốc).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharedLib.Models;

namespace SharedLib.Services
{
    /// <summary>
    /// Thông tin một workstation/line đọc từ Workstations_terminals.txt.
    /// Format dòng: index;number;name;terminal
    /// </summary>
    public class WorkstationEntry
    {
        /// <summary>Chỉ số (0-based) trong file</summary>
        public int Index { get; set; }

        /// <summary>Mã line/workstation số, ví dụ: "010"</summary>
        public string Number { get; set; }

        /// <summary>Tên hiển thị, ví dụ: "Line 1"</summary>
        public string Name { get; set; }

        /// <summary>Tên terminal phụ trách, ví dụ: "terminal01"</summary>
        public string Terminal { get; set; }
    }

    /// <summary>
    /// Đọc và cung cấp thông tin về Lines và Stations từ 2 file cấu hình.
    /// </summary>
    public class LineStationReader
    {
        // Đường dẫn tới 2 file cấu hình
        private readonly string _workstationsFilePath;
        private readonly string _linesStationsFilePath;

        // Cache danh sách đã đọc
        private List<WorkstationEntry> _workstations;
        private Dictionary<string, List<StationInfo>> _lineStationsMap;

        /// <summary>
        /// Khởi tạo reader với đường dẫn 2 file cấu hình.
        /// </summary>
        /// <param name="workstationsFilePath">Đường dẫn Workstations_terminals.txt</param>
        /// <param name="linesStationsFilePath">Đường dẫn Lines_stations.txt (có thể không tồn tại)</param>
        public LineStationReader(string workstationsFilePath, string linesStationsFilePath)
        {
            _workstationsFilePath = workstationsFilePath;
            _linesStationsFilePath = linesStationsFilePath;
        }

        /// <summary>
        /// Đọc file Workstations_terminals.txt theo format gốc eAndon.
        /// Dòng 1: tổng số workstation
        /// Dòng 2: legend/header (bỏ qua)
        /// Dòng 3+: index;number;name;terminal
        /// </summary>
        public List<WorkstationEntry> GetWorkstations()
        {
            if (_workstations != null) return _workstations;

            _workstations = new List<WorkstationEntry>();
            if (!File.Exists(_workstationsFilePath)) return _workstations;

            var lines = File.ReadAllLines(_workstationsFilePath);
            if (lines.Length < 3) return _workstations;

            // Dòng đầu là số lượng (bỏ qua, đọc hết)
            // Dòng 2 là legend (bỏ qua)
            for (int i = 2; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                if (parts.Length < 4) continue;

                // Bỏ qua dòng header (#;...)
                if (parts[0].TrimStart().StartsWith("#")) continue;

                if (!int.TryParse(parts[0].Trim(), out int idx)) continue;

                _workstations.Add(new WorkstationEntry
                {
                    Index = idx,
                    Number = parts[1].Trim(),
                    Name = parts[2].Trim(),
                    Terminal = parts[3].Trim()
                });
            }

            return _workstations;
        }

        /// <summary>
        /// Đọc file Lines_stations.txt và trả về map LineNumber → danh sách StationInfo.
        /// Format:
        ///   LINE 010; Line 1
        ///     ST-010-01; Trạm cắt laser
        ///     ST-010-02; Trạm hàn điểm
        /// </summary>
        public Dictionary<string, List<StationInfo>> GetLineStationsMap()
        {
            if (_lineStationsMap != null) return _lineStationsMap;

            _lineStationsMap = new Dictionary<string, List<StationInfo>>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(_linesStationsFilePath))
            {
                // Fallback: tạo 1 station cho mỗi line từ Workstations_terminals.txt
                foreach (var ws in GetWorkstations())
                {
                    _lineStationsMap[ws.Number] = new List<StationInfo>
                    {
                        new StationInfo(ws.Number, ws.Name, ws.Number, ws.Name)
                    };
                }
                return _lineStationsMap;
            }

            string currentLineNumber = null;
            string currentLineName = null;

            foreach (var rawLine in File.ReadAllLines(_linesStationsFilePath))
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;

                // Dòng LINE: không bắt đầu bằng khoảng trắng
                if (!rawLine.StartsWith(" ") && !rawLine.StartsWith("\t"))
                {
                    // Format: "LINE 010; Line 1"
                    var parts = rawLine.Split(new[] { ';' }, 2);
                    if (parts.Length < 2) continue;

                    string linePart = parts[0].Trim();
                    if (linePart.StartsWith("LINE ", StringComparison.OrdinalIgnoreCase))
                        currentLineNumber = linePart.Substring(5).Trim();
                    else
                        currentLineNumber = linePart;

                    currentLineName = parts[1].Trim();

                    if (!_lineStationsMap.ContainsKey(currentLineNumber))
                        _lineStationsMap[currentLineNumber] = new List<StationInfo>();
                }
                else
                {
                    // Dòng station: bắt đầu bằng khoảng trắng
                    // Format: "  ST-010-01; Trạm cắt laser"
                    if (currentLineNumber == null) continue;

                    var parts = rawLine.Split(new[] { ';' }, 2);
                    if (parts.Length < 2) continue;

                    string stationId = parts[0].Trim();
                    string stationName = parts[1].Trim();

                    if (!_lineStationsMap.ContainsKey(currentLineNumber))
                        _lineStationsMap[currentLineNumber] = new List<StationInfo>();

                    _lineStationsMap[currentLineNumber].Add(
                        new StationInfo(stationId, stationName, currentLineNumber, currentLineName));
                }
            }

            return _lineStationsMap;
        }

        /// <summary>
        /// Lấy danh sách các station của một line.
        /// Nếu line chỉ có 1 station → không cần popup chọn trạm.
        /// </summary>
        public List<StationInfo> GetStationsForLine(string lineNumber)
        {
            var map = GetLineStationsMap();
            if (map.TryGetValue(lineNumber, out var stations))
                return stations;

            // Fallback: 1 station = line
            var ws = GetWorkstations().FirstOrDefault(w => w.Number == lineNumber);
            if (ws != null)
                return new List<StationInfo> { new StationInfo(ws.Number, ws.Name, ws.Number, ws.Name) };

            return new List<StationInfo>();
        }

        /// <summary>
        /// Kiểm tra xem line có nhiều hơn 1 station không (để hiện popup StationSelectForm).
        /// </summary>
        public bool HasMultipleStations(string lineNumber)
        {
            return GetStationsForLine(lineNumber).Count > 1;
        }

        /// <summary>Reset cache để đọc lại file khi có thay đổi</summary>
        public void ResetCache()
        {
            _workstations = null;
            _lineStationsMap = null;
        }
    }
}
