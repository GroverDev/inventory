# Progreso: base de app sobre Velzon (Angular 21)

Última actualización: 2026-10-20

## Objetivo
Usar el template Velzon v4.4.3 como base de una app real, con API propia (JWT) y limpieza agresiva de demos.

## Estado actual
- `ng build` compila. No se ha probado en navegador ni se han ejecutado los tests (`ng test`).
- Git inicializado. Commit base `895fb14` = template original intacto. Para recuperar cualquier archivo borrado: `git show 895fb14:<ruta>`.
- Un solo gestor de paquetes: **npm** (`package-lock.json`). Se borraron `yarn.lock` y `bun.lock`.

## Hecho
1. **Páginas**: se borraron todas las demos. Queda solo el dashboard (`src/app/pages/dashboards/dashboard`) en la ruta `/`. También se borraron landing, extraspages y las pantallas de `account/auth/*`.
2. **Account**: quedan `login` y `register` (`/auth/login`, `/auth/register`).
3. **Store NgRx**: solo queda el slice `layout` (tema/sidebar). Se quitaron los 11 slices de demo, Authentication y `@ngrx/effects`.
4. **Auth JWT** (reemplaza fake-backend, Firebase y Authfake):
   - `core/services/auth.service.ts`: login, register, logout, `isAuthenticated()` (valida expiración del token). Token en `sessionStorage` (`token`, `currentUser`).
   - `core/helpers/jwt.interceptor.ts`: interceptor funcional; añade `Bearer` solo a URLs de `environment.apiUrl`; en 401 con sesión activa hace logout y redirige.
   - `core/guards/auth.guard.ts`: guard funcional (`authGuard`), conserva `returnUrl`.
   - `core/models/user.model.ts`: `User`, `AuthResponse`.
   - `environments/environment*.ts`: `apiUrl` (`http://localhost:3000` en dev, `/api` en prod).
5. **Dependencias**: ~41 paquetes sin uso eliminados (FullCalendar, CKEditor, echarts, chart.js, firebase, material, etc.). Tests pasados a devDependencies. `browserslist` moderno (sin IE 11).
6. **Assets**: 47 MB -> ~6-7 MB. Borradas 821 imágenes sin referencias, 26 SCSS de plugins ya inexistentes, formatos de fuente `.eot/.ttf/.svg`, Line Awesome y Summernote. Fuentes quedan en `woff2` + `woff`.
7. **Iconos**:
   - **Remixicon**: completa (2.614), uso `ri-<nombre>-line|fill`.
   - **Material Design Icons**: completa (7.323), uso `mdi mdi-<nombre>`.
   - **Boxicons**: podada a los 12 iconos que usa el dashboard (`bx-...`).

## Contrato de API asumido (confirmar o cambiar)
- `POST /auth/login` `{email, password}` -> `{token, user}`
- `POST /auth/register` `{email, firstName, password}` -> `{token, user}`
- Errores con `{message}`. `user` = `{id, email, firstName?, lastName?, role?}`.
- No existe backend ni usuario de prueba: el usuario `admin@themesbrand.com / 123456` ya no funciona.

## Pendiente / ideas
- [ ] Crear `mock-api/server.js` (Node) con usuario de prueba para probar login sin backend, o confirmar la API real (URL y rutas).
- [ ] Probar login/guard/interceptor en el navegador (`npm start`).
- [ ] Dashboard sigue con datos demo (`core/data/dashboard.ts`); vaciarlo como esqueleto o conectarlo a la API.
- [ ] Topbar con carrito y notificaciones falsas (`layouts/topbar/data.ts`); limpiar.
- [ ] Sidebar con un solo ítem. Los menús de `horizontal-topbar` y `two-column-sidebar` son copias del de `sidebar`.
- [ ] Reponer páginas que se borraron y puedan hacer falta: recuperar contraseña, 404/500.
- [ ] Decidir Boxicons: quitarla (y cambiar sus 12 iconos por Remixicon) o dejarla.
- [ ] Recortar temas SCSS (`assets/scss/theme/*`, 12 temas), `scss/rtl` y los idiomas de `assets/i18n` (8) a los que se vayan a usar.
- [ ] Actualizar `README.md` (sigue siendo el del template, dice CLI 13).
- [ ] Revisar y ejecutar los `*.spec.ts` que quedan.
- [ ] Avisos de deprecación de Sass del tema (del template, no bloquean).

## Comandos
- `npm install` · `npm start` (dev en http://localhost:4200) · `npx ng build`
- Con `.npmrc`: `legacy-peer-deps=true` (conflictos de peer deps silenciados).
