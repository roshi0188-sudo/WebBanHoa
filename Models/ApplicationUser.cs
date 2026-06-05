using Microsoft.AspNetCore.Identity;
using System;

namespace WebBanHoa.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Thêm các thuộc tính tùy chỉnh của bạn ở đây
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.Now;
    }
}