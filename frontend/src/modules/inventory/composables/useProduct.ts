import { useApi } from '@/modules/common/composables/api/useApi';
import type { Product, ProductBulkUpdate } from '@/modules/inventory/models/product.model';
import type { ResponseArray, ResponseObject, ResponsePaged } from '@/modules/common/models/response.model';

const { get, put, post } = useApi();

const useProduct = () => {
  const createProduct = async (product: Product): Promise<ResponseObject<string>> => {
    return await post<ResponseObject<string>>('Product', product);
  }

  const updateProduct = async (product: Product): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Product/${product.Id}`, product)
  }

  const getProductsByName = async (name: string): Promise<ResponseArray<Product>> => {
    return await get<ResponseArray<Product>>(
      `Product?productName=${name}`,
    );
  }

  const getProductsStock = async (productName: string, page: number, pageSize: number): Promise<ResponsePaged<Product>> => {
    const name = encodeURIComponent(productName);
    return await get<ResponsePaged<Product>>(`Product/stock?productName=${name}&page=${page}&pageSize=${pageSize}`);
  };

  const getProductsPos = async (): Promise<ResponseArray<Product>> => {
    // Fetches an optimized list of active products for POS storage
    // Assuming 'Product/pos-list' is the backend endpoint for optimized results
    return await get<ResponseArray<Product>>('Product'); 
  }

  const validateProductSelection = async (productId: string): Promise<ResponseObject<{ SalePrice: number, CurrentStock: number }>> => {
    // Ultra-fast validation endpoint
    return await get<ResponseObject<{ SalePrice: number, CurrentStock: number }>>(`Product/${productId}/validate`);
  }

  const getProductById = async (productId: string): Promise<ResponseObject<Product>> => {
    return await get<ResponseObject<Product>>(
      `Product/${productId}`,
    );
  }

  const getAllProducts = async (): Promise<ResponseArray<Product>> => {
    return await get<ResponseArray<Product>>('Product?productName=');
  }

  const bulkUpdateProducts = async (items: ProductBulkUpdate[]): Promise<ResponseObject<number>> => {
    return await put<ResponseObject<number>>('Product/bulk', items);
  }

  /**
   * Activa el seguimiento por lotes. Es una acción aparte del guardado de la
   * ficha porque no tiene vuelta atrás: el stock actual queda como existencia
   * sin lote y desde entonces cada recepción exige su código.
   */
  const activateLotTracking = async (productId: string): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>(`Product/${productId}/lot-tracking`, {});
  }

  return { getProductsByName, getProductById, updateProduct, createProduct, getProductsPos, validateProductSelection, getProductsStock, getAllProducts, bulkUpdateProducts, activateLotTracking }
}
export default useProduct;
