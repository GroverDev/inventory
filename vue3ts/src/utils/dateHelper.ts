/**
 * Fechas en horario local para los `<input type="date">` y para los filtros.
 *
 * El atajo natural, `new Date().toISOString().split('T')[0]`, devuelve la fecha
 * en UTC. En Bolivia (UTC−4) eso significa que a partir de las 20:00 la app
 * cree que ya es mañana: la recepción de pedidos proponía una fecha futura y el
 * servidor la rechazaba, y una compra registrada de noche quedaba fechada al
 * día siguiente sin que nadie lo notara.
 *
 * `sv-SE` se usa porque su formato de fecha ya es el ISO `yyyy-MM-dd` que
 * esperan los inputs y la API, pero calculado sobre la hora local.
 */
export const toIsoDate = (date: Date): string => date.toLocaleDateString('sv-SE');

/** Hoy, en horario local. */
export const todayIso = (): string => toIsoDate(new Date());

/**
 * Muestra una fecha PURA (dd/MM/yyyy) sin convertirla a horario local.
 *
 * Las fechas de compra —`purchase_date`, `estimated_delivery_date`,
 * `delivery_date`— y los vencimientos son días del calendario, no instantes:
 * viven en columnas `date` y llegan como "2026-08-21", sin hora ni zona.
 * Construir un `new Date(v)` con ese texto lo interpreta como medianoche UTC y
 * al mostrarlo en Bolivia (UTC−4) lo corre un día atrás: una compra del 28/07
 * se leería 27/07. Por eso acá el día se toma del texto, sin `Date` de por medio.
 *
 * NO usar para instantes reales —una venta, un movimiento de stock, un último
 * acceso—: ahí la hora local sí es la correcta y `toLocaleString` está bien.
 */
export const formatDateOnly = (value: string | Date | null | undefined): string => {
  if (!value) return '—';

  const iso = value instanceof Date ? toIsoDate(value) : String(value);
  // Se toma el día tal como llegó, sin construir un Date intermedio: es lo que
  // evita que la zona horaria entre en juego.
  const partes = iso.match(/^(\d{4})-(\d{2})-(\d{2})/);
  if (partes) return `${partes[3]}/${partes[2]}/${partes[1]}`;

  const fecha = new Date(iso);
  return isNaN(fecha.getTime())
    ? String(value)
    : fecha.toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

/** Primer día del mes en curso, en horario local. */
export const firstOfMonthIso = (): string => {
  const now = new Date();
  return toIsoDate(new Date(now.getFullYear(), now.getMonth(), 1));
};
