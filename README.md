# DukeShop Enterprise ERP — Đồ án Học phần Phát triển ứng dụng Web (PROG2008)

Ứng dụng Web quản trị kho hàng và bán hàng cá nhân xây dựng theo mô hình **Fullstack (HTML5/CSS3/JS + ASP.NET Core Web API .NET 10 + Entity Framework Core SQLite)**, đáp ứng 100% các tiêu chí kỹ thuật trong Rubric đánh giá cuối kỳ học phần **PROG2008**.

## 1. Kiến trúc Cơ sở dữ liệu (4 Bảng có Khóa chính & Khóa ngoại)

```
Categories (1) ────────< (N) Products (1) ────────< (N) InventoryLogs (N) >──────── (1) Users
  - Id (PK)                    - Id (PK)                   - Id (PK)                      - Id (PK)
  - Name (UNIQUE)              - Name                      - ProductId (FK -> Products)   - FullName
  - Description                - CategoryId (FK)           - ProductNameSnapshot          - Username (UNIQUE)
                               - Price (>0)                - UserId (FK -> Users)         - Email, Phone
                               - Quantity (>=0)            - ActionType                   - PasswordHash (PBKDF2)
                               - UpdatedAt                 - QuantityChange, Note         - Role (ADMIN/STAFF)
```

Script SQL đầy đủ đặt tại: `Database/dukeshop_schema_and_seed.sql`.

## 2. Các tính năng & Tiêu chuẩn bảo mật đã hoàn thiện

1. **Đăng ký tài khoản (Riêng cho Nhân viên)**: Kiểm tra trùng username, mật khẩu tối thiểu 8 ký tự, nút hiện/ẩn mật khẩu.
2. **Đăng nhập & Phân quyền bằng Bearer Token (JWT HMAC-SHA256)**:
   - Quản lý (`ADMIN`) có tài khoản tạo sẵn riêng biệt, không hiển thị công khai trên giao diện.
   - Tách biệt hoàn toàn giao diện và quyền API giữa Quản lý (Toàn quyền CRUD, lập phiếu nhập/xuất, quản lý danh mục) và Nhân viên (Chỉ xem, lọc, xem thống kê, tra cứu lịch sử kho, xuất báo cáo Excel).
3. **Đăng xuất an toàn**: Thu hồi Token tại Backend (`POST /api/auth/logout`) và xóa phiên ở Client.
4. **Quản lý tài khoản cá nhân (Hồ sơ cá nhân)**: Xem thông tin, cập nhật Họ tên/Email/SĐT (`PUT /api/auth/profile`) và Đổi mật khẩu (`PUT /api/auth/change-password`).
5. **Quên mật khẩu**: Xác minh tên đăng nhập + họ tên cho tài khoản Nhân viên và đặt lại mật khẩu mới (>= 8 ký tự).
6. **Mã hóa mật khẩu PBKDF2-HMAC-SHA256**: Không lưu mật khẩu văn bản thuần (Plain Text) trong Database.
7. **Bảo mật cấu hình**: Chuỗi kết nối CSDL và khóa bí mật Token tách ra file `appsettings.json` (kèm `appsettings.example.json` và `.gitignore`).
8. **CRUD Sản phẩm, Danh mục, Nhật ký kho, Tìm kiếm, Lọc, Sắp xếp, Phân trang (Paging) & Thống kê động + Xuất Excel CSV**.

## 3. Hướng dẫn cài đặt và chạy dự án

### Yêu cầu môi trường
- .NET SDK 10.0 (kiểm tra bằng lệnh `dotnet --version`).

### Các bước khởi chạy
1. Mở Terminal / PowerShell tại thư mục chứa file `DukeShopBackend.csproj`.
2. Nếu từng chạy phiên bản cũ trước đó, xóa file `dukeshop.db` cũ để EF Core tự tạo lại CSDL 4 bảng mới.
3. Khởi chạy ứng dụng:
   ```bash
   dotnet run
   ```
4. Mở trình duyệt truy cập địa chỉ hiển thị trên Terminal ( là `http://localhost:5000`) và nhấn `Ctrl + F5`.

## 4. Danh sách RESTful API chính

- `POST /api/auth/login` — Đăng nhập & cấp Bearer Token
- `POST /api/auth/register` — Đăng ký tài khoản Nhân viên mới
- `POST /api/auth/logout` — Đăng xuất & thu hồi Token
- `POST /api/auth/forgot-password` — Đặt lại mật khẩu cho Nhân viên
- `GET /api/auth/profile` & `PUT /api/auth/profile` — Xem & cập nhật hồ sơ cá nhân
- `PUT /api/auth/change-password` — Đổi mật khẩu đang đăng nhập
- `GET / POST / PUT / DELETE /api/products` — Quản lý sản phẩm (CRUD)
- `POST /api/products/reset` — Khôi phục 8 sản phẩm mẫu (Chỉ Quản lý)
- `GET / POST / DELETE /api/categories` — Quản lý danh mục & thống kê theo danh mục
- `GET /api/inventorylogs` & `POST /api/inventorylogs/adjust` — Xem nhật ký kho & lập phiếu Nhập/Xuất kho
