using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHoa.Migrations
{
    /// <inheritdoc />
    public partial class ThemData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Chèn dữ liệu vào bảng Categories trước (để tránh lỗi khóa ngoại)
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Hoa Khai Trương" },
                    { 2, "Hoa Sinh Nhật" },
                    { 3, "Hoa Tình Yêu" }
                });

            // 2. Chèn dữ liệu vào bảng Products
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Name", "Price", "Description", "ImageUrl", "CategoryId" },
                values: new object[,]
                {
                    {
                        1,
                        "Bó Hoa Hồng Nhạt Pastel",
                        450000m, // Thêm chữ m nếu cột Price trong DB là kiểu decimal
                        "Sự kết hợp tinh tế giữa hoa hồng hồng và lá phụ nhập khẩu mang phong cách nhẹ nhàng.",
                        "/images/hoa1.jpg",
                        3
                    },
                    {
                        2,
                        "Kệ Hoa Khai Trương Hồng Phát",
                        1250000m,
                        "Kệ hoa sang trọng với tông màu đỏ, vàng chủ đạo thay cho lời chúc làm ăn phát đạt.",
                        "/images/hoa2.jpg",
                        1
                    },
                    {
                        3,
                        "Giỏ Hoa Hướng Dương Rực Rỡ",
                        350000m,
                        "Giỏ hoa hướng dương phối cùng thanh liễu mang lại năng lượng tích cực ngày sinh nhật.",
                        "/images/hoa3.jpg",
                        2
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
