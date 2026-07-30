/// <reference types="vite/client" />

/** API que expone en `window` el script de Cloudflare Turnstile. */
interface TurnstileRenderOptions {
  sitekey: string;
  theme?: 'light' | 'dark' | 'auto';
  /** 'interaction-only' dibuja el widget solo si hace falta interacción. */
  appearance?: 'always' | 'execute' | 'interaction-only';
  callback?: (token: string) => void;
  'expired-callback'?: () => void;
  'error-callback'?: () => void;
  /** Se dispara justo antes de que el desafío pase a ser interactivo. */
  'before-interactive-callback'?: () => void;
  /** Se dispara cuando el desafío deja de necesitar interacción. */
  'after-interactive-callback'?: () => void;
}

interface TurnstileApi {
  render: (container: string | HTMLElement, options: TurnstileRenderOptions) => string;
  reset: (widgetId?: string) => void;
  remove: (widgetId?: string) => void;
}

interface Window {
  turnstile?: TurnstileApi;
}
