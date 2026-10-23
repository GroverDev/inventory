<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import simpleBar from 'simplebar-vue'

import { useLayoutStore } from '@/stores/layout'

/**
 * Panel lateral derecho: personalizador del tema (layout, colores, barra lateral, fondo...).
 * Cada ajuste vive en el store `layout` y se refleja como atributo `data-*` del <html>.
 */
localStorage.setItem('rightbar_isopen', 'false')

const layout = useLayoutStore()
const root = document.documentElement

const show = ref(false)
const showGradients = ref(false)
const dataSidebarUserShow = ref(false)

// Valores con los que arrancó el personalizador: "Reset" vuelve a ellos.
localStorage.setItem('resetValue', JSON.stringify(layout.$state))

const click = () => {
  show.value = !show.value
}

const topFunction = () => {
  document.body.scrollTop = 0
  root.scrollTop = 0
}

const hamburger = () => document.querySelector('.hamburger-icon')

// ── Ajustes: lectura/escritura sobre el store ──
const layoutTheme = computed(() => layout.layoutTheme)

const layoutType = computed({
  get: () => layout.layoutType,
  set: (value: string) => {
    localStorage.setItem('rightbar_isopen', 'true')
    layout.changeLayoutType({ layoutType: value })
    hamburger()?.classList.remove('open')
  },
})
const preloader = computed({
  get: () => layout.preloader,
  set: (value: string) => layout.changePreloader({ preloader: value }),
})
const mode = computed({
  get: () => layout.mode,
  set: (value: string) => {
    layout.changeMode({ mode: value })
    layout.changeTopbar({ topbar: 'light' })
  },
})
const sidebarSize = computed({
  get: () => layout.sidebarSize,
  set: (value: string) => layout.changeSidebarSize({ sidebarSize: value }),
})
const layoutWidth = computed({
  get: () => layout.layoutWidth,
  set: (value: string) => {
    layout.changeLayoutWidth({ layoutWidth: value })
    layout.changeSidebarSize({ sidebarSize: value === 'boxed' ? 'sm-hover' : 'lg' })
  },
})
const position = computed({
  get: () => layout.position,
  set: (value: string) => layout.changePosition({ position: value }),
})
const topbar = computed({
  get: () => layout.topbar,
  set: (value: string) => layout.changeTopbar({ topbar: value }),
})
const sidebarView = computed({
  get: () => layout.sidebarView,
  set: (value: string) => layout.changeSidebarView({ sidebarView: value }),
})
const sidebarColor = computed({
  get: () => layout.sidebarColor,
  set: (value: string) => layout.changeSidebarColor({ sidebarColor: value }),
})
const sidebarImage = computed({
  get: () => layout.sidebarImage,
  set: (value: string) => layout.changeSidebarImage({ sidebarImage: value }),
})
const visibility = computed({
  get: () => layout.visibility,
  set: (value: string) => {
    if (value === 'hidden') hamburger()?.classList.add('open')
    else hamburger()?.classList.remove('open')
    layout.changeVisibility({ visibility: value })
  },
})
const bodybg = computed({
  get: () => layout.bodybg,
  set: (value: string | undefined) => {
    root.setAttribute('data-body-image', String(value))
    layout.changeBodybg({ bodybg: value as string })
  },
})
const layoutThemes = computed({
  get: () => layout.layoutTheme,
  set: (value: string) => {
    layout.changeThemes({ layoutTheme: value })
    layout.changeMode({ mode: value === 'galaxy' ? 'dark' : 'light' })
  },
})
const themeColor = computed({
  get: () => layout.themeColor,
  set: (value: string) => layout.changeThemesColor({ themeColor: value }),
})

// ── Reflejo en el <html>: cada ajuste se publica como atributo si su valor es uno de los conocidos ──
const bindAttribute = (source: () => string | undefined, attribute: string, allowed: readonly string[]) =>
  watch(
    source,
    (value) => {
      if (value !== undefined && allowed.includes(value)) root.setAttribute(attribute, value)
    },
    { immediate: true },
  )

