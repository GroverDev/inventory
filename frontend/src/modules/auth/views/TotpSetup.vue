<template>
  <div class="setup-bg">
    <div class="geo-shape geo-1"></div>
    <div class="geo-shape geo-2"></div>

    <div class="setup-card">
      <div class="logo-wrap">
        <img src="/assets/img/logo.png" alt="Logo" class="logo-img" />
      </div>

      <!-- Step 1: show QR -->
      <template v-if="step === 1">
        <div class="step-header">
          <div class="shield-icon"><i class="fal fa-shield-check"></i></div>
          <h2 class="step-title">Activar verificación en dos pasos</h2>
          <p class="step-subtitle">
            Escanea el código QR con <strong>Google Authenticator</strong>, <strong>Authy</strong>
            u otra app TOTP.
          </p>
        </div>

        <div v-if="loading" class="qr-placeholder">
          <span class="spinner-border text-primary" role="status"></span>
        </div>

        <template v-else-if="setup">
          <div class="qr-wrap">
            <img :src="`data:image/png;base64,${setup.QrCodeBase64}`" alt="QR TOTP" class="qr-img" />
          </div>

          <div v-if="setup.SecretKey" class="secret-wrap">
            <span class="secret-label">Clave manual</span>
            <code class="secret-key">{{ setup.SecretKey }}</code>
            <button type="button" class="btn-copy" @click="copySecret" :title="copied ? 'Copiado' : 'Copiar'">
              <i :class="copied ? 'fal fa-check' : 'fal fa-copy'"></i>
            </button>
          </div>

          <div class="d-grid mt-3">
            <button class="btn btn-primary-custom" @click="step = 2">
              Continuar <i class="fal fa-arrow-right ms-1"></i>
            </button>
          </div>
        </template>
      </template>

      <!-- Step 2: confirm code -->
      <template v-else-if="step === 2">
        <div class="step-header">
          <div class="shield-icon"><i class="fal fa-mobile-alt"></i></div>
          <h2 class="step-title">Confirma el código</h2>
          <p class="step-subtitle">Ingresa el código de 6 dígitos que muestra tu app.</p>
        </div>

        <form @submit.prevent="confirmCode" novalidate>
          <div class="otp-inputs" @paste.prevent="handlePaste">
            <input
              v-for="(_, i) in digits"
              :key="i"
              :ref="el => (inputRefs[i] = el as HTMLInputElement)"
              v-model="digits[i]"
              class="otp-input"
              :class="{ 'otp-error': hasError, 'otp-filled': digits[i] }"
              type="text"
              inputmode="numeric"
              maxlength="1"
              @keydown="onKeydown($event, i)"
              @input="onInput($event, i)"
            />
          </div>

          <div v-if="hasError" class="error-msg">
            <i class="fal fa-exclamation-circle me-1"></i>
            Código incorrecto. Revisa la hora del dispositivo e inténtalo de nuevo.
          </div>

          <div class="d-grid mt-3">
            <button type="submit" class="btn btn-primary-custom" :disabled="codeLength < 6 || confirming">
              <span v-if="confirming" class="spinner-border spinner-border-sm me-2"></span>
              {{ confirming ? 'Verificando...' : 'Activar TOTP' }}
            </button>
          </div>
          <div class="text-center mt-2">
            <button type="button" class="btn-link-back" @click="step = 1">
              <i class="fal fa-arrow-left me-1"></i> Volver al QR
            </button>
          </div>
        </form>
      </template>

      <!-- Step 3: recovery codes -->
      <template v-else>
        <div class="step-header">
          <div class="shield-icon success-icon"><i class="fal fa-check-circle"></i></div>
          <h2 class="step-title">¡TOTP activado!</h2>
          <p class="step-subtitle">
            Guarda estos <strong>códigos de recuperación</strong> en un lugar seguro.
            Cada código solo puede usarse <strong>una vez</strong> y no podrás verlos de nuevo.
          </p>
        </div>

        <div class="recovery-grid">
          <code v-for="code in recoveryCodes" :key="code" class="recovery-code">{{ code }}</code>
        </div>

        <div class="d-flex gap-2 mt-3">
          <button type="button" class="btn btn-outline-secondary flex-fill" @click="downloadCodes">
            <i class="fal fa-download me-1"></i> Descargar
          </button>
          <button type="button" class="btn btn-outline-secondary flex-fill" @click="copyCodes">
            <i :class="codesCopied ? 'fal fa-check' : 'fal fa-copy'" class="me-1"></i>
            {{ codesCopied ? 'Copiados' : 'Copiar todos' }}
          </button>
        </div>

        <div class="d-grid mt-3">
          <button class="btn btn-primary-custom" @click="finish">
            Ir al inicio <i class="fal fa-arrow-right ms-1"></i>
          </button>
        </div>
      </template>

      <!-- Footer -->
      <div class="setup-footer">
        <span class="version-label">v 1.8.1</span>
        <button
          type="button"
          class="theme-toggle-btn"
          @click="themeStore.toggleTheme()"
          :title="themeStore.theme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'"
        >
          <i :class="themeStore.theme === 'dark' ? 'fal fa-sun' : 'fal fa-moon'"></i>
        </button>
        <router-link :to="{ name: 'login' }" class="back-link">← Volver</router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTotp, type TotpSetupData } from '@/modules/auth/composables/useTotp'
