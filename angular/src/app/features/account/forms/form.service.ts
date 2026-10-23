import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { AppForm } from './form.model';

@Injectable({ providedIn: 'root' })
export class FormService {
  private readonly api = inject(ApiService);

  getAll(nameForm: string) {
    return this.api.get<ResponseArray<AppForm>>(`Forms?nameForm=${encodeURIComponent(nameForm)}`);
  }

  getById(id: number) {
    return this.api.get<ResponseObject<AppForm>>(`Forms/${id}`);
  }

  create(f: AppForm) {
    return this.api.post<ResponseObject<number>>('Forms', this.body(f));
  }

  update(f: AppForm) {
    return this.api.put<ResponseObject<boolean>>(`Forms/${f.Id}`, { id: f.Id, ...this.body(f) });
  }

  remove(id: number) {
    return this.api.del<ResponseObject<boolean>>(`Forms/${id}`);
  }

  private body(f: AppForm) {
    return {
      formId: f.FormId,
      nameForm: f.NameForm,
      description: f.Description,
      orden: f.Orden,
      route: f.Route,
      controller: f.Controller,
      iconCss: f.IconCss,
      showMenu: f.ShowMenu,
      isFormRegister: f.IsFormRegister,
      moduleId: f.ModuleId,
    };
  }
}
