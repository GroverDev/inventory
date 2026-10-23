import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { AppModule } from './module.model';

@Injectable({ providedIn: 'root' })
export class ModuleService {
  private readonly api = inject(ApiService);

  getAll(nameModule: string) {
    return this.api.get<ResponseArray<AppModule>>(`Modules?nameModule=${encodeURIComponent(nameModule)}`);
  }

  getById(id: number) {
    return this.api.get<ResponseObject<AppModule>>(`Modules/${id}`);
  }

  create(m: AppModule) {
    return this.api.post<ResponseObject<number>>('Modules', this.body(m));
  }

  update(m: AppModule) {
    return this.api.put<ResponseObject<boolean>>(`Modules/${m.Id}`, { id: m.Id, ...this.body(m) });
  }

  remove(id: number) {
    return this.api.del<ResponseObject<boolean>>(`Modules/${id}`);
  }

  private body(m: AppModule) {
    return { nameModule: m.NameModule, showOrder: m.ShowOrder, route: m.Route, iconCss: m.IconCss };
  }
}
