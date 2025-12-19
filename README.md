# 🦉 Discord Bot Night Owl

Hệ thống Discord Bot mạnh mẽ được xây dựng trên nền tảng **.NET 10**, sử dụng trí tuệ nhân tạo **Gemini AI** và vận hành trên hạ tầng đám mây **Azure**.

---

## 🚀 Tính năng nổi bật
* **AI Integration:** Tích hợp mô hình `gemma-3-27b-it` mới nhất từ Google Gemini.
* **Database:** Lưu trữ dữ liệu ổn định với Azure Database for PostgreSQL.
* **Continuous Running:** Hoạt động 24/7 trên Linux VPS với cơ chế tự động khởi động lại.
* **Management:** Hệ thống lệnh quản trị (Owner) bảo mật bằng mã xác nhận.

---

## 🛠️ Thông số kỹ thuật
| Thành phần | Công nghệ |
| :--- | :--- |
| **Framework** | .NET 10.0 |
| **Hệ điều hành** | Ubuntu 24.04 (Azure VM) |
| **Cơ sở dữ liệu** | PostgreSQL (Flexible Server) |
| **Thư viện chính** | Discord.Net, EF Core, Npgsql |

---

## 📋 Quản lý dịch vụ trên VPS (Aliases)

Thay vì gõ các lệnh dài dòng, hệ thống đã được thiết lập các phím tắt (alias) để quản lý nhanh:

| Lệnh Alias | Mô tả công việc |
| :--- | :--- |
| `bot-status` | Kiểm tra trạng thái Bot (Sống/Chết) |
| `bot-log` | Xem nhật ký hoạt động thời gian thực (Real-time) |
| `bot-update` | Tự động kéo code mới, Build và Restart Bot |
| `bot-restart` | Khởi động lại dịch vụ Bot |
| `bot-stop` | Dừng hoạt động của Bot |

---

## ⚙️ Cấu hình môi trường (Environment Variables)

Thông tin bảo mật được cấu hình an toàn trong file `discordbot.service` thay vì để trong code:

* `ConnectionStrings__DefaultConnection`: Chuỗi kết nối Database Azure.
* `Discord__Token`: Token bí mật của ứng dụng Discord.
* `Gemini__Key`: API Key cho trí tuệ nhân tạo.
* `Owner__NukeDBCode` & `Owner__ShutdownCode`: Mã xác thực cho các lệnh nguy hiểm.

---

## 📂 Cấu trúc thư mục quan trọng
* `/src`: Chứa mã nguồn C# của dự án.
* `/out`: Thư mục chứa các file đã được biên dịch (Publish).
* `appsettings.json`: Cấu hình chung của ứng dụng.
* `README.md`: Tài liệu hướng dẫn này.

---

## ⚠️ Lưu ý cho người quản trị
1. **IP Whitelist:** Đảm bảo địa chỉ IP của VM luôn được phép truy cập vào Azure PostgreSQL.
2. **Azure Credits:** Kiểm tra hạn mức sử dụng (750h free tier) hằng tháng trên Portal.
3. **Backup:** Luôn commit code lên GitHub trước khi thực hiện lệnh `bot-update`.

---
*Phát triển và vận hành bởi **Nguyen Nhat Hao**.
