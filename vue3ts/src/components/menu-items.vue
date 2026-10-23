<script setup lang="ts">
import { useRouter } from 'vue-router'
import { resolveLeaf, useMenu, type MenuLeaf } from '@/composables/useMenu'

// Ítems del menú lateral (vertical y horizontal) con el HTML de Velzon,
// generados desde el menú de accesos del usuario.
const router = useRouter()
const { items } = useMenu()

const target = (leaf: MenuLeaf) => resolveLeaf(router, leaf)
</script>

<template>
  <template v-for="item in items" :key="item.id">
    <li v-if="item.isTitle" class="menu-title">
      <span>{{ item.label }}</span>
    </li>

    <li v-else class="nav-item">
      <!-- Grupo desplegable -->
      <template v-if="item.subItems">
        <a class="nav-link menu-link" :href="'#sidebarMenu' + item.id" data-bs-toggle="collapse" role="button"
          aria-expanded="false" :aria-controls="'sidebarMenu' + item.id">
          <i :class="item.icon"></i>
          <span>{{ item.label }}</span>
        </a>
        <div class="collapse menu-dropdown" :id="'sidebarMenu' + item.id">
          <ul class="nav nav-sm flex-column">
            <li v-for="sub in item.subItems" :key="sub.id" class="nav-item">
              <router-link v-if="target(sub)" :to="target(sub)!" class="nav-link">{{ sub.label }}</router-link>
              <a v-else class="nav-link text-muted" href="javascript:void(0)" style="opacity: 0.55; cursor: not-allowed"
                title="Pantalla pendiente de migrar">{{ sub.label }}</a>
            </li>
          </ul>
        </div>
      </template>

      <!-- Formulario suelto -->
      <template v-else-if="item.leaf">
        <router-link v-if="target(item.leaf)" :to="target(item.leaf)!" class="nav-link menu-link">
          <i :class="item.icon"></i>
          <span>{{ item.label }}</span>
        </router-link>
        <a v-else class="nav-link menu-link text-muted" href="javascript:void(0)"
          style="opacity: 0.55; cursor: not-allowed" title="Pantalla pendiente de migrar">
          <i :class="item.icon"></i>
          <span>{{ item.label }}</span>
        </a>
      </template>
    </li>
  </template>
</template>
