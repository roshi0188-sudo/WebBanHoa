using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using System.Threading.Tasks;
using WebBanHoa.Models;
using WebBanHoa.Repositories;

namespace WebBanHoa.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        // Tiêm cả 2 repository vào để vừa xử lý sản phẩm vừa lấy danh mục hoa
        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // 1. HIỂN THỊ DANH SÁCH SẢN PHẨM (INDEX)
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // 2. XEM CHI TIẾT SẢN PHẨM (DETAILS)
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // 3. THÊM MỚI SẢN PHẨM - GIAO DIỆN (GET)
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            // Truyền danh sách danh mục qua ViewBag để hiển thị lên DropdownList (Thẻ <select>)
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        // 4. THÊM MỚI SẢN PHẨM - XỬ LÝ DỮ LIỆU (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl, List<IFormFile> imageUrls)
        {
            if (ModelState.IsValid)
            {
                // Xử lý ảnh đại diện
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageUrl.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await imageUrl.CopyToAsync(stream); }
                    product.ImageUrl = "/images/" + fileName;
                }

                // Xử lý ảnh phụ (Giả sử bạn lưu vào một property là ImageUrls - chuỗi cách nhau bằng dấu phẩy)
                if (imageUrls != null && imageUrls.Count > 0)
                {
                    List<string> savedPaths = new List<string>();
                    foreach (var file in imageUrls)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }
                        savedPaths.Add("/images/" + fileName);
                    }
                    product.ImageUrls = savedPaths;
                }

                await _productRepository.AddAsync(product);
                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }
        // 5. CẬP NHẬT SẢN PHẨM - GIAO DIỆN (GET)
        [HttpGet] // THÊM DÒNG NÀY
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 6. CẬP NHẬT SẢN PHẨM - XỬ LÝ DỮ LIỆU (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Product product, IFormFile newImageUrl)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null) return NotFound();

            // 1. CẬP NHẬT CÁC THÔNG TIN TEXT
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Description = product.Description;

            // 2. XỬ LÝ ẢNH (Phần quan trọng nhất)
            if (newImageUrl != null && newImageUrl.Length > 0)
            {
                // Tạo tên file mới
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newImageUrl.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                // Lưu file vào thư mục
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newImageUrl.CopyToAsync(stream);
                }

                // Gán đường dẫn mới vào object
                existingProduct.ImageUrl = "/images/" + fileName;
            }
            // Nếu newImageUrl == null, thì existingProduct.ImageUrl vẫn giữ nguyên giá trị cũ -> Chính xác!

            // 3. LƯU VÀO DB
            await _productRepository.UpdateAsync(existingProduct);
            return RedirectToAction(nameof(Index));
        }

        // 7. XÓA SẢN PHẨM - XÁC NHẬN (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // 8. XÓA SẢN PHẨM - THỰC THI (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}