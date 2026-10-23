import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { remixIcon } from '../../../core/utils/icon-map';
import { SharedModule } from '../../../shared/shared.module';
import { AppModule } from './module.model';
import { ModuleService } from './module.service';

/** Módulos del menú: búsqueda por nombre, alta, edición y baja. */
@Component({
  selector: 'app-modules-admin',
  imports: [FormsModule, SharedModule],
  templateUrl: './modules-admin.component.html',
})
export class ModulesAdminComponent {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly service = inject(ModuleService);

  protected readonly modules = signal<AppModule[]>([]);
  protected filter = '';
  protected readonly icon = (css: string) => remixIcon(css, 'ri-apps-line');

  protected async search() {
    const { ok, Data } = await this.service.getAll(this.filter.trim());
    if (ok) this.modules.set(Data);
  }

  protected create() {
    this.router.navigate(['module-edit', '0']);
  }

  protected edit(m: AppModule) {
    this.router.navigate(['module-edit', m.Id]);
  }

  protected async remove(id: number) {
    if (!(await this.msg.showMessageQuestion('¿Desea eliminar el módulo?'))) return;
    const { ok } = await this.service.remove(id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'El módulo se eliminó correctamente.', MessageType: 'success' });
      await this.search();
    }
  }
}
