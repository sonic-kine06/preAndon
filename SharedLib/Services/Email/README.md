# Email Module — Thông báo tự động qua email

Module Email trong eAndon gửi **2 loại thông báo** tự động:
1. **Báo cáo tuần** (mỗi Chủ Nhật): tóm tắt MTTR/MTBF/Availability cho sếp/giám đốc
2. **Cảnh báo real-time**: khi sự cố chờ quá lâu → gửi ngay cho quản lý KTV

---

## SMTP là gì? (Giải thích đơn giản)

**SMTP** (Simple Mail Transfer Protocol) là giao thức gửi email. Khi bạn bấm "Gửi" trong Outlook hay Gmail → phần mềm dùng SMTP để chuyển email đến server. Code eAndon cũng làm y vậy — tự động, không cần mở Outlook.

Để gửi email qua SMTP, bạn cần:
- **Server**: địa chỉ mail server của công ty (ví dụ `mail.congty.com`) hoặc Gmail (`smtp.gmail.com`)
- **Port**: cổng kết nối (587 cho TLS, 465 cho SSL, 25 cho không mã hóa)
- **Tài khoản**: email và mật khẩu người gửi

---

## Cấu trúc thư mục

```
SharedLib/Services/Email/
├── EmailConfig.cs          ← Model: lưu cấu hình SMTP và danh sách người nhận
├── EmailSender.cs          ← Gửi email thực tế qua SmtpClient
├── WeeklyReportBuilder.cs  ← Tạo nội dung HTML báo cáo tuần
├── RealtimeAlertService.cs ← Kiểm tra ticket chờ quá lâu → tạo cảnh báo
├── EmailScheduler.cs       ← Timer kiểm tra: đến giờ gửi chưa?
└── README.md               ← (file này)
```

---

## Tổng quan 5 class

| Class | Vai trò | Tương tự |
|-------|---------|---------|
| `EmailConfig` | Model chứa SMTP server, port, SSL, tài khoản, danh sách người nhận | `SettingsReader` nhưng chuyên cho email |
| `EmailSender` | Gửi email thực sự qua `System.Net.Mail.SmtpClient` | "Bưu tá" — nhận thư và giao đi |
| `WeeklyReportBuilder` | Tạo nội dung HTML cho báo cáo tuần từ DailyStats | "Biên tập viên" — soạn nội dung |
| `RealtimeAlertService` | Theo dõi ticket đang mở, phát hiện chờ quá lâu | "Nhân viên giám sát" — bấm chuông báo động |
| `EmailScheduler` | Timer chạy nền, kiểm tra định kỳ xem có cần gửi email không | "Đồng hồ hẹn giờ" |

---

## Flow diagram

### Báo cáo tuần (mỗi Chủ Nhật 20:00)

```
EmailScheduler (Timer mỗi 1 phút)
    ↓ Kiểm tra: hôm nay là Sunday? Giờ là 20:xx?
    ↓ Đã gửi tuần này chưa?
    ↓ Nếu chưa:
WeeklyReportBuilder.Build()
    ↓ Đọc DailyStats 7 ngày qua từ SQLite
    ↓ Tính tổng/trung bình MTTR, MTBF, Availability
    ↓ Tạo HTML table đẹp
    ↓
EmailSender.Send(config.BossRecipients, subject, htmlBody)
    ↓ SmtpClient.Send()
    ↓
📧 Hòm thư: giamdoc@congty.com, truongphong@congty.com
```

### Cảnh báo real-time

```
RealtimeAlertService (được gọi bởi EmailScheduler mỗi 2 phút)
    ↓ GetAllOpen() → lấy danh sách ticket đang mở
    ↓ Với mỗi ticket:
    ├── Nếu Status=Yellow/Red VÀ không có KTV VÀ > 10 phút
    │   → EmailSender.Send(ManagerRecipients, "⚠️ Chưa có KTV nhận sự cố...")
    │
    ├── Nếu Status=Repairing VÀ > 30 phút
    │   → EmailSender.Send(ManagerRecipients, "⚠️ KTV đang sửa quá lâu...")
    │
    └── Nếu AnomalyDetector phát hiện bất thường
        → EmailSender.Send(ManagerRecipients, "📊 Cảnh báo downtime bất thường...")
                ↓
📧 Hòm thư: quanly.ktv@congty.com
```

