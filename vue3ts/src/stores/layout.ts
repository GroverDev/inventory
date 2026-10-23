import { defineStore } from 'pinia'

export const useLayoutStore = defineStore('layout', {
  state: () => ({
    layoutType: 'vertical',
    layoutWidth: 'fluid',
    sidebarSize: 'lg',
    topbar: 'light',
    mode: 'light',
    position: 'fixed',
    sidebarView: 'default',
    sidebarColor: 'dark',
    sidebarImage: 'none',
    preloader: 'disable',
    visibility: 'show',
    layoutTheme: 'default',
    themeColor: 'default',
    bodybg: undefined as string | undefined,
  }),
  // Las acciones conservan la firma que tenían con Vuex: reciben un objeto con la propiedad.
  actions: {
    changeLayoutType({ layoutType }: { layoutType: string }) {
      this.layoutType = layoutType
      document.body.removeAttribute('style')
    },
    changeLayoutWidth({ layoutWidth }: { layoutWidth: string }) {
      this.layoutWidth = layoutWidth
    },
    changeSidebarSize({ sidebarSize }: { sidebarSize: string }) {
      this.sidebarSize = sidebarSize
    },
    changeTopbar({ topbar }: { topbar: string }) {
      this.topbar = topbar
    },
    changeMode({ mode }: { mode: string }) {
      this.mode = mode
    },
    changePosition({ position }: { position: string }) {
      this.position = position
    },
    changeSidebarView({ sidebarView }: { sidebarView: string }) {
      this.sidebarView = sidebarView
    },
    changeSidebarColor({ sidebarColor }: { sidebarColor: string }) {
      this.sidebarColor = sidebarColor
    },
    changeSidebarImage({ sidebarImage }: { sidebarImage: string }) {
      this.sidebarImage = sidebarImage
    },
    changePreloader({ preloader }: { preloader: string }) {
      this.preloader = preloader
    },
    changeVisibility({ visibility }: { visibility: string }) {
      this.visibility = visibility
    },
    changeBodybg({ bodybg }: { bodybg: string }) {
      this.bodybg = bodybg
    },
    changeThemes({ layoutTheme }: { layoutTheme: string }) {
      this.layoutTheme = layoutTheme
    },
    changeThemesColor({ themeColor }: { themeColor: string }) {
      this.themeColor = themeColor
    },
  },
})
