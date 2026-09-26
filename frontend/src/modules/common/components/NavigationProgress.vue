<template>
  <template v-if="navigationStore.isNavigating">
    <div class="nav-progress" role="progressbar" aria-label="Cargando página">
      <div class="nav-progress-bar"></div>
    </div>
    <!-- Capa transparente: impide interactuar con la vista anterior mientras carga la nueva. -->
    <div class="nav-blocker"></div>
  </template>
</template>
<script setup lang="ts">
import { useNavigationStore } from '@/stores/navigationStore';

const navigationStore = useNavigationStore();
</script>
<style scoped>
.nav-progress {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 3px;
  overflow: hidden;
  z-index: 2600;
}
.nav-progress-bar {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 40%;
  border-radius: 0 3px 3px 0;
  background: var(--bs-primary, #0d6efd);
  box-shadow: 0 0 8px var(--bs-primary, #0d6efd);
  animation: nav-progress-slide 1.1s ease-in-out infinite;
}
@keyframes nav-progress-slide {
  0% { left: -40%; }
  100% { left: 100%; }
}
.nav-blocker {
  position: fixed;
  inset: 0;
  z-index: 2599;
  cursor: progress;
}
</style>
