<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import AuthShell from '@/components/AuthShell.vue'
import OtpInput from '@/components/OtpInput.vue'
import { useTotp } from '@/composables/useTotp'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const { verifyAndComplete, verifyWithRecovery } = useTotp()
const authStore = useAuthStore()

const userEmail = computed(() => authStore.getPendingUser?.Email ?? '')

const code = ref('')
const otp = ref<InstanceType<typeof OtpInput> | null>(null)
const hasError = ref(false)
const loading = ref(false)
const showRecovery = ref(false)
const recoveryCode = ref('')
const recoveryError = ref(false)

// El código TOTP se renueva cada 30 s: el contador se sincroniza con el reloj.
const timeLeft = ref(30)
let timerHandle: ReturnType<typeof setInterval>
const sync = () => { timeLeft.value = 30 - (Math.floor(Date.now() / 1000) % 30) }

onMounted(() => {
  // Sin un login pendiente no hay nada que verificar.
  if (!authStore.getPendingUser) {
    router.replace({ name: 'login' })
    return
  }
  sync()
  timerHandle = setInterval(sync, 500)
  otp.value?.focus()
})
onUnmounted(() => clearInterval(timerHandle))

const finish = () => router.push({ name: 'default' })

const handleSubmit = async () => {
  if (code.value.length < 6 || loading.value) return
  loading.value = true
  hasError.value = false
  const result = await verifyAndComplete(code.value)
  loading.value = false
  if (result.success) {
    finish()
  } else {
    hasError.value = true
    code.value = ''
    otp.value?.focus()
  }
}

const handleRecovery = async () => {
  if (!recoveryCode.value.trim() || loading.value) return
  loading.value = true
  recoveryError.value = false
  const result = await verifyWithRecovery(recoveryCode.value.trim())
  loading.value = false
  if (result.success) {
    finish()
  } else {
    recoveryError.value = true
    recoveryCode.value = ''
  }
}
</script>

<template>
  <AuthShell>
    <template #default>
      <div class="text-center mt-2">
        <div class="avatar-md mx-auto mb-3">
          <div class="avatar-title bg-primary-subtle text-primary display-5 rounded-circle">
            <i class="ri-shield-check-line"></i>
          </div>
        </div>
        <h5 class="text-primary">Verificación en dos pasos</h5>
        <p class="text-muted">
          Ingresa el código de 6 dígitos de tu aplicación autenticadora
          <span v-if="userEmail">para <strong>{{ userEmail }}</strong></span>
        </p>
      </div>

      <form class="mt-4" novalidate @submit.prevent="handleSubmit">
        <OtpInput ref="otp" v-model="code" :error="hasError" @complete="handleSubmit" />
        <div v-if="hasError" class="text-danger text-center mt-2 fs-13">
          <i class="ri-error-warning-line me-1"></i>Código incorrecto. Intenta de nuevo.
        </div>
        <p class="text-muted text-center mt-3 mb-0 fs-13">
          El código se renueva cada 30 segundos ({{ timeLeft }}s)
        </p>
        <div class="mt-3">
          <button type="submit" class="btn btn-success w-100" :disabled="code.length < 6 || loading">
            <span v-if="loading" class="spinner-border spinner-border-sm me-2" role="status"></span>
            {{ loading ? 'Verificando...' : 'Verificar' }}
          </button>
        </div>
        <div class="text-center mt-2">
          <button type="button" class="btn btn-link text-decoration-none" @click="showRecovery = !showRecovery">
            <i class="ri-key-2-line me-1"></i>
            {{ showRecovery ? 'Usar código TOTP' : 'Usar código de recuperación' }}
          </button>
        </div>
      </form>

      <form v-if="showRecovery" class="mt-2" novalidate @submit.prevent="handleRecovery">
        <input v-model="recoveryCode" class="form-control" :class="{ 'is-invalid': recoveryError }" type="text"
          placeholder="XXXXX-XXXXX" autocomplete="off" @input="recoveryError = false" />
        <div v-if="recoveryError" class="invalid-feedback">Código inválido o ya utilizado.</div>
        <div class="mt-2">
          <button type="submit" class="btn btn-outline-success w-100" :disabled="!recoveryCode.trim() || loading">
            {{ loading ? 'Verificando...' : 'Ingresar con código de recuperación' }}
          </button>
        </div>
      </form>
    </template>

    <template #below>
      <router-link :to="{ name: 'login' }" class="fw-semibold text-primary text-decoration-underline">
        ← Volver al inicio de sesión
      </router-link>
    </template>
  </AuthShell>
</template>
