# 🚀 HƯỚNG DẪN NHẬP MÔN A-Z — eAndon C# WinForms

> **Dành cho**: Người chưa biết gì về C# WinForms muốn tự sửa, thêm tính năng và thiết kế lại UI.
> **Thời gian**: Đọc hết ~60 phút, thực hành ~2 tiếng.

---

## ✅ UI ĐÃ CÓ SẴN — Trả lời câu hỏi "trong file code đã có sẵn UI chưa?"

**Có, 100% UI đã được lập trình sẵn trong code.** Không cần dùng Visual Designer.
Tất cả 8 form (màn hình) đều có `InitializeUI()` tự tạo toàn bộ giao diện khi chạy.

> 💡 **Câu hỏi liên quan:** *"có icon, ảnh đầy đủ rồi?? tải về có chạy ngay được không?"*
> **→ CÓ, tải về chạy ngay được.** `app.ico`, `Icon1-5.png`, `logo.png`, `alarm.wav` **đã có sẵn** trong `Assets/` — lấy từ [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) (MIT License).
> Xem chi tiết tại [`Assets/NOTICE.txt`](../Assets/NOTICE.txt) và [`Docs/README_FULL.md`](README_FULL.md) phần đầu.

### Bản đồ: File code → Màn hình hiển thị

| File code | Màn hình | Hiển thị khi nào |
|-----------|----------|------------------|
| `AndonTerminal/Forms/TerminalMainForm.cs` | Grid màu chính (Terminal) | Khởi động AndonTerminal |
| `AndonTerminal/Forms/StationSelectForm.cs` | Popup chọn trạm | Bấm ô xanh ở line có nhiều trạm |
| `AndonTerminal/Forms/AlarmTypeForm.cs` | Popup chọn Vàng/Đỏ | Bước 3: chọn mức độ |
| `AndonTerminal/Forms/EmployeeInputForm.cs` | Popup nhập Mã NV | Bước 4, 5, 7: nhập thông tin NV |
| `AndonTerminal/Forms/FixCompleteForm.cs` | Popup nhập ghi chú sửa | Bước 6: KTV sửa xong |
| `AndonDashboard/Forms/DashboardMainForm.cs` | Grid tổng quan (Dashboard) | Khởi động AndonDashboard |
| `AndonDashboard/Forms/StatisticsForm.cs` | Bảng thống kê MTTR/MTBF | Bấm nút "📊 Thống kê" |
| `AndonDashboard/Forms/TicketDetailForm.cs` | Chi tiết 1 phiếu sự cố | Double-click ô trên Dashboard |

### Giao diện thực tế của từng màn hình

**1. TerminalMainForm — Grid chính trên màn hình nhà máy:**
```
╔══════════════════════════════════════════════════════════════════╗
║  🏭 eAndon Terminal  |  terminal01               08:30:15 27/02 ║
╠══════════════════════════════════════════════════════════════════╣
║          │ Hỗ trợ TL │ Bảo trì   │ Chất lượng│ Thiếu VL  │ ĐG  ║
╠══════════╪═══════════╪═══════════╪═══════════╪═══════════╪═════╣
║ 010      │  [🟢  ✓ ] │[🟡05m30s] │ [🟢  ✓ ] │ [🟢  ✓ ] │[🟢] ║
║ Line 1   │           │           │           │           │     ║
╠══════════╪═══════════╪═══════════╪═══════════╪═══════════╪═════╣
║ 020      │  [🔴09m] │ [🟢  ✓ ] │ [🟠Sửa  ] │ [🟢  ✓ ] │[🟢] ║
║ Line 2   │           │           │           │           │     ║
╠══════════╪═══════════╪═══════════╪═══════════╪═══════════╪═════╣
║ 030      │  [🟢  ✓ ] │ [🟢  ✓ ] │ [🔵Chờ  ] │ [🟢  ✓ ] │[🟢] ║
║ Line 3   │           │           │           │           │     ║
╚══════════════════════════════════════════════════════════════════╝
```

**2. AlarmTypeForm — Popup chọn mức độ:**
```
╔════════════════════════════════════════════════════╗
║                Chọn mức độ sự cố                   ║
╠══════════════════════╦═════════════════════════════╣
║     🟡  VÀNG         ║     🔴  ĐỎ                 ║
║                      ║                             ║
║       ⚠              ║        🛑                   ║
║                      ║                             ║
║  Trạm có vấn đề      ║  Trạm đã dừng               ║
║  nhưng vẫn chạy      ║  hoàn toàn                  ║
╠══════════════════════╩═════════════════════════════╣
║                   [  Hủy  ]                        ║
╚════════════════════════════════════════════════════╝
```

**3. EmployeeInputForm — Popup nhập Mã NV:**
```
╔═══════════════════════════════════════════╗
║   Nhập thông tin Operator                 ║
║─────────────────────────────────────────  ║
║  Vui lòng nhập thông tin để báo lỗi       ║
║                                           ║
║  Mã nhân viên *                           ║
║  [NV001_____________________________]     ║
║                                           ║
║  Họ và tên *                              ║
║  [Nguyễn Văn A_____________________]     ║
║                                           ║
║  [  ✓ Xác nhận  ]  [  ✕ Hủy  ]          ║
╚═══════════════════════════════════════════╝
```

**4. StationSelectForm — Popup chọn trạm:**
```
╔══════════════════════════════════════╗
║   Line: Line 1                       ║
║   Chọn trạm bị lỗi:                  ║
║  ┌──────────────────────────────┐   ║
║  │  ST-010-01 - Trạm cắt laser  │   ║
║  └──────────────────────────────┘   ║
║  ┌──────────────────────────────┐   ║
║  │  ST-010-02 - Trạm hàn điểm  │   ║
║  └──────────────────────────────┘   ║
║  ┌──────────────────────────────┐   ║
║  │  ST-010-03 - Trạm uốn CNC   │   ║
║  └──────────────────────────────┘   ║
║  [           Hủy              ]     ║
╚══════════════════════════════════════╝
```

**5. FixCompleteForm — Popup KTV nhập ghi chú:**
```
╔════════════════════════════════════════════════╗
║  ✓ Hoàn thành sửa chữa                        ║
║  Line: Line 1  |  Trạm: ST-010-02             ║
║─────────────────────────────────────────────── ║
║  Ghi chú sửa chữa (không bắt buộc):           ║
║  ┌────────────────────────────────────────┐   ║
║  │ Thay thế motor drive, kiểm tra         │   ║
║  │ encoder, vận hành ổn định...           │   ║
║  │                                        │   ║
║  └────────────────────────────────────────┘   ║
║  [  ✓ Đã sửa xong — Chờ Leader xác nhận  ]   ║
║  [Hủy]                                        ║
╚════════════════════════════════════════════════╝
```

