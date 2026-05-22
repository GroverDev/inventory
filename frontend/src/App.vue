<template>
  <FullScreenLoaderComponent v-if="loadingStore.isLoading"></FullScreenLoaderComponent>
  <BaseDialog />
  <div v-if="layoutStore.mobileMenuOpen" class="mobile-backdrop" @click="layoutStore.closeMobileMenu()"></div>
  <RouterView />
</template>
<script setup lang="ts">
import FullScreenLoaderComponent from '@/modules/common/components/FullScreenLoaderComponent.vue';
import BaseDialog from '@/modules/common/components/BaseDialog.vue';
import { useDialogStore } from '@/stores/dialogStore';
import { useLoadingStore } from '@/modules/common/store/loadingStore';
import { useThemeStore } from '@/stores/themeStore';
import { useLayoutStore } from '@/stores/layoutStore';
import { useApp } from '@/composables/useApp';
import { RouterView } from 'vue-router';
import { provide } from 'vue';

const loadingStore = useLoadingStore();
const themeStore = useThemeStore();
const layoutStore = useLayoutStore();
const dialogStore = useDialogStore();

// Provide dialog globally for easy access
provide('dialog', dialogStore);

const { toggleFullscreen, printPage, initTooltips, initPopovers } = useApp();

themeStore.initTheme();

import { onMounted } from 'vue';

onMounted(() => {
    initTooltips();
    initPopovers();
});
</script>
<style>
/* Custom Scrollbar - Replaces smartSlimscroll */
.custom-scroll {
    overflow-y: auto;
    overflow-x: hidden;
    height: 100%;
}
.custom-scroll::-webkit-scrollbar {
    width: 6px;
    height: 6px;
}
.custom-scroll::-webkit-scrollbar-track {
    background: transparent;
}
.custom-scroll::-webkit-scrollbar-thumb {
    background-color: rgba(0,0,0,0.2);
    border-radius: 3px;
}
[data-bs-theme="dark"] .custom-scroll::-webkit-scrollbar-thumb {
    background-color: rgba(255,255,255,0.2);
}

.mobile-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background: rgba(0,0,0,0.5);
    z-index: 2499; /* Very high to override legacy layers */
    backdrop-filter: blur(2px);
    cursor: pointer;
}
</style>
<style scoped></style>
