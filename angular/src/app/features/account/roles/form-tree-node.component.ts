import { Component, computed, effect, input, output, signal, viewChild, ElementRef } from '@angular/core';

import { AppForm } from '../forms/form.model';

export interface TreeNode {
  form: AppForm;
  children: TreeNode[];
}

/** Nodo del árbol de formularios de un rol: marcar un nodo marca toda su rama. */
@Component({
  selector: 'app-form-tree-node',
  imports: [],
  styles: `.tree-row:hover { background-color: rgba(0, 0, 0, 0.04); }`,
  template: `
    <div>
      <div class="d-flex align-items-center py-1 rounded tree-row" [style.padding-left.px]="level() * 16 + 4">
        <span style="width: 16px; flex-shrink: 0;" class="me-1">
          @if (hasChildren()) {
            <i class="text-muted" [class]="isOpen() ? 'ri-arrow-down-s-line' : 'ri-arrow-right-s-line'" style="cursor: pointer;" (click)="isOpen.set(!isOpen())"></i>
          }
        </span>

        <input #checkbox type="checkbox" class="form-check-input me-2 flex-shrink-0 mt-0" [id]="'ftree-' + node().form.Id" [checked]="checkState() === 'checked'" (change)="toggle()">

        <label class="form-check-label mb-0 user-select-none" [class]="labelClass()" [for]="'ftree-' + node().form.Id" style="cursor: pointer;">
          {{ node().form.NameForm }}
        </label>
      </div>

      @if (hasChildren() && isOpen()) {
        @for (child of node().children; track child.form.Id) {
          <app-form-tree-node [node]="child" [level]="level() + 1" [selectedIds]="selectedIds()" (selectedIdsChange)="selectedIdsChange.emit($event)" />
        }
      }
    </div>
  `,
})
export class FormTreeNodeComponent {
  readonly node = input.required<TreeNode>();
  readonly level = input.required<number>();
  readonly selectedIds = input.required<number[]>();
  readonly selectedIdsChange = output<number[]>();

  protected readonly isOpen = signal(true);
  private readonly checkbox = viewChild<ElementRef<HTMLInputElement>>('checkbox');

  protected readonly hasChildren = computed(() => this.node().children.length > 0);

  private readonly allIds = computed(() => {
    const collect = (n: TreeNode): number[] => [n.form.Id, ...n.children.flatMap(collect)];
    return collect(this.node());
  });

  protected readonly checkState = computed<'checked' | 'unchecked' | 'indeterminate'>(() => {
    const selected = this.allIds().filter(id => this.selectedIds().includes(id));
    if (selected.length === 0) return 'unchecked';
    if (selected.length === this.allIds().length) return 'checked';
    return 'indeterminate';
  });

  protected readonly labelClass = computed(() => {
    if (this.level() === 0) return 'fw-bold text-uppercase small';
    if (this.level() === 1) return 'fw-semibold';
    return 'text-body';
  });

  constructor() {
    effect(() => {
      const el = this.checkbox()?.nativeElement;
      if (el) el.indeterminate = this.checkState() === 'indeterminate';
    });
  }

  protected toggle() {
    const ids = this.allIds();
    if (this.checkState() === 'checked') {
      this.selectedIdsChange.emit(this.selectedIds().filter(id => !ids.includes(id)));
    } else {
      const toAdd = ids.filter(id => !this.selectedIds().includes(id));
      this.selectedIdsChange.emit([...this.selectedIds(), ...toAdd]);
    }
  }
}
