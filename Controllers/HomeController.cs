using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using WebBanHoa.Models;
using WebBanHoa.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace WebBanHoa.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        public HomeController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // TRANG XEM CHI TI?T BÓ HOA (Khách vãng lai c?ng ph?i xem ???c ?? b?m Mua)
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product); // Tìm file: Views/Home/Display.cshtml (??u file nh?n: @model WebBanHoa.Models.Product)
        }

        // Các trang ph?, chính sách b?o m?t
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