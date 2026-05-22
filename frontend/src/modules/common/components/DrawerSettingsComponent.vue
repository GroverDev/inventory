<template>
  <aside class="app-drawer js-drawer-settings" :class="{ open: layoutStore.settingsDrawerOpen }">
    <div class="app-drawer-header">
      <div class="h4 mb-0">
        App Builder
      </div>
      <button type="button" class="btn btn-system ms-auto" @click="layoutStore.toggleSettingsDrawer()" aria-label="Close">
        <svg class="sa-icon sa-icon-2x">
          <use href="/assets/icons/sprite.svg#x"></use>
        </svg>
      </button>
    </div>

    <div class="custom-scrollbar h-100">



      <div class="d-flex justify-content-spaced w-100 app-fob-showcase-text" data-prefix="Preview">
        <div class="app-fob app-fob-lg app-fob-showcase">
          <div></div>
          <div></div>
          <div></div>
        </div>
      </div>

      <div class="mod-status primary-mod" data-prefix="Primary Settings">
        <div class="form-check">
          <input class="form-check-input" type="checkbox" id="actionHeaderFixed" data-action="toggle"
            data-class="set-header-fixed">
          <label class="form-check-label" for="actionHeaderFixed">
            Header position fixed
          </label>
        </div>
        <div class="form-check d-none d-lg-block d-xl-block d-xxl-block">
          <input class="form-check-input" type="checkbox" id="actionNavFull" data-action="toggle"
            data-class="set-nav-full" data-codependence="set-nav-collapsed">
          <label class="form-check-label" for="actionNavFull">
            Navigation full height
          </label>
        </div>
      </div>

      <div class="mod-status d-none d-lg-block d-xl-block d-xxl-block" data-prefix="Addon Settings">
        <div class="form-check">
          <input class="form-check-input" type="checkbox" id="actionNavFixed" data-action="toggle"
            data-class="set-nav-fixed">
          <label class="form-check-label" for="actionNavFixed">
            Navigation position fixed
          </label>
        </div>
        <div class="form-check">
          <input class="form-check-input" type="checkbox" id="actionNavCollapsed" data-action="toggle"
            data-class="set-nav-collapsed" data-dependency="set-nav-full">
          <label class="form-check-label" for="actionNavCollapsed">
            Navigation collapsed
          </label>
        </div>
      </div>

      <div class="mod-status" data-prefix="Misc Settings">
        <div class="form-check d-none d-lg-block d-xl-block d-xxl-block">
          <input class="form-check-input" type="checkbox" id="actionNavMinified" data-action="toggle"
            data-class="set-nav-minified">
          <label class="form-check-label" for="actionNavMinified">
            Navigation Minified
          </label>
        </div>
        <!-- <div class="form-check">
          <input class="form-check-input" type="checkbox" id="darkNavigation" data-action="toggle"
            data-class="set-nav-dark" checked>
          <label class="form-check-label" for="darkNavigation">
            Dark Navigation
          </label>
        </div> -->
      </div>

      <div class="mod-status" data-prefix="Aria Settings">
        <div class="form-check">
          <input class="form-check-input" type="checkbox" id="actionColorblindMode" data-action="toggle"
            data-class="set-colorblind-mode">
          <label class="form-check-label" for="actionColorblindMode">
            Colorblind Mode
          </label>
        </div>
        <div class="form-check">
          <input class="form-check-input" type="checkbox" id="actionHighContrastMode" data-action="toggle"
            data-class="set-high-contrast-mode">
          <label class="form-check-label" for="actionHighContrastMode">
            High Contrast Mode
          </label>
        </div>
      </div>



      <div class="d-flex" style="gap:10px">
        <button type="button" onclick="appDOM.resetStyle();" class="btn reset-button btn-outline-danger flex-grow-1">
          <i class="sa sa-reload align-base me-1"></i> Factory Reset</button>
        <button type="button" onclick="resetPanelState();" class="btn btn-outline-secondary flex-grow-1"><i
            class="sa sa-reload align-base me-1"></i> Reset Panel</button>
      </div>
    </div>
  </aside>

  <div class="backdrop" @click="layoutStore.toggleSettingsDrawer()">
  </div>
</template>

<script setup lang="ts">
import { useLayoutStore } from '@/stores/layoutStore';
const layoutStore = useLayoutStore();
</script>

<style scoped></style>
