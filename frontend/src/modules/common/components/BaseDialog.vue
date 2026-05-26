<template>
  <Teleport to="body">
    <Transition name="fade">
      <div v-if="dialogStore.isVisible" class="dialog-overlay" @click.self="dialogStore.cancel">
        <Transition name="scale">
          <div v-if="dialogStore.isVisible" class="dialog-container shadow-lg" :class="`type-${options.type}`">
            <!-- Header/Icon Section -->
            <div class="dialog-icon-wrapper">
               <div class="dialog-icon">
                  <i :class="getIconClass"></i>
               </div>
            </div>

            <!-- Content -->
            <div class="dialog-content text-center">
              <h3 v-if="options.title" class="dialog-title fw-700 mb-2">{{ options.title }}</h3>
              <p class="dialog-message text-muted mb-0">{{ options.message }}</p>
            </div>

            <!-- Actions -->
            <div class="dialog-actions d-flex gap-2 justify-content-center mt-4">
              <button 
                v-if="options.showCancel" 
                class="btn px-4" 
                :class="options.cancelButtonClass"
                @click="dialogStore.cancel">
                {{ options.cancelText }}
              </button>
              <button 
                class="btn px-4 fw-600 shadow-sm" 
                :class="options.confirmButtonClass"
                @click="dialogStore.confirm">
                {{ options.confirmText }}
              </button>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useDialogStore } from '@/stores/dialogStore';

const dialogStore = useDialogStore();
const options = computed(() => dialogStore.options);

// Cambiamos a 'fal fa-' (FontAwesome Light) que ya se usa en el POS
const getIconClass = computed(() => {
  switch (options.value.type) {
    case 'success': return 'fal fa-check-circle bounceIn';
    case 'error': return 'fal fa-times-circle shakeIn';
    case 'warning': return 'fal fa-exclamation-triangle';
    case 'question': return 'fal fa-question-circle';
    case 'info':
    default: return 'fal fa-info-circle';
  }
});
</script>

<style scoped>
.dialog-overlay {
  position: fixed;
  top: 0; left: 0; width: 100vw; height: 100vh;
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(4px);
  z-index: 19999; /* Siempre por encima de cualquier modal de la app */
  display: flex; align-items: center; justify-content: center;
  padding: 20px;
}

[data-bs-theme="dark"] .dialog-overlay {
  background: rgba(0, 0, 0, 0.7);
}

.dialog-container {
  background-color: var(--bs-body-bg);
  color: var(--bs-body-color);
  border-radius: 16px;
  width: 100%; max-width: 420px;
  padding: 30px 24px;
  position: relative;
  border: 1px solid var(--bs-border-color);
}

.dialog-icon-wrapper {
  display: flex; justify-content: center; margin-bottom: 20px;
}

.dialog-icon {
  width: 80px; height: 80px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 3rem; /* Tamaño de icono aumentado */
  border: 4px solid currentColor;
}

.type-success .dialog-icon { color: #10b981; border-color: rgba(16, 185, 129, 0.2); background: rgba(16, 185, 129, 0.1); }
.type-error .dialog-icon { color: #ef4444; border-color: rgba(239, 68, 68, 0.2); background: rgba(239, 68, 68, 0.1); }
.type-warning .dialog-icon { color: #f59e0b; border-color: rgba(245, 158, 11, 0.2); background: rgba(245, 158, 11, 0.1); }
.type-info .dialog-icon { color: #3b82f6; border-color: rgba(59, 130, 246, 0.2); background: rgba(59, 130, 246, 0.1); }
.type-question .dialog-icon { color: #875A7B; border-color: rgba(135, 90, 123, 0.2); background: rgba(135, 90, 123, 0.1); }

.dialog-title { font-size: 1.4rem; color: var(--bs-emphasis-color); }
.dialog-message { font-size: 1.05rem; line-height: 1.6; }

/* Transitions */
.fade-enter-active, .fade-leave-active { transition: opacity 0.3s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.scale-enter-active, .scale-leave-active { transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1); }
.scale-enter-from, .scale-leave-to { transform: scale(0.9); }

/* Basic Animations */
.bounceIn { animation: bounceIn 0.8s; }
@keyframes bounceIn {
  from, 20%, 40%, 60%, 80%, to { animation-timing-function: cubic-bezier(0.215, 0.61, 0.355, 1); }
  0% { opacity: 0; transform: scale3d(0.3, 0.3, 0.3); }
  20% { transform: scale3d(1.1, 1.1, 1.1); }
  40% { transform: scale3d(0.9, 0.9, 0.9); }
  60% { opacity: 1; transform: scale3d(1.03, 1.03, 1.03); }
  80% { transform: scale3d(0.97, 0.97, 0.97); }
  to { opacity: 1; transform: scale3d(1, 1, 1); }
}

.shakeIn { animation: shake 0.5s; }
@keyframes shake {
  0%, 100% { transform: translateX(0); }
  10%, 30%, 50%, 70%, 90% { transform: translateX(-5px); }
  20%, 40%, 60%, 80% { transform: translateX(5px); }
}
</style>
