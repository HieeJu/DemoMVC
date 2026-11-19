# DemoMVC

## Cấu hình môi trường

Ứng dụng yêu cầu .NET 10 và SQL Server. Connection string, thông tin khởi tạo Admin và SMTP không được lưu trong `appsettings.json`; dùng User Secrets khi phát triển hoặc biến môi trường khi triển khai:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project DemoMVC.csproj
dotnet user-secrets set "Identity:AdminEmail" "admin@example.com" --project DemoMVC.csproj
dotnet user-secrets set "Identity:AdminPassword" "<strong-password>" --project DemoMVC.csproj
```

Admin được tạo idempotent ở lần chạy đầu khi cả email và mật khẩu seed đã cấu hình; mật khẩu không được tự sinh hoặc ghi log. Nếu thiếu cấu hình, ứng dụng ghi lỗi cấu hình và bỏ qua bước tạo Admin. Mật khẩu hiện hữu không bị reset khi ứng dụng chạy lại.

SMTP có thể cấu hình qua User Secrets hoặc biến môi trường (`Email__Host`, `Email__Port`, `Email__EnableSsl`, `Email__UserName`, `Email__Password`, `Email__FromAddress`, `Email__FromName`). Khi thiếu host/from address hoặc gửi SMTP thất bại, ứng dụng ghi log và không báo gửi thành công.

Ứng dụng tự áp dụng các EF Core migration khi khởi động. Hãy sao lưu cơ sở dữ liệu trước khi triển khai lên môi trường có dữ liệu.

## Quyền truy cập

Role hệ thống `Admin` luôn có toàn quyền. Các Role khác chỉ truy cập Controller/Action được bật trong trang **Phân quyền**; action chưa được cấp quyền bị từ chối. Các luồng đăng nhập, đăng ký, xác thực email, đặt lại mật khẩu, trang Access Denied và đăng xuất được miễn kiểm tra ma trận theo đúng trạng thái xác thực của từng luồng.

## Nhập Excel

Trang **Nhập Excel** hỗ trợ bảng Student và Faculty (không hỗ trợ bảng Identity), nhận `.xlsx`/`.xls`, đọc sheet đầu tiên và yêu cầu tiêu đề cột theo tên thuộc tính:

- Student: `StudentId`, `FullName`, `Address`, `Email`, `FacultyId`
- Faculty: `FacultyId`, `FacultyName`, `MajorCode`

Preview kiểm tra cấu trúc, validation, khóa trùng trong file và quan hệ Faculty của Student. Chỉ sau khi xem preview người dùng mới chọn **Append** (chỉ thêm; khóa trùng bị báo lỗi) hoặc **Overwrite** (thay toàn bộ entity được chọn trong transaction; hủy nếu dữ liệu không hợp lệ). Faculty không thể Overwrite khi còn Student tham chiếu.

## Kiểm tra

```powershell
dotnet build DemoMVC.csproj
dotnet test DemoMVC.csproj
```
