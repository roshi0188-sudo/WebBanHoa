using Microsoft.AspNetCore.Mvc;
using WebBanHoa.Models;

namespace WebBanHoa.Repositories
{
    public interface ICategoryRepository
    {
        IEnumerable<Category> GetAllCategories();
        Category GetById(int id);
        void Add(Category category);
        void Update(Category category);
        void Delete(int id);
    }
}