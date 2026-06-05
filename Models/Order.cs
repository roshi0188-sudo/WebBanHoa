using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBanHoa.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = "Chờ xử lý"; // Chờ xử lý, Đã duyệt, Đang giao, Hoàn thành

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string PhoneNumber { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }
    }
}