import { useAuthStore } from '@/modules/auth/stores/auth.store'
import { useThemeStore } from '@/stores/themeStore'

const router    = useRouter()
const authStore = useAuthStore()
const themeStore = useThemeStore()
const { setupTotp, enableTotp } = useTotp()

const step    = ref(1)
const loading = ref(false)
const setup   = ref<TotpSetupData | null>(null)
const copied  = ref(false)
const recoveryCodes = ref<string[]>([])
const codesCopied = ref(false)

onMounted(async () => {
  if (!authStore.isAuthenticated) {
    router.replace({ name: 'login' })
    return
  }
  loading.value = true
  const res = await setupTotp()
  loading.value = false
  if (res.ok) setup.value = res.Data ?? null
})

async function copySecret() {
  if (!setup.value) return
  await navigator.clipboard.writeText(setup.value.SecretKey)
  copied.value = true
  setTimeout(() => { copied.value = false }, 2000)
}

// ── OTP inputs ──
const digits    = ref<string[]>(Array(6).fill(''))
const inputRefs: HTMLInputElement[] = []
const hasError  = ref(false)
const confirming = ref(false)
const codeLength = computed(() => digits.value.filter(Boolean).length)

function onInput(e: Event, i: number) {
  const val = (e.target as HTMLInputElement).value.replace(/\D/g, '')
  digits.value[i] = val.slice(-1)
  hasError.value = false
  if (val && i < 5) inputRefs[i + 1]?.focus()
}

function onKeydown(e: KeyboardEvent, i: number) {
  if (e.key === 'Backspace') {
    if (digits.value[i]) { digits.value[i] = '' }
    else if (i > 0) { digits.value[i - 1] = ''; inputRefs[i - 1]?.focus() }
    e.preventDefault()
  } else if (e.key === 'ArrowLeft' && i > 0) { inputRefs[i - 1]?.focus() }
  else if (e.key === 'ArrowRight' && i < 5) { inputRefs[i + 1]?.focus() }
}

function handlePaste(e: ClipboardEvent) {
  const nums = (e.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 6).split('')
  nums.forEach((n, i) => { digits.value[i] = n })
  inputRefs[Math.min(nums.length, 5)]?.focus()
}

async function confirmCode() {
  if (codeLength.value < 6 || confirming.value) return
  confirming.value = true
  hasError.value = false
  const res = await enableTotp(digits.value.join(''))
  confirming.value = false
  if (res.ok) {
    recoveryCodes.value = res.recoveryCodes
    step.value = 3
  } else {
    hasError.value = true
    digits.value = Array(6).fill('')
    inputRefs[0]?.focus()
  }
}

async function copyCodes() {
  await navigator.clipboard.writeText(recoveryCodes.value.join('\n'))
  codesCopied.value = true
  setTimeout(() => { codesCopied.value = false }, 2000)
}

