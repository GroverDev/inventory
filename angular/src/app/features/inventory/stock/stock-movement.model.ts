export interface StockMovementResponse {
  Id: string;
  ProductId: string;
  ProductName: string;
  ProductCode: string;
  StockItemId: string;
  /** Vacío cuando el movimiento es de la existencia implícita (sin lote). */
  LotCode: string | null;
  ExpiryDate: string | null;
  MovementType: string;
  Quantity: number;
  StockBefore: number;
  StockAfter: number;
  Reason: string | null;
  Observation: string | null;
  ReferenceId: string | null;
  ReferenceType: string | null;
  Created: string;
  CreatedBy: number;
}

/** Urgencia con que hay que rotar una existencia. La calcula el servidor. */
export type ExpiryStatus = 'VENCIDO' | 'CRITICO' | 'PROXIMO' | 'VIGENTE';

/** Una existencia con vencimiento, tal como la devuelve `StockMovement/expiring`. */
export interface StockExpiryResponse {
  StockItemId: string;
  ProductId: string;
  ProductCode: string;
  ProductName: string;
  LotCode: string;
  ExpiryDate: string;
  Quantity: number;
  /** Negativo si ya venció. */
  DiasRestantes: number;
  Estado: ExpiryStatus;
  /** A precio de venta: lo que se pierde si no se rota a tiempo. */
  ValorEnRiesgo: number;
}

/**
 * Una venta de un lote concreto, con el cliente que se la llevó. Es la consulta
 * de un retiro de mercado: sin los datos de contacto no sirve de nada.
 */
export interface LotTraceabilityResponse {
  LotCode: string;
  /** Número de serie, cuando la existencia se identifica unidad por unidad. */
  SerialNumber: string;
  ExpiryDate: string | null;
  ProductCode: string;
  ProductName: string;
  SaleId: string;
  SaleDate: string;
  Quantity: number;
  Cliente: string;
  DocumentNumber: string | null;
  Cellphone: string | null;
  /** Sucursal donde se hizo la venta: el retiro se busca en toda la farmacia. */
  BranchName: string;
}

/** Una unidad serializada disponible para vender. */
export interface StockSerialResponse {
  StockItemId: string;
  SerialNumber: string;
  ExpiryDate: string | null;
}

export interface StockAdjustmentRequest {
  ProductId: string;
  Quantity: number;
  Reason: string;
  Observation: string;
}

/** Dar de baja una existencia puntual (lote vencido/dañado/retirado). */
export interface StockWriteOffRequest {
  ProductId: string;
  StockItemId: string;
  Quantity: number;
  Reason: string;
  Observation: string;
}

/** Un evento de baja por vencimiento/pérdida, tal como lo devuelve el reporte de mermas. */
export interface WriteOffDetailResponse {
  ProductId: string;
  ProductCode: string;
  ProductName: string;
  LotCode: string | null;
  ExpiryDate: string | null;
  Cantidad: number;
  ValorPerdido: number;
  Reason: string | null;
  Observation: string | null;
  Created: string;
  CreatedBy: number;
  /** Sucursal donde se registró la merma. */
  BranchName: string;
}

/** Mermas acumuladas de un producto en el período consultado. */
export interface WriteOffByProductResponse {
  ProductId: string;
  ProductCode: string;
  ProductName: string;
  Unidades: number;
  ValorPerdido: number;
  Eventos: number;
}

/** Reporte de mermas por vencimiento en un período. */
export interface WriteOffReportResponse {
  TotalUnidades: number;
  TotalValorPerdido: number;
  TotalEventos: number;
  PorProducto: WriteOffByProductResponse[];
  Detalle: WriteOffDetailResponse[];
}
