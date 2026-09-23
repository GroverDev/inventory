import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';

/** Venta en espera, como la lista el servidor. */
export interface HeldSale {
  Id: string;
  Label: string;
  CustomerName: string;
  UserName: string;
  Created: string;
  ItemsCount: number;
  Total: number;
  /** Solo al retomarla: el carrito guardado, en JSON. */
  Payload?: string;
}

const { get, post, del } = useApi();

const useHeldSales = () => {
  const getHeld = () => get<ResponseArray<HeldSale>>('HeldSale');

  const hold = (label: string, customerId: string | null, itemsCount: number, total: number, payload: unknown) =>
    post<ResponseObject<string>>('HeldSale', {
      label, customerId, itemsCount, total, payload: JSON.stringify(payload),
    });

  /** La saca de la espera y devuelve su carrito. Falla si otra caja la retomó antes. */
  const take = (id: string) => post<ResponseObject<HeldSale>>(`HeldSale/${id}/take`, {});

  const discard = (id: string) => del<ResponseObject<boolean>>(`HeldSale/${id}`);

  return { getHeld, hold, take, discard };
};

export default useHeldSales;
