using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebBanHoa.Models; // 🟢 ĐÃ SỬA: Ép nhận Namespace Models gốc để nạp chuẩn xác Product, Category, Order, OrderDetail
using WebBanHoa.Models.ViewModels;

namespace WebBanHoa.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Định nghĩa các tập thực thể (Tương ứng 100% các bảng vật lý trong SQL Server)
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Ignore<CartItem>();
        }
    }
}