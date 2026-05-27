import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { CashSession } from '../models/cashSession.model';
import useCashSession from '../composables/useCashSession';

export const useCashSessionStore = defineStore('cashSession', () => {
  const { getActiveSession } = useCashSession();

  const activeSession = ref<CashSession | null>(null);
  const loaded = ref(false);

  const hasOpenSession = computed(() => activeSession.value !== null && activeSession.value.IsOpen);
  const sessionId = computed(() => activeSession.value?.Id ?? '');

  const loadActiveSession = async () => {
    const resp = await getActiveSession();
    if (resp.ok) {
      activeSession.value = resp.Data ?? null;
    } else {
      activeSession.value = null;
    }
    loaded.value = true;
  };

  const setSession = (session: CashSession | null) => {
    activeSession.value = session;
    loaded.value = true;
  };

  const clearSession = () => {
    activeSession.value = null;
  };

  return {
    activeSession,
    loaded,
    hasOpenSession,
    sessionId,
    loadActiveSession,
    setSession,
    clearSession,
  };
});
