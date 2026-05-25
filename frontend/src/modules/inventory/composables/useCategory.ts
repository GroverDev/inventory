import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { Category } from '@/modules/inventory/models/category.model';

const { get, post, put, del } = useApi();

const useCategory = () => {

  const getCategories = async (name: string = ''): Promise<ResponseArray<Category>> => {
    return await get<ResponseArray<Category>>(`Category?categoryName=${name}`);
  }

  const getCategoryById = async (id: string): Promise<ResponseObject<Category>> => {
    return await get<ResponseObject<Category>>(`Category/${id}`);
  }

  const createCategory = async (cat: Category): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('Category', {
      categoryName: cat.CategoryName,
      description: cat.Description,
      isActive: cat.IsActive,
    });
  }

  const updateCategory = async (cat: Category): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Category/${cat.Id}`, {
      id: cat.Id,
      categoryName: cat.CategoryName,
      description: cat.Description,
      isActive: cat.IsActive,
    });
  }

  const deleteCategory = async (id: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Category/${id}`);
  }

  return { getCategories, getCategoryById, createCategory, updateCategory, deleteCategory }
}

export default useCategory;
