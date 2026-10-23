import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { SharedModule } from '../../../shared/shared.module';
import { AppForm } from './form.model';
import { FormService } from './form.service';

/** Formularios del sistema (pantallas con permisos): búsqueda, alta, edición y baja. */
@Component({
  selector: 'app-forms-admin',
  imports: [FormsModule, SharedModule],
  templateUrl: './forms-admin.component.html',
})
export class FormsAdminComponent {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly service = inject(FormService);

  protected readonly forms = signal<AppForm[]>([]);
  protected filter = '';

  protected async search() {
    const { ok, Data } = await this.service.getAll(this.filter.trim());
    if (ok) this.forms.set(Data);
  }

  protected create() {
    this.router.navigate(['form-edit', '0']);
  }

  protected edit(f: AppForm) {
    this.router.navigate(['form-edit', f.Id]);
  }

  protected async remove(id: number) {
    if (!(await this.msg.showMessageQuestion('¿Desea eliminar el formulario?'))) return;
    const { ok } = await this.service.remove(id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'El formulario se eliminó correctamente.', MessageType: 'success' });
      await this.search();
    }
  }
}
