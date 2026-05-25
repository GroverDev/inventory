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

export class StockAdjustmentRequest {
  ProductId: string = ''
  Quantity: number = 0
  Reason: string = ''
  Observation: string = ''
}
