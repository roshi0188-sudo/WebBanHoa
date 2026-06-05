using Microsoft.AspNetCore.Authorization; // 🔴 Bắt buộc phải có dòng này trên đầu
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WebBanHoa.Models;
using WebBanHoa.Models.ViewModels;
using WebBanHoa.Repositories;

namespace WebBanHoa.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CART_SESSION_KEY = "BoutiqueCart";

        public CartController(IProductRepository productRepository, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        private List<CartItem> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString(CART_SESSION_KEY);
            return sessionData == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(sessionData);
        }

        private void SaveCartItems(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CART_SESSION_KEY, JsonSerializer.Serialize(cart));
        }

        //  TRANG HIỂN THỊ GIỎ HÀNG  
        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        // THÊM HOA VÀO GIỎ HÀNG 

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var cartItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (cartItem == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity
                });
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            SaveCartItems(cart);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }
            return RedirectToAction("Index");
        }

        //XÓA HOA KHỎI GIỎ 
        [Authorize]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartItems(cart);
            }
            return RedirectToAction("Index");
        }

        // TRANG ĐIỀN THÔNG TIN ĐẶT HOA 
        [Authorize]
        public async Task<IActionResult> Checkout(string selectedProducts)
        {
            var cart = GetCartItems();
            if (cart.Count == 0) return RedirectToAction("Index");

            if (!string.IsNullOrEmpty(selectedProducts))
            {
                var selectedIds = selectedProducts.Split(',').Select(int.Parse).ToList();
                cart = cart.Where(x => selectedIds.Contains(x.ProductId)).ToList();
            }

            if (cart.Count == 0) return RedirectToAction("Index");

            var currentUser = await _userManager.GetUserAsync(User);

            var order = new Order
            {
                TotalAmount = cart.Sum(x => x.TotalPrice),
                PhoneNumber = currentUser?.PhoneNumber ?? "",
                ShippingAddress = currentUser?.Address ?? ""
            };

            ViewBag.CartItems = cart;
            return View(order);
        }
        // 5. LƯU ĐƠN HÀNG VÀO DATABASE
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order model, string selectedProducts)
        {
            var cart = GetCartItems();

            // 1. Đồng bộ lọc danh sách hoa dựa trên chuỗi selectedProducts được giữ lại
            if (!string.IsNullOrEmpty(selectedProducts))
            {
                var selectedIds = selectedProducts.Split(',').Select(int.Parse).ToList();
                cart = cart.Where(x => selectedIds.Contains(x.ProductId)).ToList();
            }

            ModelState.Remove("selectedProducts");
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("OrderStatus");
            ModelState.Remove("OrderDetails");

            // Kiểm tra thủ công nếu người dùng cố tình xóa sạch dữ liệu nhập
            if (string.IsNullOrEmpty(model.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Vui lòng nhập số điện thoại người nhận.");
            }
            if (string.IsNullOrEmpty(model.ShippingAddress))
            {
                ModelState.AddModelError("ShippingAddress", "Vui lòng nhập địa chỉ giao hoa chi tiết.");
            }

            // 3. Nếu dữ liệu nhập hợp lệ -> Lưu Database và CHUYỂN TRANG THÀNH CÔNG
            if (ModelState.IsValid)
            {
                var order = new Order
                {
                    UserId = _userManager.GetUserId(User),
                    OrderDate = DateTime.Now,
                    TotalAmount = cart.Sum(x => x.TotalPrice), 
                    OrderStatus = "Chờ xử lý",
                    ShippingAddress = model.ShippingAddress,
                    PhoneNumber = model.PhoneNumber,
                    OrderDetails = new List<OrderDetail>()
                };

                foreach (var item in cart)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Xóa các sản phẩm đã mua ra khỏi giỏ hàng Session, giữ lại các món không được tích chọn
                if (!string.IsNullOrEmpty(selectedProducts))
                {
                    var selectedIds = selectedProducts.Split(',').Select(int.Parse).ToList();
                    var fullCart = GetCartItems();
                    var remainingCart = fullCart.Where(x => !selectedIds.Contains(x.ProductId)).ToList();
                    HttpContext.Session.SetString("BoutiqueCart", System.Text.Json.JsonSerializer.Serialize(remainingCart));
                }
                else
                {
                    HttpContext.Session.Remove("BoutiqueCart");
                }

                return RedirectToAction("OrderSuccess", new { id = order.Id });
            }

            model.TotalAmount = cart.Sum(x => x.TotalPrice); // Giữ nguyên tổng tiền cũ không cho sập về 0đ
            ViewBag.CartItems = cart; // Giữ nguyên danh sách hoa đã chọn mua

            return View(model);
        }
        // 6. TRANG BÁO ĐẶT HOA THÀNH CÔNG
        [Authorize]
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // 7. TRANG LỊCH SỬ ĐƠN HÀNG CỦA KHÁCH HÀNG
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            
            var userId = _userManager.GetUserId(User);

           
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate) // Đơn mới nhất xếp lên đầu
                .ToListAsync();

            return View(orders);
        }
    }
}