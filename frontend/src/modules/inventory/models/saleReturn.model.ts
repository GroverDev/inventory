export class SaleReturnDetail {
  public Id: string = '';
  public SaleDetailId: string = '';
  public ProductId: string = '';
  public ProductName: string = '';
  public QuantityReturned: number = 0;
  public UnitPrice: number = 0;
  public LineTotal: number = 0;
}

export class SaleReturn {
  public Id: string = '';
  public SaleId: string = '';
  public ReturnDate: string = '';
  public Reason: string | null = null;
  public TotalReturned: number = 0;
  public IsFullReturn: boolean = false;
  public Detail: SaleReturnDetail[] = [];
}

export class SaleReturnDetailRequest {
  public SaleDetailId: string = '';
  public ProductId: string = '';
  public QuantityReturned: number = 0;
  public UnitPrice: number = 0;
}

export class SaleReturnRequest {
  public SaleId: string = '';
  public Reason: string | null = null;
  public Detail: SaleReturnDetailRequest[] = [];
}
