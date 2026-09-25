<template>
  <div class="d-flex align-items-start gap-3">
    <div
      class="product-image-box"
      :class="{ 'product-image-box--zoom': hasImage }"
      :role="hasImage ? 'button' : undefined"
      :title="hasImage ? 'Clic para ampliar' : undefined"
      @click="hasImage && (ampliada = true)"
    >
      <img v-if="previewUrl" :src="previewUrl" alt="Vista previa" class="product-image-preview" />
      <ProductThumb v-else :path="imagePath" :size="140" full alt="Imagen del producto" />
    </div>
    <ImageLightbox
      :open="ampliada"
      :src="previewUrl || mediaUrl(imagePath)"
      :placeholder="previewUrl ? undefined : mediaUrl(imagePath, true)"
      @close="ampliada = false"
    />

    <div class="flex-grow-1">
      <input
        ref="fileInput"
        type="file"
        class="d-none"
        accept="image/jpeg,image/png,image/webp"
        @change="onFileChosen"
      />
      <!-- Algunos celulares (Xiaomi/HyperOS) no ofrecen la cámara en el selector normal:
           con `capture` se abre directo. En escritorio se ignora y abre el selector de archivos. -->
      <input
        ref="cameraInput"
        type="file"
        class="d-none"
        accept="image/*"
        capture="environment"
        @change="onFileChosen"
      />

      <div v-if="!readonly" class="d-flex flex-wrap gap-2 mb-2">
        <button type="button" class="btn btn-sm btn-outline-primary" :disabled="busy" @click="fileInput?.click()">
          <i class="fal fa-upload me-1"></i>{{ hasImage ? 'Cambiar imagen' : 'Subir imagen' }}
        </button>
        <button type="button" class="btn btn-sm btn-outline-primary" :disabled="busy" @click="cameraInput?.click()">
          <i class="fal fa-camera me-1"></i>Tomar foto
        </button>
        <button v-if="hasImage" type="button" class="btn btn-sm btn-outline-danger" :disabled="busy" @click="remove">
          <i class="fal fa-trash-alt me-1"></i>Quitar
        </button>
      </div>

      <small class="text-muted d-block">JPG, PNG o WebP. Se reduce automáticamente; máximo 5 MB.</small>
      <small v-if="isNew && pendingName" class="text-muted d-block">
        <i class="fal fa-clock me-1"></i>«{{ pendingName }}» se subirá al guardar el producto.
      </small>
      <small v-else-if="isNew" class="text-muted d-block">
        Puede elegirla ahora; se sube cuando guarde el producto.
      </small>
      <small v-if="error" class="text-danger d-block mt-1">{{ error }}</small>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue';
import ProductThumb from '@/modules/inventory/components/ProductThumb.vue';
import ImageLightbox from '@/modules/inventory/components/ImageLightbox.vue';
import { mediaUrl } from '@/utils/mediaUrl';
import useProduct from '@/modules/inventory/composables/useProduct';
import utils from '@/utils/msg';
import { ALLOWED_IMAGE_TYPES, MAX_IMAGE_BYTES, prepareImage } from '@/utils/imageResize';

const props = defineProps<{
  /** '0' mientras el producto no se ha creado. */
  productId: string;
  imagePath: string | null;
  readonly?: boolean;
}>();

const emit = defineEmits<{
  /** Con producto ya creado: la imagen se subió y esta es su nueva ruta. */
  (e: 'uploaded', path: string): void;
  (e: 'removed'): void;
  /** Con producto nuevo: el archivo elegido, para que el padre lo suba tras crearlo. */
  (e: 'picked', file: Blob | null, name: string): void;
}>();

const { uploadImage, deleteImage } = useProduct();

const fileInput = ref<HTMLInputElement | null>(null);
const cameraInput = ref<HTMLInputElement | null>(null);
const busy = ref(false);
const error = ref('');
const previewUrl = ref('');
const pendingName = ref('');
const ampliada = ref(false);

const isNew = computed(() => props.productId === '0');
const hasImage = computed(() => !!props.imagePath || !!previewUrl.value);

const setPreview = (blob: Blob | null) => {
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value);
  previewUrl.value = blob ? URL.createObjectURL(blob) : '';
};
onBeforeUnmount(() => setPreview(null));

const onFileChosen = async (event: Event) => {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';   // permite volver a elegir el mismo archivo
  if (!file) return;

  error.value = '';
  if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
    error.value = 'Formato no permitido. Use JPG, PNG o WebP.';
    return;
  }

  busy.value = true;
  try {
    const blob = await prepareImage(file);
    if (blob.size > MAX_IMAGE_BYTES) {
      error.value = 'La imagen pesa más de 5 MB.';
      return;
    }

    if (isNew.value) {
      // Todavía no hay producto al que colgarla: se muestra y el padre la sube
      // cuando el producto exista.
      setPreview(blob);
      pendingName.value = file.name;
      emit('picked', blob, file.name);
      return;
    }

    // La API avisa los errores por su cuenta (formato, tamaño...) con el modal.
    const { ok, Data: path } = await uploadImage(props.productId, blob, file.name);
    if (ok) {
      setPreview(null);
      emit('uploaded', path);
    }
  } finally {
    busy.value = false;
  }
};

const remove = async () => {
  if (isNew.value) {
    setPreview(null);
    pendingName.value = '';
    emit('picked', null, '');
    return;
  }
  if (!(await utils.showMessageQuestion('¿Desea quitar la imagen del producto?'))) return;

  busy.value = true;
  try {
    const { ok } = await deleteImage(props.productId);
    if (ok) emit('removed');
  } finally {
    busy.value = false;
  }
};
</script>

<style scoped>
.product-image-box {
  width: 140px;
  height: 140px;
  flex-shrink: 0;
  border-radius: 0.375rem;
  overflow: hidden;
  border: 1px solid var(--bs-border-color);
}
.product-image-box--zoom { cursor: zoom-in; }
.product-image-preview {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
