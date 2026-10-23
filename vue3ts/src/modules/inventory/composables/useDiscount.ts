import { useApi } from '@/api/useApi';
import type { ResponseArray, ResponseObject } from '@/models/response';
import type { Discount } from '@/modules/inventory/models/discount.model';

const { get, post, put, del } = useApi();

const useDiscount = () => {
  const getDiscounts = () =>
    get<ResponseArray<Discount>>('Discounts');

  const getDiscountById = (id: string) =>
    get<ResponseObject<Discount>>(`Discounts/${id}`);

  const createDiscount = (request: Discount) =>
    post<ResponseObject<string>>('Discounts', request);

  const updateDiscount = (id: string, request: Discount) =>
    put<ResponseObject<boolean>>(`Discounts/${id}`, request);

  const deleteDiscount = (id: string) =>
    del<ResponseObject<boolean>>(`Discounts/${id}`);

  return { getDiscounts, getDiscountById, createDiscount, updateDiscount, deleteDiscount };
};

export default useDiscount;
