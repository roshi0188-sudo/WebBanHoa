using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebBanHoa.Areas.Admin.Controllers
{
    [Area("Admin")] // Định danh vùng Admin
    [Authorize(Roles = "Admin")] // Chốt chặn bảo mật quyền Admin
    public class DashboardController : Controller
    {
        // Đường dẫn: /Admin hoặc /Admin/Dashboard
        public IActionResult Index()
        {
            return View(); // Tự động gọi file Areas/Admin/Views/Dashboard/Index.cshtml
        }
    }
}