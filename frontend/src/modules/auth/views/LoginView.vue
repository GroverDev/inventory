<template>
  <div class="login-container">
    <div class="login-wrapper">
      <!-- Left Panel -->
      <div class="info-panel">
        <div class="info-content">
          <h1 class="info-title">Ejecución de Manufactura<br>Simplificada</h1>
          <p class="info-text">
            Inicia sesión para monitorear, administrar y optimizar tus operaciones.
          </p>
        </div>
      </div>

      <!-- Right Panel -->
      <div class="form-panel">
        <div class="form-content">
          <h2 class="form-title">Iniciar Sesión</h2>
          <p class="form-subtitle">Ingresa tus credenciales para continuar</p>

          <form @submit.prevent="loginSubmit" novalidate>
            <div class="mb-4">
              <label for="email" class="form-label">Usuario</label>
              <input
                type="email"
                class="form-control"
                id="email"
                placeholder="Ingresa tu usuario"
                v-model.trim="v$.usuario.$model"
                :class="{ 'is-invalid': v$.usuario.$dirty && v$.usuario.$invalid }"
                autocomplete="off"
              >
              <div class="invalid-feedback" v-if="v$.usuario.$dirty && v$.usuario.required.$invalid">
                El usuario es obligatorio.
              </div>
              <div class="invalid-feedback" v-if="v$.usuario.$dirty && v$.usuario.email.$invalid">
                Formato de correo inválido.
              </div>
            </div>

            <div class="mb-4">
              <label for="password" class="form-label">Contraseña</label>
              <div class="password-group">
                <input
                  :type="showPassword ? 'text' : 'password'"
                  class="form-control password-input"
                  id="password"
                  placeholder="Ingresa tu contraseña"
                  v-model.trim="v$.contrasenia.$model"
                  :class="{ 'is-invalid': v$.contrasenia.$dirty && v$.contrasenia.$invalid }"
                >
                <button
                  type="button"
                  class="toggle-password"
                  :aria-label="showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                  @click="showPassword = !showPassword"
                >
                  <i class="fal" :class="showPassword ? 'fa-eye-slash' : 'fa-eye'"></i>
                </button>
              </div>
              <div class="invalid-feedback d-block" v-if="v$.contrasenia.$dirty && v$.contrasenia.required.$invalid">
                La contraseña es obligatoria.
              </div>
            </div>

            <div class="d-flex justify-content-end mb-4">
              <a href="#" class="forgot-password">¿Olvidaste tu contraseña?</a>
            </div>

            <!-- Normalmente no se ve: solo aparece si Cloudflare pide un clic,
                 y en ese caso el propio componente reserva su espacio. -->
            <TurnstileWidget
              ref="turnstileRef"
              :site-key="turnstileSiteKey"
              :theme="themeStore.theme === 'dark' ? 'dark' : 'light'"
              @update:token="turnstileToken = $event"
              @update:status="turnstileStatus = $event"
            />

            <div class="d-grid gap-2">
              <button
                type="submit"
                class="btn btn-primary login-btn"
                :disabled="!canSubmit"
              >
                Ingresar
              </button>
            </div>

            <div class="text-center mt-4">
              <a href="#" class="sign-up-link">Registrarse</a>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuth } from '@/modules/auth/composables/useAuth';
import { useRouter } from "vue-router";
import useVuelidate from '@vuelidate/core';
import { required, email } from '@vuelidate/validators';
import { computed, ref } from 'vue';
import TurnstileWidget from '@/modules/auth/components/TurnstileWidget.vue';
import { useThemeStore } from '@/stores/themeStore';

const { loginApp } = useAuth();
const router = useRouter();
const themeStore = useThemeStore();

const showPassword = ref(false);

// Sin site key configurada el widget no se muestra y el login funciona como
// siempre: quien exige el token es el backend, según la cabecera Origin.
const turnstileSiteKey = (import.meta.env.VITE_TURNSTILE_SITE_KEY ?? '') as string;
const turnstileToken = ref('');
const turnstileStatus = ref<'pending' | 'ready' | 'unavailable'>('pending');
const turnstileRef = ref<InstanceType<typeof TurnstileWidget> | null>(null);

// Solo se espera al captcha mientras esté resolviéndose. Si Cloudflare no
// carga, el formulario se habilita igual: el backend exige el token únicamente
// ante señal de abuso, así que el login limpio no debe depender del widget.
const canSubmit = computed(
  () => !turnstileSiteKey || turnstileToken.value !== '' || turnstileStatus.value === 'unavailable'
);

const loginForm = ref({
  usuario: '',
  contrasenia: ''
});

const reglas = {
  usuario: { required, email },
  contrasenia: { required }
};

