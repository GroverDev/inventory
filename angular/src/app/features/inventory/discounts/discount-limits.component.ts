import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { SharedModule } from '../../../shared/shared.module';
import { DiscountService } from './discount.service';

type Campo = 'CashierMaxPct' | 'CashierMaxAmount' | 'GeneralMaxPct' | 'GeneralMaxAmount';

/** Los inputs trabajan con texto para distinguir "vacío" (hereda) de 0. */
interface Level {
  BranchId: string | null;
  BranchName: string;
  CashierMaxPct: number | string | null;
  CashierMaxAmount: number | string | null;
  GeneralMaxPct: number | string | null;
  GeneralMaxAmount: number | string | null;
}

const CAMPOS: { key: Campo; pct: boolean }[] = [
  { key: 'CashierMaxPct', pct: true },
  { key: 'CashierMaxAmount', pct: false },
  { key: 'GeneralMaxPct', pct: true },
  { key: 'GeneralMaxAmount', pct: false },
];

/** Default del cajero cuando nadie lo configuró: el de appsettings del servidor. */
const DEFAULT_CAJERO: Record<string, string> = { CashierMaxPct: '15', CashierMaxAmount: '50' };

const vacio = (v: unknown) => v === '' || v === null || v === undefined;
const toNumber = (v: unknown): number | null => (vacio(v) ? null : Number(v));

/** Topes del descuento manual por empresa y por sucursal. */
@Component({
  selector: 'app-discount-limits',
  imports: [FormsModule, SharedModule],
  templateUrl: './discount-limits.component.html',
})
export class DiscountLimitsComponent implements OnInit {
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly discountService = inject(DiscountService);

  protected readonly campos = CAMPOS;
  protected readonly canUpdate = computed(() => this.permissions.can('discount-limits', 'update'));
  protected readonly levels = signal<Level[]>([]);
  protected readonly saving = signal(false);

  ngOnInit() {
    return this.load();
  }

  private async load() {
    const { ok, Data } = await this.discountService.getLimits();
    if (!ok) return;
    // La API omite los campos nulos: se normalizan a '' para los inputs.
    this.levels.set(Data.map(l => ({
      BranchId: l.BranchId ?? null,
      BranchName: l.BranchName ?? '',
      CashierMaxPct: l.CashierMaxPct ?? '',
      CashierMaxAmount: l.CashierMaxAmount ?? '',
      GeneralMaxPct: l.GeneralMaxPct ?? '',
      GeneralMaxAmount: l.GeneralMaxAmount ?? '',
    })));
  }

  /** Lo que rige si el campo queda vacío: el de la empresa, o el default. */
  protected heredado(i: number, campo: Campo): string {
    const empresa = this.levels()[0]?.[campo];
    if (i > 0 && !vacio(empresa)) return `empresa: ${empresa}`;
    return DEFAULT_CAJERO[campo] ? `default: ${DEFAULT_CAJERO[campo]}` : 'sin tope';
  }

  protected async save() {
    const payload = this.levels().map(l => ({
      branchId: l.BranchId,
      cashierMaxPct: toNumber(l.CashierMaxPct),
      cashierMaxAmount: toNumber(l.CashierMaxAmount),
      generalMaxPct: toNumber(l.GeneralMaxPct),
      generalMaxAmount: toNumber(l.GeneralMaxAmount),
      branchName: l.BranchName,
    }));

    this.saving.set(true);
    const { ok } = await this.discountService.saveLimits(payload);
    this.saving.set(false);
    if (!ok) return;

    await this.msg.showMessageModal({
      Description: 'Límites guardados. Los puntos de venta abiertos los toman al recargarse.',
      MessageType: 'success',
    });
    await this.load();
  }
}
