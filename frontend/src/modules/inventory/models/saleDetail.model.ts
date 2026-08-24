export class SaleDetail {
  public Id: string = '';
  public SaleId: string = '';
  public ProductId: string = '';
  public Quantity: number = 0;
  public UnitPrice: number = 0;
  public LineSubtotal: number = 0;
  public LineTotalDiscounts: number = 0;
  public LineTotal: number = 0;
  /**
   * Precio unitario efectivamente cobrado: el de lista menos el descuento de la
   * línea y menos la parte prorrateada del descuento global. Lo calcula el
   * servidor; es el que se reembolsa al devolver.
   */
  public EffectiveUnitPrice: number = 0;
  public ProductName: string = '';
  public LaboratoryName: string = '';
  /**
   * Lote del que salió la línea, elegido por FEFO al vender. Vacío si el
   * producto no usa lotes. Una venta que abarca varios lotes se parte en una
   * línea por lote, así que este campo identifica exactamente qué se entregó:
   * es lo que permite responder a un retiro del laboratorio.
   */
  public LotCode: string | null = null;
  public ExpiryDate: string | null = null;
  /** Número de serie de la unidad entregada. Responde un reclamo de garantía. */
  public SerialNumber: string | null = null;
  /**
   * Series elegidas en el mostrador al vender un producto serializado. Vacío
   * significa que el servidor elige por FEFO, que es lo correcto para lo
   * intercambiable pero no para una unidad con garantía.
   */
  public SerialNumbers: string[] = [];
  public isSelected: boolean = false;
  // Discount tracking (DiscountId is sent to the API)
  public DiscountId: string = '';
  // UI-only fields (used for recalculation when qty changes)
  public DiscountLabel: string = '';
  public DiscountType: string = '';   // 'Percentage' | 'FixedAmount'
  public DiscountValue: number = 0;
}


