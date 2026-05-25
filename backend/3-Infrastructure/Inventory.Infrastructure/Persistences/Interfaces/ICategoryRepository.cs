using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ICategoryRepository
{
    public Task<bool> CreateCategory(Category category);
    public Task<int> UpdateCategory(Category category);
    public Task<List<Category>> GetCategories(string categoryName);
    public Task<Category> GetCategory(Guid id);
    public Task<int> DeleteCategory(Guid id, int idUserModified);
}
