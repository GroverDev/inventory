import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MsgService } from '../../core/services/msg.service';
import { SharedModule } from '../shared.module';
import { CRUD_DATA_KEY, CrudColumn, CrudConfig, CrudConfigType, CrudEntity } from './crud.types';

/** Lista con búsqueda por nombre de un catálogo simple; ver `CrudConfig`. */
@Component({
  selector: 'app-crud-list',
  imports: [FormsModule, SharedModule],
  templateUrl: './crud-list.component.html',
})
export class CrudListComponent {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);

  protected readonly cfg: CrudConfig<CrudEntity> =
    inject(inject(ActivatedRoute).snapshot.data[CRUD_DATA_KEY] as CrudConfigType);

  protected readonly items = signal<CrudEntity[]>([]);
  protected filter = '';

  async search() {
    const resp = await this.cfg.list(this.filter.trim());
    if (resp.ok) this.items.set(resp.Data ?? []);
  }

  create() {
    this.router.navigate([this.cfg.editRoute, '0']);
  }

  edit(item: CrudEntity) {
    this.router.navigate([this.cfg.editRoute, item.Id]);
  }

  async remove(item: CrudEntity) {
    if (!(await this.msg.showMessageQuestion(this.cfg.confirmDelete))) return;

    const resp = await this.cfg.remove(item.Id);
    if (resp.ok) {
      await this.msg.showMessageModal({ Description: this.cfg.deleted, MessageType: 'success' });
      await this.search();
    }
  }

  protected value(item: CrudEntity, col: CrudColumn): unknown {
    return this.prop(item, col.key);
  }

  protected prop(item: CrudEntity, key: string): unknown {
    return (item as unknown as Record<string, unknown>)[key];
  }
}
