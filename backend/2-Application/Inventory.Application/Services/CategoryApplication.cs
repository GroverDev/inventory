using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class CategoryApplication(ICategoryRepository _categoryRepository) : ICategoryApplication
{
    public async Task<Response<bool>> CreateCategory(CategoryRequest categoryRequest, int createdBy)
    {
        Response<bool> respuesta = new();
        try
        {
            categoryRequest.Id = Guid.Empty.ToString();
            var category = categoryRequest.Adapt<Category>();
            category.CreatedBy = category.ModifiedBy = createdBy;
            category.Created = category.Modified = DateTime.Now;
            category.State = true;

            respuesta.Data = await _categoryRepository.CreateCategory(category);
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdateCategory(CategoryRequest categoryRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            var category = categoryRequest.Adapt<Category>();
            category.ModifiedBy = modifiedBy;
            category.Modified = DateTime.Now;

            var rowsAffected = await _categoryRepository.UpdateCategory(category);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo modificar la categoría");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> DeleteCategory(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            Guid categoryId = Guid.Parse(id);
            var rowsAffected = await _categoryRepository.DeleteCategory(categoryId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo eliminar la categoría");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<List<CategoryRequest>>> GetCategories(string categoryName)
    {
        Response<List<CategoryRequest>> categories = new() { Data = [] };
        try
        {
            var resp = await _categoryRepository.GetCategories(categoryName);
            foreach (var item in resp)
                categories.Data.Add(item.Adapt<CategoryRequest>());
            categories.ok = true;
        }
        catch (CustomException ex) { categories.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { categories.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return categories;
    }

    public async Task<Response<CategoryRequest>> GetCategory(string id)
    {
        Response<CategoryRequest> respCategory = new() { Data = new() };
        try
        {
            Guid categoryId = Guid.Parse(id);
            var category = await _categoryRepository.GetCategory(categoryId);
            respCategory.Data = category.Adapt<CategoryRequest>();
            respCategory.ok = true;
        }
        catch (CustomException ex) { respCategory.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respCategory.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return respCategory;
    }
}
