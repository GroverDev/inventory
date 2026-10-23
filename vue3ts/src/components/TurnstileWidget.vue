<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'

/**
 * Widget de Cloudflare Turnstile.
 *
 * Emite el token al resolverse el desafío y una cadena vacía cuando expira,
 * falla o se reinicia. Los tokens son de un solo uso: tras un login fallido el
 * padre debe llamar a `reset()` o el siguiente intento será rechazado.
 *
 * Emite además un estado, para que el formulario no quede inutilizable si
 * Cloudflare no carga: si el script está bloqueado o el desafío no resuelve
 * dentro de `timeoutMs`, pasa a `unavailable` y el padre habilita el envío.
 *
 * Usa `appearance: 'interaction-only'`: el widget no se dibuja salvo que
 * Cloudflare necesite un clic del usuario.
 */
type TurnstileStatus = 'pending' | 'ready' | 'unavailable'

const props = withDefaults(
  defineProps<{
    siteKey: string
    theme?: 'light' | 'dark' | 'auto'
    appearance?: 'always' | 'execute' | 'interaction-only'
    timeoutMs?: number
  }>(),
  { theme: 'auto', appearance: 'interaction-only', timeoutMs: 8000 },
)

const emit = defineEmits<{
  (e: 'update:token', token: string): void
  (e: 'update:status', status: TurnstileStatus): void
}>()

const containerRef = ref<HTMLElement | null>(null)
/** El desafío pasó a necesitar interacción: recién ahí ocupa espacio. */
const isInteractive = ref(false)
let widgetId: string | undefined
let timeoutId: number | undefined

const SCRIPT_ID = 'cf-turnstile-script'
const SCRIPT_SRC = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit'

const clearTimer = () => {
  if (timeoutId !== undefined) window.clearTimeout(timeoutId)
  timeoutId = undefined
}

/** Vuelve a esperar un token, con un plazo tras el cual se da por no disponible. */
const startPending = () => {
  clearTimer()
  emit('update:token', '')
  emit('update:status', 'pending')
  timeoutId = window.setTimeout(() => emit('update:status', 'unavailable'), props.timeoutMs)
}

const onToken = (token: string) => {
  clearTimer()
  isInteractive.value = false
  emit('update:token', token)
  emit('update:status', 'ready')
}

const loadScript = (): Promise<void> =>
  new Promise((resolve, reject) => {
    if (window.turnstile) return resolve()

    const existing = document.getElementById(SCRIPT_ID) as HTMLScriptElement | null
    if (existing) {
      existing.addEventListener('load', () => resolve())
      existing.addEventListener('error', () => reject(new Error('turnstile')))
      return
    }

    const script = document.createElement('script')
    script.id = SCRIPT_ID
    script.src = SCRIPT_SRC
    script.async = true
    script.defer = true
    script.addEventListener('load', () => resolve())
    script.addEventListener('error', () => reject(new Error('turnstile')))
    document.head.appendChild(script)
  })

const render = () => {
  if (!window.turnstile || !containerRef.value) return
  widgetId = window.turnstile.render(containerRef.value, {
    sitekey: props.siteKey,
    theme: props.theme,
    appearance: props.appearance,
    callback: onToken,
    'expired-callback': startPending,
    'error-callback': startPending,
    'before-interactive-callback': () => (isInteractive.value = true),
    'after-interactive-callback': () => (isInteractive.value = false),
  })
}

/** Pide un token nuevo. Se llama tras cada intento de login fallido. */
const reset = () => {
  startPending()
  if (window.turnstile && widgetId !== undefined) window.turnstile.reset(widgetId)
}

defineExpose({ reset })

onMounted(async () => {
  if (!props.siteKey) return
  startPending()
  try {
    await loadScript()
    render()
  } catch {
    // Script bloqueado o sin red: no se deja el formulario inservible.
    clearTimer()
    emit('update:status', 'unavailable')
  }
})

onBeforeUnmount(() => {
  clearTimer()
  if (window.turnstile && widgetId !== undefined) window.turnstile.remove(widgetId)
})
</script>

<template>
  <div v-if="siteKey" ref="containerRef" class="turnstile-widget" :class="{ 'is-interactive': isInteractive }"></div>
</template>

<style scoped>
/* Sin alto reservado: con 'interaction-only' el widget no dibuja nada en el camino normal. */
.turnstile-widget {
  display: flex;
  justify-content: center;
}
.turnstile-widget.is-interactive {
  min-height: 65px;
  margin-bottom: 1.5rem;
}
</style>
