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

export class PurchaseDeliveryDetail {
  public Id: string = '';
  public PurchaseDeliveryId: string = '';
  public ProductId: string = '';
  public DeliveryQuantity: number = 0;
  public OrderedQuantity: number = 0;
  public ProductName: string = '';
}

export class PurchaseDelivery {
  public Id: string = '';
  public PurchaseId: string = '';
  public IsActive: boolean = true;
  public DeliveryDate: string = '';
  public PurchaseStatusId: number = 3;
  public Detail: PurchaseDeliveryDetail[] = [];
}
