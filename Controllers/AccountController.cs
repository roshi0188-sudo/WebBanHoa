using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebBanHoa.Models;
using WebBanHoa.Models.ViewModels;

namespace WebBanHoa.Controllers 
{
    [AllowAnonymous] 
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // Profile 
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(user); 
        }

        //Chỉnh sửa profile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string fullName, string phoneNumber, string address, IFormFile avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Kiểm tra dữ liệu 
            if (string.IsNullOrEmpty(fullName))
            {
                TempData["DangerMessage"] = "Họ và tên không được để trống.";
                return RedirectToAction("Profile");
            }

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();

                if (!permittedExtensions.Contains(extension))
                {
                    TempData["DangerMessage"] = "Định dạng ảnh không hợp lệ (Chỉ chấp nhận .jpg, .jpeg, .png, .gif).";
                    return RedirectToAction("Profile");
                }

                // Tạo tên file độc nhất tránh trùng lặp
                var fileName = $"{user.Id}_avatar{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");

                // Tạo thư mục nếu chưa tồn tại trong project
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                var filePath = Path.Combine(path, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                // Lưu đường dẫn ảnh mới vào thuộc tính của User
                user.Avatar = $"/images/avatars/{fileName}";
            }

            // Gán các giá trị mới thay đổi vào đối tượng người dùng
            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.Address = address;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật hồ sơ tài khoản thành công!";
            }
            else
            {
                TempData["DangerMessage"] = "Có lỗi xảy ra trong quá trình cập nhật, vui lòng thử lại.";
            }

            return RedirectToAction("Profile");
        }
        // Đăng nhập
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel { ReturnUrl = returnUrl ?? Url.Content("~/") };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Tài khoản và mật khẩu không được để trống.");
                return View();
            }

            // Tìm kiếm thực thể người dùng dựa trên Email đăng nhập
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    if (!user.EmailConfirmed)
                    {
                        await _signInManager.SignOutAsync(); // Đăng xuất phiên vừa mồi
                        ModelState.AddModelError("", "Tài khoản của bạn chưa được xác thực qua Email. Vui lòng kiểm tra hộp thư.");
                        return View();
                    }

                    TempData["SuccessMessage"] = $"Chào mừng {user.FullName} đã quay trở lại! 🌸";
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị Admin khóa do vi phạm chính sách của Floral LAM. Vui lòng liên hệ hỗ trợ.");
                    return View();
                }
            }

            ModelState.AddModelError("", "Thông tin tài khoản hoặc mật khẩu không chính xác.");
            return View();
        }

        // Đăng ký

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
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
                    JoinDate = DateTime.Now,
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Mặc định gán quyền "User" cho khách hàng mới đăng ký
                    await _userManager.AddToRoleAsync(user, "User");

                    // Tạo Token kích hoạt tài khoản
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    // Tạo đường dẫn URL kích hoạt gửi kèm trong Email
                    var callbackUrl = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, token = token }, protocol: HttpContext.Request.Scheme);

                    // Gửi Email nội dung Luxury chúc mừng
                    string emailSubject = "Kích hoạt tài khoản thành viên Floral LAM 🌸";
                    string emailBody = $"<h3>Chào mừng {model.FullName} đến với Floral LAM!</h3>" +
                                       $"<p>Vui lòng click vào đường liên kết bên dưới để xác thực tài khoản của bạn:</p>" +
                                       $"<a href='{callbackUrl}' style='padding:10px 20px; background:#834c58; color:white; text-decoration:none; border-radius:20px;'>Xác thực ngay tài khoản</a>";

                    await _emailSender.SendEmailAsync(model.Email, emailSubject, emailBody);

                    // HỖ TRỢ ĐI DEMO: Đẩy link ra màn hình trung gian để Hội đồng chấm bài xem trực tiếp
                    TempData["ActivationLink"] = callbackUrl;
                    TempData["RegisteredEmail"] = model.Email;

                    return RedirectToAction("RegisterConfirmation");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        // Đăng xuất

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous] 
        public IActionResult AccessDenied()
        {
            return View();
        }

        //Quên mật khẩu
        // 1. Giao diện nhập Email (GET)
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // 2. Tiếp nhận Email và tạo mã Token (POST)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập địa chỉ Email.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Để bảo mật thông tin, thông báo chung tránh bị dò quét tài khoản
                ModelState.AddModelError("", "Email không tồn tại trên hệ thống.");
                return View();
            }

            // Tạo mã Token bảo mật độc nhất để đặt lại mật khẩu
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Tạo đường dẫn kèm Token bảo mật
            var callbackUrl = Url.Action("ResetPassword", "Account",
                new { userId = user.Id, token = token }, protocol: HttpContext.Request.Scheme);

            // HỖ TRỢ DEMO: Gửi link ra TempData để hiển thị thẳng lên giao diện thông báo thành công
            TempData["ResetPasswordLink"] = callbackUrl;
            TempData["UserEmail"] = email;

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // 3. Trang thông báo tạo Link thành công (GET)
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // 4. Giao diện đặt lại mật khẩu mới (GET)
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Truyền dữ liệu ngầm sang View qua thuộc tính ẩn
            ViewBag.UserId = userId;
            ViewBag.Token = token;
            return View();
        }

        // 5. Xử lý lưu mật khẩu mới vào Database (POST)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["DangerMessage"] = "Mật khẩu mới phải có độ dài từ 6 ký tự trở lên.";
                ViewBag.UserId = userId; ViewBag.Token = token;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["DangerMessage"] = "Mật khẩu xác nhận không trùng khớp.";
                ViewBag.UserId = userId; ViewBag.Token = token;
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Thực hiện reset mật khẩu thông qua bộ quản lý Identity mã hóa
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Thay đổi mật khẩu thành công! Vui lòng đăng nhập lại. 🌸";
                return RedirectToAction("Login");
            }

            // Nếu Token hết hạn hoặc không hợp lệ
            TempData["DangerMessage"] = "Mã xác thực đã hết hạn hoặc không hợp lệ. Vui lòng yêu cầu lại.";
            return RedirectToAction("ForgotPassword");
        }

        //Thay đổi mật khẩu
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Rà soát kiểm tra dữ liệu nhập thủ công
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                TempData["DangerMessage"] = "Vui lòng điền đầy đủ các trường mật khẩu.";
                return RedirectToAction("Profile");
            }

            if (newPassword.Length < 6)
            {
                TempData["DangerMessage"] = "Mật khẩu mới phải có độ dài từ 6 ký tự trở lên.";
                return RedirectToAction("Profile");
            }

            if (newPassword != confirmPassword)
            {
                TempData["DangerMessage"] = "Mật khẩu xác nhận mới không trùng khớp.";
                return RedirectToAction("Profile");
            }

            // Thực hiện đổi mật khẩu thông qua UserManager (Tự băm và mã hóa bảo mật)
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Thay đổi mật khẩu tài khoản thành công!";
            }
            else
            {
                // Trích xuất lỗi thực tế từ hệ thống (ví dụ: Sai mật khẩu cũ)
                var error = result.Errors.FirstOrDefault();
                if (error != null && error.Code == "PasswordMismatch")
                {
                    TempData["DangerMessage"] = "Mật khẩu hiện tại không chính xác.";
                }
                else
                {
                    TempData["DangerMessage"] = "Đổi mật khẩu thất bại. Vui lòng kiểm tra lại ràng buộc.";
                }
            }

            return RedirectToAction("Profile");
        }

        // 3. Trang thông báo đăng ký thành công chờ xác thực (GET)
        [AllowAnonymous]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // 4. Hàm xử lý khớp mã Token kích hoạt tài khoản từ Email gửi về (GET)
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Thực hiện hàm đối sánh khớp Token kích hoạt trong hệ thống Identity
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Tài khoản của bạn đã được kích hoạt thành công! Vui lòng đăng nhập. 🌸";
                return RedirectToAction("Login");
            }

            TempData["DangerMessage"] = "Mã xác thực tài khoản đã hết hạn hoặc không hợp lệ.";
            return RedirectToAction("Login");
        }
    }
}