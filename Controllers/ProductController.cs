using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string searchString, int? categoryId, string sortOrder, int page = 1)
        {
            int pageSize = 9;

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSort = sortOrder; // 🟢 Thêm dòng này để lưu trạng thái sắp xếp

            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // 1. Áp dụng Bộ lọc trước
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchString));
            }
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // 2. Áp dụng Sắp xếp (Sort)
            switch (sortOrder)
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "newest":
                default:
                    productsQuery = productsQuery.OrderByDescending(p => p.Id);
                    break;
            }

            // 3. Tính toán phân trang dựa trên truy vấn đã được sắp xếp
            int totalItems = await productsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            // 🟢 ĐÃ SỬA: Bỏ dòng .OrderByDescending(p => p.Id) thừa ở đây.
            // Truy vấn lúc này đã được sắp xếp bởi lệnh Switch ở trên.
            var filteredProducts = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

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
