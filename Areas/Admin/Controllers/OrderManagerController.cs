using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebBanHoa.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebBanHoa.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] 
    public class OrderManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var allOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderDate) 
                .ToListAsync();

            return View(allOrders);
        }

        // DUYỆT ĐƠN (Chuyển trạng thái sang "Đang giao" hoặc "Đã duyệt")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // Cập nhật trạng thái đơn hoa
            order.OrderStatus = "Đang giao";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã duyệt thành công đơn hàng #LAM-{orderId}!";
            return RedirectToAction("Index");
        }

        // HOÀN THÀNH ĐƠN (Chuyển trạng thái sang "Đã hoàn thành")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.OrderStatus = "Đã hoàn thành";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đơn hàng #LAM-{orderId} đã hoàn thành!";
            return RedirectToAction("Index");
        }

        //HỦY ĐƠN HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.OrderStatus = "Đã hủy";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["DangerMessage"] = $"Đã hủy đơn hàng #LAM-{orderId}.";
            return RedirectToAction("Index");
        }
    }
}