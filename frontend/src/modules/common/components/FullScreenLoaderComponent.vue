<template>
  <div class="loader-overlay">
    <div class="d-flex justify-content-center align-items-center vh-100">
      <svg width="50" height="50" viewBox="0 0 38 38" xmlns="http://www.w3.org/2000/svg" stroke="#fff">
        <g fill="none" fill-rule="evenodd">
          <g transform="translate(1 1)" stroke-width="2">
            <circle stroke-opacity=".5" cx="18" cy="18" r="18" />
            <path d="M36 18c0-9.94-8.06-18-18-18">
              <animateTransform attributeName="transform" type="rotate" from="0 18 18" to="360 18 18" dur="1s"
                repeatCount="indefinite" />
            </path>
          </g>
        </g>
      </svg>
    </div>
  </div>

</template>
<style scoped>
.loader-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.25);
  /* Fondo medio transparente */
  backdrop-filter: blur(2px);
  /* Difumina el fondo */

  align-items: center;
  justify-content: center;
  z-index: 1099;
  overflow: auto;

}

.spinner_9y7u {
  animation: spinner_fUkk 2.4s linear infinite;
  animation-delay: -2.4s
}

.spinner_DF2s {
  animation-delay: -1.6s
}

.spinner_q27e {
  animation-delay: -.8s
}

@keyframes spinner_fUkk {
  8.33% {
    x: 13px;
    y: 1px
  }

  25% {
    x: 13px;
    y: 1px
  }

  33.3% {
    x: 13px;
    y: 13px
  }

  50% {
    x: 13px;
    y: 13px
  }

  58.33% {
    x: 1px;
    y: 13px
  }

  75% {
    x: 1px;
    y: 13px
  }

  83.33% {
    x: 1px;
    y: 1px
  }
}
</style>
