using Microsoft.AspNetCore.Authorization;
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
        private const int SHIPPING_FEE = 30000; // Phí vận chuyển mặc định cố định cho hệ thống

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

        // ==========================================
        // KHU VỰC CÁC HÀM HIỂN THỊ GIỎ HÀNG VÀ THÊM MỚI
        // ==========================================

        // TRANG HIỂN THỊ GIỎ HÀNG  
        public IActionResult Index()
        {
            var cart = GetCartItems();

            decimal totalCartPrice = cart.Sum(item => (decimal)item.Price * item.Quantity);
            decimal discount = (decimal)(HttpContext.Session.GetInt32("VoucherDiscount") ?? 0);
            decimal finalPayment = Math.Max(0, totalCartPrice + SHIPPING_FEE - discount);

            // Bán chéo: Gợi ý 3 mẫu hoa ngẫu nhiên
            var suggestedFlowers = _context.Products.AsNoTracking().OrderBy(p => Guid.NewGuid()).Take(3).ToList();

            ViewBag.TotalCartPrice = totalCartPrice;
            ViewBag.ShippingFee = (decimal)SHIPPING_FEE;
            ViewBag.DiscountAmount = discount;
            ViewBag.FinalPayment = finalPayment;
            ViewBag.SuggestedProducts = suggestedFlowers;

            return View(cart);
        }

        // THÊM HOA VÀO GIỎ HÀNG (ĐÃ ĐỒNG BỘ NỘI DUNG THIỆP CHÚC MỪNG)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, bool HasCard = false, string? CardImage = null, string? CardContent = null)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCartItems();

            var targetCardImage = HasCard ? CardImage : null;
            var targetCardContent = HasCard ? CardContent : null;

            var cartItem = cart.FirstOrDefault(x => x.ProductId == productId &&
                                                    x.CardImage == targetCardImage &&
                                                    x.CardContent == targetCardContent);

            if (cartItem == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity,
                    CardImage = targetCardImage,
                    CardContent = targetCardContent
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

        // XÓA HOA KHỎI GIỎ 
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

        // 🟢 1. TRANG ĐIỀN THÔNG TIN ĐẶT HOA (GET CHECKOUT)
        [Authorize]
        public async Task<IActionResult> Checkout(string selectedProducts, decimal discountApplied = 0)
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

            decimal totalCartPrice = cart.Sum(x => (decimal)x.Price * x.Quantity);
            decimal discount = discountApplied > 0 ? discountApplied : (decimal)(HttpContext.Session.GetInt32("VoucherDiscount") ?? 0);
            decimal finalPayment = Math.Max(0, totalCartPrice + SHIPPING_FEE - discount);

            ViewBag.CartItems = cart;
            ViewBag.TotalPrice = totalCartPrice;
            ViewBag.ShippingFee = (decimal)SHIPPING_FEE;
            ViewBag.Discount = discount;
            ViewBag.FinalPayment = finalPayment;
            ViewBag.SelectedProductsString = selectedProducts;

            // Tìm sản phẩm đầu tiên trong nhóm thanh toán có chứa nội dung thiệp tặng kèm
            var itemWithCard = cart.FirstOrDefault(x => !string.IsNullOrEmpty(x.CardContent));

            var order = new Order
            {
                TotalAmount = finalPayment,
                PhoneNumber = currentUser?.PhoneNumber ?? "",
                ShippingAddress = currentUser?.Address ?? "",
                CardImage = itemWithCard?.CardImage,
                CardContent = itemWithCard?.CardContent
            };

            return View(order);
        }

        // 🟢 2. XỬ LÝ LƯU ĐƠN HÀNG VÀO DATABASE (POST CHECKOUT - ONE-PAGE)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(Order model, string selectedProducts, string ProvinceText, string DistrictText, string WardText, string PaymentMethod)
        {
            var cart = GetCartItems();
            if (!string.IsNullOrEmpty(selectedProducts))
            {
                var selectedIds = selectedProducts.Split(',').Select(int.Parse).ToList();
                cart = cart.Where(x => selectedIds.Contains(x.ProductId)).ToList();
            }

            ModelState.Remove("selectedProducts");
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("ReceiverName"); 
            ModelState.Remove("OrderStatus");
            ModelState.Remove("OrderDetails");

            if (string.IsNullOrEmpty(model.PhoneNumber))
                ModelState.AddModelError("PhoneNumber", "Vui lòng cung cấp số điện thoại liên hệ.");

            if (string.IsNullOrEmpty(model.ShippingAddress) || string.IsNullOrEmpty(ProvinceText) || string.IsNullOrEmpty(DistrictText) || string.IsNullOrEmpty(WardText))
                ModelState.AddModelError("ShippingAddress", "Vui lòng điền và chọn đầy đủ phân cấp địa chỉ nhận hoa.");

            if (ModelState.IsValid)
            {
                decimal totalCartPrice = cart.Sum(x => (decimal)x.Price * x.Quantity);
                decimal discount = (decimal)(HttpContext.Session.GetInt32("VoucherDiscount") ?? 0);
                decimal finalPayment = Math.Max(0, totalCartPrice + SHIPPING_FEE - discount);

                string fullCleanAddress = $"{model.ShippingAddress}, {WardText}, {DistrictText}, {ProvinceText}";

                // 🟢 ĐÃ CẢI TIẾN: Trích xuất thông tin phôi thiệp và lời chúc từ giỏ hàng hiện tại đưa vào đơn hàng mới
                var itemWithCard = cart.FirstOrDefault(x => !string.IsNullOrEmpty(x.CardContent));

                var order = new Order
                {
                    UserId = _userManager.GetUserId(User),
                    OrderDate = DateTime.Now,
                    TotalAmount = finalPayment,
                    OrderStatus = "Chờ xử lý",
                    ShippingAddress = fullCleanAddress,
                    PhoneNumber = model.PhoneNumber,
                    ReceiverName = model.ReceiverName,
                    CardImage = itemWithCard?.CardImage,      
                    CardContent = itemWithCard?.CardContent,  
                    OrderDetails = new List<OrderDetail>()
                };

                foreach (var item in cart)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = (decimal)item.Price
                    });
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("VoucherDiscount");
                if (!string.IsNullOrEmpty(selectedProducts))
                {
                    var selectedIds = selectedProducts.Split(',').Select(int.Parse).ToList();
                    var fullCart = GetCartItems();
                    var remainingCart = fullCart.Where(x => !selectedIds.Contains(x.ProductId)).ToList();
                    SaveCartItems(remainingCart);
                }
                else
                {
                    HttpContext.Session.Remove(CART_SESSION_KEY);
                }

                return RedirectToAction("OrderSuccess", new { id = order.Id });
            }

            decimal oldTotal = cart.Sum(x => (decimal)x.Price * x.Quantity);
            decimal oldDiscount = (decimal)(HttpContext.Session.GetInt32("VoucherDiscount") ?? 0);
            ViewBag.CartItems = cart;
            ViewBag.TotalPrice = oldTotal;
            ViewBag.ShippingFee = (decimal)SHIPPING_FEE;
            ViewBag.Discount = oldDiscount;
            ViewBag.FinalPayment = Math.Max(0, oldTotal + SHIPPING_FEE - oldDiscount);
            ViewBag.SelectedProductsString = selectedProducts;

            return View("Checkout", model);
        }

        // TRANG BÁO ĐẶT HOA THÀNH CÔNG
        [Authorize]
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // TRANG LỊCH SỬ ĐƠN HÀNG CỦA KHÁCH HÀNG
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            ViewBag.RewardPoints = currentUser.RewardPoints;
            ViewBag.MemberRank = currentUser.MemberRank;

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => o.UserId == currentUser.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // MÀN HÌNH CHI TIẾT ĐƠN HÀNG KHI KHÁCH CLICK XEM
        [Authorize]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }

        // ĐẶT LẠI ĐƠN HÀNG CŨ
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ReOrder(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu đơn hàng cũ!" });

            var cart = GetCartItems();

            foreach (var detail in order.OrderDetails)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    // 🟢 TỐI ƯU HÓA: Kiểm tra trùng lặp sản phẩm chính xác
                    var existingItem = cart.FirstOrDefault(x => x.ProductId == detail.ProductId && x.CardContent == order.CardContent);
                    if (existingItem == null)
                    {
                        cart.Add(new CartItem
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ImageUrl = product.ImageUrl,
                            Price = product.Price,
                            Quantity = detail.Quantity,
                            CardImage = order.CardImage,
                            CardContent = order.CardContent
                        });
                    }
                    else
                    {
                        existingItem.Quantity += detail.Quantity;
                    }
                }
            }

            SaveCartItems(cart);
            return Json(new { success = true, message = "Đã thêm toàn bộ sản phẩm cũ kèm cấu hình thiệp vào giỏ hàng thành công! Đang chuyển trang..." });
        }

        // 🟢 API AJAX TĂNG GIẢM SỐ LƯỢNG (ĐÃ SỬA LỖI ĐỂ TRÁNH BỊ SAI LỆCH THIỆP)
        [HttpPost]
        [Authorize]
        public IActionResult UpdateQuantity(int productId, int change, string? cardContent = null)
        {
            var cart = GetCartItems();

            // Tìm chính xác sản phẩm dựa vào ProductId và nội dung lời chúc đi kèm
            var item = cart.FirstOrDefault(x => x.ProductId == productId && x.CardContent == cardContent);

            bool itemRemoved = false;
            int newQty = 0;
            decimal newSubtotal = 0;

            if (item != null)
            {
                item.Quantity += change;
                newQty = item.Quantity;
                newSubtotal = (decimal)item.Price * item.Quantity;

                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                    itemRemoved = true;
                }
                SaveCartItems(cart);
            }

            decimal totalCartPrice = cart.Sum(i => (decimal)i.Price * i.Quantity);
            decimal discount = (decimal)(HttpContext.Session.GetInt32("VoucherDiscount") ?? 0);
            decimal finalPayment = Math.Max(0, totalCartPrice + SHIPPING_FEE - discount);

            return Json(new
            {
                success = true,
                itemRemoved = itemRemoved,
                newQty = newQty,
                newSubtotal = newSubtotal.ToString("#,##0"),
                totalPrice = totalCartPrice.ToString("#,##0"),
                discount = discount.ToString("#,##0"),
                finalPayment = finalPayment.ToString("#,##0")
            });
        }

        // API AJAX XÓA SẢN PHẨM KHỎI GIỎ HÀNG
        [HttpPost]
        [Authorize]
        public IActionResult RemoveItemJson(int productId, string? cardContent = null)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(x => x.ProductId == productId && x.CardContent == cardContent);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartItems(cart);
            }

            decimal totalCartPrice = cart.Sum(i => (decimal)i.Price * i.Quantity);
            decimal discount = (decimal)(HttpContext.Session.GetInt32("VoucherDiscount") ?? 0);
            decimal finalPayment = Math.Max(0, totalCartPrice + SHIPPING_FEE - discount);

            return Json(new
            {
                success = true,
                totalPrice = totalCartPrice.ToString("#,##0"),
                discount = discount.ToString("#,##0"),
                finalPayment = finalPayment.ToString("#,##0")
            });
        }

        // API ÁP MÃ VOUCHER GIẢM GIÁ MẶC ĐỊNH
        [HttpPost]
        [Authorize]
        public IActionResult ApplyCoupon(string code)
        {
            var cart = GetCartItems();
            decimal totalCartPrice = cart.Sum(i => (decimal)i.Price * i.Quantity);
            decimal discount = 0;
            bool success = false;
            string message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn!";

            if (!string.IsNullOrEmpty(code) && code.ToUpper() == "FLORALLAM")
            {
                discount = 50000;
                HttpContext.Session.SetInt32("VoucherDiscount", 50000);
                success = true;
                message = "Áp dụng mã giảm giá 50.000 đ thành công! 🌸";
            }
            else
            {
                HttpContext.Session.Remove("VoucherDiscount");
            }

            decimal finalPayment = Math.Max(0, totalCartPrice + SHIPPING_FEE - discount);

            return Json(new
            {
                success = success,
                message = message,
                totalPrice = totalCartPrice.ToString("#,##0"),
                discount = discount.ToString("#,##0"),
                finalPayment = finalPayment.ToString("#,##0")
            });
        }
    }
}