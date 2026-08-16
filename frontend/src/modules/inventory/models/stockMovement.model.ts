export class StockMovementResponse {
  Id: string = ''
  ProductId: string = ''
  ProductName: string = ''
  ProductCode: string = ''
  MovementType: string = ''
  Quantity: number = 0
  StockBefore: number = 0
  StockAfter: number = 0
  Reason: string | null = null
  Observation: string | null = null
  ReferenceId: string | null = null
  ReferenceType: string | null = null
  Created: string = ''
  CreatedBy: number = 0
}

/** Urgencia con que hay que rotar una existencia. La calcula el servidor. */
export type ExpiryStatus = 'VENCIDO' | 'CRITICO' | 'PROXIMO' | 'VIGENTE';

/** Una existencia con vencimiento, tal como la devuelve `StockMovement/expiring`. */
export class StockExpiryResponse {
  StockItemId: string = ''
  ProductId: string = ''
  ProductCode: string = ''
  ProductName: string = ''
  LotCode: string = ''
  ExpiryDate: string = ''
  Quantity: number = 0
  /** Negativo si ya venció. */
  DiasRestantes: number = 0
  Estado: ExpiryStatus = 'VIGENTE'
  /** A precio de venta: lo que se pierde si no se rota a tiempo. */
  ValorEnRiesgo: number = 0
}

export class StockAdjustmentRequest {
  ProductId: string = ''
  Quantity: number = 0
  Reason: string = ''
  Observation: string = ''
}
