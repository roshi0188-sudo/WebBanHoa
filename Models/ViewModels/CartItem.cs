using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHoa.Models.ViewModels
{
    [NotMapped]
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Price * Quantity;
        public string? CardImage { get; set; }
        public string? CardContent { get; set; }
    }
}