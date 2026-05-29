using WebBanHoa.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebBanHoa.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả sản phẩm hoa kèm theo thông tin danh mục (Bất đồng bộ)
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.Include(p => p.Category).ToListAsync();
        }

        // Lấy chi tiết sản phẩm hoa theo Id (Bất đồng bộ)
        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        }

        // Thêm mới một sản phẩm hoa vào CSDL (Bất đồng bộ)
        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            // Đánh dấu đối tượng là Modified để EF Core biết cần cập nhật tất cả các trường
            _context.Entry(product).State = EntityState.Modified;

            // Lưu thay đổi
            await _context.SaveChangesAsync();
        }
        // Xóa sản phẩm hoa ra khỏi hệ thống (Bất đồng bộ)
        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}
