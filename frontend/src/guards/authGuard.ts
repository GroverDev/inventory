import { useAuthStore } from '@/modules/auth/stores/auth.store';

const isAuthenticatedGuard = async (to: any, _: any, next: any) => {

  const authStore = useAuthStore();

  console.log(authStore.getToken);
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
