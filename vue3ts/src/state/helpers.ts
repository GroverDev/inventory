import { mapState, mapActions } from 'pinia'
import { useLayoutStore } from '@/stores/layout'

// Helpers con la misma API que tenían con Vuex, para no tocar los componentes de la plantilla.

export const layoutComputed = {
  ...mapState(useLayoutStore, [
    'layoutType', 'sidebarSize', 'layoutWidth', 'topbar', 'mode', 'position', 'sidebarView',
    'sidebarColor', 'sidebarImage', 'visibility', 'bodybg', 'layoutTheme', 'themeColor',
  ]),
}

export const layoutMethods = mapActions(useLayoutStore, [
  'changeLayoutType', 'changeLayoutWidth', 'changeSidebarSize', 'changeTopbar', 'changeMode', 'changePosition',
  'changeSidebarView', 'changeSidebarColor', 'changeSidebarImage', 'changePreloader', 'changeVisibility',
  'changeBodybg', 'changeThemes', 'changeThemesColor',
])
