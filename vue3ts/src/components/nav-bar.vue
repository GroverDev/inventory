<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/stores/auth'
import { useLayoutStore } from '@/stores/layout'

/**
 * Barra superior: logo, menú hamburguesa, pantalla completa, modo oscuro y menú de usuario.
 */
const router = useRouter()
const layout = useLayoutStore()
const user = computed(() => useAuthStore().getUser)

const root = document.documentElement

const toggleHamburgerMenu = () => {
  const windowSize = root.clientWidth
  const layoutType = root.getAttribute('data-layout')

  root.setAttribute('data-sidebar-visibility', 'show')
  const visibilityType = root.getAttribute('data-sidebar-visibility')

  if (windowSize > 767) document.querySelector('.hamburger-icon')?.classList.toggle('open')

  // Menú horizontal
  if (root.getAttribute('data-layout') === 'horizontal') {
    document.body.classList.toggle('menu')
  }

  // Menú vertical
  if (visibilityType === 'show' && (layoutType === 'vertical' || layoutType === 'semibox')) {
    if (windowSize < 1025 && windowSize > 767) {
      document.body.classList.remove('vertical-sidebar-enable')
      root.setAttribute('data-sidebar-size', root.getAttribute('data-sidebar-size') === 'sm' ? '' : 'sm')
    } else if (windowSize > 1025) {
      document.body.classList.remove('vertical-sidebar-enable')
      root.setAttribute('data-sidebar-size', root.getAttribute('data-sidebar-size') === 'lg' ? 'sm' : 'lg')
    } else if (windowSize <= 767) {
      document.body.classList.add('vertical-sidebar-enable')
      root.setAttribute('data-sidebar-size', 'lg')
    }
  }

  // Dos columnas
  if (root.getAttribute('data-layout') === 'twocolumn') {
    document.body.classList.toggle('twocolumn-panel')
  }
}

// Las variantes con prefijo de navegador no están en los tipos del DOM.
type FullscreenDocument = Document & {
  mozFullScreenElement?: Element
  webkitFullscreenElement?: Element
  cancelFullScreen?: () => void
  mozCancelFullScreen?: () => void
  webkitCancelFullScreen?: () => void
}
type FullscreenElement = HTMLElement & {
  mozRequestFullScreen?: () => void
  webkitRequestFullscreen?: (flags?: number) => void
}

const initFullScreen = () => {
  const doc = document as FullscreenDocument
  const el = root as FullscreenElement
  document.body.classList.toggle('fullscreen-enable')
  if (!doc.fullscreenElement && !doc.mozFullScreenElement && !doc.webkitFullscreenElement) {
    if (el.requestFullscreen) el.requestFullscreen()
    else if (el.mozRequestFullScreen) el.mozRequestFullScreen()
    else if (el.webkitRequestFullscreen) el.webkitRequestFullscreen(1) // Element.ALLOW_KEYBOARD_INPUT
  } else if (doc.cancelFullScreen) doc.cancelFullScreen()
  else if (doc.mozCancelFullScreen) doc.mozCancelFullScreen()
  else if (doc.webkitCancelFullScreen) doc.webkitCancelFullScreen()
}

const toggleDarkMode = () => {
  root.setAttribute('data-bs-theme', root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark')
  layout.changeMode({ mode: root.getAttribute('data-bs-theme') as string })
}

const logout = async () => {
  // Revoca la sesión en el servidor (y olvida el equipo) y limpia el navegador.
  await useAuthStore().logout()
  router.push({ name: 'login' })
}

const onScroll = () => {
  const pageTopbar = document.getElementById('page-topbar')
  if (!pageTopbar) return
  const scrolled = document.body.scrollTop >= 50 || root.scrollTop >= 50
  pageTopbar.classList.toggle('topbar-shadow', scrolled)
}

onMounted(() => {
  document.addEventListener('scroll', onScroll)
  document.getElementById('topnav-hamburger-icon')?.addEventListener('click', toggleHamburgerMenu)
})
onBeforeUnmount(() => {
  document.removeEventListener('scroll', onScroll)
  document.getElementById('topnav-hamburger-icon')?.removeEventListener('click', toggleHamburgerMenu)
})
</script>

<template>
  <header id="page-topbar">
    <div class="layout-width">
      <div class="navbar-header">
        <div class="d-flex">
          <!-- LOGO -->
          <div class="navbar-brand-box horizontal-logo">
            <router-link to="/" class="logo logo-dark">
              <span class="logo-sm">
                <img src="@/assets/images/logo-sm.png" alt="" height="22" />
              </span>
              <span class="logo-lg">
                <img src="@/assets/images/logo-dark.png" alt="" height="17" />
              </span>
            </router-link>

            <router-link to="/" class="logo logo-light">
              <span class="logo-sm">
                <img src="@/assets/images/logo-sm.png" alt="" height="22" />
              </span>
              <span class="logo-lg">
                <img src="@/assets/images/logo-light.png" alt="" height="17" />
              </span>
            </router-link>
          </div>

          <button type="button"
            class="btn btn-white btn-sm px-3 fs-16 header-item vertical-menu-btn topnav-hamburger material-shadow-none"
            id="topnav-hamburger-icon"><span class="hamburger-icon">
              <span></span>
              <span></span>
              <span></span>
            </span>
          </button>
        </div>

        <div class="d-flex align-items-center">
          <div class="ms-1 header-item d-none d-sm-flex">
            <button type="button"
              class="btn btn-ghost-secondary btn-icon btn-topbar rounded-circle material-shadow-none"
              data-toggle="fullscreen" @click="initFullScreen"><i class="ri-fullscreen-line fs-22"></i>
            </button>
          </div>

          <div class="ms-1 header-item d-none d-sm-flex">
            <button type="button"
              class="btn btn-ghost-secondary btn-icon btn-topbar rounded-circle light-dark-mode material-shadow-none"
              @click="toggleDarkMode"><i class="ri-moon-line fs-22"></i>
            </button>
          </div>

          <div class="dropdown ms-sm-3 header-item topbar-user">
            <button type="button" class="btn btn-link rounded-circle material-shadow-none" data-bs-toggle="dropdown"
              aria-expanded="false" id="page-header-user-dropdown"><span class="d-flex align-items-center">
                <img class="rounded-circle header-profile-user" src="@/assets/images/users/avatar-1.jpg"
                  alt="Avatar">
                <span class="text-start ms-xl-2">
                  <span class="d-none d-xl-inline-block ms-1 fw-medium user-name-text">{{ user?.FullName || user?.Email }}</span>
                  <span class="d-none d-xl-block ms-1 fs-12 user-name-sub-text">{{ user?.RolName }}</span>
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
        </div>
      </div>
    </div>
  </header>
</template>
