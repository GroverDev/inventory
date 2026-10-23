import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MsgService } from '../../../core/services/msg.service';
import { SharedModule } from '../../../shared/shared.module';
import { Discount, DiscountService, newDiscount } from './discount.service';

/** Descuentos del catálogo: lista y alta/edición en un modal. */
@Component({
  selector: 'app-discounts-admin',
  imports: [FormsModule, SharedModule],
  templateUrl: './discounts-admin.component.html',
})
export class DiscountsAdminComponent implements OnInit {
  private readonly msg = inject(MsgService);
  private readonly discountService = inject(DiscountService);

  protected readonly discounts = signal<Discount[]>([]);
  protected readonly loading = signal(false);
  protected readonly showModal = signal(false);
  protected readonly saving = signal(false);
  protected form: Discount = newDiscount();
  protected formErrors = { Name: false, Type: false, Value: false };

  ngOnInit() {
    return this.load();
  }

  protected formatNum(val: number): string {
    return (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  protected get valueErrorMsg(): string {
    if (!this.form.Value || this.form.Value <= 0) return 'El valor debe ser mayor a cero.';
    if (this.form.Type === 'Percentage' && this.form.Value > 100) return 'No puede superar el 100%.';
    return '';
  }

  private async load() {
    this.loading.set(true);
    const { Data } = await this.discountService.getAll();
    this.discounts.set(Data ?? []);
    this.loading.set(false);
  }

  protected openModal(discount?: Discount) {
    this.formErrors = { Name: false, Type: false, Value: false };
    if (discount) {
      this.form = { ...discount };
    } else {
      this.form = { ...newDiscount(), Type: 'Percentage', IsActive: true };
    }
    this.showModal.set(true);
  }

  protected closeModal() {
    this.showModal.set(false);
  }

  private validate(): boolean {
    const f = this.form;
    this.formErrors.Name = !f.Name;
    this.formErrors.Type = !f.Type;
    this.formErrors.Value = !f.Value || f.Value <= 0 || (f.Type === 'Percentage' && f.Value > 100);
    return !this.formErrors.Name && !this.formErrors.Type && !this.formErrors.Value;
  }

  protected async save() {
    if (!this.validate()) return;
    const confirmed = await this.msg.showMessageQuestion(this.form.Id ? '¿Desea guardar los cambios?' : '¿Desea crear el descuento?');
    if (!confirmed) return;

    this.saving.set(true);
    try {
      const editing = !!this.form.Id;
      const { ok } = editing
        ? await this.discountService.update(this.form.Id, this.form)
        : await this.discountService.create(this.form);
      if (ok) {
        await this.msg.showMessageModal({
          Description: editing ? 'Descuento actualizado correctamente.' : 'Descuento creado correctamente.',
          MessageType: 'success',
        });
        this.closeModal();
        await this.load();
      }
    } finally {
      this.saving.set(false);
    }
  }

  protected async remove(id: string) {
    if (!(await this.msg.showMessageQuestion('¿Desea eliminar este descuento?'))) return;

    const { ok } = await this.discountService.remove(id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'Descuento eliminado correctamente.', MessageType: 'success' });
      await this.load();
    }
  }
}
