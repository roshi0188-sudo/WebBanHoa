using System.ComponentModel.DataAnnotations;

namespace WebBanHoa.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(50)]
        public string Name { get; set; }
    }
}
