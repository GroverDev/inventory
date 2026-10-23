<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import simplebar from 'simplebar-vue'

import Menu from '@/components/menu.vue'
import NavBar from '@/components/nav-bar.vue'
import RightBar from '@/components/right-bar.vue'
import Footer from '@/components/footer.vue'
import {
  activateIconSidebarActive,
  bindCollapseHandlers,
  initActiveMenu,
} from '@/composables/useSidebarMenu'
import { resolveLeaf, useMenu, type MenuLeaf } from '@/composables/useMenu'
import { useLayoutStore } from '@/stores/layout'

/**
 * Layout de dos columnas: iconos a la izquierda y, al elegir uno, sus pantallas.
 * En pantallas chicas pasa a menú vertical.
 */
const router = useRouter()
const { groups } = useMenu()
const layoutType = computed(() => useLayoutStore().layoutType)
const rmenu = ref(window.localStorage.getItem('rmenu') || 'twocolumn')

const target = (leaf: MenuLeaf) => resolveLeaf(router, leaf)

document.body.removeAttribute('data-layout')
document.body.removeAttribute('data-topbar')
document.body.removeAttribute('data-layout-size')

/** Muestra el grupo de pantallas del icono pulsado. */
const updateMenu = (id: string, event: Event) => {
  document.body.classList.remove('twocolumn-panel')

  document.getElementById('navbar-nav')?.querySelectorAll('.show').forEach((item) => item.classList.remove('show'))
  document.getElementById('two-column-menu')?.querySelectorAll('.nav-icon.active').forEach((item) => item.classList.remove('active'))

  document.getElementById(id)!.classList.add('show')
  ;(event.target as HTMLElement).classList.add('active')
  activateIconSidebarActive('#' + id)
}

const closeSidebarOnOverlay = () => document.body.classList.remove('vertical-sidebar-enable')

const onResize = () => {
  if (layoutType.value !== 'twocolumn') return
  const small = document.documentElement.clientWidth < 767
  document.documentElement.setAttribute('data-layout', small ? 'vertical' : 'twocolumn')
  rmenu.value = small ? 'vertical' : 'twocolumn'
  localStorage.setItem('rmenu', rmenu.value)
  if (!small) setTimeout(initActiveMenu, 50)
}

onMounted(() => {
  initActiveMenu()
  if (rmenu.value === 'vertical' && layoutType.value === 'twocolumn') {
    document.documentElement.setAttribute('data-layout', 'vertical')
  }
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
            type="button"><i class="ri-record-circle-line"></i>
          </button>
        </div>
        <div id="scrollbar" v-if="rmenu == 'twocolumn'">
          <div class="container-fluid"><div id="two-column-menu">
              <simplebar class="twocolumn-iconview list-unstyled">
                <a class="logo"><img src="@/assets/images/logo-sm.png" alt="Logo" height="22" /></a>
                <li v-for="g in groups" :key="g.id">
                  <a v-if="g.subItems" :href="'#sidebarMenu' + g.id" class="nav-icon" role="button" :title="g.label"
                    @click.prevent="updateMenu('sidebarMenu' + g.id, $event)">
                    <i :class="g.icon"></i>
                  </a>
                  <router-link v-else-if="g.leaf && target(g.leaf)" :to="target(g.leaf)!" class="nav-icon" :title="g.label">
                    <i :class="g.icon"></i>
                  </router-link>
                </li>
              </simplebar>
            </div>
            <template v-if="layoutType === 'twocolumn'">
              <simplebar class="navbar-nav" id="navbar-nav">
                <li class="menu-title">
                  <span>Menú</span>
                </li>
                <li v-for="g in groups" :key="g.id" class="nav-item">
                  <div v-if="g.subItems" class="collapse menu-dropdown" :id="'sidebarMenu' + g.id">
                    <ul class="nav nav-sm flex-column">
                      <li v-for="sub in g.subItems" :key="sub.id" class="nav-item">
                        <router-link v-if="target(sub)" :to="target(sub)!" class="nav-link">{{ sub.label }}</router-link>
                        <a v-else class="nav-link text-muted" href="javascript:void(0)"
                          style="opacity: 0.55; cursor: not-allowed" title="Pantalla pendiente de migrar">{{ sub.label }}</a>
                      </li>
                    </ul>
                  </div>
                </li>
              </simplebar>
            </template>
          </div>
        </div>

        <!-- </BContainer>
      </div> -->

        <simplebar id="scrollbar" class="h-100" ref="scrollbar" v-if="rmenu == 'vertical'">
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