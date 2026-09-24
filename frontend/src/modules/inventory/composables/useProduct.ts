import { useApi } from '@/modules/common/composables/api/useApi';
import type { Product, ProductBulkUpdate } from '@/modules/inventory/models/product.model';
import type { ResponseArray, ResponseObject, ResponsePaged } from '@/modules/common/models/response.model';

const { get, put, post, postForm, del } = useApi();

const useProduct = () => {
  const createProduct = async (product: Product): Promise<ResponseObject<string>> => {
    return await post<ResponseObject<string>>('Product', product);
  }

  const updateProduct = async (product: Product): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Product/${product.Id}`, product)
  }

  /** branch: de qué sucursales se suma el stock. '' = la activa, un id, o 'all'. */
  const getProductsByName = async (name: string, branch = ''): Promise<ResponseArray<Product>> => {
    return await get<ResponseArray<Product>>(
      `Product?productName=${name}` + (branch ? `&branch=${encodeURIComponent(branch)}` : ''),
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
   * Activa el seguimiento del producto, por lotes o por números de serie. Es
   * una acción aparte del guardado de la ficha porque no tiene vuelta atrás: el
   * stock actual queda como existencia sin identificar y desde entonces cada
   * recepción exige el código de lo que entra.
   */
  const activateTracking = async (
    productId: string,
    modo: 'lot' | 'serial',
  ): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>(`Product/${productId}/tracking?modo=${modo}`, {});
  }

  /** Sube (o reemplaza) la imagen. Devuelve la ruta relativa que quedó guardada. */
  const uploadImage = async (productId: string, file: Blob, fileName: string): Promise<ResponseObject<string>> => {
    const form = new FormData();
    form.append('file', file, fileName);
    return await postForm<ResponseObject<string>>(`Product/${productId}/image`, form);
  }

  const deleteImage = async (productId: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Product/${productId}/image`);
  }

  return { uploadImage, deleteImage, getProductsByName, getProductById, updateProduct, createProduct, getProductsPos, validateProductSelection, getProductsStock, getAllProducts, bulkUpdateProducts, activateTracking }
}
export default useProduct;
