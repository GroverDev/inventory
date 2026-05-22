<template>
  <div class="login-bg">
    <div class="geo-shape geo-1"></div>
    <div class="geo-shape geo-2"></div>
    <div class="geo-shape geo-3"></div>

    <div class="login-card">
      <!-- Logo -->
      <div class="logo-wrap">
        <img src="/assets/img/logo.png" alt="Logo" class="logo-img" />
      </div>

      <!-- Header -->
      <div class="totp-header">
        <div class="shield-icon">
          <i class="fal fa-shield-check"></i>
        </div>
        <h2 class="totp-title">Verificación en dos pasos</h2>
        <p class="totp-subtitle">
          Ingresa el código de 6 dígitos de tu aplicación autenticadora
          <span v-if="userEmail" class="totp-email"> para <strong>{{ userEmail }}</strong></span>
        </p>
      </div>

      <!-- 6-digit inputs -->
      <form @submit.prevent="handleSubmit" novalidate>
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
            autocomplete="one-time-code"
            @keydown="onKeydown($event, i)"
            @input="onInput($event, i)"
          />
        </div>

        <!-- Error message -->
        <div v-if="hasError" class="error-msg">
          <i class="fal fa-exclamation-circle me-1"></i>
          Código incorrecto. Intenta de nuevo.
        </div>

        <!-- Timer -->
        <div class="timer-wrap">
          <svg class="timer-ring" viewBox="0 0 36 36">
            <circle cx="18" cy="18" r="15.9" fill="none" class="timer-track" stroke-width="2.5" />
            <circle
              cx="18" cy="18" r="15.9"
              fill="none"
              stroke="#7c5cbf"
              stroke-width="2.5"
              stroke-dasharray="100"
              :stroke-dashoffset="100 - timerProgress"
              stroke-linecap="round"
              transform="rotate(-90 18 18)"
            />
          </svg>
          <span class="timer-seconds">{{ timeLeft }}s</span>
        </div>
        <p class="timer-hint">El código se renueva cada 30 segundos</p>

        <!-- Submit -->
        <div class="d-grid mt-3">
          <button
            type="submit"
            class="btn btn-login"
            :disabled="codeLength < 6 || loading"
          >
            <span v-if="loading" class="spinner-border spinner-border-sm me-2" role="status"></span>
            {{ loading ? 'Verificando...' : 'Verificar' }}
          </button>
        </div>

        <div class="text-center mt-2">
          <button type="button" class="btn-link-recovery" @click="showRecovery = !showRecovery">
            <i class="fal fa-key me-1"></i>
            {{ showRecovery ? 'Usar código TOTP' : 'Usar código de recuperación' }}
          </button>
        </div>
      </form>

      <!-- Recovery code input -->
      <form v-if="showRecovery" @submit.prevent="handleRecovery" class="mt-2" novalidate>
        <input
          v-model="recoveryCode"
          class="form-control recovery-input"
          :class="{ 'is-invalid': recoveryError }"
          type="text"
          placeholder="XXXXX-XXXXX"
          autocomplete="off"
          @input="recoveryError = false"
        />
        <div v-if="recoveryError" class="error-msg mt-1">
          <i class="fal fa-exclamation-circle me-1"></i>
          Código inválido o ya utilizado.
        </div>
        <div class="d-grid mt-2">
          <button type="submit" class="btn btn-login" :disabled="!recoveryCode.trim() || loading">
            <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
            {{ loading ? 'Verificando...' : 'Ingresar con código de recuperación' }}
          </button>
        </div>
      </form>

      <!-- Footer -->
      <div class="card-footer-row mt-2">
        <span class="version-label">v 1.8.1</span>
        <button
          type="button"
          class="theme-toggle-btn"
          @click="themeStore.toggleTheme()"
          :title="themeStore.theme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'"
        >
          <i :class="themeStore.theme === 'dark' ? 'fal fa-sun' : 'fal fa-moon'"></i>
        </button>
        <router-link :to="{ name: 'login' }" class="reset-link">
          ← Volver al inicio
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTotp } from '@/modules/auth/composables/useTotp'
import { useAuthStore } from '@/modules/auth/stores/auth.store'
import { useThemeStore } from '@/stores/themeStore'

const router = useRouter()
const { verifyAndComplete, verifyWithRecovery } = useTotp()
const authStore = useAuthStore()
const themeStore = useThemeStore()

const userEmail = computed(() => authStore.getPendingUser?.Email ?? '')

