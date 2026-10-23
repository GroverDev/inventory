import { AfterViewInit, Component, ElementRef, OnDestroy, input, output, signal, viewChild } from '@angular/core';

export type TurnstileStatus = 'pending' | 'ready' | 'unavailable';

interface TurnstileApi {
  render(container: HTMLElement, options: Record<string, unknown>): string;
  reset(widgetId: string): void;
  remove(widgetId: string): void;
}

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

const SCRIPT_ID = 'cf-turnstile-script';
const SCRIPT_SRC = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';

/**
 * Widget de Cloudflare Turnstile.
 *
 * Emite el token al resolverse el desafío y una cadena vacía cuando expira,
 * falla o se reinicia. Los tokens son de un solo uso: tras un login fallido el
 * padre debe llamar a `reset()` o el siguiente intento será rechazado por
 * Cloudflare con `timeout-or-duplicate`.
 *
 * Emite además un estado para que el formulario no quede inutilizable si
 * Cloudflare no carga: si el script está bloqueado o el desafío no resuelve
 * dentro de `timeoutMs`, pasa a `unavailable` y el padre habilita el envío. El
 * backend solo exige el token ante señal de abuso, así que el login limpio
 * sigue funcionando durante una caída del servicio.
 *
 * Con `interaction-only` el widget no se dibuja salvo que Cloudflare necesite
 * un clic del usuario.
 */
@Component({
  selector: 'app-turnstile',
  template: `
    @if (siteKey()) {
      <div #container class="d-flex justify-content-center" [class.mb-3]="isInteractive()" [style.min-height.px]="isInteractive() ? 65 : null"></div>
    }
  `,
})
export class TurnstileComponent implements AfterViewInit, OnDestroy {
  readonly siteKey = input.required<string>();
  readonly theme = input<'light' | 'dark' | 'auto'>('auto');
  readonly appearance = input<'always' | 'execute' | 'interaction-only'>('interaction-only');
  readonly timeoutMs = input(8000);

  readonly token = output<string>();
  readonly status = output<TurnstileStatus>();

  private readonly container = viewChild<ElementRef<HTMLElement>>('container');
  /** El desafío pasó a necesitar interacción: recién ahí ocupa espacio. */
  protected readonly isInteractive = signal(false);

  private widgetId?: string;
  private timeoutId?: number;

  async ngAfterViewInit() {
    if (!this.siteKey()) return;
    this.startPending();
    try {
      await this.loadScript();
      this.render();
    } catch {
      // Script bloqueado o sin red: no se deja el formulario inservible.
      this.clearTimer();
      this.status.emit('unavailable');
    }
  }

  ngOnDestroy() {
    this.clearTimer();
    if (window.turnstile && this.widgetId !== undefined) window.turnstile.remove(this.widgetId);
  }

  /** Pide un token nuevo. Se llama tras cada intento de login fallido. */
  reset() {
    this.startPending();
    if (window.turnstile && this.widgetId !== undefined) window.turnstile.reset(this.widgetId);
  }

  private clearTimer() {
    if (this.timeoutId !== undefined) window.clearTimeout(this.timeoutId);
    this.timeoutId = undefined;
  }

  /** Vuelve a esperar un token, con un plazo tras el cual se da por no disponible. */
  private startPending() {
    this.clearTimer();
    this.token.emit('');
    this.status.emit('pending');
    this.timeoutId = window.setTimeout(() => this.status.emit('unavailable'), this.timeoutMs());
  }

  private onToken(token: string) {
    this.clearTimer();
    this.isInteractive.set(false);
    this.token.emit(token);
    this.status.emit('ready');
  }

  private loadScript(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (window.turnstile) return resolve();

      const existing = document.getElementById(SCRIPT_ID) as HTMLScriptElement | null;
      const script = existing ?? Object.assign(document.createElement('script'), {
        id: SCRIPT_ID, src: SCRIPT_SRC, async: true, defer: true,
      });
      script.addEventListener('load', () => resolve());
      script.addEventListener('error', () => reject(new Error('turnstile')));
      if (!existing) document.head.appendChild(script);
    });
  }

  private render() {
    const el = this.container()?.nativeElement;
    if (!window.turnstile || !el) return;

    this.widgetId = window.turnstile.render(el, {
      sitekey: this.siteKey(),
      theme: this.theme(),
      appearance: this.appearance(),
      callback: (token: string) => this.onToken(token),
      'expired-callback': () => this.startPending(),
      'error-callback': () => this.startPending(),
      // Con 'interaction-only' el widget aparece de golpe cuando hace falta un
      // clic: estos avisos permiten reservarle el espacio justo en ese momento.
      'before-interactive-callback': () => this.isInteractive.set(true),
      'after-interactive-callback': () => this.isInteractive.set(false),
    });
  }
}
