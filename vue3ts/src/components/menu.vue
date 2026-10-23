<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'

import MenuItems from '@/components/menu-items.vue'
import { bindCollapseHandlers, initActiveMenu } from '@/composables/useSidebarMenu'
import { useLayoutStore } from '@/stores/layout'

const route = useRoute()
const layoutType = computed(() => useLayoutStore().layoutType)

// Marca el ítem activo al cargar y en cada cambio de ruta.
watch(() => route.path, () => initActiveMenu(), { immediate: true })

const closeSidebarOnOverlay = () => document.body.classList.remove('vertical-sidebar-enable')

const onResize = () => {
  if (layoutType.value !== 'twocolumn') return
  const small = document.documentElement.clientWidth < 767
  document.documentElement.setAttribute('data-layout', 'vertical')
  localStorage.setItem('rmenu', small ? 'vertical' : 'twocolumn')
  if (!small) setTimeout(initActiveMenu, 50)
}

onMounted(() => {
  initActiveMenu()
  document.getElementById('overlay')?.addEventListener('click', closeSidebarOnOverlay)
  window.addEventListener('resize', onResize)
  bindCollapseHandlers()
})

onBeforeUnmount(() => {
  document.getElementById('overlay')?.removeEventListener('click', closeSidebarOnOverlay)
  window.removeEventListener('resize', onResize)
})
</script>

<template>
  <div class="container-fluid"><div id="two-column-menu"></div>

    <template v-if="layoutType === 'vertical' || layoutType === 'semibox'">
      <ul class="navbar-nav h-100" id="navbar-nav">
        <MenuItems />
      </ul>
    </template>
  </div>
</template>