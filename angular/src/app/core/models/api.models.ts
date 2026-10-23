export class Message {
  Description = '';
  MessageType = '';
  Id?: string = '';
}

export class ResponseBase {
  Message: Message = new Message();
  ok = false;
}

export class ResponseObject<T> extends ResponseBase {
  Data!: T;
}

export class ResponseArray<T> extends ResponseBase {
  Data!: T[];
}

export class ResponsePaged<T> extends ResponseBase {
  Data!: T[];
  TotalCount = 0;
  Page = 1;
  PageSize = 15;
}
