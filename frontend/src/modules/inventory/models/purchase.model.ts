import type { TrackingMode } from '@/modules/inventory/models/product.model';

export class PurchaseDetail {
  public Id: string = '';
  public PurchaseId: string = '';
  public ProductId: string = '';
  public OrderUnitPrice: number = 0;
  public OrderedQuantity: number = 1;
  public OrderFinalPrice: number = 0;
  public DeliveryUnitPrice: number = 0;
  public DeliveredQuantity: number = 0;
  public DeliveryFinalPrice: number = 0;
  public PurchaseStatusId: number = 1;
  public ProductName: string = '';
  /** Acumulado recibido en todas las recepciones de la orden. */
  public ReceivedQuantity: number = 0;
  /** Saldo por recibir. Es el tope de la próxima recepción. */
  public PendingQuantity: number = 0;
  /** Seguimiento del producto: decide si la recepción debe pedir lote. */
  public TrackingMode: TrackingMode = 'none';
}

export class Purchase {
  public Id: string = '';
  public PurchaseDate: string = '';
  public Total: number = 0;
  public IsActive: boolean = true;
  public ProviderId: string = '';
  public EstimatedDeliveryDate: string = '';
  public PurchaseStatusId: number = 1;
  public Detail: PurchaseDetail[] = [];
  public ProviderName: string = '';
  public PurchaseStatusName: string = '';
}

export class PurchaseStatus {
  public Id: number = 0;
  public Description: string = '';
}

/** Estados de una orden de compra. Los deriva el servidor, nunca el usuario. */
export const PURCHASE_STATUS = {
  REQUESTED: 1,
  PARTIALLY_RECEIVED: 2,
  TOTALLY_RECEIVED: 3,
  CANCELLED: 4,
  CLOSED: 5,
} as const;

export class PurchaseDeliveryDetail {
  public Id: string = '';
  public PurchaseDeliveryId: string = '';
  public ProductId: string = '';
  public DeliveryQuantity: number = 0;
  public OrderedQuantity: number = 0;
  public ProductName: string = '';
  /** Precio unitario facturado por el proveedor en esta entrega. */
  public UnitPrice: number = 0;
  /** Solo para la UI: saldo disponible al abrir la pantalla. */
  public PendingQuantity: number = 0;
  /** Solo para la UI: acumulado ya recibido antes de esta entrega. */
  public ReceivedQuantity: number = 0;
  /** Lote recibido. El servidor lo exige si el producto usa `tracking_mode = 'lot'`. */
  public LotCode: string = '';
  /** Vencimiento del lote en ISO (yyyy-MM-dd). Opcional incluso con lotes. */
  public ExpiryDate: string = '';
  /** Solo para la UI: decide si la fila muestra y exige los campos de lote. */
  public TrackingMode: TrackingMode = 'none';
}

export class PurchaseDelivery {
  public Id: string = '';
  public PurchaseId: string = '';
  public IsActive: boolean = true;
  public DeliveryDate: string = '';
  public PurchaseStatusId: number = 0;
  /** Uid de la operación: hace idempotente el reintento de la recepción. */
  public OperationUid: string = '';
  public Detail: PurchaseDeliveryDetail[] = [];
}
