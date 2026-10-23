<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import simplebar from 'simplebar-vue'

import NavBar from '@/components/nav-bar.vue'
import Menu from '@/components/menu.vue'
import RightBar from '@/components/right-bar.vue'
import Footer from '@/components/footer.vue'
import { useAuthStore } from '@/stores/auth'

/**
 * Layout vertical
 */
const router = useRouter()
const user = computed(() => useAuthStore().getUser)

localStorage.setItem('hoverd', 'false')
document.body.removeAttribute('data-layout')
document.body.removeAttribute('data-topbar')
document.body.removeAttribute('data-layout-size')

const logout = async () => {
  // Revoca la sesión en el servidor (y olvida el equipo) y limpia el navegador.
  await useAuthStore().logout()
  router.push({ name: 'login' })
}

const root = document.documentElement

/** Menú contraído: según el ancho de la ventana se usa el tamaño "sm" o "lg". */
const updateSidebarSize = () => {
  root.setAttribute('data-sidebar-size', window.innerWidth < 1025 ? 'sm' : 'lg')
}

/** Botón de "hover" de la barra lateral: alterna entre sm-hover y sm-hover-active. */
const toggleSidebarHover = () => {
  const size = root.getAttribute('data-sidebar-size')
  if (size === 'sm-hover') {
    localStorage.setItem('hoverd', 'true')
    root.setAttribute('data-sidebar-size', 'sm-hover-active')
  } else if (size === 'sm-hover-active') {
    localStorage.setItem('hoverd', 'false')
    root.setAttribute('data-sidebar-size', 'sm-hover')
  } else {
    root.setAttribute('data-sidebar-size', 'sm-hover')
  }
}

const closeSidebarOnOverlay = () => document.body.classList.remove('vertical-sidebar-enable')

const onResize = () => {
  document.body.classList.remove('vertical-sidebar-enable')
  document.querySelector('.hamburger-icon')?.classList.add('open')
  updateSidebarSize()
}

onMounted(() => {
  if (localStorage.getItem('hoverd') === 'true') root.setAttribute('data-sidebar-size', 'sm-hover-active')

  document.getElementById('overlay')?.addEventListener('click', closeSidebarOnOverlay)
  if (window.screen.width < 1025) root.setAttribute('data-sidebar-size', 'sm')
  window.addEventListener('resize', onResize)
})

onUnmounted(() => {
  document.getElementById('overlay')?.removeEventListener('click', closeSidebarOnOverlay)
  window.removeEventListener('resize', onResize)
})
</script>
  
<template>
  <div id="layout-wrapper">
    <NavBar />
    <div>
      <!-- ========== Left Sidebar Start ========== -->
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
            @click="toggleSidebarHover"
            type="button"><i class="ri-record-circle-line"></i>
          </button>
        </div>

        <div class="dropdown sidebar-user m-1 rounded">
          <button type="button" class="btn btn-link rounded-circle" data-bs-toggle="dropdown" aria-expanded="false"
            id="page-header-user-dropdown"><span class="d-flex align-items-center gap-2">
              <img class="rounded header-profile-user" src="@/assets/images/users/avatar-1.jpg" alt="Avatar">
              <span class="text-start">
                <span class="d-block fw-medium sidebar-user-name-text">{{ user?.FullName || user?.Email }}</span>
                <span class="d-block fs-14 sidebar-user-name-sub-text">{{ user?.RolName }}</span>
              </span>
            </span></button>
          <div class="dropdown-menu dropdown-menu-end">
            <h6 class="dropdown-header">¡Bienvenido{{ user?.FullName ? ', ' + user.FullName : '' }}!</h6>
            <a class="dropdown-item" href="javascript:void(0)" @click="logout"><i
                class="ri-logout-box-r-line text-muted fs-16 align-middle me-1"></i>
              <span class="align-middle">Cerrar sesión</span>
            </a>
          </div>
        </div>

        <simplebar id="scrollbar" class="h-100" ref="scrollbar">
          <Menu></Menu>
        </simplebar>
        <div class="sidebar-background"></div>
      </div>
      <!-- Left Sidebar End -->
      <!-- Vertical Overlay-->
      <div class="vertical-overlay" id="overlay"></div>
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
</template>