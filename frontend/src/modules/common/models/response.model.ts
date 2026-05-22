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
