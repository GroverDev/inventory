<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="lightbox"
      role="dialog"
      aria-modal="true"
      :aria-label="caption || 'Imagen ampliada'"
      @click.self="$emit('close')"
    >
      <button type="button" class="lightbox-close" aria-label="Cerrar" @click="$emit('close')">
        <i class="fal fa-times"></i>
      </button>

      <figure class="lightbox-figure">
        <!-- Mientras llega la grande se muestra la miniatura, que ya está en caché. -->
        <img :src="loaded || !placeholder ? src : placeholder" :alt="caption" @error="failed = true" />
        <figcaption v-if="caption">{{ caption }}</figcaption>
        <p v-if="failed" class="text-white-50 small mb-0">No se pudo cargar la imagen.</p>
      </figure>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue';

const props = defineProps<{
  open: boolean;
  /** Imagen completa. */
  src: string;
  /** Se ve mientras carga la completa (la miniatura). Opcional. */
  placeholder?: string;
  caption?: string;
}>();
const emit = defineEmits<{ close: [] }>();

const loaded = ref(false);
const failed = ref(false);

const onKey = (e: KeyboardEvent) => {
  if (e.key !== 'Escape') return;
  // Cierra solo el visor: sin esto el Esc también llegaría al modal de abajo.
  e.stopPropagation();
  emit('close');
};

watch(() => props.open, (abierto) => {
  if (abierto) {
    loaded.value = false;
    failed.value = false;
    window.addEventListener('keydown', onKey, true);
    if (props.placeholder) {
      const pre = new Image();
      pre.onload = () => { loaded.value = true; };
      pre.src = props.src;
    }
  } else {
    window.removeEventListener('keydown', onKey, true);
  }
});
onBeforeUnmount(() => window.removeEventListener('keydown', onKey, true));
</script>

<style scoped>
/* Por encima de los modales de Bootstrap (1055) y de su fondo (1050). */
.lightbox {
  position: fixed;
  inset: 0;
  z-index: 2000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: rgba(0, 0, 0, 0.85);
}
.lightbox-figure {
  margin: 0;
  text-align: center;
}
.lightbox-figure img {
  display: block;
  margin: 0 auto;
  max-width: min(92vw, 800px);
  max-height: 80vh;
  object-fit: contain;
  border-radius: 0.5rem;
  background: #fff;
}
.lightbox-figure figcaption {
  margin-top: 0.75rem;
  color: #fff;
  font-size: 0.95rem;
}
.lightbox-close {
  position: absolute;
  top: 0.75rem;
  right: 1rem;
  width: 2.5rem;
  height: 2.5rem;
  border: 0;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.15);
  color: #fff;
  font-size: 1.4rem;
  line-height: 1;
}
.lightbox-close:hover { background: rgba(255, 255, 255, 0.3); }
</style>
