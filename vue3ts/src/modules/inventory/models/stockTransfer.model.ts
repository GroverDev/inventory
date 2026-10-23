import type { TrackingMode } from '@/modules/inventory/models/product.model';

export type TransferStatus = 'borrador' | 'enviado' | 'recibido' | 'anulado';

/** Lo que viajó de una existencia del origen. */
export interface StockTransferLot {
  Id: string;
  DetailId: string;
  LotCode: string;
  ExpiryDate: string | null;
  SerialNumber: string;
  QuantitySent: number;
  QuantityReceived: number | null;
}

export interface StockTransferDetail {
  Id: string;
  ProductId: string;
  ProductCode: string;
  ProductName: string;
  TrackingMode: TrackingMode;
  Quantity: number;
  /** Stock actual del producto en el origen. */
  AvailableAtOrigin: number;
  Lots: StockTransferLot[];
}

export interface StockTransfer {
  Id: string;
  Number: number;
  OriginBranchId: string;
  OriginBranchName: string;
  DestBranchId: string;
  DestBranchName: string;
  Status: TransferStatus;
  Notes: string;
  /** 'salida' si la sucursal activa es el origen, 'entrada' si es el destino. */
  Direction: 'salida' | 'entrada';
  Created: string;
  CreatedByName: string;
  SentAt: string | null;
  SentByName: string;
  ReceivedAt: string | null;
  ReceivedByName: string;
  LinesCount: number;
  TotalQuantity: number;
  Detail: StockTransferDetail[];
}

/** Línea del borrador mientras se arma en pantalla. */
export interface TransferLine {
  ProductId: string;
  ProductCode: string;
  ProductName: string;
  /** Stock en la sucursal activa (el origen) al momento de agregarlo. */
  Available: number;
  Quantity: number;
}

export const transferStatusLabel: Record<TransferStatus, string> = {
  borrador: 'Borrador',
  enviado: 'En tránsito',
  recibido: 'Recibido',
  anulado: 'Anulado',
};

export const transferStatusBadge: Record<TransferStatus, string> = {
  borrador: 'bg-secondary',
  enviado: 'bg-warning text-dark',
  recibido: 'bg-success',
  anulado: 'bg-light text-muted border',
};
