<template>
  <router-view></router-view>
</template>

<script setup lang="ts">
</script>

<style>
/* ── Auth CSS variables – light mode ── */
:root {
  --auth-bg:               #f0f2f5;
  --auth-geo1-bg:          rgba(43, 132, 234, 0.08);
  --auth-geo2-bg:          rgba(62, 158, 93, 0.07);
  --auth-geo3-bg:          rgba(124, 92, 191, 0.06);
  --auth-card-bg:          #ffffff;
  --auth-card-shadow:      0 8px 32px rgba(0,0,0,0.10);
  --auth-shield-bg:        rgba(124, 92, 191, 0.10);
  --auth-title-color:      #1a1a2e;
  --auth-subtitle-color:   #6b7280;
  --auth-email-color:      #374151;

  --auth-otp-color:        #1a1a2e;
  --auth-otp-border:       #d1d5db;
  --auth-otp-bg:           #f9fafb;
  --auth-input-bg:         #ffffff;
  --auth-otp-filled-border:#7c5cbf;
  --auth-otp-filled-bg:    #f5f0ff;
  --auth-otp-error-bg:     #fff5f5;

  --auth-timer-track:      #e5e7eb;
  --auth-timer-hint:       #9ca3af;

  --auth-footer-border:    #e5e7eb;
  --auth-version-color:    #9ca3af;

  --auth-qr-border:              #e5e7eb;
  --auth-secret-wrap-bg:         #f9fafb;
  --auth-secret-wrap-border:     #e5e7eb;
  --auth-secret-label:           #6b7280;
  --auth-secret-key:             #374151;
}

/* ── Auth CSS variables – dark mode ── */
[data-bs-theme="dark"] {
  --auth-bg:               #0f1117;
  --auth-geo1-bg:          rgba(43, 132, 234, 0.12);
  --auth-geo2-bg:          rgba(62, 158, 93, 0.10);
  --auth-geo3-bg:          rgba(124, 92, 191, 0.09);
  --auth-card-bg:          #1c1f2e;
  --auth-card-shadow:      0 8px 32px rgba(0,0,0,0.40);
  --auth-shield-bg:        rgba(124, 92, 191, 0.18);
  --auth-title-color:      #f1f5f9;
  --auth-subtitle-color:   #94a3b8;
  --auth-email-color:      #e2e8f0;

  --auth-otp-color:        #f1f5f9;
  --auth-otp-border:       #374151;
  --auth-otp-bg:           #252836;
  --auth-input-bg:         #2d3148;
  --auth-otp-filled-border:#9b7de0;
  --auth-otp-filled-bg:    #2d2550;
  --auth-otp-error-bg:     #2d1515;

  --auth-timer-track:      #374151;
  --auth-timer-hint:       #6b7280;

  --auth-footer-border:    #2d3148;
  --auth-version-color:    #6b7280;

  --auth-qr-border:              #374151;
  --auth-secret-wrap-bg:         #252836;
  --auth-secret-wrap-border:     #374151;
  --auth-secret-label:           #94a3b8;
  --auth-secret-key:             #e2e8f0;
}
</style>
