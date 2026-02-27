// File: AndonTerminal/Forms/EmployeeInputForm.cs
// Mô tả: Popup nhập Mã NV + Họ tên, dùng chung cho 3 vai trò:
//   - Operator khi báo lỗi (Bước 4)
//   - KTV khi nhận sửa (Bước 5)
//   - Leader khi xác nhận (Bước 7)
// Validate: cả 2 trường không được trống.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace AndonTerminal.Forms
{
    /// <summary>
    /// Form popup nhập thông tin nhân viên (Mã NV + Họ tên).
    /// Dùng chung cho Operator, KTV và Leader.
    /// Sau khi confirm, EmployeeId và EmployeeName được gán.
    /// </summary>
    public class EmployeeInputForm : Form
    {
        /// <summary>Mã nhân viên đã nhập</summary>
        public string EmployeeId { get; private set; }

        /// <summary>Họ tên nhân viên đã nhập</summary>
        public string EmployeeName { get; private set; }

        // Controls nội bộ
        private TextBox _txtEmployeeId;
        private TextBox _txtEmployeeName;
        private Label _lblError;

        // Màu nền tối theo thiết kế
        private static readonly Color BackgroundColor = Color.FromArgb(44, 62, 80);
        // Màu input field
        private static readonly Color InputBackColor = Color.FromArgb(52, 73, 94);
        // Màu nút xanh lá (OK)
        private static readonly Color OkButtonColor = Color.FromArgb(46, 204, 113);
        // Màu chữ trắng
        private static readonly Color TextColor = Color.White;

        /// <summary>
        /// Khởi tạo form với tiêu đề và hướng dẫn theo vai trò.
        /// </summary>
        /// <param name="title">Tiêu đề form, ví dụ: "Nhập thông tin Operator"</param>
        /// <param name="instruction">Hướng dẫn chi tiết hiển thị trên form</param>
        public EmployeeInputForm(string title, string instruction)
        {
            InitializeUI(title, instruction);
        }

        private void InitializeUI(string title, string instruction)
        {
            // ── Cài đặt Form ──
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BackgroundColor;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Size = new Size(420, 340);

            int x = 20, w = 380;

            // ── Label hướng dẫn ──
            var lblInstruction = new Label
            {
                Text = instruction,
                ForeColor = Color.FromArgb(189, 195, 199),
                Font = new Font("Segoe UI", 10f),
                AutoSize = false,
                Bounds = new Rectangle(x, 15, w, 45),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblInstruction);

            // ── Label + TextBox Mã NV ──
            var lblId = new Label
            {
                Text = "Mã nhân viên *",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = false,
                Bounds = new Rectangle(x, 65, w, 22)
            };
            this.Controls.Add(lblId);

            _txtEmployeeId = new TextBox
            {
                BackColor = InputBackColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Bounds = new Rectangle(x, 90, w, 32),
                PlaceholderText = "Ví dụ: NV001"
            };
            this.Controls.Add(_txtEmployeeId);

            // ── Label + TextBox Họ tên ──
            var lblName = new Label
            {
                Text = "Họ và tên *",
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = false,
                Bounds = new Rectangle(x, 135, w, 22)
            };
            this.Controls.Add(lblName);

            _txtEmployeeName = new TextBox
            {
                BackColor = InputBackColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Bounds = new Rectangle(x, 160, w, 32),
                PlaceholderText = "Ví dụ: Nguyễn Văn A"
            };
            this.Controls.Add(_txtEmployeeName);

            // ── Label lỗi validation ──
            _lblError = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(231, 76, 60),
                Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                AutoSize = false,
                Bounds = new Rectangle(x, 200, w, 22)
            };
            this.Controls.Add(_lblError);

            // ── Nút OK ──
            var btnOK = new Button
            {
                Text = "✓  Xác nhận",
                ForeColor = TextColor,
                BackColor = OkButtonColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(x, 230, 180, 45)
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);
            this.AcceptButton = btnOK;

            // ── Nút Hủy ──
            var btnCancel = new Button
            {
                Text = "✕  Hủy",
                ForeColor = TextColor,
                BackColor = Color.FromArgb(149, 165, 166),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f),
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(220, 230, 180, 45),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;

            // Focus vào textbox đầu tiên khi mở
            this.ActiveControl = _txtEmployeeId;
        }

        /// <summary>
        /// Xử lý click OK: validate và đóng form nếu hợp lệ.
        /// </summary>
        private void BtnOK_Click(object sender, EventArgs e)
        {
            string id = _txtEmployeeId.Text.Trim();
            string name = _txtEmployeeName.Text.Trim();

            // Kiểm tra không trống
            if (string.IsNullOrWhiteSpace(id))
            {
                _lblError.Text = "⚠ Vui lòng nhập Mã nhân viên!";
                _txtEmployeeId.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                _lblError.Text = "⚠ Vui lòng nhập Họ và tên!";
                _txtEmployeeName.Focus();
                return;
            }

            // Hợp lệ → gán kết quả và đóng
            EmployeeId = id;
            EmployeeName = name;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
