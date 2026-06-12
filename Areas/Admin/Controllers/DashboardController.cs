using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebBanHoa.Models;

namespace WebBanHoa.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 📊 1. GIAO DIỆN BIỂU ĐỒ TỔNG QUAN
        public async Task<IActionResult> Index()
        {
            var revenue = await _context.Orders
                .Where(o => o.OrderStatus == "Đã hoàn thành")
                .SumAsync(o => o.TotalAmount);
            ViewBag.TotalRevenue = revenue.ToString("#,##0");

            ViewBag.PendingOrders = await _context.Orders
                .CountAsync(o => o.OrderStatus == "Chờ xử lý");

            ViewBag.TotalProducts = await _context.Products.CountAsync();

            ViewBag.TotalUsers = await _userManager.Users.CountAsync();

            return View();
        }

        // 👥 2. THÊM MỚI HÀM NÀY: GIAO DIỆN DANH SÁCH NGƯỜI DÙNG (/Admin/Dashboard/UserList)
        public async Task<IActionResult> UserList()
        {
            // Lấy danh sách toàn bộ người dùng từ Identity chuyển ra ngoài giao diện
            var users = await _userManager.Users.ToListAsync();
            return View(users); // Sẽ tìm file Areas/Admin/Views/Dashboard/UserList.cshtml
        }

        // 🔒 3. HÀM XỬ LÝ KHÓA TÀI KHOẢN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccount(string userId, int days = 30)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userId)
            {
                TempData["DangerMessage"] = "Bạn không thể tự khóa tài khoản Admin của chính mình!";
                return RedirectToAction(nameof(UserList));
            }

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(days));

            TempData["SuccessMessage"] = $"Đã khóa tài khoản {user.Email} thành công trong {days} ngày. 🔒";

            // 🟢 ĐÃ SỬA: Điều hướng quay trở lại đúng trang danh sách UserList của Dashboard
            return RedirectToAction(nameof(UserList));
        }

        // 🔓 4. HÀM XỬ LÝ MỞ KHÓA TÀI KHOẢN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);

            TempData["SuccessMessage"] = $"Đã mở khóa tài khoản {user.Email} thành công. 🔓";

            // 🟢 ĐÃ SỬA: Điều hướng quay trở lại đúng trang danh sách UserList của Dashboard
            return RedirectToAction(nameof(UserList));
        }
    }
}