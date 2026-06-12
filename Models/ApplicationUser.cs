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

        public int RewardPoints { get; set; }

        // Thuộc tính động tự quy đổi điểm sang Hạng hiển thị ngoài giao diện
        public string MemberRank => RewardPoints switch
        {
            >= 5000 => "Thành viên Kim Cương 💎",
            >= 2000 => "Thành viên Vàng 🥇",
            >= 500 => "Thành viên Bạc 🥈",
            _ => "Thành viên Đồng 🥉"
        };
    }
}