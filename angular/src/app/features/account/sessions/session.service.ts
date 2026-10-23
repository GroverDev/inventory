import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { ConnectedUser, Session } from './session.model';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly api = inject(ApiService);

  getConnectedUsers() {
    return this.api.get<ResponseArray<ConnectedUser>>('Sessions/connected');
  }

  getUserSessions(uuid: string) {
    return this.api.get<ResponseArray<Session>>(`Sessions/user/${uuid}`);
  }

  close(id: number) {
    return this.api.del<ResponseObject<boolean>>(`Sessions/${id}`);
  }

  closeAllUserSessions(uuid: string) {
    return this.api.del<ResponseObject<boolean>>(`Sessions/user/${uuid}`);
  }
}
