import { Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { SharedModule } from '../../../shared/shared.module';
import { AdminService } from './admin.service';

/** Borra los datos de negocio de la farmacia actual, conservando usuarios y permisos. */
@Component({
  selector: 'app-company-reset',
  imports: [FormsModule, SharedModule],
  templateUrl: './company-reset.component.html',
})
export class CompanyResetComponent {
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly msg = inject(MsgService);
  private readonly admin = inject(AdminService);

  protected readonly expectedPhrase = 'RESETEAR EMPRESA';

  protected currentPassword = '';
  protected confirmationPhrase = '';
  protected createBackup = true;

  protected get phraseOk() {
    return this.confirmationPhrase.trim() === this.expectedPhrase;
  }

  protected get canSubmit() {
    return this.currentPassword.length > 0 && this.phraseOk;
  }

  protected goBack() {
    this.location.back();
  }

  protected async onSubmit() {
    if (!this.canSubmit) return;

    const confirmed = await this.msg.showMessageQuestion(
      '¿Está TOTALMENTE seguro? Se eliminarán todos los datos de negocio de esta farmacia de forma irreversible.');
    if (!confirmed) return;

    const { ok, Message } = await this.admin.resetCompany({
      CurrentPassword: this.currentPassword,
      ConfirmationPhrase: this.confirmationPhrase.trim(),
      SkipBackup: !this.createBackup,
    });

    if (ok) {
      await this.msg.showMessage(Message);
      // La sesión sigue siendo válida: el reinicio conserva los usuarios. Se vuelve
      // al inicio porque las pantallas abiertas quedaron con datos que ya no existen.
      this.router.navigate(['/']);
    }
  }
}
