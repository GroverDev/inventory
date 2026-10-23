import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';
import { SharedModule } from '../../../shared/shared.module';
import { AdminService, CreateTenantPayload, CreateTenantResult } from './admin.service';

/** Mismo patrón que exige el validador del servidor. */
const SLUG_PATTERN = /^[a-z0-9]+(-[a-z0-9]+)*$/;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const emptyForm = (): CreateTenantPayload => ({ Name: '', Slug: '', AdminEmail: '', AdminFullName: '', AdminPassword: '' });

/** Alta de una farmacia nueva (operación de plataforma). */
@Component({
  selector: 'app-company-create',
  imports: [FormsModule, SharedModule, FormActionsComponent],
  templateUrl: './company-create.component.html',
})
export class CompanyCreateComponent {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly admin = inject(AdminService);

  protected form = emptyForm();
  protected submitted = false;
  protected readonly saving = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly created = signal<CreateTenantResult | null>(null);
  /** El identificador deja de seguir al nombre en cuanto se edita a mano. */
  private slugTouched = false;

  protected readonly actions: FormAction[] = [
    { label: 'Crear empresa', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.create(), disabled: () => this.saving() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  protected get nameInvalid() { return !this.form.Name.trim(); }
  protected get slugRequired() { return !this.form.Slug.trim(); }
  protected get slugFormat() { return !SLUG_PATTERN.test(this.form.Slug.trim()); }
  protected get fullNameInvalid() { return !this.form.AdminFullName.trim(); }
  protected get emailRequired() { return !this.form.AdminEmail.trim(); }
  protected get emailFormat() { return !EMAIL_PATTERN.test(this.form.AdminEmail.trim()); }
  protected get passwordInvalid() { return this.form.AdminPassword.length < 8; }

  /** Propone el identificador a partir del nombre: nadie quiere tipearlo dos veces. */
  protected onName() {
    if (this.slugTouched) return;
    this.form.Slug = this.form.Name
      .toLowerCase()
      .normalize('NFD').replace(/[̀-ͯ]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }

  protected onSlugInput() {
    this.slugTouched = true;
  }

  protected async create() {
    this.submitted = true;
    // Se marcan todos los campos para que el que falta se pinte de rojo con su
    // motivo, en vez de dejar el botón apagado sin explicar por qué.
    if (this.nameInvalid || this.slugRequired || this.slugFormat || this.fullNameInvalid
      || this.emailRequired || this.emailFormat || this.passwordInvalid) return;

    const payload: CreateTenantPayload = {
      Name: this.form.Name.trim(),
      Slug: this.form.Slug.trim(),
      AdminFullName: this.form.AdminFullName.trim(),
      AdminEmail: this.form.AdminEmail.trim(),
      AdminPassword: this.form.AdminPassword,
    };
    if (!(await this.msg.showMessageQuestion(`¿Crear la farmacia "${payload.Name}" con administrador ${payload.AdminEmail}?`))) return;

    this.saving.set(true);
    try {
      const { ok, Data } = await this.admin.createTenant(payload);
      if (ok) this.created.set(Data);
    } finally {
      this.saving.set(false);
    }
  }

  protected another() {
    this.form = emptyForm();
    this.slugTouched = false;
    this.submitted = false;
    this.created.set(null);
  }

  protected back() {
    this.router.navigate(['/']);
  }
}
