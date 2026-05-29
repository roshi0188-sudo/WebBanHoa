using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebBanHoa.Models;
using WebBanHoa.Repositories;

namespace WebBanHoa.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;

        // Tiêm ICategoryRepository vào thông qua Constructor
        public CategoryController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // 1. HIỂN THỊ DANH SÁCH DANH MỤC (INDEX)
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        // 2. THÊM DANH MỤC - GIAO DIỆN (GET)
        public IActionResult Add()
        {
            return View();
        }

        // 3. THÊM DANH MỤC - XỬ LÝ DỮ LIỆU (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryRepository.AddAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 4. CẬP NHẬT DANH MỤC - GIAO DIỆN (GET)
        public async Task<IActionResult> Update(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // 5. CẬP NHẬT DANH MỤC - XỬ LÝ DỮ LIỆU (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _categoryRepository.UpdateAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 6. XÓA DANH MỤC - XÁC NHẬN (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // 7. XÓA DANH MỤC - THỰC THI (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _categoryRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}