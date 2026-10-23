import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { remixIcon } from '../../../core/utils/icon-map';
import { SharedModule } from '../../../shared/shared.module';
import { PaymentMethod } from '../sales/sale.model';
import { CashService } from './cash.service';

/** Reglas del cierre de caja de la empresa: medios que se arquean, topes de observación y de supervisor. */
@Component({
  selector: 'app-cash-close-settings',
  imports: [FormsModule, SharedModule],
  templateUrl: './cash-close-settings.component.html',
})
export class CashCloseSettingsComponent implements OnInit {
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly cashService = inject(CashService);

  protected readonly canUpdate = computed(() => this.permissions.can('cash-close-settings', 'update'));
  protected readonly icon = (fa: string) => remixIcon(fa);

  protected readonly methods = signal<PaymentMethod[]>([]);
  protected readonly saving = signal(false);
  protected noteThreshold: number | null = 10;
  // '' = sin tope / sin límite (se guarda null).
  protected supervisorThreshold: number | '' | null = '';
  protected maxAttempts: number | '' | null = '';
  protected requireDenominations = false;

  protected get showMobileNotice(): boolean {
    return (this.supervisorThreshold !== '' && this.supervisorThreshold !== null) || (this.maxAttempts !== '' && this.maxAttempts !== null) || this.requireDenominations;
  }

  async ngOnInit() {
    const { ok, Data } = await this.cashService.getCloseSettings();
    if (!ok) return;
    this.noteThreshold = Data.NoteThreshold;
    this.supervisorThreshold = Data.SupervisorThreshold ?? '';
    this.maxAttempts = Data.MaxAttempts ?? '';
    this.requireDenominations = Data.RequireDenominations;
    this.methods.set(Data.Methods);
  }

  private optional(v: number | '' | null): number | null {
    return v === '' || v === null ? null : Number(v);
  }

  protected async save() {
    const warn = (Description: string) => this.msg.showMessageModal({ Description, MessageType: 'warning' });

    if (!((this.noteThreshold ?? -1) >= 0)) return warn('El monto no puede ser negativo.');
    const tope = this.optional(this.supervisorThreshold);
    if (tope !== null && tope < (this.noteThreshold ?? 0)) {
      return warn('El monto que pide supervisor no puede ser menor al que pide observación.');
    }
    const intentos = this.optional(this.maxAttempts);
    if (intentos !== null && (!Number.isInteger(intentos) || intentos < 1)) {
      return warn('Los intentos tienen que ser un número entero desde 1.');
    }

    this.saving.set(true);
    const { ok } = await this.cashService.saveCloseSettings({
      noteThreshold: this.noteThreshold!,
      supervisorThreshold: tope,
      maxAttempts: intentos,
      requireDenominations: this.requireDenominations,
      methods: this.methods().map(m => ({ id: m.Id, requiresCount: m.RequiresCount })),
    });
    this.saving.set(false);
    if (ok) {
      await this.msg.showMessageModal({
        Description: 'Configuración guardada. Los puntos de venta la toman al recargarse.',
        MessageType: 'success',
      });
    }
  }
}
