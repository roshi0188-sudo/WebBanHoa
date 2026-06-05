using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHoa.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebBanHoa.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Tính tổng doanh thu các đơn hàng đã hoàn thành
            var revenue = await _context.Orders
                .Where(o => o.OrderStatus == "Đã hoàn thành")
                .SumAsync(o => o.TotalAmount);
            ViewBag.TotalRevenue = revenue.ToString("#,##0");

            // Đếm số đơn chờ duyệt
            ViewBag.PendingOrders = await _context.Orders
                .CountAsync(o => o.OrderStatus == "Chờ xử lý");

            // Đếm số mẫu hoa trong kho
            ViewBag.TotalProducts = await _context.Products.CountAsync();

            // Đếm số lượng khách hàng đăng ký tài khoản
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            return View();
        }
    }
}