using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface ICategoryApplication
{
    public Task<Response<bool>> CreateCategory(CategoryRequest categoryRequest, int createdBy);
    public Task<Response<bool>> UpdateCategory(CategoryRequest categoryRequest, int modifiedBy);
    public Task<Response<bool>> DeleteCategory(string id, int modifiedBy);
    public Task<Response<List<CategoryRequest>>> GetCategories(string categoryName);
    public Task<Response<CategoryRequest>> GetCategory(string id);
}
