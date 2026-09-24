import { useAuthStore } from '@/modules/auth/stores/auth.store';

const isAuthenticatedGuard = async (to: any, _: any, next: any) => {

  const authStore = useAuthStore();

  document.title = to.meta.title;
  if (authStore.getToken !== '' && authStore.getToken != null) {
    next();
  } else {
    next({ name: 'login' });
  }
};
export {
  isAuthenticatedGuard
}
