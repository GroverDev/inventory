<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'

// Código de 6 dígitos en casillas separadas. v-model devuelve los dígitos ingresados.
const props = defineProps<{ modelValue: string; error?: boolean; autofocus?: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: string]; complete: [value: string] }>()

const LENGTH = 6
const inputs = ref<HTMLInputElement[]>([])

const digits = computed(() => Array.from({ length: LENGTH }, (_, i) => props.modelValue[i] ?? ''))

const setDigits = (list: string[]) => {
  const value = list.join('')
  emit('update:modelValue', value)
  if (value.length === LENGTH) emit('complete', value)
}

const focus = (i: number) => nextTick(() => inputs.value[i]?.focus())

const onInput = (e: Event, i: number) => {
  const val = (e.target as HTMLInputElement).value.replace(/\D/g, '').slice(-1)
  const next = [...digits.value]
  next[i] = val
  ;(e.target as HTMLInputElement).value = val
  setDigits(next)
  if (val && i < LENGTH - 1) focus(i + 1)
}

const onKeydown = (e: KeyboardEvent, i: number) => {
  if (e.key === 'Backspace') {
    const next = [...digits.value]
    if (next[i]) next[i] = ''
    else if (i > 0) { next[i - 1] = ''; focus(i - 1) }
    setDigits(next)
    e.preventDefault()
  } else if (e.key === 'ArrowLeft' && i > 0) focus(i - 1)
  else if (e.key === 'ArrowRight' && i < LENGTH - 1) focus(i + 1)
}

const onPaste = (e: ClipboardEvent) => {
  const nums = (e.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, LENGTH).split('')
  setDigits(nums)
  focus(Math.min(nums.length, LENGTH - 1))
}

defineExpose({ focus: () => focus(0) })
</script>

<template>
  <div class="d-flex justify-content-center gap-2" @paste.prevent="onPaste">
    <input v-for="(d, i) in digits" :key="i" :ref="(el) => (inputs[i] = el as HTMLInputElement)" :value="d"
      class="form-control form-control-lg text-center fw-semibold px-0" :class="{ 'is-invalid': error }"
      style="width: 3rem" type="text" inputmode="numeric" maxlength="1" autocomplete="one-time-code"
      :autofocus="autofocus && i === 0" @input="onInput($event, i)" @keydown="onKeydown($event, i)" />
  </div>
</template>
