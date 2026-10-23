import { Injectable, computed, signal } from '@angular/core';

/** Pantalla de carga global. Usa contador: varias llamadas en paralelo no la apagan antes de tiempo. */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly pending = signal(0);
  readonly isLoading = computed(() => this.pending() > 0);

  show(): void {
    this.pending.update(n => n + 1);
  }

  hide(): void {
    this.pending.update(n => Math.max(0, n - 1));
  }
}
