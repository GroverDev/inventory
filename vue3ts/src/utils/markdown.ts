import { marked } from 'marked';
import DOMPurify from 'dompurify';

/**
 * Convierte markdown en HTML seguro para mostrar.
 *
 * Dos decisiones que importan:
 *
 * **`breaks: true`** — el markdown estándar colapsa los saltos de línea simples,
 * así que un prospecto copiado y pegado se vería como un único bloque de texto.
 * Con esta opción se respeta el formato original, y quien quiera títulos y
 * listas puede escribirlos igual.
 *
 * **DOMPurify** — el texto viene copiado de una web o de un PDF y puede traer
 * HTML pegado. Markdown deja pasar el HTML crudo, así que sin sanitizar esto
 * sería una puerta abierta: un `<script>` o un `onerror` dentro del prospecto
 * se ejecutaría al abrirlo.
 */
export const renderMarkdown = (texto: string): string => {
  if (!texto) return '';

  const html = marked.parse(texto, { breaks: true, async: false }) as string;

  return DOMPurify.sanitize(html, {
    // Lo que puede aparecer en un prospecto y nada más. Sin `a` a propósito:
    // un enlace en un prospecto copiado no aporta y agrega superficie.
    ALLOWED_TAGS: [
      'p', 'br', 'strong', 'em', 'u', 'del',
      'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
      'ul', 'ol', 'li', 'blockquote', 'code', 'pre', 'hr',
      'table', 'thead', 'tbody', 'tr', 'th', 'td',
    ],
    ALLOWED_ATTR: [],
  });
};
