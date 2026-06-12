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
            // Thêm ký tự BOM để khi mở bằng Excel không bị lỗi font Tiếng Việt
            var resultBytes = Encoding.UTF8.GetPreamble().Concat(fileBytes).ToArray();

            return File(resultBytes, "text/csv", $"Bao_Cao_Don_Hang_Floral_LAM_{DateTime.Now:yyyyMMdd}.csv");
        }

        //  MÀN HÌNH DASHBOARD DOANH THU 
        public async Task<IActionResult> RevenueDashboard()
        {
            var orders = await _context.Orders.Include(o => o.OrderDetails).ToListAsync();

            // Tính toán 4 chỉ số hiệu suất kinh doanh chính (KPI)
            decimal totalRevenue = orders.Where(o => o.OrderStatus != "Đã hủy").Sum(o => o.TotalAmount);
            int totalOrders = orders.Count;
            decimal aov = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // Giả lập Tỷ lệ chuyển đổi hệ thống dựa trên lưu lượng truy cập thực tế (Ví dụ mẫu: 4000 traffic)
            double conversionRate = totalOrders > 0 ? Math.Round(((double)totalOrders / 4000) * 100, 2) : 0;

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.AOV = aov;
            ViewBag.ConversionRate = conversionRate;

            // Xử lý bảng xếp hạng sản phẩm bán chạy nhất (Top-selling products)
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
                .Take(5) // Lấy ra Top 5 sản phẩm cao nhất
                .ToListAsync();

            ViewBag.TopProducts = topProducts;

            // Phân nhóm dữ liệu tổng doanh thu theo tháng phục vụ thư viện biểu đồ Chart.js
            var monthlyData = orders
                .Where(o => o.OrderStatus != "Đã hủy")
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
                Amount = orders
                    .Where(o => o.OrderStatus != "Đã hủy" && o.OrderDate.Date == date.Date)
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
            // Nạp đơn hàng kèm thông tin User liên kết
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            order.OrderStatus = "Đã hoàn thành";
            _context.Orders.Update(order);

            // TÍNH XU TÍCH LŨY
            if (order.User != null)
            {
                // Công thức mẫu: Cứ 10,000 đ tổng hóa đơn sẽ quy đổi được 1 điểm thưởng
                int pointsEarned = (int)(order.TotalAmount / 10000);

                // Cộng dồn vào quỹ điểm hiện tại của User
                order.User.RewardPoints += pointsEarned;

                _context.Users.Update(order.User);

                TempData["SuccessMessage"] = $"Đơn hàng #LAM-{orderId} đã hoàn thành! Khách hàng được tích lũy thêm +{pointsEarned} Pts.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đơn hàng #LAM-{orderId} đã hoàn thành!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // HỦY ĐƠN HÀNG
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