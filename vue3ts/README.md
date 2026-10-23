# Punto de Venta — Velzon

Frontend en Vue 3 + TypeScript + Vite + Pinia sobre la plantilla Velzon. Es la contraparte de `../frontend`
con otro aspecto visual; consume el mismo backend.

```sh
npm install
npm run dev          # http://localhost:5173 (puerto fijo: el CORS del backend lo permite)
npm run build        # type-check + build de producción
npm run type-check
```

`.env` (ver `.env.production` para producción):

- `VITE_API_SERVICIOS` — URL base del backend (`http://localhost:6001/api/`). Usar `localhost`, no `127.0.0.1`
  (la cookie del refresh token es `SameSite=Lax`).
- `VITE_TURNSTILE_SITE_KEY` — opcional; vacía = sin captcha en el login.
- `VITE_MEDIA_URL` — dominio de las imágenes de productos, con `/` final.

## Estructura

- `src/modules/<modulo>/{views,composables,models,router}` — lógica de negocio (inventory, reports, user-account),
  portada de `../frontend`. El `name` de cada ruta coincide con el `url` del formulario en el menú de accesos.
- `src/api` — cliente HTTP con token, loading y renovación de sesión ante 401.
- `src/stores` — Pinia (auth, layout, loading, dialog). `src/composables` — menú, permisos, sesión.
- `src/layouts`, `src/components` — layouts de Velzon (vertical, horizontal, dos columnas) y piezas comunes.
- `src/assets/scss` — SCSS de Velzon, podado a lo que se usa. `src/assets/fonts` conserva los packs de iconos
  completos (Boxicons, Line Awesome, Material Design, Remix) hasta decidir cuál usar.

`_referencia/` guarda respaldos locales de la plantilla (no se versiona).