function downloadCodes() {
  const text = recoveryCodes.value.join('\n')
  const blob = new Blob([text], { type: 'text/plain' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'recovery-codes.txt'
  a.click()
  URL.revokeObjectURL(url)
}

function finish() {
  router.replace({ name: 'inventory-dashboard' })
}
</script>

<style scoped>
.setup-bg {
  min-height: 100vh;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--auth-bg);
  position: relative;
  overflow: hidden;
}

.geo-shape { position: absolute; pointer-events: none; }
.geo-1 { width: 55vw; height: 55vw; top: -20vw; left: -15vw; background: var(--auth-geo1-bg); clip-path: polygon(0 0,100% 20%,80% 100%,0 80%); }
.geo-2 { width: 45vw; height: 50vw; bottom: -15vw; right: -10vw; background: var(--auth-geo2-bg); clip-path: polygon(20% 0,100% 10%,100% 100%,0 90%); }

.setup-card {
  position: relative; z-index: 10;
  background: var(--auth-card-bg);
  border-radius: 12px;
  padding: 2rem 2.2rem 1.6rem;
  width: 100%; max-width: 420px;
  box-shadow: var(--auth-card-shadow);
}

.logo-wrap { text-align: center; margin-bottom: 1.2rem; }
.logo-img  { max-height: 56px; max-width: 160px; object-fit: contain; }

.step-header { text-align: center; margin-bottom: 1.4rem; }
.shield-icon {
  display: inline-flex; align-items: center; justify-content: center;
  width: 54px; height: 54px; border-radius: 50%;
  background: var(--auth-shield-bg); color: #7c5cbf;
  font-size: 1.4rem; margin-bottom: 0.7rem;
}
.success-icon { background: rgba(62,158,93,0.12); color: #3e9e5d; }
.step-title    { font-size: 1.2rem; font-weight: 700; color: var(--auth-title-color); margin-bottom: 0.35rem; }
.step-subtitle { font-size: 0.85rem; color: var(--auth-subtitle-color); line-height: 1.5; margin: 0; }

.qr-placeholder { display: flex; justify-content: center; padding: 2rem 0; }
.qr-wrap { display: flex; justify-content: center; margin-bottom: 1rem; }
.qr-img  { width: 180px; height: 180px; border: 3px solid var(--auth-qr-border); border-radius: 8px; }

.secret-wrap {
  display: flex; align-items: center; gap: 0.5rem;
  background: var(--auth-secret-wrap-bg);
  border: 1px solid var(--auth-secret-wrap-border);
  border-radius: 6px; padding: 0.5rem 0.8rem;
  margin-bottom: 0.5rem;
}
.secret-label { font-size: 0.75rem; color: var(--auth-secret-label); font-weight: 600; white-space: nowrap; }
.secret-key   { flex: 1; font-size: 0.82rem; letter-spacing: 1.5px; color: var(--auth-secret-key); word-break: break-all; }
.btn-copy { background: none; border: none; color: #7c5cbf; cursor: pointer; font-size: 0.9rem; padding: 0 2px; }

.otp-inputs { display: flex; gap: 0.5rem; justify-content: center; margin-bottom: 0.6rem; }
.otp-input {
  width: 44px; height: 52px; text-align: center;
  font-size: 1.4rem; font-weight: 700;
  color: var(--auth-otp-color);
  border: 2px solid var(--auth-otp-border);
  border-radius: 8px;
  background: var(--auth-otp-bg);
  outline: none; transition: border-color .15s, box-shadow .15s, background .15s;
}
.otp-input:focus  { border-color: #7c5cbf; background: var(--auth-input-bg); box-shadow: 0 0 0 3px rgba(124,92,191,0.18); }
.otp-input.otp-filled { border-color: var(--auth-otp-filled-border); background: var(--auth-otp-filled-bg); }
.otp-input.otp-error  { border-color: #e53e3e; background: var(--auth-otp-error-bg); animation: shake .35s ease; }
@keyframes shake { 0%,100% { transform: translateX(0) } 20% { transform: translateX(-5px) } 60% { transform: translateX(5px) } }

.error-msg { text-align: center; color: #e53e3e; font-size: 0.82rem; margin-bottom: 0.4rem; }

/* Recovery codes */
.recovery-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.4rem 0.6rem;
  background: var(--auth-secret-wrap-bg);
  border: 1px solid var(--auth-secret-wrap-border);
  border-radius: 8px;
  padding: 0.8rem;
}
.recovery-code {
  font-size: 0.85rem;
  letter-spacing: 1px;
  color: var(--auth-secret-key);
  text-align: center;
  background: var(--auth-otp-bg);
  border-radius: 4px;
  padding: 0.25rem 0.4rem;
}

.btn-primary-custom {
  background-color: #7c5cbf; color: #fff; border: none;
  border-radius: 6px; padding: 0.6rem; font-size: 0.95rem; font-weight: 600;
  transition: background-color .2s, box-shadow .2s; cursor: pointer; width: 100%;
}
.btn-primary-custom:hover:not(:disabled) { background-color: #6a4aaa; box-shadow: 0 4px 12px rgba(124,92,191,0.35); }
.btn-primary-custom:disabled { background-color: #c4b0e8; cursor: not-allowed; }

.btn-link-back { background: none; border: none; color: #7c5cbf; font-size: 0.83rem; cursor: pointer; }
.btn-link-back:hover { text-decoration: underline; }

/* ── Footer ─────────────────────────────────────────────── */
.setup-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 1rem;
  padding-top: 0.6rem;
  border-top: 1px solid var(--auth-footer-border);
}

.version-label { font-size: 0.78rem; color: var(--auth-version-color); }

.theme-toggle-btn {
  background: none;
  border: none;
  color: var(--auth-version-color);
  cursor: pointer;
  font-size: 0.9rem;
  padding: 0 4px;
  line-height: 1;
  transition: color 0.2s;
}

.theme-toggle-btn:hover { color: #7c5cbf; }

.back-link {
  font-size: 0.78rem;
  color: #7c5cbf;
  text-decoration: none;
}

.back-link:hover { text-decoration: underline; }
</style>
