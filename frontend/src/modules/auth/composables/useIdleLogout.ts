import { onBeforeUnmount, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';

import { useAuthStore } from '@/modules/auth/stores/auth.store';
import utils from '@/utils/msg';

/// Minutos sin actividad tras los cuales se cierra una sesión NO recordada.
const IDLE_MINUTES = 30;

const ACTIVITY_EVENTS = ['mousedown', 'keydown', 'touchstart', 'scroll', 'wheel'] as const;

/**
 * Cierra la sesión tras un rato sin actividad, para que una pestaña olvidada
 * en un equipo compartido no quede abierta. Solo rige para sesiones que el
 * usuario no marcó como "mantener iniciada": quien la pidió eligió no volver a
 * identificarse. Se monta una vez, desde App.vue.
 */
export const useIdleLogout = () => {
  const auth = useAuthStore();
  const router = useRouter();
  let timer: ReturnType<typeof setTimeout> | null = null;
  let lastReset = 0;

  const stop = () => {
    if (timer) clearTimeout(timer);
    timer = null;
  };

  const expire = async () => {
    stop();
    if (!auth.isAuthenticated) return;
    // El cierre por inactividad no lo decide el usuario: el equipo conserva su
    // confianza (a diferencia del botón "Cerrar sesión").
    await auth.logout(true);
    await router.push({ name: 'login' });
    utils.showMessageModal({
      Description: 'Tu sesión se cerró por inactividad. Inicia sesión nuevamente.',
      MessageType: 'info',
    });
  };

  const arm = () => {
    stop();
    if (auth.isAuthenticated && !auth.remember) {
      timer = setTimeout(expire, IDLE_MINUTES * 60 * 1000);
    }
  };

  // Con throttle: mousedown/scroll pueden dispararse muchas veces por segundo.
  const onActivity = () => {
    const now = Date.now();
    if (now - lastReset < 5000) return;
    lastReset = now;
    if (timer) arm();
  };

  onMounted(() => {
    ACTIVITY_EVENTS.forEach((e) => window.addEventListener(e, onActivity, { passive: true }));
  });
  onBeforeUnmount(() => {
    ACTIVITY_EVENTS.forEach((e) => window.removeEventListener(e, onActivity));
    stop();
  });

  // Arranca al iniciar sesión y se detiene al cerrarla.
  watch(() => [auth.isAuthenticated, auth.remember], arm, { immediate: true });
};