const v$ = useVuelidate(reglas, loginForm);

const loginSubmit = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const ok = await loginApp(
    loginForm.value.usuario,
    loginForm.value.contrasenia,
    turnstileToken.value
  );

  if (ok.success) {
    router.push({ name: 'inventory-dashboard' });
    return;
  }

  if (ok.requireTotp) {
    router.push({ name: ok.totpSetupRequired ? 'totp-setup' : 'totp' });
    return;
  }

  // El token de Turnstile es de un solo uso: si el login no prosperó hay que
  // pedir uno nuevo o el siguiente intento fallaría por token duplicado.
  turnstileRef.value?.reset();
}
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: var(--auth-bg);
  padding: 2rem;
  position: relative;
  z-index: 10;
  pointer-events: auto;
}

.login-wrapper {
  display: flex;
  width: 100%;
  max-width: 1200px;
  min-height: 700px;
  background: var(--auth-card-bg);
  /* Mismo radio que la tarjeta de TotpView, la pantalla hermana. */
  border-radius: 12px;
  overflow: hidden;
  box-shadow: var(--auth-card-shadow);
}

/* Left Panel */
.info-panel {
  flex: 1;
  /* Color plano de respaldo si el navegador no aplica el degradado. */
  background: var(--bs-primary);
  /* Misma diagonal azul→verde del diseño original, pero tomada del tema
     activo: sigue a la plantilla si se cambia el theme. */
  background: linear-gradient(135deg, var(--bs-primary) 0%, var(--bs-success) 100%);
  color: #fff;
  padding: 4rem;
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
}

.info-content {
  position: relative;
  z-index: 2;
}

.info-title {
  font-size: 3rem;
  font-weight: 700;
  line-height: 1.2;
  margin-bottom: 1.5rem;
}

.info-text {
  font-size: 1.1rem;
  opacity: 0.9;
  max-width: 400px;
  line-height: 1.6;
}

/* Abstract decorations for the left panel since image generation failed */
.info-panel::before {
  content: '';
  position: absolute;
  bottom: -50px;
  left: -50px;
  width: 200px;
  height: 200px;
  border-radius: 50%;
  background: rgba(255,255,255,0.1);
}

.info-panel::after {
  content: '';
  position: absolute;
  top: -100px;
  right: -100px;
  width: 400px;
  height: 400px;
  border-radius: 50%;
  background: rgba(255,255,255,0.05);
}

/* Right Panel */
.form-panel {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--auth-card-bg);
  padding: 4rem;
}

.form-content {
  width: 100%;
  max-width: 400px;
}

.form-title {
  font-size: 2rem;
  font-weight: 700;
  color: var(--auth-title-color);
  margin-bottom: 0.5rem;
  text-align: center;
}

.form-subtitle {
  color: var(--auth-subtitle-color);
  text-align: center;
  margin-bottom: 3rem;
  font-size: 0.95rem;
}

.form-label {
  font-weight: 600;
  font-size: 0.9rem;
  color: var(--auth-email-color);
  margin-bottom: 0.5rem;
}

/* Los inputs usan .form-control tal cual lo define el template (radio
   --bs-border-radius: 0.375rem), igual que el resto de la aplicación.
   Aquí solo se ajusta la altura, que en el login es mayor que en las grillas. */
.form-control {
  padding: 0.7rem 1rem;
  font-size: 0.95rem;
}

.password-group {
  position: relative;
}

.password-input {
  padding-right: 3rem;
}

/* Corre el ícono de validación de Bootstrap para no chocar con el ojo */
.password-input.is-invalid {
  background-position: right 2.75rem center;
}

.toggle-password {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  border: none;
  background: transparent;
  padding: 0.4rem 0.6rem;
  cursor: pointer;
  color: #666;
  line-height: 1;
}

.toggle-password:hover {
  color: var(--bs-primary);
}

.forgot-password {
  color: var(--bs-primary);
  text-decoration: none;
  font-size: 0.9rem;
  font-weight: 500;
}

/* Botón primario del template; solo se ajusta el alto para acompañar
   a los inputs. Sin gradiente ni pastilla: el mismo botón que en el resto
   de la aplicación. */
.login-btn {
  padding: 0.7rem;
  font-weight: 600;
  font-size: 1rem;
}

.sign-up-link {
  color: var(--bs-primary);
  text-decoration: none;
  font-weight: 600;
}

/* Responsive adjustments */
@media (max-width: 992px) {
  .login-wrapper {
    max-width: 500px;
    min-height: auto;
  }
  
  .info-panel {
    display: none;
  }
  
  .form-panel {
    padding: 3rem 2rem;
  }
}
</style>
