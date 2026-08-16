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

/** Primer día del mes en curso, en horario local. */
export const firstOfMonthIso = (): string => {
  const now = new Date();
  return toIsoDate(new Date(now.getFullYear(), now.getMonth(), 1));
};
