<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'

import MenuItems from '@/components/menu-items.vue'
import NavBar from '@/components/nav-bar.vue'
import RightBar from '@/components/right-bar.vue'
import Footer from '@/components/footer.vue'
import { bindCollapseHandlers, markActiveByPath } from '@/composables/useSidebarMenu'

/**
 * Layout horizontal
 */
const route = useRoute()

// Marca el ítem activo al cargar y en cada cambio de ruta.
watch(() => route.path, (path) => markActiveByPath(path), { immediate: true })

onMounted(bindCollapseHandlers)
</script>

<template>
  <div>
    <div id="layout-wrapper">
      <NavBar />
      <!-- ========== App Menu ========== -->
      <div class="app-menu navbar-menu">
        <!-- LOGO -->
        <div class="navbar-brand-box">
          <!-- Dark Logo-->
          <router-link to="/" class="logo logo-dark">
            <span class="logo-sm">
              <img src="@/assets/images/logo-sm.png" alt="" height="22" />
            </span>
            <span class="logo-lg">
              <img src="@/assets/images/logo-dark.png" alt="" height="17" />
            </span>
          </router-link>
          <!-- Light Logo-->
          <router-link to="/" class="logo logo-light">
            <span class="logo-sm">
              <img src="@/assets/images/logo-sm.png" alt="" height="22" />
            </span>
            <span class="logo-lg">
              <img src="@/assets/images/logo-light.png" alt="" height="17" />
            </span>
          </router-link>
          <button class="btn btn-secondary btn-sm p-0 fs-20 header-item float-end btn-vertical-sm-hover"
            id="vertical-hover"
            type="button"><i class="ri-record-circle-line"></i>
          </button>
        </div>
        <div id="scrollbar">
          <div class="container-fluid"><ul class="navbar-nav h-100" id="navbar-nav">
        <MenuItems />
      </ul>
          </div>
          <!-- Sidebar -->
        </div>
        <!-- Left Sidebar End -->
        <!-- Vertical Overlay-->
        <div class="vertical-overlay"></div>
      </div>
      <!-- ============================================================== -->
      <!-- Start Page Content here -->
      <!-- ============================================================== -->

      <div class="main-content">
        <div class="page-content">
          <!-- Start Content-->
          <div class="container-fluid"><slot />
          </div>
        </div>
        <Footer />
      </div>
      <RightBar />
    </div>
  </div>
</template>