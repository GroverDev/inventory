/**
 * URL pública de una imagen de producto.
 *
 * La API devuelve solo la ruta relativa ("{public_id}/products/{id}/{guid}.webp");
 * el dominio de medios lo pone cada cliente con `VITE_MEDIA_URL`. Así cambiar de
 * dominio, o pasar a un CDN, no toca los datos.
 *
 * Devuelve '' si no hay imagen o no hay dominio configurado: quien la usa
 * muestra entonces el marcador.
 */
const base = (import.meta.env.VITE_MEDIA_URL ?? '').trim().replace(/\/?$/, '/');

/**
 * @param thumb miniatura (200 px) en vez de la imagen completa (800 px). Para
 *              listados y el POS: pesa una fracción y carga al instante.
 */
export const mediaUrl = (path: string | null | undefined, thumb = false): string => {
  if (!path || base === '/') return '';
  const relative = thumb ? path.replace(/\.webp$/i, '_thumb.webp') : path;
  return base + relative.replace(/^\/+/, '');
};
