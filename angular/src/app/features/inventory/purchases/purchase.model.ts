import { TrackingMode } from '../products/product.model';

export interface PurchaseDetail {
  Id: string;
  PurchaseId: string;
  ProductId: string;
  OrderUnitPrice: number;
  OrderedQuantity: number;
  OrderFinalPrice: number;
  DeliveryUnitPrice: number;
  DeliveredQuantity: number;
  DeliveryFinalPrice: number;
  PurchaseStatusId: number;
  ProductName: string;
  /** Acumulado recibido en todas las recepciones de la orden. */
  ReceivedQuantity: number;
  /** Saldo por recibir. Es el tope de la próxima recepción. */
  PendingQuantity: number;
  /** Seguimiento del producto: decide si la recepción debe pedir lote. */
  TrackingMode: TrackingMode;
}

export const newPurchaseDetail = (): PurchaseDetail => ({
  Id: '',
  PurchaseId: '',
  ProductId: '',
  OrderUnitPrice: 0,
  OrderedQuantity: 1,
  OrderFinalPrice: 0,
  DeliveryUnitPrice: 0,
  DeliveredQuantity: 0,
  DeliveryFinalPrice: 0,
  PurchaseStatusId: 1,
  ProductName: '',
  ReceivedQuantity: 0,
  PendingQuantity: 0,
  TrackingMode: 'none',
});

export interface Purchase {
  Id: string;
  PurchaseDate: string;
  Total: number;
  IsActive: boolean;
  ProviderId: string;
  EstimatedDeliveryDate: string;
  PurchaseStatusId: number;
  Detail: PurchaseDetail[];
  ProviderName: string;
  /** Sucursal que hizo el pedido (reportes consolidados). */
  BranchName?: string;
  PurchaseStatusName: string;
}

export const newPurchase = (): Purchase => ({
  Id: '',
  PurchaseDate: '',
  Total: 0,
  IsActive: true,
  ProviderId: '',
  EstimatedDeliveryDate: '',
  PurchaseStatusId: 1,
  Detail: [],
  ProviderName: '',
  PurchaseStatusName: '',
});

export interface PurchaseStatus {
  Id: number;
  Description: string;
}

/** Estados de una orden de compra. Los deriva el servidor, nunca el usuario. */
export const PURCHASE_STATUS = {
  REQUESTED: 1,
  PARTIALLY_RECEIVED: 2,
  TOTALLY_RECEIVED: 3,
  CANCELLED: 4,
  CLOSED: 5,
} as const;

export interface PurchaseDeliveryDetail {
  Id: string;
  PurchaseDeliveryId: string;
  ProductId: string;
  DeliveryQuantity: number;
  OrderedQuantity: number;
  ProductName: string;
  /** Precio unitario facturado por el proveedor en esta entrega. */
  UnitPrice: number;
  /** Solo para la UI: saldo disponible al abrir la pantalla. */
  PendingQuantity: number;
  /** Solo para la UI: acumulado ya recibido antes de esta entrega. */
  ReceivedQuantity: number;
  /** Lote recibido. El servidor lo exige si el producto usa `tracking_mode = 'lot'`. */
  LotCode: string;
  /** Vencimiento del lote en ISO (yyyy-MM-dd). Opcional incluso con lotes. */
  ExpiryDate: string;
  /** Solo para la UI: decide si la fila muestra y exige los campos de lote. */
  TrackingMode: TrackingMode;
  /**
   * Números de serie recibidos, uno por unidad. El servidor exige que sean
   * tantos como unidades cuando el producto se identifica por serie.
   */
  SerialNumbers: string[];
}

export interface PurchaseDelivery {
  Id: string;
  PurchaseId: string;
  IsActive: boolean;
  DeliveryDate: string;
  PurchaseStatusId: number;
  /** Uid de la operación: hace idempotente el reintento de la recepción. */
  OperationUid: string;
  Detail: PurchaseDeliveryDetail[];
}

export const newPurchaseDelivery = (): PurchaseDelivery => ({
  Id: '',
  PurchaseId: '',
  IsActive: true,
  DeliveryDate: '',
  PurchaseStatusId: 0,
  OperationUid: '',
  Detail: [],
});

/**
 * Color del estado en variantes `subtle` + `emphasis`: el tema oscuro las
 * redefine, así que el par fondo/texto se recalcula solo al cambiar de tema.
 */
export const purchaseStatusBadge = (statusId: number): string => {
  const base = 'badge border';
  if (statusId === PURCHASE_STATUS.TOTALLY_RECEIVED) return `${base} bg-success-subtle text-success-emphasis border-success-subtle`;
  if (statusId === PURCHASE_STATUS.PARTIALLY_RECEIVED) return `${base} bg-warning-subtle text-warning-emphasis border-warning-subtle`;
  if (statusId === PURCHASE_STATUS.CANCELLED) return `${base} bg-danger-subtle text-danger-emphasis border-danger-subtle`;
  if (statusId === PURCHASE_STATUS.CLOSED) return `${base} bg-secondary-subtle text-secondary-emphasis border-secondary-subtle`;
  return `${base} bg-info-subtle text-info-emphasis border-info-subtle`;
};

export const purchaseStatusLabel = (statusId: number): string => {
  if (statusId === PURCHASE_STATUS.TOTALLY_RECEIVED) return 'Tot. Recibido';
  if (statusId === PURCHASE_STATUS.PARTIALLY_RECEIVED) return 'Parc. Recibido';
  if (statusId === PURCHASE_STATUS.CANCELLED) return 'Cancelado';
  if (statusId === PURCHASE_STATUS.CLOSED) return 'Cerrado';
  return 'Solicitado';
};
