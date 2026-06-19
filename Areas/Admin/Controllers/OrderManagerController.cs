using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        //MÀN HÌNH QUẢN LÝ ĐƠN HÀNG 
        public async Task<IActionResult> Index(string statusFilter)
        {
            // Nạp thêm thông tin User để lấy được tên hiển thị của tài khoản
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            // Hệ thống bộ lọc thông minh theo yêu cầu của module
            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(o => o.OrderStatus == statusFilter);
            }

            var allOrders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            // Giữ lại trạng thái lọc để thắp sáng active tab ngoài giao diện View
            ViewBag.CurrentFilter = statusFilter;

            return View(allOrders);
        }

        //  XUẤT DỮ LIỆU ĐƠN HÀNG HÀNG LOẠT (EXPORT CSV)
        public async Task<IActionResult> ExportToCsv()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var csv = new StringBuilder();
            // Thiết lập tiêu đề cột cho file xuất dữ liệu
            csv.AppendLine("Mã đơn,Khách hàng,Số điện thoại,Ngày đặt,Tổng tiền,Trạng thái");

            foreach (var order in orders)
            {
                csv.AppendLine($"#LAM-{order.Id},{order.User?.FullName},{order.PhoneNumber},{order.OrderDate:dd/MM/yyyy HH:mm},{order.TotalAmount},{order.OrderStatus}");
            }

            var fileBytes = Encoding.UTF8.GetBytes(csv.ToString());
            
            var resultBytes = Encoding.UTF8.GetPreamble().Concat(fileBytes).ToArray();

            return File(resultBytes, "text/csv", $"Bao_Cao_Don_Hang_Floral_LAM_{DateTime.Now:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> RevenueDashboard()
        {
            var ordersQuery = _context.Orders.AsQueryable();

            // 1. Tính doanh thu thực tế (Chỉ lấy đơn thành công)
            decimal totalRevenue = await ordersQuery
                .Where(o => o.OrderStatus == "Đã hoàn thành" || o.OrderStatus == "Đã giao")
                .SumAsync(o => o.TotalAmount);

            // 2. Tổng số đơn hàng thành công thực tế trong hệ thống
            int totalOrders = await ordersQuery
                .Where(o => o.OrderStatus == "Đã hoàn thành" || o.OrderStatus == "Đã giao")
                .CountAsync();

            // 3. Giá trị trung bình trên một đơn hàng (AOV)
            decimal aov = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            int totalTraffic = await _context.VisitorLogs.CountAsync();

            // Công thức tính thực tế: (Tổng đơn hàng / Tổng lượt truy cập) * 100
            double conversionRate = totalTraffic > 0
                ? Math.Round(((double)totalOrders / totalTraffic) * 100, 2)
                : 0;

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.AOV = aov;
            ViewBag.ConversionRate = conversionRate;
            ViewBag.TotalTraffic = totalTraffic; 

            var topProducts = await _context.OrderDetails
                .Include(d => d.Product)
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    ProductName = g.First().Product.Name,
                    ImageUrl = g.First().Product.ImageUrl,
                    TotalQuantity = g.Sum(d => d.Quantity),
                    TotalSales = g.Sum(d => d.Price * d.Quantity)
                })
                .OrderByDescending(p => p.TotalQuantity)
                .Take(5)
                .ToListAsync();

            ViewBag.TopProducts = topProducts;

            var ordersList = await ordersQuery.ToListAsync();

            var monthlyData = ordersList
                .Where(o => o.OrderStatus == "Đã hoàn thành" || o.OrderStatus == "Đã giao")
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new { Month = g.Key, Amount = g.Sum(o => o.TotalAmount) })
                .OrderBy(g => g.Month)
                .ToList();

            ViewBag.ChartLabels = monthlyData.Select(m => $"Tháng {m.Month}").ToArray();
            ViewBag.ChartData = monthlyData.Select(m => m.Amount).ToArray();

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .Reverse()
                .ToList();

            var weeklyData = last7Days.Select(date => new
            {
                Date = date.ToString("dd/MM"),
                Amount = ordersList
                    .Where(o => (o.OrderStatus == "Đã hoàn thành" || o.OrderStatus == "Đã giao") && o.OrderDate.Date == date.Date)
                    .Sum(o => o.TotalAmount)
            }).ToList();

            ViewBag.WeekLabels = weeklyData.Select(d => d.Date).ToArray();
            ViewBag.WeekData = weeklyData.Select(d => d.Amount).ToArray();

            return View();
        }

        // DUYỆT ĐƠN 
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

        // HOÀN THÀNH ĐƠN 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.OrderStatus = "Đã hoàn thành";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đơn hàng #LAM-{orderId} đã hoàn thành xuất sắc! ✅";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.OrderStatus = "Đã hủy";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["DangerMessage"] = $"Đã hủy đơn hàng #LAM-{orderId}! ❌";

            return RedirectToAction("Index");
        }
    }
}