# Assets — Tài nguyên cấu hình và media

Thư mục này chứa **tất cả file cần thiết** để ứng dụng eAndon chạy được.  
Sau khi `git clone`, **không cần thêm hay tải về gì thêm** — tất cả đã có sẵn.

---

## Danh sách file

### File cấu hình (bắt buộc)

| File | Mô tả |
|------|-------|
| `settings.txt` | Cấu hình hệ thống: số alarm, tên trạng thái, tên file icon/sound, email |
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

## Format file cấu hình chi tiết

### `settings.txt` — Cấu hình hệ thống

**Format**: Mỗi dòng có dạng `Key : Value` (dấu cách trước và sau dấu hai chấm). Dòng bắt đầu bằng `#` là comment.

```
# ─────────── Cấu hình Alarm Types ───────────
Number of alarm types to display : 5

Alarm label 1 : Ho tro Teamleader
Alarm image file 1 : Icon1.png
Alarm label 2 : Ho tro Bao tri
Alarm image file 2 : Icon2.png
Alarm label 3 : Ho tro Chat luong
Alarm image file 3 : Icon3.png
Alarm label 4 : Thieu vat lieu
Alarm image file 4 : Icon4.png
Alarm label 5 : Dong goi
Alarm image file 5 : Icon5.png

# ─────────── Cấu hình trạng thái ───────────
Green status name : Binh thuong
Yellow status name : Co van de
Red status name : Da dung
Orange status name : Dang sua
Blue status name : Cho Leader

# ─────────── Cấu hình âm thanh / logo ───────────
Alarm sound file : alarm.wav
Image file for company logo : logo.png

# ─────────── Yêu cầu xác nhận Leader ───────────
Require Leader Confirmation : true

# ─────────── Cấu hình Email ───────────
Email SMTP Server : mail.congty.com
Email SMTP Port : 587
Email Use SSL : true
Email Sender Address : andon@congty.com
Email Sender Password : matkhau123
Email Boss Recipients : giamdoc@congty.com|truongphong@congty.com
Email Manager Recipients : quanly.ktv@congty.com
Email Weekly Report Day : Sunday
Email Weekly Report Hour : 20
Email Alert No Tech Minutes : 10
Email Alert Long Repair Minutes : 30
Email Alert Anomaly Enabled : true
Email Maintenance Reminder Enabled : true
```

**Ảnh hưởng khi sửa**:
- `Number of alarm types to display` → thay đổi số cột trong grid (cần restart)
- `Require Leader Confirmation : false` → Bước 7 tự động, không cần Leader bấm
- `Email SMTP Server` → nếu để trống hoặc sai → email không gửi được (không crash app)

---

### `Workstations_terminals.txt` — Phân công Lines

**Format**:
```
<số lượng line>
#;Workstation nr.;Workstation name;Terminal name;
<index>;<mã số>;<tên>;<terminal>
```

**Ví dụ đầy đủ**:
```
6
#;Workstation nr.;Workstation name;Terminal name;
0;010;Line 1;terminal01
1;020;Line 2;terminal01
2;030;Line 3;terminal02
3;040;Line 4;terminal02
4;050;Line 5;terminal03
5;060;Line 6;terminal03
```

**Quan trọng**:
- Số dòng đầu (`6`) phải khớp với số line thực tế
- `Terminal name` không phân biệt hoa/thường khi khớp
- 1 terminal có thể phụ trách nhiều line (gán cùng tên terminal)

---

### `Lines_stations.txt` — Danh sách Trạm

**Format**:
```
LINE <mã line>; <tên line>
  <mã trạm>; <tên trạm>
  <mã trạm>; <tên trạm>
```

**Quy tắc**:
- Dòng `LINE`: KHÔNG thụt đầu dòng
- Dòng Station: thụt đầu dòng bằng ít nhất 1 khoảng trắng
- Nếu file không tồn tại → mỗi Line = 1 Station mặc định (tương thích gốc)

**Ví dụ**:
```
LINE 010; Line 1
  ST-010-01; Tram cat laser
  ST-010-02; Tram han diem
  ST-010-03; Tram uon CNC
LINE 020; Line 2
  ST-020-01; Tram may
LINE 030; Line 3
  ST-030-01; Tram A
  ST-030-02; Tram B
```

---

## Cách tùy chỉnh

### Đổi icon alarm
Thay file `Icon1.png` … `Icon5.png` bằng ảnh của bạn.  
Kích thước khuyến nghị: **32×32 px** hoặc **64×64 px** (ứng dụng tự scale).

### Thêm loại alarm mới (ví dụ loại thứ 6)
Trong `settings.txt`:
```
Number of alarm types to display : 6
Alarm label 6 : Su co An toan
Alarm image file 6 : Icon6.png
```
Đặt file `Icon6.png` vào thư mục `Assets/`. Restart app.

### Cấu hình Email
Điền đầy đủ phần Email trong `settings.txt` (xem mẫu ở trên). Nếu không cấu hình, email service sẽ không gửi nhưng app vẫn chạy bình thường.

---

## Tình huống thực tế: Thêm Line 7 và Line 8 vào nhà máy

Giả sử nhà máy mở thêm 2 dây chuyền mới: Line 7 và Line 8, do terminal04 phụ trách.

**Bước 1** — Sửa `Workstations_terminals.txt` (thêm 2 dòng, cập nhật số đầu file):
```
8                             ← đổi từ 6 → 8
#;Workstation nr.;Workstation name;Terminal name;
0;010;Line 1;terminal01
1;020;Line 2;terminal01
2;030;Line 3;terminal02
3;040;Line 4;terminal02
4;050;Line 5;terminal03
5;060;Line 6;terminal03
6;070;Line 7;terminal04       ← thêm mới
7;080;Line 8;terminal04       ← thêm mới
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

**Không cần sửa Dashboard, KHÔNG cần rebuild.** Dashboard tự nhận diện Lines mới.

> 💡 **Tóm tắt**: Thêm Line mới → sửa **2 file** (`Workstations_terminals.txt` + `Lines_stations.txt`) → chạy thêm 1 instance Terminal. Xong!

---

## Ví dụ: thêm terminal04 (phiên bản ngắn gọn)

Giống phần trên nhưng rút gọn để copy nhanh:

```
# Workstations_terminals.txt — thêm 2 dòng cuối, cập nhật số đầu thành 8
6;070;Line 7;terminal04
7;080;Line 8;terminal04
```

```
# Lines_stations.txt — thêm vào cuối file
LINE 070; Line 7
  ST-070-01; Tram moi A
LINE 080; Line 8
  ST-080-01; Tram moi C
```

```bash
AndonTerminal.exe terminal04
```

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
