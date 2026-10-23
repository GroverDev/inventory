/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_SERVICIOS: string
  readonly VITE_TURNSTILE_SITE_KEY?: string
  readonly VITE_MEDIA_URL?: string
}

interface TurnstileApi {
  render: (container: HTMLElement, options: Record<string, unknown>) => string
  reset: (widgetId?: string) => void
  remove: (widgetId?: string) => void
}

interface Window {
  turnstile?: TurnstileApi
}

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}
