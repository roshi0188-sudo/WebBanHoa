using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHoa.Models;
using WebBanHoa.Repositories;

namespace WebBanHoa.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        // Tiêm cả 2 repository vào để vừa xử lý sản phẩm vừa lấy danh mục hoa
        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository, ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            // Nạp danh sách danh mục để hiển thị ra các nút bấm ngoài View
            ViewBag.Categories = await _context.Categories.ToListAsync();

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentCategory = categoryId;

            // Khởi tạo truy vấn LINQ từ bảng Products
            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchString));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            var filteredProducts = await productsQuery.ToListAsync();
            return View(filteredProducts);
        }




        [AllowAnonymous] 
        public async Task<IActionResult> Display(int id)
        {
            
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);


            if (product == null)
            {
                return NotFound();
            }


            return View(product);
        }
    }
}
