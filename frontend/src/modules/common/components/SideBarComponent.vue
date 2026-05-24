<template>
  <aside class="app-sidebar d-flex flex-column" style="z-index: 2500 !important;">
    <!-- Filter Input (Hidden if empty/not used externally, or we can add it here explicitly if desired) -->
    <!-- For now, assuming external input sets layoutStore.filterText -->
    
    <div class="app-logo flex-shrink-0">
         <img src="/assets/img/logo.png" alt="logo">

      <!-- Logo Backdrop Animation-x -->
      <div class="logo-backdrop">
        <div class="blobs">
          <svg viewbox="0 0 1200 1200">
            <g class="blob blob-1">
              <path d="M 100 600 q 0 -700, 500 -500 t 500 500 t -500 500 T 100 600 z" />
            </g>
            <g class="blob blob-2">
              <path d="M 100 600 q -50 -400, 500 -500 t 450 550 t -500 500 T 100 600 z" />
            </g>
            <g class="blob blob-3">
              <path d="M 100 600 q 0 -400, 500 -500 t 400 500 t -500 500 T 100 600 z" />
            </g>
            <g class="blob blob-4">
              <path d="M 150 600 q 0 -600, 500 -500 t 500 550 t -500 500 T 150 600 z" />
            </g>
            <g class="blob blob-1 alt">
              <path d="M 150 600 q 0 -600, 500 -500 t 500 550 t -500 500 T 150 600 z" />
            </g>
            <g class="blob blob-2 alt">
              <path d="M 100 600 q 100 -600, 500 -500 t 400 500 t -500 500 T 100 600 z" />
            </g>
            <g class="blob blob-3 alt">
              <path d="M 100 600 q 0 -400, 500 -500 t 400 500 t -500 500 T 100 600 z" />
            </g>
            <g class="blob blob-4 alt">
              <path d="M 150 600 q 0 -600, 500 -500 t 500 550 t -500 500 T 150 600 z" />
            </g>
          </svg>
        </div>
      </div>
    </div>

    <nav id="js-primary-nav" class="primary-nav flex-grow-1 custom-scroll">
      <ul id="js-nav-menu" class="nav-menu">
        <template v-for="superpadre in filteredMenu" :key="superpadre.IdFormulario">
          <li class="nav-title">
            <span>{{ superpadre.titulo ?? '' }}</span>
          </li>
          <template v-for="padre in superpadre.Children" :key="padre.IdFormulario">
            <li class="nav-item" :class="{ 'open': isMenuOpen(padre.IdFormulario) }">
              <a href="javascript:void(0)" :title="padre.titulo" data-filter-tags @click="toggleMenu(padre.IdFormulario)">
                <svg class="sa-icon">
                  <use href="/assets/icons/sprite.svg#cpu"></use>
                </svg>
                <!-- <i class="sa-icon" :class="`${padre.classIcon}`"></i> -->
                <span class="nav-link-text" data-i18n>{{ padre.titulo }}</span>
                <span class="collapse-sign" v-if="padre.Children.length > 0">
                    <i :class="isMenuOpen(padre.IdFormulario) ? 'sa sa-chevron-up' : 'sa sa-chevron-down'"></i>
                </span>
              </a>
              <ul v-show="isMenuOpen(padre.IdFormulario)" style="display: block;">
                <template v-for="hijo in padre.Children" :key="hijo.IdFormulario">
                  <li class="nav-item" :class="{ active: isActive(hijo) }">
                    <a href="javascript:void(0)" @click="seleccionoOpcion(hijo)" :title="hijo.titulo">
                      <span class="nav-link-text" data-i18n>{{ hijo.titulo }}</span>
                    </a>
                  </li>
                </template>
              </ul>
            </li>
          </template>
        </template>
      </ul>

      <div class="no-results-msg pt-3 info-container">
        <h6 class="mb-1"> No existe menú disponible.</h6>
      </div>
    </nav>

    <div class="nav-footer">
      <svg class="sa-icon sa-thin">
        <use href="/assets/icons/sprite.svg#wifi"></use>
      </svg>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import { useLayoutStore } from '@/stores/layoutStore';
import { AccessMenu } from '@/modules/auth/models/acccessMenu.interface';

const authStore = useAuthStore();
const layoutStore = useLayoutStore();
const router = useRouter();
const route = useRoute();

const accessMenu = ref<AccessMenu[]>([]);
const openMenuId = ref<string | number | null>(null);

const isActive = (item: AccessMenu) => {
  if (!item.url) return false;
  // Logic matches seleccionoOpcion: remove params to find base route name
  const itemRouteName = item.url.replace('/:id', '');
  return route.name === itemRouteName;
};

const filteredMenu = computed(() => {
    if (!layoutStore.filterText) return accessMenu.value;
    
    const lowerFilter = layoutStore.filterText.toLowerCase();
    
    // Deep filter logic
    return accessMenu.value.map(superpadre => {
        const children = superpadre.Children.filter(padre => {
            const padreMatch = padre.titulo && padre.titulo.toLowerCase().includes(lowerFilter);
            const childrenMatch = padre.Children.some(hijo => hijo.titulo && hijo.titulo.toLowerCase().includes(lowerFilter));
            return padreMatch || childrenMatch;
        }).map(padre => {
             const matchingChildren = padre.Children.filter(hijo => hijo.titulo && hijo.titulo.toLowerCase().includes(lowerFilter));
             if (padre.titulo && padre.titulo.toLowerCase().includes(lowerFilter)) return padre;
             return { ...padre, Children: matchingChildren };
        });
        
        if (children.length > 0) {
            return { ...superpadre, Children: children };
        }
        return null;
    }).filter(item => item !== null) as AccessMenu[];
});

const toggleMenu = (id: string | number) => {
  if (openMenuId.value === id) {
    openMenuId.value = null;
  } else {
    openMenuId.value = id;
  }
};

const isMenuOpen = (id: string | number) => openMenuId.value === id;

declare global {
  interface Window {
    Navigation: new (element: HTMLElement, options?: any) => any
  }
}

onMounted(async () => {
  console.log('MENU');
  console.log(authStore.getAccessMenu);

  accessMenu.value = authStore.getAccessMenu;
  console.log(accessMenu.value);
});

const seleccionoOpcion = (opcion: AccessMenu) => {

  layoutStore.closeMobileMenu();

  let routeString = opcion.url.replace('/:id', '');
  if (routeString.includes('/:id')) {
    routeString = opcion.url.replace('/:id', '');
    router.push({ name: routeString, params: { id: opcion.IdFormulario } });
  }
  else {
    router.push({ name: routeString });
  }

}
</script>

<style scoped>
aside {
  overflow: hidden;
}

nav {
  /*
   * .custom-scroll global tiene height:100% que choca con flex-grow:1:
   * le dice "sé tan alto como el aside completo" mientras flex le asigna
   * solo el espacio sobrante → el nav se desborda, nunca hay overflow → sin scroll.
   * height:auto deja que flex controle el tamaño.
   */
  height: auto !important;
  /* sin min-height:0 un hijo flex no puede encogerse por debajo de su contenido */
  min-height: 0;
  overflow-y: auto !important;
  -webkit-overflow-scrolling: touch;
}

@media (max-width: 991px) {
  aside {
    /* dvh descuenta la barra del navegador (mejor que vh en Chrome/Safari móvil) */
    height: 100dvh;
  }
}
</style>
