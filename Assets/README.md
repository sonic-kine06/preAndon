# Assets — Tài nguyên cấu hình và media

Thư mục này chứa **tất cả file cần thiết** để ứng dụng eAndon chạy được.  
Sau khi `git clone`, **không cần thêm hay tải về gì thêm** — tất cả đã có sẵn.

---

## Danh sách file

### File cấu hình (bắt buộc)

| File | Mô tả |
|------|-------|
| `settings.txt` | Cấu hình hệ thống: số alarm, tên trạng thái, tên file icon/sound |
| `Workstations_terminals.txt` | Phân công Lines cho các Terminal |
| `Lines_stations.txt` | Danh sách Trạm (Station) trong mỗi Line |

### Icon và ảnh

| File | Mô tả | Kích thước |
|------|-------|-----------|
| `app.ico` | Icon cửa sổ ứng dụng (title bar, taskbar Windows) | 165 KB |
| `Icon1.png` | Icon loại alarm 1 (Ho tro Teamleader) | ~3.5 KB |
| `Icon2.png` | Icon loại alarm 2 (Ho tro Bao tri) | ~5 KB |
| `Icon3.png` | Icon loại alarm 3 (Ho tro Chat luong) | ~4 KB |
| `Icon4.png` | Icon loại alarm 4 (Thieu vat lieu) | ~28 KB |
| `Icon5.png` | Icon loại alarm 5 (Dong goi) | ~3 KB |
| `logo.png` | Logo công ty hiển thị trong UI | ~29 KB |

### Âm thanh

| File | Mô tả |
|------|-------|
| `alarm.wav` | Âm thanh cảnh báo khi alarm được bấm |

### Attribution

| File | Mô tả |
|------|-------|
| `NOTICE.txt` | Nội dung đầy đủ MIT License attribution cho các file trên |

---

## Nguồn gốc icon/ảnh/âm thanh

> **TẤT CẢ** icon, ảnh và âm thanh được lấy từ:  
> 🔗 **https://github.com/vitplanocka/eAndon**  
> 📄 **MIT License** — Copyright (c) 2020 Vit Planocka  
> Xem nội dung đầy đủ tại `NOTICE.txt`

**MIT License cho phép**: sử dụng, sao chép, phân phối, sửa đổi tự do — **chỉ cần giữ nguyên thông báo bản quyền**.

---

## Cách tùy chỉnh

### Đổi icon alarm
Thay file `Icon1.png` … `Icon5.png` bằng ảnh của bạn.  
Kích thước khuyến nghị: **32×32 px** hoặc **64×64 px** (ứng dụng tự scale).

### Đổi icon cửa sổ
Thay file `app.ico` bằng icon `.ico` của công ty bạn.

### Đổi logo
Thay file `logo.png` bằng logo của công ty bạn.

### Đổi âm thanh cảnh báo
Thay file `alarm.wav` bằng file `.wav` của bạn.  
Hoặc sửa trong `settings.txt`: `Alarm sound file : my_alarm.wav`

### Thêm loại alarm mới (ví dụ loại thứ 6)
Trong `settings.txt`:
```
Number of alarm types to display : 6
Alarm label 6 : Su co An toan
Alarm image file 6 : Icon6.png
```
Đặt file `Icon6.png` vào thư mục `Assets/`.

---

## Format file cấu hình

### settings.txt
```
# Dòng comment bắt đầu bằng #
Key : Value
```

### Workstations_terminals.txt
```
<số lượng line>
#;Workstation nr.;Workstation name;Terminal name;
<index>;<mã số>;<tên>;<terminal>
```
**Quan trọng**: Cột `Terminal name` phải khớp với tên terminal khi chạy (không phân biệt hoa/thường):
```bash
AndonTerminal.exe terminal01   # khớp với "terminal01" hoặc "Terminal01" trong file
```

### Lines_stations.txt
```
LINE <mã line>; <tên line>
  <mã trạm>; <tên trạm>
```
- Dòng LINE: KHÔNG thụt đầu dòng
- Dòng Station: thụt đầu dòng bằng khoảng trắng/tab

---

## Ví dụ: thêm terminal04

**Bước 1** — Sửa `Workstations_terminals.txt`:
```
8                          ← cập nhật tổng số (6 → 8)
#;Workstation nr.;Workstation name;Terminal name;
0;010;Line 1;terminal01
1;020;Line 2;terminal01
2;030;Line 3;terminal02
3;040;Line 4;terminal02
4;050;Line 5;terminal03
5;060;Line 6;terminal03
6;070;Line 7;terminal04   ← thêm mới
7;080;Line 8;terminal04   ← thêm mới
```

**Bước 2** — Thêm vào `Lines_stations.txt`:
```
LINE 070; Line 7
  ST-070-01; Tram moi A
  ST-070-02; Tram moi B
LINE 080; Line 8
  ST-080-01; Tram moi C
```

**Bước 3** — Chạy terminal mới:
```bash
AndonTerminal.exe terminal04
```

**KHÔNG cần sửa Dashboard, KHÔNG cần rebuild.** Dashboard tự nhận diện Lines mới.
