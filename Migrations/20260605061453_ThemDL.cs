using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHoa.Migrations
{
    /// <inheritdoc />
    public partial class ThemDL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- DỮ LIỆU CỬA HÀNG HOA ---
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Hoa Khai Trương" },
                    { 2, "Hoa Sinh Nhật" },
                    { 3, "Hoa Tình Nhân" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Name", "Price", "Description", "ImageUrl", "CategoryId" },
                values: new object[,]
                {
                    { 1, "Bó Hoa Hồng Nhạt Pastel", 450000m, "Sự kết hợp tinh tế giữa hoa hồng hồng và lá phụ nhập khẩu mang phong cách nhẹ nhàng.", "/images/hoa1.jpg", 3 },
                    { 2, "Hoa Khai Trương Hồng Phát", 1250000m, "Kệ hoa sang trọng với tông màu đỏ, vàng chủ đạo thay cho lời chúc làm ăn phát đạt.", "/images/hoa2.jpg", 1 },
                    { 3, "Giỏ Hoa Hướng Dương Rực Rỡ", 350000m, "Giỏ hoa hướng dương phối cùng thanh liễu mang lại năng lượng tích cực ngày sinh nhật.", "/images/hoa3.jpg", 2 }
                });

            // --- THÊM DỮ LIỆU ĐĂNG NHẬP  ---

            string adminRoleId = "role-admin-id-001";
            string userRoleId = "role-user-id-002";

            string adminUserId = "user-admin-id-100";
            string customerUserId = "user-customer-id-200";

            // 1. Chèn Quyền vào bảng AspNetRoles
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { adminRoleId, "Admin", "ADMIN", null },
                    { userRoleId, "User", "USER", null }
                });

            // Tạo bộ mã hóa mật khẩu chuẩn theo thực thể ApplicationUser mới
            var passwordHasher = new PasswordHasher<WebBanHoa.Models.ApplicationUser>();

            // Khởi tạo thông tin tĩnh cho tài khoản Admin để băm mật khẩu
            var adminUser = new WebBanHoa.Models.ApplicationUser
            {
                Id = adminUserId,
                UserName = "admin@floral.com",
                NormalizedUserName = "ADMIN@FLORAL.COM",
                Email = "admin@floral.com",
                NormalizedEmail = "ADMIN@FLORAL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@123");

            // Khởi tạo thông tin tĩnh cho tài khoản Khách hàng để băm mật khẩu
            var customerUser = new WebBanHoa.Models.ApplicationUser
            {
                Id = customerUserId,
                UserName = "customer@floral.com",
                NormalizedUserName = "CUSTOMER@FLORAL.COM",
                Email = "customer@floral.com",
                NormalizedEmail = "CUSTOMER@FLORAL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            customerUser.PasswordHash = passwordHasher.HashPassword(customerUser, "User@123");

            // 2. Chèn Tài khoản vào bảng AspNetUsers 
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[]
                {
                    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
                    "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
                    "FullName", "Address", "Avatar", "JoinDate"
                },
                values: new object[,]
                {
                    {
                        adminUser.Id, adminUser.UserName, adminUser.NormalizedUserName, adminUser.Email, adminUser.NormalizedEmail,
                        adminUser.EmailConfirmed, adminUser.PasswordHash, adminUser.SecurityStamp, adminUser.ConcurrencyStamp,
                        false, false, true, 0,
                        "Võ Dương Hồng Lam", "Thành phố Hồ Chí Minh", "/images/avatar-admin.jpg", DateTime.Now
                    },
                    {
                        customerUser.Id, customerUser.UserName, customerUser.NormalizedUserName, customerUser.Email, customerUser.NormalizedEmail,
                        customerUser.EmailConfirmed, customerUser.PasswordHash, customerUser.SecurityStamp, customerUser.ConcurrencyStamp,
                        false, false, true, 0,
                        "Bella Miller", "123 Đường ", "/images/avatar-user1.jpg", DateTime.Now
                    }
                });

            // 3. Gán quyền cho tài khoản trong bảng trung gian AspNetUserRoles
            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" },
                values: new object[,]
                {
                    { adminUserId, adminRoleId },     // Tài khoản admin nhận quyền Admin
                    { customerUserId, userRoleId }    // Tài khoản customer nhận quyền User
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
