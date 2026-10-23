import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FormAction, FormActionsComponent } from '../form-actions/form-actions.component';

import { MsgService } from '../../core/services/msg.service';
import { SharedModule } from '../shared.module';
import { CRUD_DATA_KEY, CrudConfig, CrudConfigType, CrudEntity, CrudField } from './crud.types';

/** Formulario de alta/edición de un catálogo simple; ver `CrudConfig`. */
@Component({
  selector: 'app-crud-edit',
  imports: [ReactiveFormsModule, SharedModule, FormActionsComponent],
  templateUrl: './crud-edit.component.html',
})
export class CrudEditComponent implements OnInit {
  protected readonly actions: FormAction[] = [
    { label: 'Grabar', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.save(), disabled: () => this.isSaved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);

  protected readonly cfg: CrudConfig<CrudEntity> = inject(this.route.snapshot.data[CRUD_DATA_KEY] as CrudConfigType);

  protected readonly form = new FormGroup<Record<string, FormControl>>({});
  protected readonly submitted = signal(false);
  /** Bloquea el formulario mientras se guarda y tras guardar. */
  protected readonly isSaved = signal(false);

  private model: CrudEntity = this.cfg.newModel();

  protected get isNew(): boolean {
    return !this.model.Id;
  }

  async ngOnInit() {
    for (const field of this.cfg.sections.flatMap(s => s.fields)) {
      this.form.addControl(field.key, new FormControl((this.model as unknown as Record<string, unknown>)[field.key] ?? '', this.validatorsFor(field)));
    }

    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== '0') {
      const resp = await this.cfg.get(id);
      if (resp.ok) {
        this.model = resp.Data;
        this.form.patchValue(resp.Data as unknown as Record<string, unknown>);
      }
    }
  }

  back() {
    this.router.navigate([this.cfg.listRoute]);
  }

  invalid(field: CrudField): boolean {
    const control = this.form.controls[field.key];
    return this.submitted() && control.invalid;
  }

  async save() {
    this.submitted.set(true);
    if (this.form.invalid) return;
    if (!(await this.msg.showMessageQuestion(this.cfg.confirmSave))) return;

    const value = this.form.getRawValue();
    // Los textos se guardan sin espacios sobrantes en los extremos.
    for (const key of Object.keys(value)) {
      if (typeof value[key] === 'string') value[key] = value[key].trim();
    }
    const payload = { ...this.model, ...value } as CrudEntity;

    const creating = this.isNew;
    const resp = creating ? await this.cfg.create(payload) : await this.cfg.update(payload);
    if (!resp.ok) return;

    this.isSaved.set(true);
    await this.msg.showMessageModal({ Description: creating ? this.cfg.created : this.cfg.updated, MessageType: 'success' });
    this.back();
  }

  private validatorsFor(field: CrudField): ValidatorFn[] {
    const validators: ValidatorFn[] = [];
    if (field.required) validators.push(Validators.required);
    if (field.minLength) validators.push(Validators.minLength(field.minLength));
    return validators;
  }
}
