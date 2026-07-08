# MemoLens - Database Design

## 1. Nguyên tắc thiết kế cơ sở dữ liệu

Cơ sở dữ liệu ban đầu của MemoLens tập trung vào người dùng, kỷ niệm, ảnh, thẻ và album.

Quan trọng: ảnh không được lưu trực tiếp trong cơ sở dữ liệu. File ảnh sẽ được lưu trong thư mục uploads của ứng dụng, ví dụ `wwwroot/uploads/memories`. Cơ sở dữ liệu chỉ lưu đường dẫn ảnh để ứng dụng có thể hiển thị lại.

Kiểu dữ liệu dưới đây được đề xuất cho SQL Server và có thể điều chỉnh nhẹ khi triển khai với ASP.NET MVC.

## 2. Bảng Users

### Mục đích

Lưu thông tin tài khoản người dùng. Mỗi người dùng có bộ kỷ niệm riêng.

### Fields

| Field | Data Type | Key | Explanation |
| --- | --- | --- | --- |
| Id | int | Primary Key | Mã định danh người dùng |
| FullName | nvarchar(100) |  | Tên hiển thị của người dùng |
| Email | nvarchar(255) | Unique | Email đăng nhập |
| PasswordHash | nvarchar(255) |  | Mật khẩu đã được hash, không lưu mật khẩu thô |
| CreatedAt | datetime2 |  | Thời điểm tạo tài khoản |
| UpdatedAt | datetime2 |  | Thời điểm cập nhật tài khoản gần nhất |

### Relationships

- Một user có nhiều memories.
- Một user có nhiều albums.

## 3. Bảng Memories

### Mục đích

Lưu thông tin chính của một kỷ niệm.

### Fields

| Field | Data Type | Key | Explanation |
| --- | --- | --- | --- |
| Id | int | Primary Key | Mã định danh kỷ niệm |
| UserId | int | Foreign Key -> Users.Id | Người sở hữu kỷ niệm |
| Title | nvarchar(150) |  | Tiêu đề kỷ niệm |
| Story | nvarchar(max) |  | Ghi chú hoặc câu chuyện |
| Mood | nvarchar(50) |  | Tâm trạng, ví dụ: vui, bình yên, nhớ, buồn |
| MemoryDate | date |  | Ngày xảy ra kỷ niệm |
| Location | nvarchar(200) |  | Địa điểm của kỷ niệm |
| IsPrivate | bit |  | Mặc định là true để kỷ niệm riêng tư |
| CreatedAt | datetime2 |  | Thời điểm tạo |
| UpdatedAt | datetime2 |  | Thời điểm cập nhật gần nhất |

### Relationships

- Một memory thuộc về một user.
- Một memory có nhiều memory images.
- Một memory có thể có nhiều tags.
- Một memory có thể nằm trong nhiều albums.

## 4. Bảng MemoryImages

### Mục đích

Lưu đường dẫn các ảnh thuộc về một kỷ niệm.

### Fields

| Field | Data Type | Key | Explanation |
| --- | --- | --- | --- |
| Id | int | Primary Key | Mã định danh ảnh |
| MemoryId | int | Foreign Key -> Memories.Id | Kỷ niệm chứa ảnh |
| ImagePath | nvarchar(500) |  | Đường dẫn file ảnh trong thư mục uploads |
| Caption | nvarchar(255) |  | Chú thích ngắn cho ảnh, có thể để trống |
| SortOrder | int |  | Thứ tự hiển thị ảnh |
| UploadedAt | datetime2 |  | Thời điểm upload ảnh |

### Relationships

- Một memory image thuộc về một memory.

### Lưu ý quan trọng

Không lưu file ảnh trực tiếp trong bảng này. Chỉ lưu đường dẫn như `/uploads/memories/user-1/photo-001.jpg`.

## 5. Bảng Tags

### Mục đích

Lưu danh sách thẻ để người dùng phân loại và tìm kiếm kỷ niệm.

### Fields

| Field | Data Type | Key | Explanation |
| --- | --- | --- | --- |
| Id | int | Primary Key | Mã định danh tag |
| UserId | int | Foreign Key -> Users.Id | Người tạo tag |
| Name | nvarchar(50) |  | Tên tag, ví dụ: gia đình, du lịch, sinh nhật |
| CreatedAt | datetime2 |  | Thời điểm tạo tag |

### Relationships

- Một tag thuộc về một user.
- Một tag có thể được gắn vào nhiều memories.

## 6. Bảng MemoryTags

### Mục đích

Lưu quan hệ nhiều-nhiều giữa memories và tags.

### Fields

| Field | Data Type | Key | Explanation |
| --- | --- | --- | --- |
| MemoryId | int | Primary Key, Foreign Key -> Memories.Id | Kỷ niệm được gắn tag |
| TagId | int | Primary Key, Foreign Key -> Tags.Id | Tag được gắn vào kỷ niệm |

### Relationships

- Một memory có thể có nhiều tags.
- Một tag có thể thuộc về nhiều memories.

## 7. Bảng Albums

### Mục đích

Lưu album cá nhân để gom nhiều kỷ niệm theo chủ đề.

### Fields

| Field | Data Type | Key | Explanation |
| --- | --- | --- | --- |
| Id | int | Primary Key | Mã định danh album |
| UserId | int | Foreign Key -> Users.Id | Người sở hữu album |
| Name | nvarchar(100) |  | Tên album |
| Description | nvarchar(500) |  | Mô tả ngắn về album |
| CoverImagePath | nvarchar(500) |  | Đường dẫn ảnh bìa album, có thể để trống |
| CreatedAt | datetime2 |  | Thời điểm tạo album |
| UpdatedAt | datetime2 |  | Thời điểm cập nhật album |

### Relationships

- Một album thuộc về một user.
- Một album có thể chứa nhiều memories.

## 8. Bảng AlbumMemories

### Mục đích

Lưu quan hệ nhiều-nhiều giữa albums và memories.

### Fields

| Field | Data Type | Key | Explanation |
| --- | --- | --- | --- |
| AlbumId | int | Primary Key, Foreign Key -> Albums.Id | Album chứa kỷ niệm |
| MemoryId | int | Primary Key, Foreign Key -> Memories.Id | Kỷ niệm nằm trong album |
| AddedAt | datetime2 |  | Thời điểm thêm kỷ niệm vào album |

### Relationships

- Một album có thể chứa nhiều memories.
- Một memory có thể nằm trong nhiều albums.

## 9. Gợi ý ràng buộc dữ liệu

- Email trong Users nên là duy nhất.
- Memory.UserId là bắt buộc.
- MemoryImage.MemoryId là bắt buộc.
- Tags nên thuộc về từng user để tránh lẫn dữ liệu giữa các tài khoản.
- Khi truy vấn dữ liệu, luôn lọc theo UserId để bảo vệ quyền riêng tư.
- Có thể dùng soft delete trong tương lai, nhưng MVP có thể dùng xóa trực tiếp để đơn giản.

