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
          <div class="illustration-placeholder">
            <!-- Illustration would go here -->
            <div class="turbine-graphic"></div>
          </div>
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
                class="form-control custom-input" 
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
                  class="form-control custom-input" 
                  id="password" 
                  placeholder="Ingresa tu contraseña"
                  v-model.trim="v$.contrasenia.$model"
                  :class="{ 'is-invalid': v$.contrasenia.$dirty && v$.contrasenia.$invalid }"
                >
                <i 
                  class="toggle-password bi" 
                  :class="showPassword ? 'bi-eye-slash' : 'bi-eye'"
                  @click="showPassword = !showPassword"
                ></i>
                <div class="invalid-feedback" v-if="v$.contrasenia.$dirty && v$.contrasenia.required.$invalid">
                  La contraseña es obligatoria.
                </div>
              </div>
            </div>

            <div class="d-flex justify-content-end mb-4">
              <a href="#" class="forgot-password">¿Olvidaste tu contraseña?</a>
            </div>

            <div class="d-grid gap-2">
              <button type="submit" class="btn btn-primary custom-btn">
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
import { ref } from 'vue';

const { loginApp } = useAuth();
const router = useRouter();

const showPassword = ref(false);

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

  const ok = await loginApp(loginForm.value.usuario, loginForm.value.contrasenia);
  if (ok.success) {
    router.push({ name: 'inventory-dashboard' });
  } else if (ok.requireTotp) {
    router.push({ name: ok.totpSetupRequired ? 'totp-setup' : 'totp' });
  }
}
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #f5f5f5;
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
  background: white;
  border-radius: 24px;
  overflow: hidden;
  box-shadow: 0 10px 40px rgba(0,0,0,0.05);
}

/* Left Panel */
.info-panel {
  flex: 1;
  background: linear-gradient(135deg, #2b84ea 0%, #3e9e5d 100%);
  color: white;
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
  background: white;
  padding: 4rem;
}

.form-content {
  width: 100%;
  max-width: 400px;
}

.form-title {
  font-size: 2rem;
  font-weight: 700;
  color: #333;
  margin-bottom: 0.5rem;
  text-align: center;
}

.form-subtitle {
  color: #666;
  text-align: center;
  margin-bottom: 3rem;
  font-size: 0.95rem;
}

.form-label {
  font-weight: 600;
  font-size: 0.9rem;
  color: #444;
  margin-bottom: 0.5rem;
}

.custom-input {
  background-color: #f3f4f6;
  border: 1px solid transparent;
  padding: 0.8rem 1rem;
  border-radius: 50px; /* Fully rounded */
  font-size: 0.95rem;
  transition: all 0.3s ease;
}

.custom-input:focus {
  background-color: white;
  border-color: #3e9e5d;
  box-shadow: 0 0 0 4px rgba(62, 158, 93, 0.1);
}

.password-group {
  position: relative;
}

.toggle-password {
  position: absolute;
  right: 15px;
  top: 50%;
  transform: translateY(-50%);
  cursor: pointer;
  color: #666;
}

.forgot-password {
  color: #3e9e5d;
  text-decoration: none;
  font-size: 0.9rem;
  font-weight: 500;
}

.custom-btn {
  background: linear-gradient(90deg, #2b84ea 0%, #3e9e5d 100%);
  border: none;
  padding: 0.8rem;
  border-radius: 50px;
  font-weight: 600;
  font-size: 1rem;
  letter-spacing: 0.5px;
  transition: transform 0.2s;
}

.custom-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(43, 132, 234, 0.3);
}

.sign-up-link {
  color: #2b84ea;
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
