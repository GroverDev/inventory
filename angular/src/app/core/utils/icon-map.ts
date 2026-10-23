/**
 * La API manda los íconos de medios de pago y módulos como clases de FontAwesome
 * Light (`fal fa-qrcode`), que Velzon no incluye. Esta tabla las traduce a su
 * equivalente de Remixicon (`ri-<nombre>-line`); lo que no figura cae en `fallback`.
 */
const FA_TO_REMIX: Record<string, string> = {
  // Medios de pago
  'money-bill-wave': 'ri-money-dollar-circle-line',
  'money-bill': 'ri-money-dollar-circle-line',
  'money-bill-alt': 'ri-money-dollar-circle-line',
  'money-check-alt': 'ri-bank-card-line',
  'credit-card': 'ri-bank-card-line',
  'qrcode': 'ri-qr-code-line',
  'wallet': 'ri-wallet-3-line',
  'coins': 'ri-coin-line',
  'university': 'ri-bank-line',
  'landmark': 'ri-bank-line',
  'mobile': 'ri-smartphone-line',
  'mobile-alt': 'ri-smartphone-line',
  'hand-holding-usd': 'ri-hand-coin-line',
  'exchange': 'ri-arrow-left-right-line',
  'exchange-alt': 'ri-arrow-left-right-line',
  // Módulos y formularios
  'boxes': 'ri-archive-line',
  'box': 'ri-archive-line',
  'box-open': 'ri-archive-line',
  'broadcast-tower': 'ri-base-station-line',
  'chart-bar': 'ri-bar-chart-2-line',
  'chart-line': 'ri-line-chart-line',
  'chart-pie': 'ri-pie-chart-line',
  'cogs': 'ri-settings-3-line',
  'cog': 'ri-settings-3-line',
  'exclamation-triangle': 'ri-error-warning-line',
  'percent': 'ri-percent-line',
  'receipt': 'ri-receipt-line',
  'shopping-cart': 'ri-shopping-cart-2-line',
  'shopping-bag': 'ri-shopping-bag-3-line',
  'store': 'ri-store-2-line',
  'store-alt': 'ri-store-2-line',
  'window': 'ri-window-line',
  'file': 'ri-file-line',
  'users': 'ri-group-line',
  'user': 'ri-user-line',
  'truck': 'ri-truck-line',
  'tags': 'ri-price-tag-3-line',
  'tag': 'ri-price-tag-3-line',
  'flask': 'ri-flask-line',
  'ruler': 'ri-ruler-line',
  'cash-register': 'ri-store-3-line',
};

/** Traduce una clase de FontAwesome (`fal fa-qrcode`) a Remixicon; `fallback` si no hay equivalente. */
export const remixIcon = (faClass: string | null | undefined, fallback = 'ri-wallet-3-line'): string => {
  const name = /fa-([a-z0-9-]+)/.exec(faClass ?? '')?.[1];
  return (name && FA_TO_REMIX[name]) || fallback;
};
