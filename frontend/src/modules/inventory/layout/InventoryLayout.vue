<template>
  <div class="app-wrap">
    <HeaderComponent />
    <SideBarComponent />
    <div class="backdrop" data-action="toggle-swap" data-toggleclass="app-mobile-menu-open"></div>
    <main class="app-body">
      <div class="app-content">
        <router-view />
      </div>
      <FooterComponent />
    </main>
    <DrawerComponent />
    <DrawerSettingsComponent />
  </div>
</template>
<script setup lang="ts">
import DrawerComponent from '@/modules/common/components/DrawerComponent.vue';
import DrawerSettingsComponent from '@/modules/common/components/DrawerSettingsComponent.vue';
import FooterComponent from '@/modules/common/components/FooterComponent.vue';
import HeaderComponent from '@/modules/common/components/HeaderComponent.vue';
import SideBarComponent from '@/modules/common/components/SideBarComponent.vue';
</script>
