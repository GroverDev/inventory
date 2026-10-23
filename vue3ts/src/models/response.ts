export class Message {
  public Description: string = ''
  public MessageType: string = ''
  public Id?: string = ''
}

export class ResponseBase {
  Message: Message = new Message()
  ok: boolean = false
}

export class ResponseObject<T> extends ResponseBase {
  Data!: T
}

export class ResponseArray<T> extends ResponseBase {
  Data!: T[]
}

export class ResponsePaged<T> extends ResponseBase {
  Data!: T[]
  TotalCount: number = 0
  Page: number = 1
  PageSize: number = 15
}
