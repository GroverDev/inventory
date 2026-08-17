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

/**
 * Una venta de un lote concreto, con el cliente que se la llevó. Es la consulta
 * de un retiro de mercado: sin los datos de contacto no sirve de nada.
 */
export class LotTraceabilityResponse {
  LotCode: string = ''
  /** Número de serie, cuando la existencia se identifica unidad por unidad. */
  SerialNumber: string = ''
  ExpiryDate: string | null = null
  ProductCode: string = ''
  ProductName: string = ''
  SaleId: string = ''
  SaleDate: string = ''
  Quantity: number = 0
  Cliente: string = ''
  DocumentNumber: string | null = null
  Cellphone: string | null = null
}

/** Una unidad serializada disponible para vender. */
export class StockSerialResponse {
  StockItemId: string = ''
  SerialNumber: string = ''
  ExpiryDate: string | null = null
}

export class StockAdjustmentRequest {
  ProductId: string = ''
  Quantity: number = 0
  Reason: string = ''
  Observation: string = ''
}
