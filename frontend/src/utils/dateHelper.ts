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
 * Hace falta porque algunas fechas viven en columnas con zona horaria pero
 * representan un día, no un instante: `purchase_date` y
 * `estimated_delivery_date` se guardan como medianoche UTC. Pasarlas por
 * `new Date(v).toLocaleDateString()` las corre un día atrás en Bolivia (UTC−4):
 * una compra del 28/07 se lee 27/07.
 *
 * NO usar para instantes reales —una venta, un movimiento de stock, un último
 * acceso—: ahí la hora local sí es la correcta y `toLocaleDateString` está bien.
 * Tampoco hace falta para los vencimientos: vienen de una columna `date`, sin
 * zona, así que el navegador ya los interpreta como locales.
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