onMounted(() => {
  if (!authStore.getPendingUser) {
    router.replace({ name: 'login' })
  }
  startTimer()
  inputRefs[0]?.focus()
})
onUnmounted(() => clearInterval(timerHandle))

// ── OTP inputs ──────────────────────────────────────────
const digits = ref<string[]>(Array(6).fill(''))
const inputRefs: HTMLInputElement[] = []
const hasError = ref(false)
const loading = ref(false)
const codeLength = computed(() => digits.value.filter(Boolean).length)
const showRecovery = ref(false)
const recoveryCode = ref('')
const recoveryError = ref(false)

function onInput(e: Event, i: number) {
  const val = (e.target as HTMLInputElement).value.replace(/\D/g, '')
  digits.value[i] = val.slice(-1)
  hasError.value = false
  if (val && i < 5) {
    inputRefs[i + 1]?.focus()
  } else if (val && i === 5) {
    // Auto-submit when 6th digit entered
    handleSubmit()
  }
}

function onKeydown(e: KeyboardEvent, i: number) {
  if (e.key === 'Backspace') {
    if (digits.value[i]) {
      digits.value[i] = ''
    } else if (i > 0) {
      digits.value[i - 1] = ''
      inputRefs[i - 1]?.focus()
    }
    e.preventDefault()
  } else if (e.key === 'ArrowLeft' && i > 0) {
    inputRefs[i - 1]?.focus()
  } else if (e.key === 'ArrowRight' && i < 5) {
    inputRefs[i + 1]?.focus()
  }
}

function handlePaste(e: ClipboardEvent) {
  const text = e.clipboardData?.getData('text') ?? ''
  const nums = text.replace(/\D/g, '').slice(0, 6).split('')
  nums.forEach((n, i) => { digits.value[i] = n })
  inputRefs[Math.min(nums.length, 5)]?.focus()
}

// ── Timer ────────────────────────────────────────────────
const timeLeft = ref(30)
const timerProgress = computed(() => ((30 - timeLeft.value) / 30) * 100)
let timerHandle: ReturnType<typeof setInterval>

function startTimer() {
  const sync = () => { timeLeft.value = 30 - (Math.floor(Date.now() / 1000) % 30) }
  sync()
  timerHandle = setInterval(sync, 500)
}

// ── Submit ───────────────────────────────────────────────
async function handleSubmit() {
  if (codeLength.value < 6 || loading.value) return
  loading.value = true
  hasError.value = false

  const result = await verifyAndComplete(digits.value.join(''))
  loading.value = false

  if (result.success) {
    router.push({ name: 'inventory-dashboard' })
  } else {
    hasError.value = true
    digits.value = Array(6).fill('')
    inputRefs[0]?.focus()
  }
}

async function handleRecovery() {
  if (!recoveryCode.value.trim() || loading.value) return
  loading.value = true
  recoveryError.value = false

  const result = await verifyWithRecovery(recoveryCode.value.trim())
  loading.value = false

  if (result.success) {
    router.push({ name: 'inventory-dashboard' })
  } else {
    recoveryError.value = true
    recoveryCode.value = ''
  }
}
</script>

<style scoped>
/* ── Background ─────────────────────────────────────────── */
.login-bg {
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
.geo-1 {
  width: 55vw; height: 55vw;
  top: -20vw; left: -15vw;
  background: var(--auth-geo1-bg);
  clip-path: polygon(0 0, 100% 20%, 80% 100%, 0 80%);
}
.geo-2 {
  width: 45vw; height: 50vw;
  bottom: -15vw; right: -10vw;
  background: var(--auth-geo2-bg);
  clip-path: polygon(20% 0, 100% 10%, 100% 100%, 0 90%);
}
.geo-3 {
  width: 30vw; height: 30vw;
  top: 30%; left: 5vw;
  background: var(--auth-geo3-bg);
  clip-path: polygon(50% 0%, 100% 50%, 50% 100%, 0% 50%);
}

/* ── Card ───────────────────────────────────────────────── */
.login-card {
  position: relative;
  z-index: 10;
  background: var(--auth-card-bg);
  border-radius: 12px;
  padding: 2rem 2.2rem 1.4rem;
  width: 100%;
  max-width: 400px;
  box-shadow: var(--auth-card-shadow);
}

/* ── Logo ───────────────────────────────────────────────── */
.logo-wrap { text-align: center; margin-bottom: 1.4rem; }
.logo-img  { max-height: 60px; max-width: 180px; object-fit: contain; }

/* ── Header ─────────────────────────────────────────────── */
.totp-header { text-align: center; margin-bottom: 1.6rem; }

.shield-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 56px; height: 56px;
  border-radius: 50%;
  background: var(--auth-shield-bg);
  color: #7c5cbf;
  font-size: 1.5rem;
  margin-bottom: 0.8rem;
}

