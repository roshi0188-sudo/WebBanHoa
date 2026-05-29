using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using WebBanHoa.Models;
using WebBanHoa.Repositories;

namespace WebBanHoa.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;

        // Tiêm IProductRepository vào thông qua Constructor ?? qu?n lý d? li?u t?p trung
        public HomeController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // L?y danh sách hoa th?c t? hi?n th? lên Trang ch?
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // S?A L?I: Chuy?n hành ??ng Display sang Async và dùng ?úng _productRepository thay vì _context
        // ???ng d?n th?c t? khi ch?y h? th?ng: /Home/Display/{id}
        public async Task<IActionResult> Display(int id)
        {
            // G?i hàm l?y chi ti?t s?n ph?m kèm danh m?c (Category) t? Repository
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}