using WebBanHoa.Models;
using System.Collections.Generic;
using System.Linq;
namespace WebBanHoa.Repositories
{
    public class MockProductRepository : IProductRepository
    {
        private readonly List<Product> _products;

        public MockProductRepository()
        {
           
            _products = new List<Product>
            {
                new Product {
                    Id = 1,
                    Name = "Hoa mix màu 1",
                    Price = 650000,
                    Description = "Màu sắc ngọt ngào cho ngày kỷ niệm.",
                    ImageUrl = "/images/hoa1.jpg",
                    CategoryId = 2,
                    Ingredients = "Hồng Thạch Thảo, Cẩm Tú Cầu, Lá Bạc"
                },
                new Product {
                    Id = 2,
                    Name = "Hoa mix màu 2",
                    Price = 450000,
                    Description = "Sự kết hợp hoàn hảo của nhiều loại hoa.",
                    ImageUrl = "/images/hoa2.jpg",
                    CategoryId = 2,
                    Ingredients = "Hoa Hướng Dương, Cát Tường, Nơ lụa"
                },
                new Product {
                    Id = 3,
                    Name = "Hoa mix màu 3",
                    Price = 500000,
                    OldPrice = 600000,
                    Description = "Sắc xanh tươi mát cho buổi hẹn hò.",
                    ImageUrl = "/images/hoa3.jpg",
                    CategoryId = 2,
                    Ingredients = "Hoa Hồng Trắng, Tulip Xanh, Lá phụ"
                }
            };
        }

        public IEnumerable<Product> GetAll()
        {
            return _products;
        }

        public Product GetById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        public void Add(Product product)
        {
            product.Id = _products.Any() ? _products.Max(p => p.Id) + 1 : 1;
            _products.Add(product);
        }

        public void Update(Product product)
        {
            var index = _products.FindIndex(p => p.Id == product.Id);
            if (index != -1)
            {
                _products[index] = product;
            }
        }

        public void Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _products.Remove(product);
            }
        }
    }
}
