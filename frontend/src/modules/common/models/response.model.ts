import { Message } from '@/modules/common/models/message.model';

export class ResponseBase {
  Message: Message = new Message();
  ok: boolean = false;
}

export class ResponseObject<T> extends ResponseBase {
  Data!: T;
}

export class ResponseArray<T> extends ResponseBase {
  Data!: T[];
}

export class ResponsePaged<T> extends ResponseBase {
  Data!: T[];
  TotalCount: number = 0;
  Page: number = 1;
  PageSize: number = 15;
}
