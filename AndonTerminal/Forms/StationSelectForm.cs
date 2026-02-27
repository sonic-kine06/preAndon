// File: AndonTerminal/Forms/StationSelectForm.cs
// Mô tả: Popup cho phép Operator chọn trạm cụ thể khi một Line có nhiều Station.
// Giao diện: nền tối, mỗi station là 1 nút lớn, click → chọn → đóng dialog.
// Hiển thị khi bước [2] trong luồng 7 bước.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SharedLib.Models;

namespace AndonTerminal.Forms
{
    /// <summary>
    /// Form popup cho Operator chọn trạm bị lỗi khi Line có nhiều Station.
    /// Sau khi chọn, thuộc tính SelectedStation được gán và form đóng lại.
    /// </summary>
    public class StationSelectForm : Form
    {
        // Trạm được chọn bởi Operator
        public StationInfo SelectedStation { get; private set; }

        // Danh sách station truyền vào
        private readonly List<StationInfo> _stations;

        // Màu nền tối theo thiết kế UI
        private static readonly Color BackgroundColor = Color.FromArgb(44, 62, 80);
        // Màu nút station (xanh dương đậm)
        private static readonly Color ButtonColor = Color.FromArgb(52, 152, 219);
        // Màu nút khi hover
        private static readonly Color ButtonHoverColor = Color.FromArgb(41, 128, 185);
        // Màu chữ trắng
        private static readonly Color TextColor = Color.White;

        public StationSelectForm(List<StationInfo> stations, string lineName)
        {
            _stations = stations ?? new List<StationInfo>();
            InitializeUI(lineName);
        }

        private void InitializeUI(string lineName)
        {
            // ── Cài đặt Form ──
            this.Text = "Chọn trạm bị lỗi";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BackgroundColor;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Tính kích thước form dựa trên số lượng station
            int buttonWidth = 320;
            int buttonHeight = 70;
            int padding = 15;
            int formWidth = buttonWidth + padding * 4;
            int formHeight = padding * 3 + 60 + (_stations.Count * (buttonHeight + padding)) + 60;
            this.Size = new Size(formWidth, formHeight);

            // ── Label tiêu đề ──
            // Hiển thị tên line để Operator xác nhận đúng line
            var lblTitle = new Label
            {
                Text = $"Line: {lineName}",
                ForeColor = Color.FromArgb(189, 195, 199),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(padding, padding, formWidth - padding * 2, 25)
            };
            this.Controls.Add(lblTitle);

            // ── Label hướng dẫn ──
            var lblInstruction = new Label
            {
                Text = "Chọn trạm bị lỗi:",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(padding, padding + 30, formWidth - padding * 2, 35)
            };
            this.Controls.Add(lblInstruction);

            // ── Tạo nút cho mỗi station ──
            int yOffset = padding * 2 + 65;
            foreach (var station in _stations)
            {
                // Đóng gói station vào biến local để tránh closure bug
                var capturedStation = station;

                var btn = new Button
                {
                    Text = $"{station.StationId}\n{station.StationName}",
                    ForeColor = TextColor,
                    BackColor = ButtonColor,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Bounds = new Rectangle(padding * 2, yOffset, buttonWidth, buttonHeight),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = 0;

                // Hiệu ứng hover
                btn.MouseEnter += (s, e) => btn.BackColor = ButtonHoverColor;
                btn.MouseLeave += (s, e) => btn.BackColor = ButtonColor;

                // Click → gán kết quả và đóng form
                btn.Click += (s, e) =>
                {
                    SelectedStation = capturedStation;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };

                this.Controls.Add(btn);
                yOffset += buttonHeight + padding;
            }

            // ── Nút Hủy ──
            var btnCancel = new Button
            {
                Text = "Hủy",
                ForeColor = TextColor,
                BackColor = Color.FromArgb(149, 165, 166),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f),
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(padding * 2, yOffset + 5, buttonWidth, 45),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;
        }
    }
}
