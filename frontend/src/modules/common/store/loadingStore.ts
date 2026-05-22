import { ref, } from 'vue'
import { defineStore } from 'pinia'

export const useLoadingStore = defineStore('loading', () => {
  const isLoading = ref(false)

  const show = () => {
    isLoading.value = true;
  }
  const hide = () => {
    isLoading.value = false;
  }
  return {
    isLoading,
    //setLoading,
    show,
    hide
  }
})
