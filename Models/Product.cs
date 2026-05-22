using System.ComponentModel.DataAnnotations;

namespace WebBanHoa.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(100)]
        public string Name { get; set; }

        [Range(1000, 100000000, ErrorMessage = "Giá sản phẩm phải từ 1,000đ trở lên")]
        public decimal Price { get; set; }

        public decimal? OldPrice { get; set; } // Giá cũ để làm gạch ngang giảm giá

        public string? Description { get; set; }

        public string? Ingredients { get; set; } // Thành phần bó hoa 

        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }

        public List<string>? ImageUrls { get; set; }
    }
}
