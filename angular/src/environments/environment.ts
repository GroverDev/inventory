export const environment = {
  production: false,
  // URL base de la API, con barra final (equivale a VITE_API_SERVICIOS del frontend Vue).
  // localhost y no 127.0.0.1: con 127.0.0.1 el navegador no enviaría la cookie SameSite=Lax del refresh token.
  apiUrl: 'http://localhost:6001/api/',
  // Dominio de medios (imágenes de productos), con barra final. En desarrollo lo sirve la propia API.
  mediaUrl: 'http://localhost:6001/media/',
  // Site key de prueba de Cloudflare Turnstile: siempre aprueba. Vacía = el captcha no se muestra.
  turnstileSiteKey: '1x00000000000000000000AA'
};
