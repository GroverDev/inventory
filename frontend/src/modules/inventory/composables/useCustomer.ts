import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { Customer } from '@/modules/inventory/models/customer.model';

const { get, post, put, del } = useApi();

const useCustomer = () => {

  const getCustomers = async (name: string = ''): Promise<ResponseArray<Customer>> => {
    return await get<ResponseArray<Customer>>(`Customers?CustomerName=${name}`);
  }

  const getCustomerById = async (id: string): Promise<ResponseObject<Customer>> => {
    return await get<ResponseObject<Customer>>(`Customers/${id}`);
  }

  const createCustomer = async (customer: Customer): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('Customers', {
      fullName: customer.FullName,
      documentNumber: customer.DocumentNumber,
      email: customer.Email,
      cellphone: customer.Cellphone,
      isActive: customer.IsActive,
    });
  }

  const updateCustomer = async (customer: Customer): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Customers/${customer.Id}`, {
      id: customer.Id,
      fullName: customer.FullName,
      documentNumber: customer.DocumentNumber,
      email: customer.Email,
      cellphone: customer.Cellphone,
      isActive: customer.IsActive,
    });
  }

  const deleteCustomer = async (id: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Customers/${id}`);
  }

  return { getCustomers, getCustomerById, createCustomer, updateCustomer, deleteCustomer }
}
export default useCustomer;