**6. DashboardMainForm — Giám sát toàn nhà máy:**
```
╔══════════════════════════════════════════════════════════════════╗
║  🏭 eAndon Dashboard         08:30:15  27/02   [ 📊 Thống kê ]  ║
╠══════════════════════════════════════════════════════════════════╣
║  🟢 Green  🟡 Yellow  🔴 Red  🟠 Repairing  🔵 WaitLeader       ║
╠══════════╤═══════════╤═══════════╤═══════════╤═══════════╤═════╣
║          │ Hỗ trợ TL │ Bảo trì   │ Chất lượng│ Thiếu VL  │ ĐG  ║
╠══════════╪═══════════╪═══════════╪═══════════╪═══════════╪═════╣
║ 010 L1   │  [🟢  ✓ ] │[🟡05m30s] │ [🟢  ✓ ] │ [🟢  ✓ ] │[🟢] ║
╠══════════╪═══════════╪═══════════╪═══════════╪═══════════╪═════╣
║ 020 L2   │  [🔴09m] │ [🟢  ✓ ] │ [🟠Sửa  ] │ [🟢  ✓ ] │[🟢] ║
╠══════════╪═══════════╪═══════════╪═══════════╪═══════════╪═════╣
║  Cập nhật từ file lúc 08:30:20                                  ║
╚══════════════════════════════════════════════════════════════════╝
```

**7. StatisticsForm — Thống kê MTTR/MTBF:**
```
╔═══════════════════════════════════════════════════════════════════╗
║  📊 Thống kê Daily Stats — eAndon                                ║
╠═══════════════════════════════════════════════════════════════════╣
║ Từ ngày [20/02] Đến ngày [27/02] Line [010] [🔍Lọc] [📉TopDown] ║
╠════════╤════════╤═════════╤════════╤═════════╤══════╤═══════════╣
║ Ngày   │Mã Line │Tên Line │Sự cố  │Downtime │Avail.│ MTTR(ph)  ║
╠════════╪════════╪═════════╪════════╪═════════╪══════╪═══════════╣
║27/02   │  010   │ Line 1  │   3   │ 01:15:30│97.5% │   15.2    ║
║26/02   │  010   │ Line 1  │   5   │ 02:30:00│94.8% │   18.0    ║
╠════════╧════════╧═════════╧════════╧═════════╧══════╧═══════════╣
║  Tổng cộng: 10 bản ghi | Từ 2026-02-20 đến 2026-02-27          ║
╚═══════════════════════════════════════════════════════════════════╝
```

**8. TicketDetailForm — Chi tiết 1 phiếu:**
```
╔══════════════════════════════════════════╗
║  Chi tiết Phiếu — TKT-20260227-001      ║
╠══════════════════════════════════════════╣
║  🎫 TKT-20260227-001           [xanh]   ║
║  Trạng thái: Closed                     ║
║─────────────────────────────────────────║
║  📍 Địa điểm                            ║
║    Line:      010 — Line 1              ║
║    Trạm:      ST-010-01                 ║
║    Loại:      [2] Hỗ trợ Bảo trì       ║
║    Mức độ:    Yellow                    ║
║─────────────────────────────────────────║
║  👷 Bước 1-4: Operator báo lỗi          ║
║    Thời gian: 27/02/2026 08:30:00       ║
║    Mã NV:     NV001                     ║
║    Họ tên:    Nguyễn Văn A              ║
║─────────────────────────────────────────║
║  🔧 Bước 5: KTV nhận sửa               ║
║  ✅ Bước 6: KTV hoàn thành             ║
║  👔 Bước 7: Leader xác nhận            ║
║  📊 Tổng downtime: 45 phút 30 giây     ║
╠══════════════════════════════════════════╣
║  [Đóng]                                 ║
╚══════════════════════════════════════════╝
```

### Cách UI được xây dựng — Code-only (không dùng Designer)

Mỗi form có hàm `InitializeUI()` tự tạo toàn bộ control:

```csharp
// Ví dụ từ AlarmTypeForm.cs — tạo Panel màu vàng:
var panelYellow = new Panel
{
    BackColor = Color.FromArgb(241, 196, 15),  // màu vàng
    Bounds = new Rectangle(20, 20, 290, 260)   // x=20, y=20, rộng=290, cao=260
};
// Thêm icon, nhãn vào panel:
var lblIcon = new Label { Text = "⚠", Font = new Font("Segoe UI", 48f) };
panelYellow.Controls.Add(lblIcon);
// Thêm panel vào form:
this.Controls.Add(panelYellow);
```

> 💡 **Điều này có nghĩa gì?** Bạn có thể sửa toàn bộ giao diện chỉ bằng cách **chỉnh sửa code C#** —
> đổi màu, di chuyển nút, thêm control — mà **không cần biết dùng Visual Designer**.

---

## MỤC LỤC

