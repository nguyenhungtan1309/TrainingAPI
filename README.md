# HỆ THỐNG QUẢN LÝ NHÂN VIÊN (EMPLOYEE MANAGEMENT SYSTEM API)

Hệ thống RESTful Web API quản lý nhân viên được xây dựng trên nền tảng ASP.NET Core, tích hợp truy vấn OData, Real-time SignalR, Caching Redis, Logging Log4Net và triển khai trên máy chủ IIS.

---

## 1. Yêu cầu môi trường & Công cụ (Prerequisites)

* **Hệ điều hành:** Windows 10/11 hoặc Windows Server.
* **SDK:** .NET 8.0 SDK (hoặc phiên bản tương thích).
* **Cơ sở dữ liệu:** Microsoft SQL Server (hoặc SQL Server Express / Developer).
* **Bộ nhớ đệm (Cache):** Redis (chạy qua Docker Container hoặc Linux/WSL2).
* **Web Server:** Internet Information Services (IIS) đã bật tính năng và cài đặt **ASP.NET Core Hosting Bundle**.

---

## 2. Chuẩn bị Hạ tầng (Database & Redis)

### 2.1. Cấu hình Cơ sở dữ liệu SQL Server
1. Mở **SQL Server Management Studio (SSMS)** và kết nối tới server cục bộ.
2. Tạo cơ sở dữ liệu `TrainingDB` và các bảng dữ liệu bằng cách chạy script:
```sql
CREATE DATABASE TrainingDB;
GO
USE TrainingDB;
GO

CREATE TABLE Department (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE Employee (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    position NVARCHAR(50),
    salary DECIMAL(18, 2) NOT NULL,
    DepartmentId INT NULL,
    CONSTRAINT FK_Employee_Department FOREIGN KEY (DepartmentId) REFERENCES Department(Id)
);
```
### 2.2. Khởi chạy Redis Server
Khởi chạy container Redis thông qua Docker:
```Bash
docker run --name my-redis -d -p 6379:6379 redis
```

---

## 3. Đóng gói mã nguồn (Build & Publish)

Mở Terminal tại thư mục chứa file dự án TrainingAPI.csproj.

Thực thi lệnh biên dịch mã nguồn sang chế độ Release:
```PowerShell
dotnet publish -c Release -o "C:\Deploy\TrainingAPI_Release"
```
Sau khi hoàn tất, kiểm tra thư mục C:\Deploy\TrainingAPI_Release phải có đầy đủ các tệp:
* TrainingAPI.dll, TrainingAPI.exe
* web.config (cấu hình môi trường IIS)
* appsettings.json (cấu hình kết nối)
* log4net.config (cấu hình ghi log)

---

## 4. Cấu hình Triển khai trên IIS (Deploy to IIS)

### 4.1. Tạo Application Pool
1. Mở công cụ IIS Manager.
2. Chuột phải vào Application Pools > chọn Add Application Pool...
3. Thiết lập thông số:
  * Name: TrainingAPIPool
  * .NET CLR Version: Chọn No Managed Code (bắt buộc đối với ASP.NET Core)
  * Managed pipeline mode: Integrated
4. Nhấn OK.

### 4.2 Tạo Website
1. Tại IIS Manager, chuột phải vào Sites > chọn Add Website...
2. Cấu hình thông số:
   * Site name: TrainingAPI_Site
   * Application pool: Chọn TrainingAPIPool
   * Physical path: Trỏ đến thư mục C:\Deploy\TrainingAPI_Release
   * Binding Port: 8080 (hoặc port khác chưa bị chiếm dụng)
3. Nhấn OK.

### 4.3 Phân quyền thư mục (Permissions)
Ứng dụng cần quyền ghi file cho Log4Net:
1. Mở File Explorer, tìm đến thư mục C:\Deploy\TrainingAPI_Release.
2. Chuột phải > chọn Properties > tab Security > bấm Edit...
3. Bấm Add..., nhập tên IIS_IUSRS và nhấn Check Names > OK.
4. Tích chọn quyền Modify và Write > nhấn Apply > OK.

### 4.4 Cấu hình Chuỗi kết nối Database (appsettings.json)
Mở file C:\Deploy\TrainingAPI_Release\appsettings.json bằng trình soạn thảo và cập nhật chuỗi kết nối:
```JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TrainingDB;User Id=sa;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 5. Kiểm tra hoạt động hệ thống (Verification)

Sau khi khởi động Web Site trên IIS, kiểm tra các dịch vụ:
1. REST API & OData:
   * Truy cập: http://localhost:8080/api/employees
   * Kiểm tra truy vấn OData: http://localhost:8080/api/employees?$filter=salary gt 10000000&$orderby=name
2. Redis Caching:
   * Gửi request GET lần 1 (dữ liệu truy vấn từ SQL Server và lưu vào cache 10 phút).
   * Gửi request GET lần 2 (thời gian phản hồi giảm dưới 10ms, dữ liệu trả về từ Redis).
   * Thực hiện POST/PUT/DELETE: Cache tự động xóa và dữ liệu mới được đồng bộ.
3. Real-time SignalR:
   * Mở file client chat.html và trỏ endpoint kết nối đến http://localhost:8080/chathub.
   * Gửi API thêm/sửa/xóa nhân viên, thông báo tự động đẩy xuống trình duyệt ngay lập tức.
4. Hệ thống Ghi log:
   * Kiểm tra thư mục C:\Deploy\TrainingAPI_Release\Logs để đảm bảo file log theo ngày api-log-yyyyMMdd.txt được tạo và ghi nhận đầy đủ lịch sử request.
