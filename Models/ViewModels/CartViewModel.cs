using System.Collections.Generic;
using WebBanHoa.Models; 

namespace WebBanHoa.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal TotalCartPrice { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPayment { get; set; }

        // Danh sách chứa sản phẩm gợi ý bán chéo (Cross-selling)
        public List<Product> SuggestedProducts { get; set; } = new List<Product>();
    }
}