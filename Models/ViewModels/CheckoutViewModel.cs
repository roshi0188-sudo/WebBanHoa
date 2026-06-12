using System.Collections.Generic;

namespace WebBanHoa.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal TotalPrice { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalPayment { get; set; }
    }
}