import { ref } from 'vue'
import { defineStore } from 'pinia'

// Milisegundos antes de mostrar el indicador: evita el parpadeo en navegaciones instantáneas.
const SHOW_DELAY_MS = 120
// Tope de seguridad: si la navegación no termina, se libera la pantalla.
const MAX_BLOCK_MS = 15000

export const useNavigationStore = defineStore('navigation', () => {
  const isNavigating = ref(false)

  let showTimer: ReturnType<typeof setTimeout> | null = null
  let maxTimer: ReturnType<typeof setTimeout> | null = null

  const clearTimers = () => {
    if (showTimer) clearTimeout(showTimer)
    if (maxTimer) clearTimeout(maxTimer)
    showTimer = null
    maxTimer = null
  }

  const start = () => {
    // Una redirección dispara otro beforeEach: no reiniciar si ya está en curso.
    if (showTimer || isNavigating.value) return
    showTimer = setTimeout(() => {
      isNavigating.value = true
    }, SHOW_DELAY_MS)
    maxTimer = setTimeout(done, MAX_BLOCK_MS)
  }

  const done = () => {
    clearTimers()
    isNavigating.value = false
  }

  return { isNavigating, start, done }
})
