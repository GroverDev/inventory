import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { remixIcon } from '../../../core/utils/icon-map';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';
import { SharedModule } from '../../../shared/shared.module';
import { AppModule } from '../modules/module.model';
import { ModuleService } from '../modules/module.service';
import { AppForm, newForm } from './form.model';
import { FormService } from './form.service';

@Component({
  selector: 'app-form-edit',
  imports: [FormsModule, SharedModule, FormActionsComponent],
  templateUrl: './form-edit.component.html',
})
export class FormEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly service = inject(FormService);
  private readonly moduleService = inject(ModuleService);

  protected form: AppForm = newForm();
  protected modules: AppModule[] = [];
  protected parentForms: AppForm[] = [];
  protected readonly isSaved = signal(false);
  protected submitted = false;

  protected readonly actions: FormAction[] = [
    { label: 'Grabar', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.save(), disabled: () => this.isSaved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  protected get iconClass() {
    return remixIcon(this.form.IconCss, 'ri-file-line');
  }

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== '0') {
      const { ok, Data } = await this.service.getById(parseInt(id, 10));
      if (ok) this.form = Data;
    }

    const [mods, forms] = await Promise.all([this.moduleService.getAll(''), this.service.getAll('')]);
    if (mods.ok) this.modules = mods.Data;
    if (forms.ok) this.parentForms = forms.Data.filter(f => f.Id !== this.form.Id);
  }

  protected back() {
    this.router.navigate(['forms-admin']);
  }

  protected async save() {
    this.submitted = true;
    this.form.NameForm = this.form.NameForm.trim();
    this.form.Route = this.form.Route.trim();
    this.form.Controller = this.form.Controller.trim();
    // El select devuelve null al elegir la opción vacía; la API espera 0.
    this.form.ModuleId = this.form.ModuleId || 0;
    this.form.FormId = this.form.FormId || 0;

    if (!this.form.NameForm || !this.form.Route || !this.form.Controller) return;
    if (!(await this.msg.showMessageQuestion('¿Desea guardar el formulario?'))) return;

    if (this.form.Id === 0) {
      const { ok, Data } = await this.service.create(this.form);
      if (!ok) return;
      this.isSaved.set(true);
      this.form.Id = Data;
      await this.msg.showMessageModal({ Description: 'El formulario se creó correctamente.', MessageType: 'success' });
    } else {
      const { ok, Data } = await this.service.update(this.form);
      if (!ok || !Data) return;
      await this.msg.showMessageModal({ Description: 'El formulario se actualizó correctamente.', MessageType: 'success' });
    }
    this.back();
  }
}
