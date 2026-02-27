// File: AndonTerminal/Forms/FixCompleteForm.cs
// Mô tả: Popup cho KTV nhập ghi chú sửa chữa khi hoàn thành bước 6.
// Giao diện: TextBox multiline để nhập ghi chú + nút "Đã sửa xong".
//
// GIAO DIỆN THỰC TẾ (460×380):
// ╔════════════════════════════════════════════════╗
// ║  ✓ Hoàn thành sửa chữa           [xanh lá]   ║ ← lblTitle
// ║  Line: [lineName] | Trạm: [stationName]       ║ ← lblInfo
// ║  Loại: [alarmTypeName]                        ║
// ╠════════════════════════════════════════════════╣
// ║  Ghi chú sửa chữa (không bắt buộc):          ║ ← lblNote
// ║  ┌──────────────────────────────────────────┐  ║
// ║  │ [TextBox multiline, 4 dòng, scroll dọc] │  ║ ← _txtNote
// ║  │ PlaceholderText: "Thay thế motor..."    │  ║
// ║  └──────────────────────────────────────────┘  ║
// ║                                                ║
// ║  [ ✓ Đã sửa xong — Chờ Leader xác nhận ]     ║ ← btnDone (xanh lá, chiều ngang)
// ║  [Hủy]                                        ║ ← btnCancel (xám, nhỏ)
// ╚════════════════════════════════════════════════╝
//
// CÁC CONTROL CHÍNH:
//   lblTitle   (Label)    Bounds=(20,15,420,30)   → ForeColor=xanh lá
//   lblInfo    (Label)    Bounds=(20,50,420,40)   → hiển thị Line/Trạm/Loại
//   _txtNote   (TextBox)  Bounds=(20,130,420,130) → Multiline=true, ScrollBars=Vertical
//   btnDone    (Button)   Bounds=(20,275,420,50)  → FixNote = _txtNote.Text
//   btnCancel  (Button)   Bounds=(20,330,130,30)  → DialogResult=Cancel
//
// ĐỂ SỬA GIAO DIỆN:
//   - Thêm dropdown nguyên nhân lỗi: xem hướng dẫn Docs/UI_CUSTOMIZE.md#7

using System;
using System.Drawing;
using System.Windows.Forms;

namespace AndonTerminal.Forms
{
    /// <summary>
    /// Form popup cho KTV nhập ghi chú sửa chữa và xác nhận hoàn thành.
    /// Sau khi confirm, FixNote có nội dung ghi chú (có thể rỗng).
    /// </summary>
    public class FixCompleteForm : Form
    {
        /// <summary>Ghi chú sửa chữa của KTV (có thể để trống)</summary>
        public string FixNote { get; private set; }

        // TextBox nhập ghi chú
        private TextBox _txtNote;

        // Màu nền tối
        private static readonly Color BackgroundColor = Color.FromArgb(44, 62, 80);
        // Màu input
        private static readonly Color InputBackColor = Color.FromArgb(52, 73, 94);
        // Màu nút xanh lá
        private static readonly Color OkButtonColor = Color.FromArgb(46, 204, 113);
        // Màu chữ trắng
        private static readonly Color TextColor = Color.White;

        /// <summary>
        /// Khởi tạo form với thông tin ticket đang sửa.
        /// </summary>
        /// <param name="lineName">Tên Line đang sửa để hiển thị trên form</param>
        /// <param name="stationName">Tên trạm đang sửa</param>
        /// <param name="alarmTypeName">Tên loại alarm</param>
        public FixCompleteForm(string lineName, string stationName, string alarmTypeName)
        {
            InitializeUI(lineName, stationName, alarmTypeName);
        }

        private void InitializeUI(string lineName, string stationName, string alarmTypeName)
        {
            // ── Cài đặt Form ──
            this.Text = "Xác nhận hoàn thành sửa chữa";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BackgroundColor;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Size = new Size(460, 380);

            int x = 20, w = 420;

            // ── Label tiêu đề ──
            var lblTitle = new Label
            {
                Text = "✓ Hoàn thành sửa chữa",
                ForeColor = Color.FromArgb(46, 204, 113),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds = new Rectangle(x, 15, w, 30)
            };
            this.Controls.Add(lblTitle);

            // ── Thông tin ticket ──
            var lblInfo = new Label
            {
                Text = $"Line: {lineName}  |  Trạm: {stationName}\nLoại: {alarmTypeName}",
                ForeColor = Color.FromArgb(189, 195, 199),
                Font = new Font("Segoe UI", 10f),
                AutoSize = false,
                Bounds = new Rectangle(x, 50, w, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblInfo);

            // ── Label ghi chú ──
            var lblNote = new Label
            {
                Text = "Ghi chú sửa chữa (không bắt buộc):",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = false,
                Bounds = new Rectangle(x, 100, w, 25)
            };
            this.Controls.Add(lblNote);

            // ── TextBox ghi chú (multiline) ──
            // Cho phép KTV nhập chi tiết về nguyên nhân và cách xử lý
            _txtNote = new TextBox
            {
                BackColor = InputBackColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Bounds = new Rectangle(x, 130, w, 130),
                PlaceholderText = "Ví dụ: Thay thế motor drive, kiểm tra encoder, vận hành ổn định..."
            };
            this.Controls.Add(_txtNote);

            // ── Nút Đã sửa xong ──
            var btnDone = new Button
            {
                Text = "✓  Đã sửa xong — Chờ Leader xác nhận",
                ForeColor = TextColor,
                BackColor = OkButtonColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(x, 275, w, 50)
            };
            btnDone.FlatAppearance.BorderSize = 0;
            btnDone.Click += (s, e) =>
            {
                FixNote = _txtNote.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnDone);
            this.AcceptButton = btnDone;

            // ── Nút Hủy ──
            var btnCancel = new Button
            {
                Text = "Hủy",
                ForeColor = TextColor,
                BackColor = Color.FromArgb(149, 165, 166),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(x, 330, 130, 30),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;

            // Focus vào textbox ghi chú
            this.ActiveControl = _txtNote;
        }
    }
}
