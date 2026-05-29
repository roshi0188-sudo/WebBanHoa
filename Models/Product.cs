using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHoa.Models
{
    public class Product
    {
        [Key] // Khóa chính tự động tăng
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm hoa không được để trống")]
        [StringLength(150, ErrorMessage = "Tên sản phẩm không được vượt quá 150 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Giá bán không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả bó hoa")]
        public string? Description { get; set; }

        // Khóa ngoại liên kết tới bảng Category
        [Required(ErrorMessage = "Vui lòng chọn danh mục cho sản phẩm")]
        [Display(Name = "Mã danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // Thuộc tính phụ phục vụ lưu trữ nhiều ảnh phụ (nếu có dùng ở controller cũ)
        [NotMapped] // Báo cho EF Core biết trường này không cần tạo cột trong SQL Server
        public List<string>? ImageUrls { get; set; }= new List<string>();
    }
}