.totp-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--auth-title-color);
  margin-bottom: 0.4rem;
}

.totp-subtitle {
  font-size: 0.85rem;
  color: var(--auth-subtitle-color);
  line-height: 1.5;
  margin: 0;
}

.totp-email { color: var(--auth-email-color); }

/* ── OTP inputs ─────────────────────────────────────────── */
.otp-inputs {
  display: flex;
  gap: 0.5rem;
  justify-content: center;
  margin-bottom: 0.6rem;
}

.otp-input {
  width: 44px;
  height: 52px;
  text-align: center;
  font-size: 1.4rem;
  font-weight: 700;
  color: var(--auth-otp-color);
  border: 2px solid var(--auth-otp-border);
  border-radius: 8px;
  background: var(--auth-otp-bg);
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s, background 0.15s;
  caret-color: #7c5cbf;
}

.otp-input:focus {
  border-color: #7c5cbf;
  background: var(--auth-input-bg);
  box-shadow: 0 0 0 3px rgba(124,92,191,0.18);
}

.otp-input.otp-filled {
  border-color: var(--auth-otp-filled-border);
  background: var(--auth-otp-filled-bg);
}

.otp-input.otp-error {
  border-color: #e53e3e;
  background: var(--auth-otp-error-bg);
  animation: shake 0.35s ease;
}

@keyframes shake {
  0%, 100% { transform: translateX(0); }
  20%       { transform: translateX(-5px); }
  60%       { transform: translateX(5px); }
}

/* ── Error ──────────────────────────────────────────────── */
.error-msg {
  text-align: center;
  color: #e53e3e;
  font-size: 0.82rem;
  margin-bottom: 0.4rem;
}

/* ── Timer ──────────────────────────────────────────────── */
.timer-wrap {
  position: relative;
  width: 44px;
  height: 44px;
  margin: 0.6rem auto 0.2rem;
}

.timer-ring {
  width: 44px;
  height: 44px;
  transform: none;
}

.timer-ring circle {
  transition: stroke-dashoffset 0.5s linear;
}

.timer-track {
  stroke: var(--auth-timer-track);
}

.timer-seconds {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.72rem;
  font-weight: 700;
  color: #7c5cbf;
}

.timer-hint {
  text-align: center;
  font-size: 0.75rem;
  color: var(--auth-timer-hint);
  margin: 0;
}

/* ── Button ─────────────────────────────────────────────── */
.btn-login {
  background-color: #7c5cbf;
  color: #fff;
  border: none;
  border-radius: 6px;
  padding: 0.6rem;
  font-size: 0.95rem;
  font-weight: 600;
  letter-spacing: 0.3px;
  transition: background-color 0.2s, box-shadow 0.2s;
}

.btn-login:hover:not(:disabled) {
  background-color: #6a4aaa;
  box-shadow: 0 4px 12px rgba(124,92,191,0.35);
  color: #fff;
}

.btn-login:disabled {
  background-color: #c4b0e8;
  cursor: not-allowed;
}

/* ── Footer ─────────────────────────────────────────────── */
.card-footer-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
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

.theme-toggle-btn:hover {
  color: #7c5cbf;
}

.reset-link {
  font-size: 0.78rem;
  color: #7c5cbf;
  text-decoration: none;
}

.reset-link:hover { text-decoration: underline; }

.btn-link-recovery {
  background: none; border: none; color: #7c5cbf;
  font-size: 0.82rem; cursor: pointer; padding: 0;
}
.btn-link-recovery:hover { text-decoration: underline; }

.recovery-input {
  font-size: 0.92rem; letter-spacing: 1.5px;
  border: 2px solid var(--auth-otp-border);
  background: var(--auth-otp-bg);
  color: var(--auth-otp-color);
  border-radius: 8px; padding: 0.5rem 0.8rem;
}
.recovery-input:focus { border-color: #7c5cbf; box-shadow: 0 0 0 3px rgba(124,92,191,0.18); outline: none; }

@media (max-width: 480px) {
  .login-card { margin: 1rem; padding: 1.6rem 1rem 1.2rem; }
  .otp-input  { width: 38px; height: 46px; font-size: 1.2rem; }
}
</style>
