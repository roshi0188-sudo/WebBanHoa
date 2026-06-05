using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebBanHoa.Models;
using WebBanHoa.Models.ViewModels;

namespace WebBanHoa.Controllers // 🔴 ĐÃ SỬA: Đưa về Namespace gốc ngoài trang chủ
{
    [AllowAnonymous] // Cho phép tất cả mọi người (chưa đăng nhập) đều vào được trang này
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // ==========================================
        // KHU VỰC: ĐĂNG NHẬP (LOGIN)
        // ==========================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Tránh lỗi nạp trang vòng lặp, nếu returnUrl trống thì mặc định về trang chủ ngoài
            var model = new LoginViewModel { ReturnUrl = returnUrl ?? Url.Content("~/") };

            // 🔴 ĐÃ SỬA: Gọi View ngắn gọn để MVC tự tìm đến Views/Account/Login.cshtml màu hồng của bạn
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null) // 🛠️ BỔ SUNG: Tham số returnUrl để hứng dữ liệu từ Form gửi lên
        {
            // Đồng bộ lại giá trị: nếu tham số hàm trống thì lấy từ trong Model, nếu vẫn trống thì mặc định về trang chủ "/"
            returnUrl ??= model.ReturnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    if (user != null)
                    {
                        // 🔴 LUỒNG 1: Nếu là Admin, ưu tiên đá thẳng vào khu vực quản trị Area Admin
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                        }
                    }

                    // 🟢 LUỒNG 2: Nếu có returnUrl hợp lệ và không phải trang kẹt, điều hướng về trang đó
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && returnUrl != "/Account/Login")
                    {
                        return LocalRedirect(returnUrl);
                    }

                    // 🔵 LUỒNG 3 (BẾN ĐỖ AN TOÀN): Khi mới chạy lên bấm Login luôn (returnUrl trống hoặc là "/"), đưa về trang chủ
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
            }

            // Đảm bảo giữ lại giá trị đường dẫn cũ để form không bị mất dữ liệu khi load lại trang báo lỗi
            model.ReturnUrl = returnUrl;
            return View(model);
        }

        // ==========================================
        // KHU VỰC: ĐĂNG KÝ (REGISTER)
        // ==========================================

        [HttpGet]
        public IActionResult Register()
        {
            // 🔴 ĐÃ SỬA: Tự động tìm đến Views/Account/Register.cshtml ngoài gốc
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    JoinDate = DateTime.Now
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // ==========================================
        // KHU VỰC: ĐĂNG XUẤT (LOGOUT)
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}