bindAttribute(() => mode.value, 'data-bs-theme', ['dark', 'light'])
bindAttribute(() => layoutType.value, 'data-layout', ['horizontal', 'vertical', 'twocolumn', 'semibox'])
bindAttribute(() => layoutWidth.value, 'data-layout-width', ['fluid', 'boxed'])
bindAttribute(() => position.value, 'data-layout-position', ['fixed', 'scrollable'])
bindAttribute(() => topbar.value, 'data-topbar', ['light', 'dark'])
bindAttribute(() => sidebarSize.value, 'data-sidebar-size', ['lg', 'sm', 'md', 'sm-hover'])
bindAttribute(() => sidebarView.value, 'data-layout-style', ['detached', 'default'])
bindAttribute(() => sidebarColor.value, 'data-sidebar', ['dark', 'light', 'gradient', 'gradient-2', 'gradient-3', 'gradient-4'])
bindAttribute(() => sidebarImage.value, 'data-sidebar-image', ['img-1', 'img-2', 'img-3', 'img-4', 'none'])
bindAttribute(() => visibility.value, 'data-sidebar-visibility', ['show', 'hidden'])
bindAttribute(
  () => layoutThemes.value,
  'data-theme',
  ['default', 'saas', 'corporate', 'galaxy', 'material', 'creative', 'minimal', 'modern', 'interactive', 'classic', 'vintage'],
)
bindAttribute(() => themeColor.value, 'data-theme-colors', ['default', 'green', 'purple', 'blue'])

watch(
  () => preloader.value,
  (value) => {
    if (value === 'enable' || value === 'disable') root.setAttribute('data-preloader', value)
    localStorage.setItem('data-preloader', value)
  },
  { immediate: true },
)

/** Imagen de fondo del cuerpo: se recuerda en sessionStorage entre cambios de ajuste. */
const applyBodyImage = (value: string | undefined) => {
  const setImage = (image: string) => {
    sessionStorage.setItem('data-body-image', image)
    root.setAttribute('data-body-image', image)
  }
  switch (value) {
    case 'img-1':
      sessionStorage.setItem('data-sidebabodyr-image', 'img-1')
      root.setAttribute('data-body-image', 'img-1')
      if (document.getElementById('theme-settings-offcanvas')) root.removeAttribute('data-sidebar-image')
      break
    case 'img-2':
    case 'img-3':
    case 'none':
      setImage(value)
      break
    default: {
      const stored = sessionStorage.getItem('data-body-image')
      if (stored === 'img-1') {
        setImage('img-1')
        if (document.getElementById('theme-settings-offcanvas')) {
          const sidebarImg = document.getElementById('sidebar-img')
          if (sidebarImg) sidebarImg.style.display = 'none'
          root.removeAttribute('data-sidebar-image')
        }
      } else if (stored === 'img-2' || stored === 'img-3' || stored === 'none') {
        setImage(stored)
      }
    }
  }
}
watch(() => bodybg.value, applyBodyImage, { immediate: true })

watch(dataSidebarUserShow, (value) => {
  if (value) root.setAttribute('data-sidebar-user-show', 'true')
  else root.removeAttribute('data-sidebar-user-show')
})

// ── Acciones ──
const resetLayout = () => {
  const reset = JSON.parse(localStorage.getItem('resetValue') as string) as typeof layout.$state
  root.setAttribute('data-sidebar-size', 'lg')
  layout.changeMode({ mode: reset.mode })
  layout.changeSidebarColor({ sidebarColor: reset.sidebarColor })
  layout.changeLayoutType({ layoutType: reset.layoutType })
  layout.changeTopbar({ topbar: reset.topbar })
  layout.changeLayoutWidth({ layoutWidth: reset.layoutWidth })
  layout.changeSidebarSize({ sidebarSize: reset.sidebarSize })
  layout.changeSidebarImage({ sidebarImage: reset.sidebarImage })
  layout.changePreloader({ preloader: reset.preloader })
  layout.changeSidebarView({ sidebarView: reset.sidebarView })
  layout.changeVisibility({ visibility: reset.visibility })
  layout.changePosition({ position: reset.position })
  layout.changeThemesColor({ themeColor: reset.themeColor })
}

const onSideBarColorClick = (color: string) => {
  if (color !== 'gradient') {
    showGradients.value = false
  } else {
    showGradients.value = true
    layout.changeSidebarColor({ sidebarColor: 'gradient' })
  }
}

