export const environment = {
  production: true,
  // URL base de la API, con barra final (equivale a VITE_API_SERVICIOS del frontend Vue).
  apiUrl: 'https://api.ideanueva.com/api/',
  // Dominio de medios (imágenes de productos), servido por Nginx, con barra final.
  mediaUrl: 'https://media.ideanueva.com/',
  // Site key pública del widget de Cloudflare Turnstile. Vacía = el captcha no se muestra.
  turnstileSiteKey: '0x4AAAAAAEB-6Vgbu4Fh_iVQ'
};
