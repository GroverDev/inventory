<template>
  <!-- Tarjetas: totales de un período o de un turno -->
  <div v-if="variant === 'cards' && items.length" class="row g-2">
    <div v-for="m in items" :key="m.PaymentMethodId ?? 'sin-medio'" class="col-6 col-md-3">
      <div class="border rounded p-2 h-100">
        <div class="small text-muted text-truncate">
          <i :class="m.IconCss || 'fal fa-wallet'" class="me-1"></i>{{ m.Name }}
        </div>
        <div class="fw-bold fs-5">Bs. {{ fmt(m.Net) }}</div>
        <small v-if="m.Refunded > 0" class="text-warning-emphasis d-block">
          cobrado {{ fmt(m.Collected) }} · devuelto − {{ fmt(m.Refunded) }}
        </small>
      </div>
    </div>
  </div>

  <!-- En línea: dentro de una celda de tabla -->
  <div v-else-if="variant === 'inline' && items.length" class="small text-muted">
    <span v-for="(m, i) in items" :key="m.PaymentMethodId ?? 'sin-medio'" class="text-nowrap">
      <span v-if="i > 0" class="mx-1">·</span>
      <i :class="m.IconCss || 'fal fa-wallet'" class="me-1"></i>{{ m.Name }} {{ fmt(m.Net) }}
    </span>
  </div>
</template>

<script setup lang="ts">
import type { PaymentMethodTotal } from '@/modules/inventory/models/sale.model';

/**
 * Neto por medio de pago, calculado en el servidor: cobrado (entregado menos el
 * vuelto) menos lo reintegrado por devoluciones con ese medio.
 */
withDefaults(defineProps<{ items: PaymentMethodTotal[]; variant?: 'cards' | 'inline' }>(), { variant: 'cards' });

const fmt = (v: number) => (v ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
</script>
