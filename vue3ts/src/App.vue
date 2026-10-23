<script setup lang="ts">
import AppDialog from '@/components/AppDialog.vue'
import FullScreenLoader from '@/components/FullScreenLoader.vue'
import { useIdleLogout } from '@/composables/useIdleLogout'
import { useLoadingStore } from '@/stores/loading'

const loadingStore = useLoadingStore()

// Cierra por inactividad las sesiones que no se marcaron como "mantener iniciada".
useIdleLogout()
</script>

<template>
  <FullScreenLoader v-if="loadingStore.isLoading" />
  <AppDialog />
  <router-view></router-view>
</template>
