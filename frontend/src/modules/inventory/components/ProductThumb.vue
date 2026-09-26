<template>
  <span
    class="product-thumb"
    :class="{ 'product-thumb--fill': fill }"
    :style="fill ? undefined : { width: `${size}px`, height: `${size}px` }"
  >
    <img
      v-if="src && !failed"
      :src="src"
      :alt="alt"
      loading="lazy"
      decoding="async"
      @error="failed = true"
    />
    <i v-else class="fal fa-image" :style="{ fontSize: fill ? '2rem' : `${Math.round(size / 2.5)}px` }"></i>
  </span>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { mediaUrl } from '@/utils/mediaUrl';

const props = withDefaults(defineProps<{
  /** Ruta relativa que devuelve la API (`ImagePath`). */
  path?: string | null;
  /** Lado en px; se ignora con `fill`. */
  size?: number;
  /** Ocupa todo el ancho disponible, en cuadrado. Para las tarjetas. */
  fill?: boolean;
  /** Imagen completa (800 px) en vez de la miniatura. */
  full?: boolean;
  alt?: string;
}>(), { path: null, size: 40, fill: false, full: false, alt: '' });

const src = computed(() => mediaUrl(props.path, !props.full));

// Si el archivo no está (404) se muestra el marcador en vez del ícono roto del
// navegador. Al cambiar la ruta se vuelve a intentar.
const failed = ref(false);
watch(() => props.path, () => { failed.value = false; });
</script>

<style scoped>
.product-thumb {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  overflow: hidden;
  border-radius: 0.375rem;
  background: var(--bs-secondary-bg);
  color: var(--bs-secondary-color);
}
.product-thumb--fill {
  display: flex;
  width: 100%;
  aspect-ratio: 1 / 1;
  border-radius: 0.375rem 0.375rem 0 0;
}
.product-thumb img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
