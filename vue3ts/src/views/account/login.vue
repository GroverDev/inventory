<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import useVuelidate from '@vuelidate/core'
import { email, helpers, required } from '@vuelidate/validators'

import AuthShell from '@/components/AuthShell.vue'
import TurnstileWidget from '@/components/TurnstileWidget.vue'
import { useAuth } from '@/composables/useAuth'

const { loginApp } = useAuth()
const router = useRouter()

const showPassword = ref(false)
// Desmarcado por defecto: sin él la sesión muere al cerrar el navegador.
const rememberMe = ref(false)
const processing = ref(false)

// Sin site key configurada el widget no se muestra y el login funciona como siempre:
// quien exige el token es el backend, según la cabecera Origin.
const turnstileSiteKey = (import.meta.env.VITE_TURNSTILE_SITE_KEY ?? '') as string
const turnstileToken = ref('')
const turnstileStatus = ref<'pending' | 'ready' | 'unavailable'>('pending')
const turnstileRef = ref<InstanceType<typeof TurnstileWidget> | null>(null)

// Solo se espera al captcha mientras esté resolviéndose. Si Cloudflare no carga, el
// formulario se habilita igual: el backend exige el token únicamente ante señal de abuso.
const canSubmit = computed(
  () => !processing.value && (!turnstileSiteKey || turnstileToken.value !== '' || turnstileStatus.value === 'unavailable'),
)

const form = ref({ usuario: '', contrasenia: '' })

const rules = {
  usuario: {
    required: helpers.withMessage('El usuario es obligatorio.', required),
    email: helpers.withMessage('Formato de correo inválido.', email),
  },
  contrasenia: { required: helpers.withMessage('La contraseña es obligatoria.', required) },
}
const v$ = useVuelidate(rules, form)

const submit = async () => {
  if (!(await v$.value.$validate())) return

  processing.value = true
  try {
    const ok = await loginApp(form.value.usuario, form.value.contrasenia, turnstileToken.value, rememberMe.value)

    if (ok.success) {
      router.push({ name: 'default' })
      return
    }
    if (ok.requireTotp) {
      router.push({ name: ok.totpSetupRequired ? 'totp-setup' : 'totp' })
      return
    }
    // El token de Turnstile es de un solo uso: tras un login fallido hay que pedir uno nuevo.
    turnstileRef.value?.reset()
  } finally {
    processing.value = false
  }
}
</script>

<template>
  <AuthShell>
    <div class="text-center mt-2">
      <h5 class="text-primary">¡Bienvenido!</h5>
      <p class="text-muted">Inicia sesión para continuar.</p>
    </div>

    <div class="p-2 mt-4">
      <form novalidate @submit.prevent="submit">
        <div class="mb-3">
          <label for="email" class="form-label">Usuario</label>
          <input id="email" v-model.trim="v$.usuario.$model" type="email" class="form-control" autocomplete="username"
            placeholder="Ingresa tu correo" :class="{ 'is-invalid': v$.usuario.$dirty && v$.usuario.$invalid }" />
          <div class="invalid-feedback">{{ v$.usuario.$errors[0]?.$message }}</div>
        </div>

        <div class="mb-3">
          <label class="form-label" for="password-input">Contraseña</label>
          <div class="position-relative auth-pass-inputgroup mb-3">
            <input id="password-input" v-model.trim="v$.contrasenia.$model" :type="showPassword ? 'text' : 'password'"
              class="form-control pe-5" autocomplete="current-password" placeholder="Ingresa tu contraseña"
              :class="{ 'is-invalid': v$.contrasenia.$dirty && v$.contrasenia.$invalid }" />
            <button class="btn btn-link position-absolute end-0 top-0 text-decoration-none text-muted" type="button"
              :aria-label="showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'" @click="showPassword = !showPassword">
              <i :class="showPassword ? 'ri-eye-off-fill' : 'ri-eye-fill'" class="align-middle"></i>
            </button>
            <div class="invalid-feedback">{{ v$.contrasenia.$errors[0]?.$message }}</div>
          </div>
        </div>

        <div class="form-check">
          <input id="auth-remember-check" v-model="rememberMe" class="form-check-input" type="checkbox" />
          <label class="form-check-label" for="auth-remember-check">
            Mantener sesión y recordar este equipo por 30 días
          </label>
          <div class="form-text">
            No pedirá contraseña ni código de verificación en este equipo.
            <strong>No lo marques en equipos compartidos o de otras personas.</strong>
          </div>
        </div>

        <!-- Normalmente no se ve: solo aparece si Cloudflare pide un clic. -->
        <div class="mt-3">
          <TurnstileWidget ref="turnstileRef" :site-key="turnstileSiteKey"
            @update:token="turnstileToken = $event" @update:status="turnstileStatus = $event" />
        </div>

        <div class="mt-4">
          <button class="btn btn-success w-100" type="submit" :disabled="!canSubmit">
            <span v-if="processing" class="spinner-border spinner-border-sm me-2" role="status"></span>
            {{ processing ? 'Ingresando...' : 'Ingresar' }}
          </button>
        </div>
      </form>
    </div>
  </AuthShell>
</template>