---

## Cấu hình trong `settings.txt`

```
# ─── Cấu hình SMTP ───
Email SMTP Server : mail.congty.com
Email SMTP Port : 587
Email Use SSL : true
Email Sender Address : andon@congty.com
Email Sender Password : matkhau_smtp

# ─── Danh sách người nhận ───
# Dùng | để phân cách nhiều địa chỉ
Email Boss Recipients : giamdoc@congty.com|truongphong@congty.com
Email Manager Recipients : quanly.ktv@congty.com|truongca@congty.com

# ─── Lịch báo cáo tuần ───
Email Weekly Report Day : Sunday       # Monday/Tuesday/.../Sunday
Email Weekly Report Hour : 20          # 0-23 (giờ 20 = 8 tối)

# ─── Ngưỡng cảnh báo real-time ───
Email Alert No Tech Minutes : 10       # Chưa có KTV nhận sau 10 phút → gửi cảnh báo
Email Alert Long Repair Minutes : 30   # Đang sửa > 30 phút → gửi cảnh báo

# ─── Tính năng nâng cao ───
Email Alert Anomaly Enabled : true     # Cảnh báo khi downtime bất thường (Z-score)
Email Maintenance Reminder Enabled : true  # Nhắc bảo dưỡng từ TimePatternDetector
```

---

## Khởi tạo EmailScheduler

`EmailScheduler` được khởi tạo trong `DashboardMainForm`:

```csharp
var emailConfig = EmailConfig.FromSettings(_settings);
if (emailConfig.IsValid())
{
    var scheduler = new EmailScheduler(emailConfig, _incidentService, _dailyStatsService, _analytics);
    scheduler.Start();  // Bắt đầu timer nền
}
```

**`emailConfig.IsValid()`**: Kiểm tra có SMTP Server và Sender Address không. Nếu không điền → scheduler không chạy → app vẫn hoạt động bình thường.

---

## Cách test email

### Test cơ bản (gửi thủ công)

Thêm đoạn code tạm vào `DashboardMainForm.Load`:
```csharp
var emailConfig = EmailConfig.FromSettings(_settings);
var sender = new EmailSender(emailConfig);
sender.Send(
    new[] { "email_test@congty.com" },
    "Test từ eAndon",
    "<h1>Hello từ eAndon!</h1><p>Email hoạt động.</p>"
);
```

Nếu không có exception → email đang hoạt động. Nếu có `SmtpException` → kiểm tra cấu hình SMTP.

### Test với Gmail

```
Email SMTP Server : smtp.gmail.com
Email SMTP Port : 587
Email Use SSL : true
Email Sender Address : your_email@gmail.com
Email Sender Password : app_password_16_ky_tu
```

> **Lưu ý**: Gmail yêu cầu dùng **App Password** (không phải mật khẩu đăng nhập). Vào Google Account → Security → 2-Step Verification → App passwords.

---

## EmailConfig — Kiểm tra cấu hình hợp lệ

```csharp
public bool IsValid()
{
    return !string.IsNullOrWhiteSpace(SmtpServer)
        && !string.IsNullOrWhiteSpace(SenderAddress);
}
```

Phương thức này chỉ kiểm tra **có điền server và sender không**, không kiểm tra kết nối thực tế. Nếu muốn test kết nối → dùng `EmailSender.Send(...)` với try/catch.

---

## Lưu ý bảo mật

- **Không commit mật khẩu** vào git. File `settings.txt` **không** nên được commit khi có mật khẩu thật.
- Thêm `Assets/settings.txt` vào `.gitignore` (nhưng giữ `Assets/settings.example.txt` làm mẫu)
- Nên dùng email dành riêng cho hệ thống (không dùng email cá nhân của nhân viên)
