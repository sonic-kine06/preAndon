// File: AndonTerminal/Forms/AlarmTypeForm.cs
// Mô tả: Popup cho Operator chọn mức độ sự cố (Yellow hoặc Red).
// Giao diện: 2 panel lớn — Yellow bên trái, Red bên phải + nút Cancel.
// Giống Alarm_type.vb gốc eAndon nhưng chuyển sang C#.
// Hiển thị ở bước [3] trong luồng 7 bước.

using System;
using System.Drawing;
using System.Windows.Forms;
using SharedLib.Services;

namespace AndonTerminal.Forms
{
    /// <summary>
    /// Form popup chọn mức độ sự cố (Yellow/Red).
    /// Sau khi chọn, thuộc tính SelectedSeverity có giá trị "Yellow" hoặc "Red".
    /// </summary>
    public class AlarmTypeForm : Form
    {
        /// <summary>Mức độ được chọn: "Yellow" hoặc "Red" (null nếu Cancel)</summary>
        public string SelectedSeverity { get; private set; }

        // Màu nền tối
        private static readonly Color BackgroundColor = Color.FromArgb(44, 62, 80);
        // Màu vàng (Yellow alarm)
        private static readonly Color YellowColor = Color.FromArgb(241, 196, 15);
        // Màu đỏ (Red alarm)
        private static readonly Color RedColor = Color.FromArgb(192, 57, 43);
        // Màu chữ tối trên nền vàng
        private static readonly Color DarkTextColor = Color.FromArgb(44, 62, 80);
        // Màu chữ trắng
        private static readonly Color WhiteText = Color.White;

        public AlarmTypeForm(SettingsReader settings)
        {
            InitializeUI(settings);
        }

        private void InitializeUI(SettingsReader settings)
        {
            // ── Cài đặt Form ──
            this.Text = settings?.AlarmWindowTitle ?? "Chọn mức độ sự cố";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BackgroundColor;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Size = new Size(700, 380);

            // ── Panel Yellow (bên trái) ──
            // Chiếm 45% chiều rộng form, màu vàng
            var panelYellow = new Panel
            {
                BackColor = YellowColor,
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(20, 20, 290, 260)
            };

            // Icon cảnh báo Yellow
            var lblYellowIcon = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI", 48f, FontStyle.Bold),
                ForeColor = DarkTextColor,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Bounds = new Rectangle(0, 20, 290, 100)
            };
            panelYellow.Controls.Add(lblYellowIcon);

            // Nhãn trạng thái Yellow
            var lblYellowStatus = new Label
            {
                Text = "VÀNG",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = DarkTextColor,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Bounds = new Rectangle(0, 120, 290, 50)
            };
            panelYellow.Controls.Add(lblYellowStatus);

            // Mô tả chi tiết Yellow từ settings
            string yellowDesc = settings?.AlarmWindowYellowLabel ?? "Trạm có vấn đề nhưng vẫn chạy";
            var lblYellowDesc = new Label
            {
                Text = yellowDesc,
                Font = new Font("Segoe UI", 11f),
                ForeColor = DarkTextColor,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Bounds = new Rectangle(10, 170, 270, 70),
            };
            panelYellow.Controls.Add(lblYellowDesc);

            // Click handler cho toàn bộ panel Yellow
            EventHandler yellowClick = (s, e) =>
            {
                SelectedSeverity = "Yellow";
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            panelYellow.Click += yellowClick;
            lblYellowIcon.Click += yellowClick;
            lblYellowStatus.Click += yellowClick;
            lblYellowDesc.Click += yellowClick;

            this.Controls.Add(panelYellow);

            // ── Panel Red (bên phải) ──
            // Chiếm 45% chiều rộng form, màu đỏ
            var panelRed = new Panel
            {
                BackColor = RedColor,
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(390, 20, 290, 260)
            };

            // Icon dừng Red
            var lblRedIcon = new Label
            {
                Text = "🛑",
                Font = new Font("Segoe UI", 48f, FontStyle.Bold),
                ForeColor = WhiteText,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Bounds = new Rectangle(0, 20, 290, 100)
            };
            panelRed.Controls.Add(lblRedIcon);

            // Nhãn trạng thái Red
            var lblRedStatus = new Label
            {
                Text = "ĐỎ",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = WhiteText,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Bounds = new Rectangle(0, 120, 290, 50)
            };
            panelRed.Controls.Add(lblRedStatus);

            // Mô tả chi tiết Red từ settings
            string redDesc = settings?.AlarmWindowRedLabel ?? "Trạm đã dừng hoàn toàn";
            var lblRedDesc = new Label
            {
                Text = redDesc,
                Font = new Font("Segoe UI", 11f),
                ForeColor = WhiteText,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Bounds = new Rectangle(10, 170, 270, 70),
            };
            panelRed.Controls.Add(lblRedDesc);

            // Click handler cho toàn bộ panel Red
            EventHandler redClick = (s, e) =>
            {
                SelectedSeverity = "Red";
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            panelRed.Click += redClick;
            lblRedIcon.Click += redClick;
            lblRedStatus.Click += redClick;
            lblRedDesc.Click += redClick;

            this.Controls.Add(panelRed);

            // ── Nút Hủy ở giữa dưới ──
            string cancelLabel = settings?.AlarmWindowCancelLabel ?? "Hủy";
            var btnCancel = new Button
            {
                Text = cancelLabel,
                ForeColor = WhiteText,
                BackColor = Color.FromArgb(149, 165, 166),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f),
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(270, 295, 160, 45),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;
        }
    }
}
