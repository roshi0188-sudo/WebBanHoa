using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHoa.Models;
using WebBanHoa.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. CẤU HÌNH KẾT NỐI CƠ SỞ DỮ LIỆU SQL SERVER
// ====================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ====================================================================
// 2. CẤU HÌNH CƠ CHẾ ĐĂNG NHẬP (IDENTITY CHUẨN)
// ====================================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false; // Tắt bớt ràng buộc phức tạp nếu cần test nhanh
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ====================================================================
// 3. CẤU HÌNH ĐƯỜNG DẪN ĐIỀU HƯỚNG PHÂN QUYỀN COOKIE
// ====================================================================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";               // Nếu chưa đăng nhập mà vào trang cấm, đá về đây
    options.AccessDeniedPath = "/Account/AccessDenied"; // Nếu sai quyền (Customer cố vào Admin), đá về đây
    options.ExpireTimeSpan = TimeSpan.FromDays(7);       // Giữ đăng nhập trong vòng 7 ngày
    options.SlidingExpiration = true;
});

// ====================================================================
// 4. KÍCH HOẠT BỘ NHỚ ĐỆM VÀ SESSION (BẮT BUỘC CHO GIỎ HÀNG)
// ====================================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);     // Giỏ hàng tự hủy nếu khách bất động quá 30 phút
    options.Cookie.HttpOnly = true;                     // Bảo mật cookie giỏ hàng khỏi tấn công XSS
    options.Cookie.IsEssential = true;                  // Bắt buộc chạy bất kể người dùng chặn cookie
});

// ====================================================================
// 5. ĐĂNG KÝ KIẾN TRÚC MVC VÀ CÁC REPOSITORY HỆ THỐNG
// ====================================================================
builder.Services.AddControllersWithViews();

// Đăng ký các Repository điều hướng dữ liệu thật
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

// 🛠️ BỔ SUNG ĐỀ PHÒNG: Nếu sau này bạn viết riêng IOrderRepository để quản lý đơn hàng
// builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();


var app = builder.Build();

// ====================================================================
// CẤU HÌNH MIDDLEWARE PIPELINE (Thứ tự các dòng này cực kỳ quan trọng)
// ====================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔴 VỊ TRÍ CHIẾN LƯỢC: Khởi động Session (Giỏ hàng) ngay sau UseRouting và trước Auth
app.UseSession();

// Chốt chặn kiểm tra danh tính và phân quyền tài khoản
app.UseAuthentication();
app.UseAuthorization();

// ====================================================================
// 6. ĐỊNH TUYẾN ROUTING (Phân tách phân hệ Admin và Khách hàng)
// ====================================================================

// Tuyến đường ưu tiên 1: Quét các Area trước (Ví dụ: /Admin/ProductManager/Index)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Tuyến đường mặc định ưu tiên 2: Ngoài trang chủ khách hàng (Ví dụ: /Home/Index hoặc /Cart/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();