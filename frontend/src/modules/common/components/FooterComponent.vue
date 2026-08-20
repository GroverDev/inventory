
<template>
  <footer class="app-footer">
    <div class="app-footer-content flex-grow-1">
      Innovar &copy; {{ anioActual }}
      <span class="hidden-mobile"> All rights reserved, </span>
      <span class="fw-bold">Innovar.</span>
      <a href="#top" class="ms-auto hidden-mobile" aria-label="Ir arriba">
        <svg class="sa-icon sa-thick sa-icon-primary">
          <use href="/assets/icons/sprite.svg#arrow-up"></use>
        </svg>
      </a>
    </div>
  </footer>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';

const anioActual = ref(0);


onMounted(async () => {
  const fecha = new Date();
  anioActual.value = fecha.getFullYear();
});
</script>

<style scoped></style>
