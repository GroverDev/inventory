import { Component, ElementRef, computed, input, output, signal, viewChildren } from '@angular/core';

/** Seis casillas para un código numérico de un solo uso (pegar, borrar y flechas incluidos). */
@Component({
  selector: 'app-otp-input',
  template: `
    <div class="d-flex gap-2 justify-content-center" (paste)="onPaste($event)">
      @for (digit of digits(); track $index) {
        <input
          #cell
          type="text"
          inputmode="numeric"
          maxlength="1"
          autocomplete="one-time-code"
          class="form-control form-control-lg text-center fw-bold px-0"
          style="width: 2.9rem; height: 3.3rem"
          [class.is-invalid]="hasError()"
          [value]="digit"
          [attr.aria-label]="'Dígito ' + ($index + 1)"
          (keydown)="onKeydown($event, $index)"
          (input)="onInput($event, $index)"
        />
      }
    </div>
  `,
})
export class OtpInputComponent {
  readonly hasError = input(false);
  /** Se emite en cada cambio con lo escrito hasta el momento. */
  readonly codeChange = output<string>();
  /** Se emite al completar los seis dígitos. */
  readonly completed = output<string>();

  protected readonly digits = signal<string[]>(Array(6).fill(''));
  readonly length = computed(() => this.digits().filter(Boolean).length);
  private readonly cells = viewChildren<ElementRef<HTMLInputElement>>('cell');

  get code(): string {
    return this.digits().join('');
  }

  clear() {
    this.digits.set(Array(6).fill(''));
    this.codeChange.emit('');
    this.focus(0);
  }

  focus(index: number) {
    this.cells()[index]?.nativeElement.focus();
  }

  protected onInput(e: Event, i: number) {
    const input = e.target as HTMLInputElement;
    const value = input.value.replace(/\D/g, '').slice(-1);
    input.value = value;
    this.set(i, value);

    if (value && i < 5) this.focus(i + 1);
    else if (value && i === 5 && this.length() === 6) this.completed.emit(this.code);
  }

  protected onKeydown(e: KeyboardEvent, i: number) {
    if (e.key === 'Backspace') {
      if (this.digits()[i]) {
        this.set(i, '');
      } else if (i > 0) {
        this.set(i - 1, '');
        this.focus(i - 1);
      }
      e.preventDefault();
    } else if (e.key === 'ArrowLeft' && i > 0) {
      this.focus(i - 1);
    } else if (e.key === 'ArrowRight' && i < 5) {
      this.focus(i + 1);
    }
  }

  protected onPaste(e: ClipboardEvent) {
    e.preventDefault();
    const nums = (e.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 6).split('');
    this.digits.set(Array.from({ length: 6 }, (_, i) => nums[i] ?? ''));
    this.codeChange.emit(this.code);
    this.focus(Math.min(nums.length, 5));
  }

  private set(i: number, value: string) {
    this.digits.update(d => d.map((x, idx) => (idx === i ? value : x)));
    this.codeChange.emit(this.code);
  }
}
