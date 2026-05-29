using System.ComponentModel.DataAnnotations;

namespace WebBanHoa.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = null!;


        public virtual ICollection<Product>? Products { get; set; }
    }
}
