import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { Product } from '@/modules/inventory/models/product.model';

export const usePosStore = defineStore('pos', () => {
  const products = ref<Product[]>([]);
  const lastUpdate = ref<number>(Date.now());
  const isLoading = ref(false);

  const setProducts = (productList: Product[]) => {
    products.value = productList;
    lastUpdate.value = Date.now();
  };

  const updateProductFromValidation = (id: string, price: number, stock: number) => {
    const product = products.value.find(p => p.Id === id);
    if (product) {
      product.SalePrice = price;
      product.CurrentStock = stock;
      lastUpdate.value = Date.now();
    }
  };

  const searchProductsLocal = (query: string): Product[] => {
    if (!query || query.length < 2) return [];
    
    const searchLower = query.toLowerCase();
    return products.value.filter(p => 
      p.ProductName.toLowerCase().includes(searchLower) ||
      p.ProductCode?.toLowerCase().includes(searchLower) ||
      p.LaboratoryName?.toLowerCase().includes(searchLower)
    ).slice(0, 50); // Optimized for display
  };

  return {
    products,
    lastUpdate,
    isLoading,
    setProducts,
    updateProductFromValidation,
    searchProductsLocal
  };
});
