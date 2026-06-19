using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebBanHoa.Models;
using WebBanHoa.Repositories;

namespace WebBanHoa.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Khóa chặt toàn bộ Controller này chỉ cho Admin vào
    public class ProductManagerController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;
        public ProductManagerController(IProductRepository productRepository, ICategoryRepository categoryRepository, ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId)
        {
            var products = await _productRepository.GetAllAsync();

            var query = products.AsQueryable();
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentCategory = categoryId; 

            return View(query.ToList());
        }

        [ActionName("Create")]
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View("Add"); 
        }

        // THÊM MỚI SẢN PHẨM (POST)
        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl, List<IFormFile> imageUrls)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageUrl.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await imageUrl.CopyToAsync(stream); }
                    product.ImageUrl = "/images/" + fileName;
                }

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
            return View("Add", product); 
        }

        // 3. CẬP NHẬT SẢN PHẨM (GET) - Đồng bộ tên Edit ra bên ngoài View
        [ActionName("Edit")]
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View("Update", product); 
        }

        // CẬP NHẬT SẢN PHẨM (POST)
        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Product product, IFormFile newImageUrl)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null) return NotFound();

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Description = product.Description;

            if (newImageUrl != null && newImageUrl.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newImageUrl.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create)) { await newImageUrl.CopyToAsync(stream); }
                existingProduct.ImageUrl = "/images/" + fileName;
            }

            await _productRepository.UpdateAsync(existingProduct);
            return RedirectToAction(nameof(Index));
        }

        // 4. XÓA SẢN PHẨM (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // XÓA SẢN PHẨM (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        // 5. XEM CHI TIẾT SẢN PHẨM (DETAILS)
        [ActionName("Details")] 
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetByIdAsync(id); 
            if (product == null) return NotFound();

            return View("Details", product); 
        }
    }
}