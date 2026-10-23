<template>
  <Teleport to="body">
    <div class="d-flex align-items-center justify-content-center position-fixed top-0 start-0 w-100 h-100"
      style="background: rgba(0, 0, 0, 0.15); z-index: 3000" role="status" aria-live="polite">
      <div class="spinner-border text-primary" style="width: 3rem; height: 3rem">
        <span class="visually-hidden">Cargando...</span>
      </div>
    </div>
  </Teleport>
</template>
