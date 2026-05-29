using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBanHoa.Models
{
    public class ProductImage
    {
        [Key] // Khóa chính tự động tăng trong SQL Server
        public int Id { get; set; }

        [Required]
        [Display(Name = "Đường dẫn ảnh phụ")]
        public string Url { get; set; } = null!;// Lưu đường dẫn tương đối (Ví dụ: /images/hoa_phu_1.jpg)

        // Khóa ngoại liên kết tới bảng Product
        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
