import { Component, computed, inject, input, model } from '@angular/core';

import { AuthService } from '../../core/services/auth.service';

const ALL = 'all';

/**
 * Sucursal de un reporte. '' = la activa, un id = esa, 'all' = consolidado. Ofrece
 * solo las sucursales en las que el usuario está habilitado; el backend lo vuelve a
 * validar (BranchScope). Con una sola sucursal no hay nada que elegir: no se muestra.
 */
@Component({
  selector: 'app-branch-filter',
  template: `
    @if (branches().length > 1) {
      <div [class]="colClass()">
        <label class="form-label" [for]="id">Sucursal</label>
        <select [id]="id" class="form-select" [value]="value()" (change)="value.set($any($event.target).value)">
          <option value="">{{ currentName() }} (actual)</option>
          @for (b of others(); track b.BranchId) {<option [value]="b.BranchId">{{ b.Name }}</option>}
          <option [value]="ALL">Todas mis sucursales</option>
        </select>
      </div>
    }
  `,
})
export class BranchFilterComponent {
  private readonly auth = inject(AuthService);

  protected readonly ALL = ALL;
  protected readonly id = `branch-filter-${Math.random().toString(36).slice(2, 8)}`;

  readonly value = model('');
  readonly colClass = input('col-12 col-md-3');

  protected readonly branches = computed(() => this.auth.user()?.Branches ?? []);
  protected readonly currentName = computed(() => this.auth.user()?.BranchName ?? '');
  protected readonly others = computed(() => this.branches().filter(b => b.BranchId !== this.auth.user()?.BranchId));
}
