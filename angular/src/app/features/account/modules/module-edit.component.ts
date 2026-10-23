import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { remixIcon } from '../../../core/utils/icon-map';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';
import { SharedModule } from '../../../shared/shared.module';
import { AppModule, newModule } from './module.model';
import { ModuleService } from './module.service';

@Component({
  selector: 'app-module-edit',
  imports: [FormsModule, SharedModule, FormActionsComponent],
  templateUrl: './module-edit.component.html',
})
export class ModuleEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly service = inject(ModuleService);

  protected module: AppModule = newModule();
  protected readonly isSaved = signal(false);
  protected submitted = false;

  protected readonly actions: FormAction[] = [
    { label: 'Grabar', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.save(), disabled: () => this.isSaved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  protected get iconClass() {
    return remixIcon(this.module.IconCss, 'ri-apps-line');
  }

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== '0') {
      const { ok, Data } = await this.service.getById(parseInt(id, 10));
      if (ok) this.module = Data;
    }
  }

  protected back() {
    this.router.navigate(['modules-admin']);
  }

  protected async save() {
    this.submitted = true;
    this.module.NameModule = this.module.NameModule.trim();
    this.module.Route = this.module.Route.trim();
    if (!this.module.NameModule || !this.module.Route) return;
    if (!(await this.msg.showMessageQuestion('¿Desea guardar el módulo?'))) return;

    if (this.module.Id === 0) {
      const { ok, Data } = await this.service.create(this.module);
      if (!ok) return;
      this.isSaved.set(true);
      this.module.Id = Data;
      await this.msg.showMessageModal({ Description: 'El módulo se creó correctamente.', MessageType: 'success' });
    } else {
      const { ok, Data } = await this.service.update(this.module);
      if (!ok || !Data) return;
      await this.msg.showMessageModal({ Description: 'El módulo se actualizó correctamente.', MessageType: 'success' });
    }
    this.back();
  }
}
