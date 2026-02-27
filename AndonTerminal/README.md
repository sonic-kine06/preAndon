# AndonTerminal — Terminal sản xuất

Đây là ứng dụng **WinForms** mà Operator/KTV/Leader dùng tại mỗi máy tính trên sàn sản xuất.

---

## Mỗi Terminal hiển thị gì?

Mỗi instance Terminal chỉ hiển thị **Line được phân công** cho nó trong `Assets/Workstations_terminals.txt`.

```
Ví dụ Workstations_terminals.txt (1 terminal = 1 line):
  0;010;Line 1;terminal01   ← terminal01 phụ trách Line 010
  1;020;Line 2;terminal02   ← terminal02 phụ trách Line 020
  2;030;Line 3;terminal03   ← terminal03 phụ trách Line 030
  3;040;Line 4;terminal04   ← terminal04 phụ trách Line 040
  4;050;Line 5;terminal05   ← terminal05 phụ trách Line 050
  5;060;Line 6;terminal06   ← terminal06 phụ trách Line 060
```

Khi chạy `AndonTerminal.exe terminal01`, form chỉ hiển thị Line 1 (1 hàng duy nhất).  
*(So sánh không phân biệt chữ hoa/thường — `terminal01` và `Terminal01` đều khớp.)*

> 💡 **Nếu cần 1 terminal phụ trách nhiều line** (ví dụ giám sát 2 line từ 1 màn hình),
> chỉ cần gán cùng tên terminal cho nhiều dòng trong file:
> ```
> 0;010;Line 1;terminal01
> 1;020;Line 2;terminal01   ← cùng terminal01 → hiển thị cả 2 hàng
> ```

---

## Cách chạy một Terminal

### Cách 1 — Visual Studio
```
Chuột phải AndonTerminal → Properties → Debug → Command line arguments: terminal01
F5 để chạy
```

### Cách 2 — Command line
```bash
cd AndonTerminal/bin/Debug/net8.0-windows/
AndonTerminal.exe terminal01   # Line 1
AndonTerminal.exe terminal02   # Line 2
AndonTerminal.exe terminal03   # Line 3
# ... mở 6 cửa sổ riêng biệt cho 6 terminal
```

### Cách 3 — dotnet run
```bash
dotnet run --project AndonTerminal -- terminal01
dotnet run --project AndonTerminal -- terminal02
dotnet run --project AndonTerminal -- terminal06
```

---

## Copy ra nhiều Terminal — kết nối Dashboard có thay đổi không?

**KHÔNG thay đổi gì cả.** Dashboard tự động đọc **tất cả file** `Data/*.txt`.

Mỗi Terminal ghi 1 file riêng:
```
terminal01.exe → ghi Data/terminal01.txt
terminal02.exe → ghi Data/terminal02.txt
terminal03.exe → ghi Data/terminal03.txt
```

Dashboard dùng `FileSystemWatcher` theo dõi thư mục `Data/` — khi **bất kỳ** file `.txt` nào thay đổi, Dashboard tự cập nhật ngay lập tức. **Không cần cấu hình thêm gì.**

```
Sơ đồ kết nối:
  terminal01.exe  →  Data/terminal01.txt  ─┐
  terminal02.exe  →  Data/terminal02.txt  ─┼──►  AndonDashboard.exe
  terminal03.exe  →  Data/terminal03.txt  ─┘      (đọc toàn bộ Data/*.txt)
```

---

## Cấu hình thêm Terminal mới

**Bước 1**: Thêm Lines vào `Assets/Workstations_terminals.txt`:
```
# Thêm terminal04 phụ trách Line 7 và Line 8
6;070;Line 7;terminal04
7;080;Line 8;terminal04
```
*(Cập nhật số đếm dòng đầu file: `6` → `8`)*

**Bước 2**: Chạy thêm 1 instance:
```bash
AndonTerminal.exe terminal04
```

**Không cần sửa code, không cần rebuild** — chỉ sửa file txt.

---

## Cấu trúc thư mục

```
AndonTerminal/
├── Program.cs               ← Entry point: đọc settings + khởi tạo services
├── AndonTerminal.csproj     ← Project file (.NET 8 WinForms)
├── Forms/
│   ├── TerminalMainForm.cs  ← Form chính: grid Lines × Alarm Types
│   ├── StationSelectForm.cs ← Popup bước 2: chọn trạm bị lỗi
│   ├── AlarmTypeForm.cs     ← Popup bước 3: chọn mức độ Yellow/Red
│   ├── EmployeeInputForm.cs ← Popup bước 4/5/7: nhập Mã NV + Tên
│   └── FixCompleteForm.cs   ← Popup bước 6: ghi chú sửa chữa
└── README.md                ← (file này)
```

---

## Luồng 7 bước trong TerminalMainForm.cs

```
[Bước 1] Operator click ô XANH
    → [Bước 2] StationSelectForm (chọn trạm — chỉ hiện nếu Line có > 1 trạm)
    → [Bước 3] AlarmTypeForm (chọn Yellow hoặc Red)
    → [Bước 4] EmployeeInputForm (nhập Mã + Tên Operator)
    → Ô đổi màu 🟡/🔴, bắt đầu đếm giờ, ghi SQLite + log file

[Bước 5] KTV click ô 🟡/🔴
    → EmployeeInputForm (nhập Mã + Tên KTV)
    → Ô đổi màu 🟠 (Repairing)

[Bước 6] KTV click ô 🟠
    → FixCompleteForm (ghi chú sửa chữa)
    → Ô đổi màu 🔵 (WaitLeader)

[Bước 7] Leader click ô 🔵
    → EmployeeInputForm (nhập Mã + Tên Leader)
    → Ô về 🟢, ticket đóng, cập nhật DailyStats
```

---

## Nguồn icon/ảnh

| File | Nguồn | License |
|------|-------|---------|
| `Assets/app.ico` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/Icon1-5.png` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/alarm.wav` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |
| `Assets/logo.png` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |

Xem `Assets/NOTICE.txt` để biết đầy đủ nội dung MIT License.
