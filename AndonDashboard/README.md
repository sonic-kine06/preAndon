# AndonDashboard — Màn hình giám sát tổng quan

Đây là ứng dụng **WinForms** dành cho **Quản lý/Leader** xem tổng quan toàn bộ sàn sản xuất trên 1 màn hình duy nhất.

---

## Dashboard hoạt động thế nào?

Dashboard **KHÔNG kết nối trực tiếp với Terminal**. Thay vào đó:

```
Terminal01  ghi  →  Data/terminal01.txt
Terminal02  ghi  →  Data/terminal02.txt
Terminal03  ghi  →  Data/terminal03.txt

                       ▲
          FileSystemWatcher theo dõi toàn bộ Data/*.txt
                       │
               AndonDashboard.exe
           (tự cập nhật ngay khi có file thay đổi)
```

**Ưu điểm**: Thêm bao nhiêu Terminal tùy ý, Dashboard tự nhận diện — không cần cấu hình thêm gì.

---

## Chạy Dashboard

```bash
# dotnet run
dotnet run --project AndonDashboard

# Hoặc exe trực tiếp
cd AndonDashboard/bin/Debug/net8.0-windows/
AndonDashboard.exe
```

---

## Khi copy ra nhiều Terminal — Dashboard có thay đổi không?

**KHÔNG.** Dashboard đọc `Data/*.txt` (tất cả file `.txt` trong thư mục Data).  
Mỗi Terminal mới chỉ cần:
1. Thêm entries vào `Assets/Workstations_terminals.txt`
2. Chạy `AndonTerminal.exe <tên_terminal_mới>`

Dashboard tự động hiển thị Lines mới ngay khi khởi động.

---

## Giao diện

```
╔══════════════════════════════════════════════════════════════════╗
║  🏭 eAndon Dashboard        08:30:15  27/02/2026   📊 Thống kê ║
╠══════════════════════════════════════════════════════════════════╣
║  🟢 Bình thường  🟡 Yellow  🔴 Red  🟠 Repairing  🔵 WaitLeader║
╠══════════════════════════════════════════════════════════════════╣
║        │ Hỗ trợ TL │  Bảo trì  │ Chất lượng │ Thiếu VL │      ║
║  Line1 │ [🟢  ✓  ] │ [🟡 5m  ] │ [🟢  ✓  ] │ [🟢  ✓ ] │      ║
║  Line2 │ [🔴  9m ] │ [🟢  ✓  ] │ [🟠 Sửa ] │ [🟢  ✓ ] │      ║
║  Line3 │ [🟢  ✓  ] │ [🟢  ✓  ] │ [🟢  ✓  ] │ [🟢  ✓ ] │      ║
╠══════════════════════════════════════════════════════════════════╣
║  Cập nhật từ file lúc 08:30:20                                  ║
╚══════════════════════════════════════════════════════════════════╝
```

- Click vào ô → `TicketDetailForm` xem chi tiết phiếu sự cố
- Click "📊 Thống kê" → `StatisticsForm` xem MTTR/MTBF/Availability

---

## Cấu trúc thư mục

```
AndonDashboard/
├── Program.cs                  ← Entry point
├── AndonDashboard.csproj       ← Project file (.NET 8 WinForms)
├── Forms/
│   ├── DashboardMainForm.cs    ← Form chính: grid + FileSystemWatcher + Timer
│   ├── TicketDetailForm.cs     ← Chi tiết 1 ticket khi click ô
│   └── StatisticsForm.cs       ← Thống kê MTTR/MTBF/Availability
└── README.md                   ← (file này)
```

---

## Nguồn icon/ảnh

| File | Nguồn | License |
|------|-------|---------|
| `Assets/app.ico` | [vitplanocka/eAndon](https://github.com/vitplanocka/eAndon) | MIT |

Xem `Assets/NOTICE.txt` để biết đầy đủ nội dung MIT License.