onMounted(() => {
  // Botón "volver arriba": aparece al bajar más de 100px.
  const backToTop = document.getElementById('back-to-top')
  if (backToTop) {
    window.onscroll = () => {
      backToTop.style.display = document.body.scrollTop > 100 || root.scrollTop > 100 ? 'block' : 'none'
    }
  }

  // Precarga (preloader): oculta el elemento, con retardo si está habilitada.
  const preloaderEl = document.getElementById('preloader')
  const hidePreloader = () => {
    if (!preloaderEl) return
    preloaderEl.style.opacity = '0'
    preloaderEl.style.visibility = 'hidden'
  }
  if (localStorage.getItem('data-preloader') === 'enable') {
    root.setAttribute('data-preloader', 'enable')
    setTimeout(hidePreloader, 1000)
  } else {
    root.setAttribute('data-preloader', 'disable')
    hidePreloader()
  }
})
</script>

<template>
  <div>
    <div id="preloader">
      <div id="status">
        <div class="spinner-border text-primary avatar-sm" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
      </div>
    </div>

    <button @click="topFunction"
      class="btn btn-danger btn-icon"
      id="back-to-top"
      type="button"><i class="ri-arrow-up-line"></i>
    </button>

    <div class="customizer-setting d-none d-md-block" @click="click">
      <div class="btn-info rounded-pill shadow-lg btn btn-icon btn-lg p-2" data-bs-toggle="offcanvas"
        data-bs-target="#theme-settings-offcanvas" aria-controls="theme-settings-offcanvas" id="theme-settings-btn">
        <i class="ri-settings-3-line ri-spin fs-22"></i>
      </div>
    </div>
    <div v-if="show" class="offcanvas-backdrop fade show" @click="click"></div>
    <div class="offcanvas offcanvas-end border-0" :class="{ show: show }"
      :style="{ visibility: show ? 'visible' : 'hidden' }" id="theme-settings-offcanvas" tabindex="-1">
      <div class="offcanvas-header d-flex align-items-center bg-primary bg-gradient p-3">
        <div class="me-2">
          <h5 class="m-0 me-2 text-white">Theme Customizer</h5>
        </div>
        <button class="btn btn-link btn-close btn-close-white ms-auto" id="customizerclose-btn" @click="click" type="button">
        </button>
      </div>
      <div class="offcanvas-body p-0">
      <simpleBar class="h-100">
        <div class="p-4">
          <h6 class="mb-0 fw-semibold text-uppercase">Layout</h6>
          <p class="text-muted">Choose your layout</p>

          <div class="row gy-3"><div class="col-4"><div class="form-check card-radio">
                <input id="customizer-layout01" name="data-layout" type="radio" value="vertical"
                  class="form-check-input" v-model="layoutType" />
                <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="customizer-layout01">
                  <span class="d-flex gap-1 h-100">
                    <span class="flex-shrink-0">
                      <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                        <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                      </span>
                    </span>
                    <span class="flex-grow-1">
                      <span class="d-flex h-100 flex-column">
                        <span class="bg-light d-block p-1"></span>
                        <span class="bg-light d-block p-1 mt-auto"></span>
                      </span>
                    </span>
                  </span>
                </label>
              </div>
              <h5 class="fs-13 text-center mt-2">Vertical</h5>
            </div>
            <div class="col-4"><div class="form-check card-radio">
                <input id="customizer-layout02" name="data-layout" type="radio" value="horizontal"
                  class="form-check-input" v-model="layoutType" />
                <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="customizer-layout02">
                  <span class="d-flex h-100 flex-column gap-1">
                    <span class="bg-light d-flex p-1 gap-1 align-items-center">
                      <span class="d-block p-1 bg-primary-subtle rounded me-1"></span>
                      <span class="d-block p-1 pb-0 px-2 bg-primary-subtle ms-auto"></span>
                      <span class="d-block p-1 pb-0 px-2 bg-primary-subtle"></span>
                    </span>
                    <span class="bg-light d-block p-1"></span>
                    <span class="bg-light d-block p-1 mt-auto"></span>
                  </span>
                </label>
              </div>
              <h5 class="fs-13 text-center mt-2">Horizontal</h5>
            </div>
            <div class="col-4"><div class="form-check card-radio">
                <input id="customizer-layout03" name="data-layout" type="radio" value="twocolumn"
                  class="form-check-input" v-model="layoutType" />
                <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="customizer-layout03">
                  <span class="d-flex gap-1 h-100">
                    <span class="flex-shrink-0">
                      <span class="bg-light d-flex h-100 flex-column gap-1">
                        <span class="d-block p-1 bg-primary-subtle mb-2"></span>
                        <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                      </span>
                    </span>
                    <span class="flex-shrink-0">
                      <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                      </span>
                    </span>
                    <span class="flex-grow-1">
                      <span class="d-flex h-100 flex-column">
                        <span class="bg-light d-block p-1"></span>
                        <span class="bg-light d-block p-1 mt-auto"></span>
                      </span>
                    </span>
                  </span>
                </label>
              </div>
              <h5 class="fs-13 text-center mt-2">Two Column</h5>
            </div>
            <div class="col-4"><div class="form-check card-radio">
                <input id="customizer-layout04" name="data-layout" type="radio" value="semibox" class="form-check-input"
                  v-model="layoutType" />
                <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="customizer-layout04">
                  <span class="d-flex gap-1 h-100">
                    <span class="flex-shrink-0 p-1">
                      <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                        <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                      </span>
                    </span>
                    <span class="flex-grow-1">
                      <span class="d-flex h-100 flex-column pt-1 pe-2">
                        <span class="bg-light d-block p-1"></span>
                        <span class="bg-light d-block p-1 mt-auto"></span>
                      </span>
                    </span>
                  </span>
                </label>
              </div>
              <h5 class="fs-13 text-center mt-2">Semi Box</h5>
            </div>
          </div>

          <div class="form-check form-switch form-switch-md mb-3 mt-4">
            <input type="checkbox" class="form-check-input" id="sidebarUserProfile" v-model="dataSidebarUserShow">
            <label class="form-check-label sidebar-profile" for="sidebarUserProfile">Sidebar User Profile Avatar</label>
          </div>

          <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Theme</h6>
          <p class="text-muted">Choose your suitable Theme.</p>

          <div class="row"><div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme01" name="data-theme" type="radio" value="default" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme01">
                  <img src="@/assets/images/demo/default.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Default</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme02" name="data-theme" type="radio" value="saas" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme02">
                  <img src="@/assets/images/demo/saas.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Sass</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme03" name="data-theme" type="radio" value="corporate" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme03">
                  <img src="@/assets/images/demo/corporate.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Corporate</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme04" name="data-theme" type="radio" value="galaxy" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme04">
                  <img src="@/assets/images/demo/galaxy.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Galaxy</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme05" name="data-theme" type="radio" value="material" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme05">
                  <img src="@/assets/images/demo/material.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Material</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme06" name="data-theme" type="radio" value="creative" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme06">
                  <img src="@/assets/images/demo/creative.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Creative</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme07" name="data-theme" type="radio" value="minimal" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme07">
                  <img src="@/assets/images/demo/minimal.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Minimal</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme08" name="data-theme" type="radio" value="modern" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme08">
                  <img src="@/assets/images/demo/modern.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Modern</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme09" name="data-theme" type="radio" value="interactive"
                  class="form-check-input" v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme09">
                  <img src="@/assets/images/demo/interactive.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Interactive</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme10" name="data-theme" type="radio" value="classic" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme10">
                  <img src="@/assets/images/demo/classic.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Classic</h5>
            </div>
            <div class="col-6"><div class="form-check card-radio">
                <input id="customizer-theme11" name="data-theme" type="radio" value="vintage" class="form-check-input"
                  v-model="layoutThemes">
                <label class="form-check-label p-0" for="customizer-theme11">
                  <img src="@/assets/images/demo/vintage.png" alt="" class="img-fluid">
                </label>
              </div>
              <h5 class="fs-13 text-center fw-medium mt-2">Vintage</h5>
            </div>
          </div>

          <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Color Scheme</h6>
          <p class="text-muted">Choose Light or Dark Scheme.</p>

          <div class="colorscheme-cardradio">
            <div class="row"><div class="col-4"><div class="form-check card-radio">
                  <input class="form-check-input" type="radio" name="data-bs-theme" id="layout-mode-light" value="light"
                    v-model="mode" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="layout-mode-light">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Light</h5>
              </div>

              <div class="col-4"><div class="form-check card-radio dark">
                  <input class="form-check-input" v-model="mode" type="radio" name="data-bs-theme" id="layout-mode-dark"
                    value="dark" />
                  <label class="form-check-label p-0 avatar-md w-100 bg-dark material-shadow" for="layout-mode-dark">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-white bg-opacity-10 d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-white bg-opacity-10 rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-white bg-opacity-10 d-block p-1"></span>
                          <span class="bg-white bg-opacity-10 d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Dark</h5>
              </div>
            </div>
          </div>
          <div id="sidebar-visibility" v-if="layoutType == 'semibox'">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">
              Sidebar Visibility
            </h6>
            <p class="text-muted">Choose show or Hidden sidebar.</p>

            <div class="row"><div class="col-4"><div class="form-check card-radio">
                  <input class="form-check-input" type="radio" name="data-sidebar-visibility"
                    id="sidebar-visibility-show" value="show" v-model="visibility" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-visibility-show">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0 p-1">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column pt-1 pe-2">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Show</h5>
              </div>
              <div class="col-4"><div class="form-check card-radio">
                  <input class="form-check-input" type="radio" name="data-sidebar-visibility"
                    id="sidebar-visibility-hidden" value="hidden" v-model="visibility" />
                  <label class="form-check-label p-0 avatar-md w-100 px-2 material-shadow"
                    for="sidebar-visibility-hidden">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column pt-1 px-2">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Hidden</h5>
              </div>
            </div>
          </div>

          <div v-if="layoutType == 'vertical' || layoutType == 'horizontal'" id="layout-width">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Layout Width</h6>
            <p class="text-muted">Choose Fluid or Boxed layout.</p>

            <div class="row"><div class="col-4"><div class="form-check card-radio">
                  <input class="form-check-input" type="radio" v-model="layoutWidth" name="data-layout-width"
                    id="layout-width-fluid" value="fluid" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="layout-width-fluid">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Fluid</h5>
              </div>
              <div class="col-4"><div class="form-check card-radio">
                  <input class="form-check-input" type="radio" v-model="layoutWidth" name="data-layout-width"
                    id="layout-width-boxed" value="boxed" />
                  <label class="form-check-label p-0 avatar-md w-100 px-2 material-shadow" for="layout-width-boxed">
                    <span class="d-flex gap-1 h-100 border-start border-end">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Boxed</h5>
              </div>
            </div>
          </div>

          <div v-if="layoutType != 'twocolumn'" id="layout-position">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">
              Layout Position
            </h6>
            <p class="text-muted">
              Choose Fixed or Scrollable Layout Position.
            </p>

            <div class="btn-group radio" role="group">
              <input type="radio" class="btn-check" name="data-layout-position" id="layout-position-fixed" value="fixed"
                v-model="position" />
              <label class="btn btn-light w-sm" for="layout-position-fixed">Fixed</label>

              <input type="radio" class="btn-check" name="data-layout-position" id="layout-position-scrollable"
                value="scrollable" v-model="position" />
              <label class="btn btn-light w-sm ms-0" for="layout-position-scrollable">Scrollable</label>
            </div>
          </div>
          <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Topbar Color</h6>
          <p class="text-muted">Choose Light or Dark Topbar Color.</p>

          <div class="row"><div class="col-4"><div class="form-check card-radio">
                <input class="form-check-input" type="radio" name="data-topbar" id="topbar-color-light" value="light"
                  v-model="topbar" />
                <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="topbar-color-light">
                  <span class="d-flex gap-1 h-100">
                    <span class="flex-shrink-0">
                      <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                        <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>

                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                      </span>
                    </span>
                    <span class="flex-grow-1">
                      <span class="d-flex h-100 flex-column">
                        <span class="bg-light d-block p-1"></span>
                        <span class="bg-light d-block p-1 mt-auto"></span>
                      </span>
                    </span>
                  </span>
                </label>
              </div>
              <h5 class="fs-13 text-center mt-2">Light</h5>
            </div>
            <div class="col-4"><div class="form-check card-radio">
                <input class="form-check-input" type="radio" name="data-topbar" id="topbar-color-dark" value="dark"
                  v-model="topbar" />
                <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="topbar-color-dark">
                  <span class="d-flex gap-1 h-100">
                    <span class="flex-shrink-0">
                      <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                        <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>

                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                      </span>
                    </span>
                    <span class="flex-grow-1">
                      <span class="d-flex h-100 flex-column">
                        <span class="bg-primary d-block p-1"></span>
                        <span class="bg-light d-block p-1 mt-auto"></span>
                      </span>
                    </span>
                  </span>
                </label>
              </div>
              <h5 class="fs-13 text-center mt-2">Dark</h5>
            </div>
          </div>
          <div v-if="layoutType == 'vertical' ||
            (layoutType == 'semibox' && visibility == 'show')
          " id="sidebar-size">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Sidebar Size</h6>
            <p class="text-muted">Choose a size of Sidebar.</p>

            <div class="row"><div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" type="radio" name="data-sidebar-size" id="sidebar-size-default"
                    v-model="sidebarSize" value="lg" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-size-default">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>

                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Default</h5>
              </div>

              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" type="radio" name="data-sidebar-size" id="sidebar-size-compact"
                    v-model="sidebarSize" value="md" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-size-compact">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 bg-primary-subtle rounded mb-2"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Compact</h5>
              </div>

              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="sidebarSize" type="radio" name="data-sidebar-size"
                    id="sidebar-size-small" value="sm" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-size-small">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1">
                          <span class="d-block p-1 bg-primary-subtle mb-2"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Small (Icon View)</h5>
              </div>

              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="sidebarSize" type="radio" name="data-sidebar-size"
                    id="sidebar-size-small-hover" value="sm-hover" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-size-small-hover">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1">
                          <span class="d-block p-1 bg-primary-subtle mb-2"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Small Hover View</h5>
              </div>
            </div>
          </div>

          <div v-if="layoutType == 'vertical'" id="sidebar-view">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Sidebar View</h6>
            <p class="text-muted">Choose Default or Detached Sidebar view.</p>

            <div class="row"><div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="sidebarView" type="radio" name="data-layout-style"
                    id="sidebar-view-default" value="default" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-view-default">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>

                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Default</h5>
              </div>
              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="sidebarView" type="radio" name="data-layout-style"
                    id="sidebar-view-detached" value="detached" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-view-detached">
                    <span class="d-flex h-100 flex-column">
                      <span class="bg-light d-flex p-1 gap-1 align-items-center px-2">
                        <span class="d-block p-1 bg-primary-subtle rounded me-1"></span>
                        <span class="d-block p-1 pb-0 px-2 bg-primary-subtle ms-auto"></span>

                        <span class="d-block p-1 pb-0 px-2 bg-primary-subtle"></span>
                      </span>
                      <span class="d-flex gap-1 h-100 p-1 px-2">
                        <span class="flex-shrink-0">
                          <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                            <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                            <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                            <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          </span>
                        </span>
                      </span>
                      <span class="bg-light d-block p-1 mt-auto px-2"></span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Detached</h5>
              </div>
            </div>
          </div>

          <div v-if="layoutType == 'vertical' ||
            layoutType === 'twocolumn' ||
            (layoutType === 'semibox' && visibility === 'show')
          " id="sidebar-color">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Sidebar Color</h6>
            <p class="text-muted">Choose Ligth or Dark Sidebar Color.</p>

            <div class="row"><div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="sidebarColor" type="radio" name="data-sidebar"
                    id="sidebar-color-light" value="light" @click="onSideBarColorClick('light')" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-color-light">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-white border-end d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>

                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Light</h5>
              </div>
              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="sidebarColor" type="radio" name="data-sidebar"
                    id="sidebar-color-dark" value="dark" @click="onSideBarColorClick('dark')" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="sidebar-color-dark">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-primary d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-white bg-opacity-10 rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Dark</h5>
              </div>
              <div class="col-4"><button class="btn btn-link avatar-md w-100 p-0 overflow-hidden border"
                  @click="onSideBarColorClick('gradient')"
                  type="button"><span class="d-flex gap-1 h-100">
                    <span class="flex-shrink-0">
                      <span class="bg-vertical-gradient d-flex h-100 flex-column gap-1 p-1">
                        <span class="d-block p-1 px-2 bg-white bg-opacity-10 rounded mb-2"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                        <span class="d-block p-1 px-2 pb-0 bg-white bg-opacity-10"></span>
                      </span>
                    </span>
                    <span class="flex-grow-1">
                      <span class="d-flex h-100 flex-column">
                        <span class="bg-light d-block p-1"></span>
                        <span class="bg-light d-block p-1 mt-auto"></span>
                      </span>
                    </span>
                  </span>
                </button>
                <h5 class="fs-13 text-center mt-2">Gradient</h5>
              </div>
            </div>
            <div v-if="showGradients" id="collapseBgGradient">
              <div class="d-flex gap-2 flex-wrap img-switch p-2 px-3 bg-light rounded">
                <div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" type="radio" name="data-sidebar" id="sidebar-color-gradient"
                    value="gradient" v-model="sidebarColor" />
                  <label class="form-check-label p-0 avatar-xs rounded-circle" for="sidebar-color-gradient">
                    <span class="avatar-title rounded-circle bg-vertical-gradient"></span>
                  </label>
                </div>
                <div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" type="radio" name="data-sidebar" id="sidebar-color-gradient-2"
                    value="gradient-2" v-model="sidebarColor" />
                  <label class="form-check-label p-0 avatar-xs rounded-circle" for="sidebar-color-gradient-2">
                    <span class="avatar-title rounded-circle bg-vertical-gradient-2"></span>
                  </label>
                </div>
                <div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" type="radio" name="data-sidebar" id="sidebar-color-gradient-3"
                    value="gradient-3" v-model="sidebarColor" />
                  <label class="form-check-label p-0 avatar-xs rounded-circle" for="sidebar-color-gradient-3">
                    <span class="avatar-title rounded-circle bg-vertical-gradient-3"></span>
                  </label>
                </div>
                <div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" type="radio" name="data-sidebar" id="sidebar-color-gradient-4"
                    value="gradient-4" v-model="sidebarColor" />
                  <label class="form-check-label p-0 avatar-xs rounded-circle" for="sidebar-color-gradient-4">
                    <span class="avatar-title rounded-circle bg-vertical-gradient-4"></span>
                  </label>
                </div>
              </div>
            </div>
          </div>

          <div v-if="layoutType == 'vertical' ||
            layoutType === 'twocolumn' ||
            (layoutType === 'semibox' && visibility === 'show')
          " id="sidebar-img">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Sidebar Images</h6>
            <p class="text-muted">Choose a image of Sidebar.</p>

            <div class="d-flex gap-2 flex-wrap img-switch">
              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" v-model="sidebarImage" type="radio" name="data-sidebar-img"
                  id="sidebarimg-none" value="none" />
                <label class="form-check-label p-0 avatar-sm h-auto" for="sidebarimg-none">
                  <span class="avatar-md w-auto bg-light d-flex align-items-center justify-content-center">
                    <i class="ri-close-fill fs-20"></i>
                  </span>
                </label>
              </div>

              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" v-model="sidebarImage" type="radio" name="data-sidebar-img"
                  id="sidebarimg-01" value="img-1" />
                <label class="form-check-label p-0 avatar-sm h-auto" for="sidebarimg-01">
                  <img src="@/assets/images/sidebar/img-1.jpg" alt="" class="avatar-md w-auto object-fit-cover" />
                </label>
              </div>

              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" v-model="sidebarImage" type="radio" name="data-sidebar-img"
                  id="sidebarimg-02" value="img-2" />
                <label class="form-check-label p-0 avatar-sm h-auto" for="sidebarimg-02">
                  <img src="@/assets/images/sidebar/img-2.jpg" alt="" class="avatar-md w-auto object-fit-cover" />
                </label>
              </div>
              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" v-model="sidebarImage" type="radio" name="data-sidebar-img"
                  id="sidebarimg-03" value="img-3" />
                <label class="form-check-label p-0 avatar-sm h-auto" for="sidebarimg-03">
                  <img src="@/assets/images/sidebar/img-3.jpg" alt="" class="avatar-md w-auto object-fit-cover" />
                </label>
              </div>
              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" v-model="sidebarImage" type="radio" name="data-sidebar-img"
                  id="sidebarimg-04" value="img-4" />
                <label class="form-check-label p-0 avatar-sm h-auto" for="sidebarimg-04">
                  <img src="@/assets/images/sidebar/img-4.jpg" alt="" class="avatar-md w-auto object-fit-cover" />
                </label>
              </div>
            </div>
          </div>

          <div id="sidebar-color">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Primary Color</h6>
            <p class="text-muted">Choose a color of Primary.</p>

            <div class="d-flex flex-wrap gap-2">
              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" type="radio" name="data-theme-colors" id="themeColor-01" value="default"
                  v-model="themeColor">
                <label class="form-check-label avatar-xs p-0" for="themeColor-01"></label>
              </div>
              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" type="radio" name="data-theme-colors" id="themeColor-02" value="green"
                  v-model="themeColor">
                <label class="form-check-label avatar-xs p-0" for="themeColor-02"></label>
              </div>
              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" type="radio" name="data-theme-colors" id="themeColor-03" value="purple"
                  v-model="themeColor">
                <label class="form-check-label avatar-xs p-0" for="themeColor-03"></label>
              </div>
              <div class="form-check sidebar-setting card-radio">
                <input class="form-check-input" type="radio" name="data-theme-colors" id="themeColor-04" value="blue"
                  v-model="themeColor">
                <label class="form-check-label avatar-xs p-0" for="themeColor-04"></label>
              </div>
            </div>
          </div>

          <div id="preloader-menu">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Preloader</h6>
            <p class="text-muted">Choose a preloader.</p>

            <div class="row"><div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="preloader" type="radio" name="data-preloader"
                    id="preloader-view-custom" value="enable" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="preloader-view-custom">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                    <div id="status" class="d-flex align-items-center justify-content-center">
                      <div class="spinner-border text-primary avatar-xxs m-auto" role="status">
                        <span class="visually-hidden">Loading...</span>
                      </div>
                    </div>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Enable</h5>
              </div>
              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="preloader" type="radio" name="data-preloader"
                    id="preloader-view-none" value="disable" />
                  <label class="form-check-label p-0 avatar-md w-100 material-shadow" for="preloader-view-none">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Disable</h5>
              </div>
            </div>
          </div>

          <div id="body-img" v-if="layoutTheme == 'galaxy' ||
            (layoutTheme === 'galaxy' && visibility === 'show')">
            <h6 class="mt-4 mb-0 fw-semibold text-uppercase">Background Image</h6>
            <p class="text-muted">Choose a body background image.</p>

            <div class="row"><div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="bodybg" type="radio" name="data-body-image"
                    id="body-img-none" value="none">
                  <label class="form-check-label p-0 avatar-md w-100" data-body-image="none" for="body-img-none">
                    <span class="d-flex gap-1 h-100">
                      <span class="flex-shrink-0">
                        <span class="bg-light d-flex h-100 flex-column gap-1 p-1">
                          <span class="d-block p-1 px-2 bg-primary-subtle rounded mb-2"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                          <span class="d-block p-1 px-2 pb-0 bg-primary-subtle"></span>
                        </span>
                      </span>
                      <span class="flex-grow-1">
                        <span class="d-flex h-100 flex-column">
                          <span class="bg-light d-block p-1"></span>
                          <span class="bg-light d-block p-1 mt-auto"></span>
                        </span>
                      </span>
                    </span>
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">None</h5>
              </div>
              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="bodybg" type="radio" name="data-body-image" id="body-img-one"
                    value="img-1">
                  <label class="form-check-label p-0 avatar-md w-100" for="body-img-one">
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">One</h5>
              </div>

              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="bodybg" type="radio" name="data-body-image" id="body-img-two"
                    value="img-2">
                  <label class="form-check-label p-0 avatar-md w-100" for="body-img-two">
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Two</h5>
              </div>

              <div class="col-4"><div class="form-check sidebar-setting card-radio">
                  <input class="form-check-input" v-model="bodybg" type="radio" name="data-body-image"
                    id="body-img-three" value="img-3">
                  <label class="form-check-label p-0 avatar-md w-100" for="body-img-three">
                  </label>
                </div>
                <h5 class="fs-13 text-center mt-2">Three</h5>
              </div>
            </div>
          </div>
        </div>
      </simpleBar>
      </div>
      <div class="border-top p-3 text-center">
        <button type="button" class="btn btn-light w-100" id="reset-layout" @click="resetLayout">
          Reset
        </button>
      </div>
    </div>
  </div>
</template>

<style lang="scss">
.b-overlay-wrap {
  .b-overlay {
    z-index: 1005 !important;
  }
}
</style>