- [PHẦN 1 — Cài đặt môi trường](#phần-1--cài-đặt-môi-trường)
- [PHẦN 2 — Mở project và chạy lần đầu](#phần-2--mở-project-và-chạy-lần-đầu)
- [PHẦN 3 — Hiểu cấu trúc project](#phần-3--hiểu-cấu-trúc-project)
- [PHẦN 4 — Thay đổi cấu hình KHÔNG cần code](#phần-4--thay-đổi-cấu-hình-không-cần-code)
- [PHẦN 5 — Thay đổi giao diện UI](#phần-5--thay-đổi-giao-diện-ui)
- [PHẦN 6 — Thêm tính năng mới (code)](#phần-6--thêm-tính-năng-mới-code)
- [PHẦN 7 — Cơ sở dữ liệu SQLite](#phần-7--cơ-sở-dữ-liệu-sqlite)
- [PHẦN 8 — Debug và xử lý lỗi](#phần-8--debug-và-xử-lý-lỗi)
- [PHẦN 9 — Checklist thay đổi thường gặp](#phần-9--checklist-thay-đổi-thường-gặp)

---

## PHẦN 1 — Cài đặt môi trường

### 1.1 Phần mềm cần cài

| Phần mềm | Link | Ghi chú |
|----------|------|---------|
| **Visual Studio 2022 Community** | https://visualstudio.microsoft.com/vs/community/ | **Miễn phí**, chọn workload ".NET Desktop Development" |
| **.NET 8 SDK** | Tự động cài cùng Visual Studio | Hoặc tải tại https://dotnet.microsoft.com/download |
| **DB Browser for SQLite** | https://sqlitebrowser.org/ | Xem và chỉnh database, **khuyến khích cài** |
| **Git** | https://git-scm.com/ | Để clone code về máy |

### 1.2 Cài Visual Studio

1. Tải Visual Studio Community 2022 (miễn phí).
2. Khi cài, màn hình "Workloads" → tick vào **".NET desktop development"**.
3. Nhấn **Install** → đợi 10-20 phút.

> ⚠️ **Quan trọng**: Phải chọn đúng workload ".NET desktop development",
> nếu không sẽ không biên dịch được WinForms.

---

## PHẦN 2 — Mở project và chạy lần đầu

### 2.1 Clone code về máy

Mở **Command Prompt** hoặc **PowerShell**, gõ:

```bash
# Chọn thư mục bạn muốn lưu code, ví dụ D:\Projects
cd D:\Projects

# Clone repository
git clone https://github.com/sonic-kine06/preAndon.git

# Vào thư mục vừa clone
cd preAndon
```

### 2.2 Mở bằng Visual Studio

1. Mở Visual Studio 2022.
2. Chọn **"Open a project or solution"**.
3. Chọn file `eAndonCSharp.sln` trong thư mục vừa clone.
4. Visual Studio sẽ tự động restore NuGet packages (mất ~1 phút lần đầu).

### 2.3 Cấu trúc trong Visual Studio

Bên phải màn hình, "Solution Explorer" sẽ hiển thị:

```
📁 Solution 'eAndonCSharp'
  📁 AndonDashboard        ← Ứng dụng Dashboard (Manager xem)
    📁 Forms
      📄 DashboardMainForm.cs
      📄 StatisticsForm.cs
      📄 TicketDetailForm.cs
    📄 Program.cs
  📁 AndonTerminal         ← Ứng dụng Terminal (Operator dùng)
    📁 Forms
      📄 TerminalMainForm.cs
      📄 AlarmTypeForm.cs
      📄 EmployeeInputForm.cs
      📄 FixCompleteForm.cs
      📄 StationSelectForm.cs
    📄 Program.cs
  📁 SharedLib             ← Thư viện dùng chung
    📁 Models
      📄 IncidentTicket.cs
      📄 StationInfo.cs
    📁 Services
      📁 Analytics
      📄 AlarmLogger.cs
      📄 DailyStatsService.cs
      📄 IncidentService.cs
      📄 LineStationReader.cs
      📄 SettingsReader.cs
```

### 2.4 Build toàn bộ project

Nhấn **Ctrl+Shift+B** hoặc menu **Build → Build Solution**.

Nếu thấy `Build succeeded` ở dưới cùng → thành công! 🎉

### 2.5 Chạy lần đầu

**Cách 1 — Từ Visual Studio**:
1. Trong Solution Explorer, click phải vào **AndonTerminal**.
2. Chọn **"Set as Startup Project"**.
3. Nhấn **F5** để chạy Terminal.

**Cách 2 — Từ Command Line**:
```bash
# Chạy Terminal
dotnet run --project AndonTerminal

# Chạy Dashboard (mở cửa sổ khác)
dotnet run --project AndonDashboard
```

> 💡 **Tip**: Để chạy cả 2, click phải vào Solution trong Solution Explorer →
> **Properties** → **Multiple startup projects** → Set cả 2 thành "Start".

---

## PHẦN 3 — Hiểu cấu trúc project

### 3.1 Sơ đồ tổng quan — Dữ liệu chảy như thế nào

```
Operator bấm nút
      │
      ▼
TerminalMainForm.cs
  (Hiển thị grid ô màu)
      │ Click ô → mở popup
      │
      ├─► StationSelectForm.cs   (Bước 2: chọn trạm)
      ├─► AlarmTypeForm.cs       (Bước 3: chọn Yellow/Red)
      ├─► EmployeeInputForm.cs   (Bước 4: nhập mã NV)
      │
      ▼
IncidentService.cs              (Logic nghiệp vụ)
  → OpenIncident()              (Tạo ticket mới)
      │
      ▼
SQLite Database                 (Lưu trữ)
  Data/eandon.db
  Bảng: Tickets
      │
      │         File Data/terminalXX.txt
      │         (Ghi ra file mỗi 5 giây)
      │                  │
      ▼                  ▼
DashboardMainForm.cs     FileSystemWatcher
  (Manager theo dõi)  ← (Đọc file khi có thay đổi)
```

### 3.2 Mỗi file làm gì — Tóm tắt nhanh

| File | Vai trò | Khi nào cần sửa |
|------|---------|-----------------|
| `Assets/settings.txt` | Cấu hình số alarm, tên trạng thái | Thêm/đổi loại alarm, tên màu |
| `Assets/Workstations_terminals.txt` | Danh sách Line và Terminal | Thêm line mới, đổi tên |
| `Assets/Lines_stations.txt` | Danh sách trạm trong mỗi line | Thêm trạm mới |
| `TerminalMainForm.cs` | UI chính của Terminal | Sửa màu, layout, thêm nút |
| `DashboardMainForm.cs` | UI chính của Dashboard | Sửa màu, layout, thêm nút |
| `AlarmTypeForm.cs` | Popup chọn Yellow/Red | Sửa text, màu, thêm mức độ |
| `EmployeeInputForm.cs` | Popup nhập Mã NV + Tên | Thêm field, validation |
| `FixCompleteForm.cs` | Popup KTV nhập ghi chú sửa | Thêm field |
| `StationSelectForm.cs` | Popup chọn trạm | Sửa layout nút |
| `IncidentTicket.cs` | Model phiếu sự cố | Thêm field mới |
| `IncidentService.cs` | Logic DB cho ticket | Thêm query, bước mới |
| `StatisticsForm.cs` | Màn hình thống kê | Thêm cột, biểu đồ |

### 3.3 Luồng 7 bước sự cố

```
[1] Ô Grid xanh (Green)
    Operator thấy sự cố → Bấm vào ô xanh

[2] StationSelectForm (nếu line có nhiều trạm)
    → Chọn trạm cụ thể đang gặp sự cố

[3] AlarmTypeForm
    → Chọn Yellow (vẫn chạy) hoặc Red (đã dừng)

[4] EmployeeInputForm — "Nhập thông tin Operator"
    → Nhập Mã NV và Họ tên Operator

[5] Ô Grid chuyển vàng/đỏ, timer bắt đầu đếm
    KTV đến → Bấm vào ô vàng/đỏ
    → EmployeeInputForm — "Nhập thông tin KTV"
    → Ô chuyển cam (Repairing)

[6] KTV sửa xong → Bấm vào ô cam
    → FixCompleteForm — Nhập ghi chú sửa
    → Ô chuyển xanh dương (WaitLeader) hoặc xanh lá nếu không cần Leader

[7] Leader đến → Bấm vào ô xanh dương
    → EmployeeInputForm — "Xác nhận của Leader"
    → Ticket đóng, ô chuyển xanh lá (Green)
    → DailyStats được cập nhật tự động
```

### 3.4 Màu 5 trạng thái — Giải mã

```
🟢 Green     (0) = Bình thường, không có sự cố
🟡 Yellow    (1) = Báo lỗi nhưng máy vẫn chạy được
🔴 Red       (2) = Máy đã dừng, nghiêm trọng
🟠 Orange    (3) = KTV đang sửa chữa (Repairing)
🔵 Blue      (4) = Chờ Leader xác nhận (WaitLeader)
   Closed    (5) = Đã đóng (không hiển thị, chỉ trong DB)
```

Các màu này được định nghĩa trong `IncidentTicket.cs`:
```csharp
public enum TicketStatus
{
    Green = 0,
    Yellow = 1,
    Red = 2,
    Repairing = 3,
    WaitLeader = 4,
    Closed = 5
}
```

---

## PHẦN 4 — Thay đổi cấu hình KHÔNG cần code

> 💡 **Tất cả thay đổi trong phần này chỉ cần sửa file text**, không cần build lại.

### 4.1 Thêm Line mới

**File**: `Assets/Workstations_terminals.txt`

```
6          ← Số lượng line (PHẢI cập nhật khi thêm)
#;Workstation nr.;Workstation name;Terminal name;
0;010;Line 1;terminal01
1;020;Line 2;terminal01
2;030;Line 3;terminal02
3;040;Line 4;terminal02
4;050;Line 5;terminal03
5;060;Line 6;terminal03
6;070;Line 7 MỚI;terminal04    ← Thêm dòng này
```

> ⚠️ Đừng quên cập nhật con số đầu tiên (6 → 7) và index cuối cùng (5 → 6).

**File**: `Assets/Lines_stations.txt` — thêm trạm cho line mới:

```
LINE 070; Line 7 MỚI
  ST-070-01; Trạm lắp ráp X
  ST-070-02; Trạm kiểm tra Y
```

### 4.2 Thêm loại Alarm

**File**: `Assets/settings.txt`

```ini
# Tăng số lượng alarm (ví dụ từ 5 lên 6)
Number of alarm types to display : 6

# Thêm nhãn mới
Alarm label 6 : Yêu cầu an toàn
Alarm image file 6 : Icon6.png
```

### 4.3 Đổi tên các trạng thái

```ini
Green status name : Bình thường
Yellow status name : Chờ hỗ trợ
Red status name : Dừng khẩn
Orange status name : Đang sửa
Blue status name : Chờ duyệt
```

### 4.4 Tắt xác nhận Leader (đóng ticket ngay sau KTV sửa xong)

```ini
Require Leader Confirmation : false
```

### 4.5 Đổi âm thanh cảnh báo

```ini
Alarm sound file : my_alarm.wav
```

Đặt file `my_alarm.wav` vào thư mục `Assets/`.

---

## PHẦN 5 — Thay đổi giao diện UI

### 5.1 WinForms cơ bản — Cần biết gì

**WinForms** là framework UI của Windows. Mỗi cửa sổ là một `Form`, bên trong chứa **Controls** (nút bấm, nhãn, text box...).

Các Controls thường dùng:

| Control | Class | Dùng để |
|---------|-------|---------|
| Nút bấm | `Button` | Click để thực hiện hành động |
| Nhãn text | `Label` | Hiển thị chữ (không nhập được) |
| Ô nhập chữ | `TextBox` | Cho người dùng nhập |
| Panel | `Panel` | Khung chứa các control khác |
| Grid dữ liệu | `DataGridView` | Hiển thị bảng dữ liệu |
| Chọn ngày | `DateTimePicker` | Chọn ngày tháng |

### 5.2 Hệ tọa độ — `Bounds` và `Location`

Trong WinForms, vị trí và kích thước control được đặt bằng:

```csharp
// Cách 1: Dùng Bounds (x, y, width, height)
myButton.Bounds = new Rectangle(10, 20, 200, 50);
// → button tại x=10, y=20, rộng=200px, cao=50px

// Cách 2: Tách riêng
myButton.Location = new Point(10, 20);  // vị trí
myButton.Size = new Size(200, 50);      // kích thước

// Gốc tọa độ (0,0) là góc TRÊN TRÁI của form/panel chứa
// x tăng dần sang PHẢI
// y tăng dần xuống DƯỚI
```

```
(0,0) ─────────────────────► x tăng dần
  │
  │   Button tại (10, 20)
  │       ┌──────────┐
  │       │  Button  │  (10,20) đến (210,70)
  │       └──────────┘
  ▼
y tăng dần
```

### 5.3 Thay đổi màu sắc

Tất cả màu được định nghĩa ngay đầu mỗi Form class:

**Ví dụ trong `TerminalMainForm.cs`:**
```csharp
// Tìm các dòng này ở đầu class để sửa màu:
private static readonly Color ColorGreen      = Color.FromArgb(46, 204, 113);   // RGB: R=46, G=204, B=113
private static readonly Color ColorYellow     = Color.FromArgb(241, 196, 15);
private static readonly Color ColorRed        = Color.FromArgb(192, 57, 43);
private static readonly Color ColorOrange     = Color.FromArgb(230, 126, 34);
private static readonly Color ColorBlue       = Color.FromArgb(52, 152, 219);
private static readonly Color ColorBackground = Color.FromArgb(44, 62, 80);    // Nền tối
private static readonly Color ColorHeader     = Color.FromArgb(36, 50, 64);    // Header tối hơn
```

**Cách tìm màu RGB:**
1. Dùng Paint (Windows) hoặc https://colorpicker.me/
2. Chọn màu bạn muốn → ghi lại R, G, B
3. Sửa `Color.FromArgb(R, G, B)` tương ứng

**Ví dụ đổi màu nền sang màu xám nhạt:**
```csharp
// Trước
private static readonly Color ColorBackground = Color.FromArgb(44, 62, 80);
// Sau (xám nhạt)
private static readonly Color ColorBackground = Color.FromArgb(240, 240, 240);
```

### 5.4 Thay đổi font chữ và kích thước

Tìm các dòng `new Font(...)` trong code:

```csharp
// Format: new Font("Tên font", kích thước, kiểu chữ)
new Font("Segoe UI", 14f, FontStyle.Bold)    // Segoe UI, 14pt, đậm
new Font("Segoe UI", 10f)                    // Segoe UI, 10pt, thường
new Font("Arial", 12f, FontStyle.Italic)     // Arial, 12pt, nghiêng

// FontStyle có thể kết hợp:
FontStyle.Bold | FontStyle.Italic            // Vừa đậm vừa nghiêng
```

**Font thường dùng cho UI tiếng Việt:**
- `"Segoe UI"` — hiện đại, đẹp trên Windows 10/11 ✅
- `"Arial"` — phổ biến, tương thích tốt ✅
- `"Times New Roman"` — serif, truyền thống
- `"Consolas"` — monospace, tốt cho code/ID

### 5.5 Thêm một Button mới vào Dashboard

**Ví dụ**: Thêm nút "📤 Xuất CSV" vào header của DashboardMainForm.

Tìm đoạn code tạo Panel header trong `DashboardMainForm.cs`, sau đó thêm:

```csharp
// Tìm đoạn này trong InitializeUI():
var btnStats = new Button { ... };
panelTop.Controls.Add(btnStats);

// Thêm ngay SAU đó:
var btnExport = new Button
{
    Text = "📤 Xuất CSV",
    ForeColor = Color.White,
    BackColor = Color.FromArgb(39, 174, 96),      // màu xanh lá
    FlatStyle = FlatStyle.Flat,
    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
    Cursor = Cursors.Hand,
    Bounds = new Rectangle(this.Width - 360, 15, 130, 35),  // đặt bên trái nút Thống kê
    Anchor = AnchorStyles.Top | AnchorStyles.Right
};
btnExport.FlatAppearance.BorderSize = 0;
btnExport.Click += (s, e) =>
{
    // ── [TODO] Viết logic xuất CSV tại đây ──
    MessageBox.Show("Tính năng xuất CSV — bạn sẽ code tại đây!", "Thông báo");
};
panelTop.Controls.Add(btnExport);
```

### 5.6 Thêm một Label thông tin vào Terminal

**Ví dụ**: Hiển thị "Ca: Sáng" ở header Terminal.

Tìm đoạn tạo `panelHeader` trong `TerminalMainForm.cs`:

```csharp
// Thêm vào sau khi tạo lblTime:
var lblShift = new Label
{
    Name = "lblShift",
    Text = GetCurrentShiftName(),    // hàm tự viết bên dưới
    ForeColor = Color.FromArgb(46, 204, 113),  // màu xanh lá
    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
    AutoSize = false,
    Bounds = new Rectangle(this.Width - 450, 15, 150, 30),
    TextAlign = ContentAlignment.MiddleRight,
    Anchor = AnchorStyles.Top | AnchorStyles.Right
};
panelHeader.Controls.Add(lblShift);
```

Thêm hàm helper vào phần `// ► [TODO] MỞ RỘNG TERMINAL`:
```csharp
private string GetCurrentShiftName()
{
    int hour = DateTime.Now.Hour;
    if (hour >= 6 && hour < 14) return "Ca: Sáng (06-14h)";
    if (hour >= 14 && hour < 22) return "Ca: Chiều (14-22h)";
    return "Ca: Đêm (22-06h)";
}
```

### 5.7 Thay đổi kích thước ô Grid

Trong `TerminalMainForm.cs`, tìm phần đầu `InitializeUI()`:

```csharp
// Tìm và sửa các giá trị này:
int cellWidth  = 120;   // chiều rộng mỗi ô (pixel)
int cellHeight = 80;    // chiều cao mỗi ô (pixel)
int headerH    = 50;    // chiều cao hàng header (tên Alarm)
int rowHeaderW = 150;   // chiều rộng cột header (tên Line)
int padding    = 5;     // khoảng cách giữa các ô
```

### 5.8 Đổi từ màu nền tối sang sáng (Light Mode)

Thay thế tất cả màu tối bằng màu sáng. Ví dụ trong `DashboardMainForm.cs`:

```csharp
// TRƯỚC (Dark Mode):
private static readonly Color ColorBackground = Color.FromArgb(44, 62, 80);
private static readonly Color ColorHeader     = Color.FromArgb(36, 50, 64);

// SAU (Light Mode):
private static readonly Color ColorBackground = Color.FromArgb(245, 245, 245);   // xám rất nhạt
private static readonly Color ColorHeader     = Color.FromArgb(52, 73, 94);      // xanh đậm giữ nguyên

// Và thay màu chữ:
// ForeColor = Color.White; → ForeColor = Color.FromArgb(44, 62, 80);
```

---

## PHẦN 6 — Thêm tính năng mới (code)

### 6.1 Thêm một trường (field) mới vào Phiếu sự cố

**Ví dụ**: Thêm trường "Số lượng sản phẩm bị lỗi" (`DefectQuantity`).

**Bước 1**: Thêm property vào `IncidentTicket.cs`:
```csharp
// Thêm vào cuối class IncidentTicket, trước dấu }
/// <summary>Số lượng sản phẩm bị lỗi (Operator nhập khi báo)</summary>
public int? DefectQuantity { get; set; }
```

**Bước 2**: Thêm cột vào database (trong `IncidentService.cs`, hàm `InitializeDatabase`):
```csharp
// Tìm dòng cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Tickets..."
// Thêm cột DefectQuantity vào cuối danh sách cột:
DefectQuantity INTEGER DEFAULT 0
```

> ⚠️ Nếu database đã tồn tại, cần chạy câu SQL sau một lần để thêm cột:
> ```sql
> ALTER TABLE Tickets ADD COLUMN DefectQuantity INTEGER DEFAULT 0;
> ```
> (Dùng DB Browser for SQLite để chạy câu SQL này)

**Bước 3**: Thêm field vào `EmployeeInputForm.cs` hoặc tạo form mới:
```csharp
// Thêm TextBox mới vào EmployeeInputForm hoặc FixCompleteForm
private TextBox _txtDefectQty;

// Trong InitializeUI(), thêm sau txtEmployeeName:
var lblDefect = new Label { Text = "Số lượng NG:", ... };
this.Controls.Add(lblDefect);

_txtDefectQty = new TextBox { PlaceholderText = "Ví dụ: 5", ... };
this.Controls.Add(_txtDefectQty);

// Thêm property public:
public int DefectQuantity { get; private set; }

// Trong BtnOK_Click, thêm:
int.TryParse(_txtDefectQty.Text.Trim(), out int qty);
DefectQuantity = qty;
```

**Bước 4**: Lưu vào DB trong `IncidentService.OpenIncident()`:
```csharp
// Thêm vào INSERT statement:
cmd.Parameters.AddWithValue("@DefectQuantity", employeeForm.DefectQuantity);
```

### 6.2 Thêm bước mới vào luồng sự cố

**Ví dụ**: Thêm "Bước 3.5: Chụp ảnh sự cố" giữa bước 3 và 4.

Trong `TerminalMainForm.cs`, hàm `HandleNewAlarm()`:

```csharp
// Tìm đoạn này (giữa bước 3 và 4):
// Bước 3: Chọn Yellow/Red
using (var alarmForm = new AlarmTypeForm(_settings))
{
    if (alarmForm.ShowDialog(this) != DialogResult.OK) return;
    severity = alarmForm.SelectedSeverity;
}

// Thêm NGAY SAU:
// Bước 3.5 (tùy chọn): Chụp ảnh
// ── [TODO] Bỏ comment đoạn dưới để bật tính năng chụp ảnh ──
// string photoPath = null;
// var result = MessageBox.Show(
//     "Bạn có muốn chụp ảnh sự cố không?",
//     "Chụp ảnh", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
// if (result == DialogResult.Yes)
// {
//     // Mở webcam hoặc chọn file ảnh
//     using (var openDialog = new OpenFileDialog())
//     {
//         openDialog.Filter = "Ảnh|*.jpg;*.png;*.bmp";
//         if (openDialog.ShowDialog() == DialogResult.OK)
//             photoPath = openDialog.FileName;
//     }
// }
```

### 6.3 Thêm query mới vào IncidentService

**Ví dụ**: Lấy ticket theo mã KTV.

Thêm vào phần `// ► [TODO] MỞ RỘNG INCIDENT SERVICE` trong `IncidentService.cs`:

```csharp
/// <summary>
/// Lấy tất cả ticket đã được giao cho một KTV cụ thể.
/// </summary>
/// <param name="technicianId">Mã nhân viên KTV</param>
/// <param name="date">Lọc theo ngày yyyy-MM-dd (null = tất cả)</param>
public List<IncidentTicket> GetTicketsByTechnician(string technicianId, string date = null)
{
    string where = "WHERE TechnicianId = @TechId";
    if (!string.IsNullOrEmpty(date))
        where += " AND ReportDate = @Date";
    where += " ORDER BY TechCheckinAt DESC";

    var result = new List<IncidentTicket>();
    using (var conn = CreateConnection())
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = $"SELECT * FROM Tickets {where}";
        cmd.Parameters.AddWithValue("@TechId", technicianId);
        if (!string.IsNullOrEmpty(date))
            cmd.Parameters.AddWithValue("@Date", date);

        using (var reader = cmd.ExecuteReader())
            while (reader.Read()) result.Add(MapTicket(reader));
    }
    return result;
}
```

### 6.4 Xuất dữ liệu ra CSV

Thêm hàm vào `IncidentService.cs`:

```csharp
/// <summary>
/// Xuất tất cả ticket trong khoảng ngày ra file CSV.
/// </summary>
public void ExportToCsv(string filePath, string fromDate, string toDate)
{
    var tickets = QueryTickets(
        $"WHERE ReportDate >= '{fromDate}' AND ReportDate <= '{toDate}' ORDER BY ReportedAt");

    var sb = new System.Text.StringBuilder();

    // Header
    sb.AppendLine("TicketId,LineNumber,LineName,StationName,AlarmTypeName," +
                  "Severity,ReportedAt,OperatorName,TechnicianName,TechCheckinAt," +
                  "TechFixedAt,FixNote,LeaderName,LeaderConfirmedAt,Status");

    // Data
    foreach (var t in tickets)
    {
        sb.AppendLine($"\"{t.TicketId}\",\"{t.LineNumber}\",\"{t.LineName}\"," +
                      $"\"{t.StationName}\",\"{t.AlarmTypeName}\",\"{t.Severity}\"," +
                      $"\"{t.ReportedAt:yyyy-MM-dd HH:mm:ss}\",\"{t.OperatorName}\"," +
                      $"\"{t.TechnicianName}\",\"{t.TechCheckinAt:yyyy-MM-dd HH:mm:ss}\"," +
                      $"\"{t.TechFixedAt:yyyy-MM-dd HH:mm:ss}\",\"{t.FixNote}\"," +
                      $"\"{t.LeaderName}\",\"{t.LeaderConfirmedAt:yyyy-MM-dd HH:mm:ss}\"," +
                      $"\"{t.CurrentStatus}\"");
    }

    System.IO.File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
}
```

---

## PHẦN 7 — Cơ sở dữ liệu SQLite

### 7.1 Xem database với DB Browser for SQLite

1. Cài **DB Browser for SQLite** (miễn phí).
2. Mở file `Data/eandon.db` (trong thư mục project).
3. Tab **"Browse Data"** → chọn bảng **"Tickets"** để xem phiếu sự cố.
4. Tab **"Execute SQL"** → chạy câu SQL tùy ý.

### 7.2 Cấu trúc bảng Tickets

```sql
CREATE TABLE Tickets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,  -- ID tự tăng
    TicketId TEXT UNIQUE,                  -- Mã phiếu duy nhất: TKT-20260227-123
    LineNumber TEXT,                       -- Mã line: "010"
    LineName TEXT,                         -- Tên line: "Line 1"
    StationId TEXT,                        -- Mã trạm: "ST-010-01"
    StationName TEXT,                      -- Tên trạm: "Trạm cắt laser"
    AlarmTypeIndex INTEGER,                -- Chỉ số alarm: 1, 2, 3...
    AlarmTypeName TEXT,                    -- Tên alarm: "Hỗ trợ Bảo trì"
    Severity TEXT,                         -- "Yellow" hoặc "Red"
    ReportedAt TEXT,                       -- "2026-02-27 08:30:00"
    OperatorId TEXT,                       -- Mã NV Operator
    OperatorName TEXT,                     -- Tên Operator
    TechCheckinAt TEXT,                    -- Thời điểm KTV nhận sửa
    TechnicianId TEXT,                     -- Mã NV KTV
    TechnicianName TEXT,                   -- Tên KTV
    TechFixedAt TEXT,                      -- Thời điểm KTV sửa xong
    FixNote TEXT,                          -- Ghi chú sửa chữa
    LeaderConfirmedAt TEXT,                -- Thời điểm Leader xác nhận
    LeaderId TEXT,                         -- Mã NV Leader
    LeaderName TEXT,                       -- Tên Leader
    Status INTEGER DEFAULT 1,             -- 0=Green,1=Yellow,2=Red,3=Repairing,4=WaitLeader,5=Closed
    ReportDate TEXT                        -- "2026-02-27" (để query theo ngày)
);
```

### 7.3 Các câu SQL hữu ích

```sql
-- Xem tất cả ticket hôm nay
SELECT * FROM Tickets WHERE ReportDate = date('now') ORDER BY ReportedAt DESC;

-- Đếm số sự cố theo line tuần này
SELECT LineNumber, LineName, COUNT(*) as TotalIncidents
FROM Tickets
WHERE ReportDate >= date('now', '-7 days')
GROUP BY LineNumber, LineName
ORDER BY TotalIncidents DESC;

-- Tính thời gian trung bình để KTV đến nơi (response time, phút)
SELECT
    TechnicianName,
    COUNT(*) as RepairCount,
    ROUND(AVG((julianday(TechFixedAt) - julianday(TechCheckinAt)) * 24 * 60), 1) as AvgRepairMin
FROM Tickets
WHERE Status = 5   -- chỉ ticket đã đóng
  AND TechnicianName IS NOT NULL
GROUP BY TechnicianName
ORDER BY AvgRepairMin;

-- Xóa tất cả ticket test (có TicketId bắt đầu bằng "TEST")
DELETE FROM Tickets WHERE TicketId LIKE 'TEST%';

-- Reset database (xóa tất cả, dùng khi test)
DELETE FROM Tickets;
DELETE FROM DailyStats;
```

### 7.4 Thêm cột vào database

Nếu bạn đã thêm field mới vào `IncidentTicket.cs` nhưng database cũ chưa có cột đó:

```sql
-- Chạy trong DB Browser for SQLite, tab "Execute SQL":
ALTER TABLE Tickets ADD COLUMN DefectQuantity INTEGER DEFAULT 0;
ALTER TABLE Tickets ADD COLUMN PhotoPath TEXT;
ALTER TABLE Tickets ADD COLUMN ShiftName TEXT;
```

---

## PHẦN 8 — Debug và xử lý lỗi

### 8.1 Dùng Breakpoint trong Visual Studio

**Breakpoint** = điểm dừng trong code. Khi chạy Debug, chương trình dừng lại tại đó để bạn kiểm tra giá trị biến.

**Ví dụ thực tế với eAndon**: Bạn muốn xem lúc Operator click ô xanh, biến `cellData` có đúng không.

1. Mở `TerminalMainForm.cs`
2. Click vào cạnh trái dòng `HandleGreenClick(cellData)` → xuất hiện **chấm đỏ 🔴**
3. Nhấn **F5** để chạy Debug
4. Trên Terminal, click vào 1 ô xanh → Visual Studio dừng lại tại breakpoint
5. Di chuột lên `cellData` trong code → tooltip hiện giá trị
6. Cửa sổ **Locals** (bên dưới) → thấy tất cả biến đang có
7. Cửa sổ **Watch** → gõ biểu thức bất kỳ để xem: `cellData.LineNumber`, `cellData.AlarmTypeIndex`, v.v.

**Phím tắt Debug**:
| Phím | Hành động |
|------|-----------|
| F5 | Chạy/Tiếp tục Debug |
| F9 | Bật/Tắt breakpoint tại dòng hiện tại |
| F10 | Step Over — chạy dòng hiện tại, KHÔNG vào hàm được gọi |
| F11 | Step Into — chạy dòng hiện tại, VÀO hàm được gọi |
| Shift+F5 | Dừng Debug |

### 8.2 In ra để debug (Debug.WriteLine)

```csharp
// Thêm using ở đầu file:
using System.Diagnostics;

// Trong code, in ra để xem giá trị:
Debug.WriteLine($"[DEBUG] TicketId = {ticket.TicketId}");
Debug.WriteLine($"[DEBUG] Cell status = {cell.Status}");

// Xem kết quả trong Visual Studio → menu View → Output Window
```

### 8.3 NuGet — Quản lý thư viện bên ngoài

**NuGet** là hệ thống quản lý package cho .NET — giống `pip` trong Python hay `npm` trong Node.js.

**eAndon dùng NuGet để cài**: `System.Data.SQLite` (kết nối SQLite database)

**Cách xem packages đã cài**:
- Chuột phải `SharedLib` trong Solution Explorer → **Manage NuGet Packages**
- Tab **Installed** → thấy `System.Data.SQLite 1.0.118`

**Cách restore packages khi lần đầu clone** (nếu build lỗi `missing reference`):
```
Chuột phải Solution (dòng trên cùng trong Solution Explorer)
→ Restore NuGet Packages
```

Hoặc qua command line:
```bash
dotnet restore
```

**Cách cài thêm package mới**:
1. Chuột phải project cần thêm → **Manage NuGet Packages**
2. Tab **Browse** → gõ tên package → chọn → **Install**
3. Hoặc qua Package Manager Console:
   ```
   Tools → NuGet Package Manager → Package Manager Console
   PM> Install-Package TenPackage -ProjectName TenProject
   ```

### 8.4 Git trong Visual Studio — Commit, Push, Pull

Visual Studio có giao diện Git tích hợp sẵn (menu **Git** hoặc cửa sổ **Git Changes**).

**Xem thay đổi**: Menu **Git → Git Changes** (Ctrl+0, Ctrl+G) → thấy các file đã sửa

**Commit thay đổi**:
1. Cửa sổ Git Changes → gõ message vào ô **Enter a message**
2. Nhấn **Commit All** (chỉ lưu local) hoặc **Commit All and Push** (lưu + đẩy lên GitHub)

**Pull (lấy code mới từ GitHub)**:
- Menu **Git → Pull** hoặc nhấn mũi tên xuống ↓ trong thanh trạng thái dưới cùng

**Thao tác thường dùng với eAndon**:
```
Bạn sửa settings.txt hoặc sửa code xong:
1. Git Changes → thấy file đã sửa
2. Gõ message: "Update alarm types in settings.txt"
3. Commit All and Push → code lên GitHub
```

> **Lưu ý**: `Data/`, `Logs/`, và `Assets/settings.txt` (nếu có password) nên trong `.gitignore`  
> để không vô tình commit data nhạy cảm lên GitHub.

### 8.5 Lỗi thường gặp và cách sửa

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| `NullReferenceException` | Dùng object chưa được khởi tạo | Kiểm tra `if (obj != null)` trước khi dùng |
| `Cannot open database` | Đường dẫn file DB sai | Kiểm tra `Data/eandon.db` có tồn tại không |
| Grid trống khi mở | File `Workstations_terminals.txt` sai format | Xem lại Phần 4.1 |
| Không nghe âm thanh | File WAV không tồn tại | Đặt file vào `Assets/` và kiểm tra tên |
| Build lỗi `CS0246` | Thiếu `using` directive | Thêm `using SharedLib.Models;` hoặc namespace phù hợp |
| `SQLiteException: table already exists` | Chạy lần đầu bình thường | Không sao, `CREATE TABLE IF NOT EXISTS` tự bỏ qua |
| `missing reference SharedLib` | NuGet chưa restore | Chuột phải Solution → Restore NuGet Packages |

### 8.6 Xem log file

Mỗi ngày có 1 file log tại `Logs/alarmlog_YYYY-MM-DD.txt`:

```
Event DateTime | 2026-02-27 08:30:15; Workstation | 010 - Trạm cắt laser; Alarm type | Hỗ trợ Bảo trì; New Color | Yellow; Length of alarm (seconds) | 0
Event DateTime | 2026-02-27 09:15:30; Workstation | 010 - Trạm cắt laser; Alarm type | Hỗ trợ Bảo trì; New Color | Green; Length of alarm (seconds) | 2715
```

---

## PHẦN 9 — Checklist thay đổi thường gặp

### ✅ Thêm Line/Trạm mới (không cần code)
- [ ] Sửa `Assets/Workstations_terminals.txt` — thêm dòng + cập nhật số đầu
- [ ] Sửa `Assets/Lines_stations.txt` — thêm block LINE + các ST
- [ ] Restart ứng dụng để load lại

### ✅ Đổi tên/màu các trạng thái (không cần code)
- [ ] Sửa `Assets/settings.txt` — các key `*status name`
- [ ] Restart ứng dụng

### ✅ Thêm loại Alarm mới (không cần code)
- [ ] Tăng `Number of alarm types to display` trong `settings.txt`
- [ ] Thêm `Alarm label N` và `Alarm image file N`
- [ ] Restart ứng dụng

### ✅ Đổi màu UI (cần build lại)
- [ ] Tìm const màu đầu class Form cần sửa
- [ ] Sửa `Color.FromArgb(R, G, B)`
- [ ] **Ctrl+Shift+B** → Build → F5 để chạy lại

### ✅ Thêm nút mới vào Dashboard/Terminal (cần build lại)
- [ ] Tạo `new Button { ... }` với Bounds phù hợp
- [ ] Gán `Click` event handler
- [ ] Thêm vào panel: `panelTop.Controls.Add(myButton)`
- [ ] Build và chạy test

### ✅ Thêm field mới vào Ticket (cần build lại)
- [ ] Thêm property vào `IncidentTicket.cs`
- [ ] Thêm cột vào SQL trong `IncidentService.InitializeDatabase()`
- [ ] Nếu DB đã tồn tại: chạy `ALTER TABLE Tickets ADD COLUMN ...`
- [ ] Thêm `cmd.Parameters.AddWithValue(...)` vào `OpenIncident()`
- [ ] Thêm mapping trong `MapTicket()`
- [ ] Thêm UI để nhập/hiển thị field mới
- [ ] Build và test

### ✅ Thêm query mới (cần build lại)
- [ ] Thêm method vào `IncidentService.cs` (phần TODO)
- [ ] Gọi method từ Form tương ứng
- [ ] Build và test

---

## PHỤ LỤC — Tài liệu tham khảo

| Tài liệu | Link | Ghi chú |
|----------|------|---------|
| Hướng dẫn WinForms | https://learn.microsoft.com/en-us/dotnet/desktop/winforms/ | Chính thức của Microsoft |
| SQLite Tutorial | https://www.sqlitetutorial.net/ | Học SQL từ cơ bản |
| C# cơ bản | https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/ | Tour C# ngắn gọn |
| DB Browser SQLite | https://sqlitebrowser.org/ | Tải DB Browser miễn phí |
| Bảng màu RGB | https://colorpicker.me/ | Chọn màu và lấy RGB |

---

## CÁC FILE DOCS KHÁC

| File | Nội dung |
|------|---------|
| `Docs/UI_CUSTOMIZE.md` | Giải thích chi tiết từng Form, cách thiết kế lại giao diện |
| `Docs/DATABASE.md` | Cấu trúc bảng SQLite chi tiết, CRUD walkthrough |
| `Docs/ANALYTICS.md` | Giải thích các model Analytics (EWMA, Z-score, v.v.) |
| `Docs/CSHARP_VS_VBNET.md` | So sánh C# và VB.NET dựa trên code eAndon thực tế |
| `Docs/FAQ.md` | 20 câu hỏi thường gặp với đáp án |
| `Docs/README_FULL.md` | Tài liệu đầy đủ hệ thống |
| `SharedLib/Services/Email/README.md` | Hướng dẫn cấu hình Email notification |

---

> 💬 **Câu hỏi hoặc gặp vấn đề?** Kiểm tra phần [Debug và xử lý lỗi](#phần-8--debug-và-xử-lý-lỗi)
> hoặc xem file log tại `Logs/alarmlog_YYYY-MM-DD.txt`.
