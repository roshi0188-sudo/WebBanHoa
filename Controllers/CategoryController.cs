using Microsoft.AspNetCore.Mvc;
using WebBanHoa.Models;
using WebBanHoa.Repositories;

namespace WebBanHoa.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // 1. Hiển thị danh sách danh mục hoa
        public IActionResult Index()
        {
            var categories = _categoryRepository.GetAllCategories();
            return View(categories);
        }

        // 2. Mở form thêm mới danh mục
        public IActionResult Add()
        {
            return View();
        }

        // Xử lý lưu danh mục mới
        [HttpPost]
        public IActionResult Add(Category category)
        {
            // SỬA TẠI ĐÂY: Loại bỏ ModelState.IsValid để tránh lỗi nghẽn do thuộc tính Id tự tăng 
            if (!string.IsNullOrEmpty(category.Name))
            {
                _categoryRepository.Add(category);
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("Name", "Tên danh mục không được để trống");
            return View(category);
        }

        // 3. Mở form chỉnh sửa danh mục
        public IActionResult Update(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // Xử lý cập nhật danh mục
        [HttpPost]
        public IActionResult Update(Category category)
        {
            
            if (!string.IsNullOrEmpty(category.Name))
            {
                _categoryRepository.Update(category);
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("Name", "Tên danh mục không được để trống");
            return View(category);
        }

        // 4. Mở trang hiển thị xác nhận xóa danh mục
        public IActionResult Delete(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // Xử lý thực thi xóa
        [HttpPost, ActionName("DeleteConfirmed")]
        public IActionResult DeleteConfirmed(int id)
        {
            _categoryRepository.Delete(id);
            return RedirectToAction("Index");
        }
    }
}