<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import AuthShell from '@/components/AuthShell.vue'
import OtpInput from '@/components/OtpInput.vue'
import { useTotp, type TotpSetupData } from '@/composables/useTotp'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const { setupTotp, enableTotp } = useTotp()

const step = ref(1)
const loading = ref(false)
const setup = ref<TotpSetupData | null>(null)
const copied = ref(false)
const recoveryCodes = ref<string[]>([])
const codesCopied = ref(false)

const code = ref('')
const otp = ref<InstanceType<typeof OtpInput> | null>(null)
const hasError = ref(false)
const confirming = ref(false)

onMounted(async () => {
  // Esta pantalla se visita con un token real ya emitido (2FA obligatorio pendiente).
  if (!authStore.isAuthenticated) {
    router.replace({ name: 'login' })
    return
  }
  loading.value = true
  const res = await setupTotp()
  loading.value = false
  if (res.ok) setup.value = res.Data ?? null
})

const flash = (flag: typeof copied) => {
  flag.value = true
  setTimeout(() => { flag.value = false }, 2000)
}

const copySecret = async () => {
  if (!setup.value) return
  await navigator.clipboard.writeText(setup.value.SecretKey)
  flash(copied)
}

const confirmCode = async () => {
  if (code.value.length < 6 || confirming.value) return
  confirming.value = true
  hasError.value = false
  const res = await enableTotp(code.value)
  confirming.value = false
  if (res.ok) {
    recoveryCodes.value = res.recoveryCodes
    step.value = 3
  } else {
    hasError.value = true
    code.value = ''
    otp.value?.focus()
  }
}

const copyCodes = async () => {
  await navigator.clipboard.writeText(recoveryCodes.value.join('\n'))
  flash(codesCopied)
}

const downloadCodes = () => {
  const blob = new Blob([recoveryCodes.value.join('\n')], { type: 'text/plain' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'recovery-codes.txt'
  a.click()
  URL.revokeObjectURL(url)
}

// El menú de accesos no se cargó todavía: el login se detuvo antes por el TOTP pendiente.
const finish = async () => {
  await authStore.getAccessMenuApi()
  router.replace({ name: 'default' })
}
</script>

<template>
  <AuthShell>
    <template #default>
      <!-- Paso 1: QR -->
      <template v-if="step === 1">
        <div class="text-center mt-2">
          <h5 class="text-primary">Activar verificación en dos pasos</h5>
          <p class="text-muted">
            Escanea el código QR con <strong>Google Authenticator</strong>, <strong>Authy</strong> u otra app TOTP.
          </p>
        </div>
        <div v-if="loading" class="text-center py-4">
          <span class="spinner-border text-primary" role="status"></span>
        </div>
        <template v-else-if="setup">
          <div class="text-center">
            <img :src="`data:image/png;base64,${setup.QrCodeBase64}`" alt="QR TOTP" width="200" height="200" />
          </div>
          <div v-if="setup.SecretKey" class="input-group mt-3">
            <span class="input-group-text">Clave manual</span>
            <input class="form-control" :value="setup.SecretKey" readonly />
            <button type="button" class="btn btn-outline-secondary" :title="copied ? 'Copiado' : 'Copiar'" @click="copySecret">
              <i :class="copied ? 'ri-check-line' : 'ri-file-copy-line'"></i>
            </button>
          </div>
          <div class="mt-3">
            <button class="btn btn-success w-100" @click="step = 2">Continuar</button>
          </div>
        </template>
      </template>

      <!-- Paso 2: confirmar código -->
      <template v-else-if="step === 2">
        <div class="text-center mt-2">
          <h5 class="text-primary">Confirma el código</h5>
          <p class="text-muted">Ingresa el código de 6 dígitos que muestra tu app.</p>
        </div>
        <form novalidate @submit.prevent="confirmCode">
          <OtpInput ref="otp" v-model="code" :error="hasError" autofocus @complete="confirmCode" />
          <div v-if="hasError" class="text-danger text-center mt-2 fs-13">
            <i class="ri-error-warning-line me-1"></i>Código incorrecto. Revisa la hora del dispositivo e inténtalo de nuevo.
          </div>
          <div class="mt-3">
            <button type="submit" class="btn btn-success w-100" :disabled="code.length < 6 || confirming">
              <span v-if="confirming" class="spinner-border spinner-border-sm me-2"></span>
              {{ confirming ? 'Verificando...' : 'Activar TOTP' }}
            </button>
          </div>
          <div class="text-center mt-2">
            <button type="button" class="btn btn-link text-decoration-none" @click="step = 1">← Volver al QR</button>
          </div>
        </form>
      </template>

      <!-- Paso 3: códigos de recuperación -->
      <template v-else>
        <div class="text-center mt-2">
          <h5 class="text-success">¡TOTP activado!</h5>
          <p class="text-muted">
            Guarda estos <strong>códigos de recuperación</strong> en un lugar seguro. Cada código solo puede usarse
            <strong>una vez</strong> y no podrás verlos de nuevo.
          </p>
        </div>
        <div class="row g-2">
          <div v-for="c in recoveryCodes" :key="c" class="col-6"><code class="d-block text-center">{{ c }}</code></div>
        </div>
        <div class="d-flex gap-2 mt-3">
          <button type="button" class="btn btn-outline-secondary flex-fill" @click="downloadCodes">
            <i class="ri-download-2-line me-1"></i>Descargar
          </button>
          <button type="button" class="btn btn-outline-secondary flex-fill" @click="copyCodes">
            <i :class="codesCopied ? 'ri-check-line' : 'ri-file-copy-line'" class="me-1"></i>
            {{ codesCopied ? 'Copiados' : 'Copiar todos' }}
          </button>
        </div>
        <div class="mt-3">
          <button class="btn btn-success w-100" @click="finish">Ir al inicio</button>
        </div>
      </template>
    </template>

    <template #below>
      <router-link :to="{ name: 'login' }" class="fw-semibold text-primary text-decoration-underline">← Volver</router-link>
    </template>
  </AuthShell>
</template>
