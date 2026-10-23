import { useApi } from '@/api/useApi';
import type { ResponseObject } from '@/models/response';

/** Topes del descuento manual vigentes en la sucursal activa. */
export interface PosSettings {
  /** Tope del cajero: por encima pide autorización de supervisor. */
  MaxCashierDiscountPct: number;
  MaxCashierDiscountAmount: number;
  /** Tope máximo para cualquier rol, ni con autorización. Ausente = sin tope. */
  MaxDiscountPct?: number | null;
  MaxDiscountAmount?: number | null;
}

const { get } = useApi();

const usePosSettings = () => {
  const getPosSettings = () =>
    get<ResponseObject<PosSettings>>('Settings/pos');

  return { getPosSettings };
};

export default usePosSettings;
