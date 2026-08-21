# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All commands run from `frontend/`:

```sh
npm install          # install dependencies
npm run dev          # dev server with HMR
npm run build        # type-check + production build
npm run type-check   # vue-tsc type checking only
npm run lint         # ESLint with auto-fix
npm run format       # Prettier on src/
```

Docker:
```sh
docker build -t punto-venta-front .
docker run -d --name punto-venta-web -p 8080:80 punto-venta-front
```

## Environment

The app requires `VITE_API_SERVICIOS` in a `.env` file pointing to the backend base URL:
```
VITE_API_SERVICIOS=https://api.example.com/
VITE_TURNSTILE_SITE_KEY=          # optional: Cloudflare Turnstile site key for the login captcha
```

`VITE_TURNSTILE_SITE_KEY` is optional — when empty the captcha widget is not rendered and login behaves as before. Its counterpart is the API's `Turnstile` section (`Enabled`, `SecretKey` via the `Turnstile__SecretKey` env var, `ChallengeAfterFailedAttempts`, `OutageThreshold`, `OutageMinutes`).

The captcha is enforced in two stages, both in `LoginController.Authenticate`:
1. **Scope** — the request's `Origin` must match `Cors:AllowedOrigins`. Never the client-supplied `LoginFrom`, so the mobile app is unaffected and the web cannot opt out by editing its payload.
2. **Suspicion** — the token is only verified once the account has recent failed attempts (same window as `LoginSettings.LockoutMinutes`). A clean login never calls Cloudflare, so a Cloudflare outage cannot lock anyone out on the happy path.

Verification is fail-closed: a rejected token always blocks, and so does a one-off infrastructure failure. Only after `OutageThreshold` consecutive infrastructure failures does `TurnstileCircuitBreaker` declare an outage and let logins through for `OutageMinutes`, then retry on its own. `Enabled` is read through `IOptionsMonitor`, so it can be switched off without restarting when `appsettings.json` is a mounted volume.

## Backend (local)

Correr desde `backend/1-Services/Services.Api`. Requiere .NET 10 SDK y una base Postgres accesible en `localhost:5432` (rol `app_pos`, base `punto_venta` — restaurar el backup más reciente de `db/` con `pg_restore`).

El `.env` (raíz, junto a `docker-compose.yml`) solo lo lee `docker compose` — .NET no carga archivos `.env`. Corriendo el backend suelto (`dotnet run`, sin Docker) los secretos se cargan con `dotnet user-secrets` en su lugar:

```sh
cd backend/1-Services/Services.Api
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:Secret" "<random >=64 bytes, p.ej. openssl rand -base64 64>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=punto_venta;Username=app_pos;Password=<pwd>;Port=5432;"
dotnet user-secrets set "MfaSettings:EncryptionKeyHex" "<64 hex chars, p.ej. openssl rand -hex 32>"   # solo si se prueba MFA/TOTP

dotnet run --project backend/1-Services/Services.Api   # levanta en http://localhost:6001
```

`appsettings.json` versiona placeholders (`REEMPLAZAR_VIA_ENV*`) en vez de secretos reales, a propósito: `ConnectionStringResolver` y la firma JWT fallan ruidosamente si detectan el marcador, en vez de arrancar con un secreto inválido en silencio. Si el backend no arranca o el login falla citando ese texto, falta uno de los `dotnet user-secrets set` de arriba — no es un bug de código.

## Architecture

**Stack:** Vue 3 (Composition API) + Vite + TypeScript + Pinia + Vue Router 4 + Axios + Bootstrap (external HTML template).

### Module structure

Business logic lives in `src/modules/<module>/` with a consistent internal layout:
- `layout/` — layout components wrapping the module's views
- `views/` — page-level components (routed)
- `composables/` — data-fetching and business logic hooks
- `models/` — TypeScript classes/interfaces for API contracts
- `router/` — Vue Router route config exported as a plain object
- `stores/` — Pinia stores (module-scoped)

Current modules: `auth`, `common`, `inventory`, `reports`, `security`, `user-account`.

### Routing

`src/router/index.ts` assembles the router from each module's exported route object. All authenticated routes use the `isAuthenticatedGuard` (`src/guards/authGuard.ts`), which reads the token from `authStore` and redirects to `login` if absent.

Layouts used as route components:
- `InventoryLayout` — main app shell (used by inventory, reports, security, user-account)
- `AuthLayout` — login page
- `PointOfSaleLayout` — dedicated POS view

### API layer

`src/modules/common/composables/api/useApi.ts` wraps Axios with:
- Auto-attach `Bearer` token from `authStore`
- Full-screen loading spinner (show/hide around every call)
- Offline detection
- Error modal on `!response.ok` or HTTP errors

All API responses extend `ResponseBase` (`{ ok: boolean, Message: Message }`). Use `ResponseObject<T>` for single-item responses and `ResponseArray<T>` for lists. The base URL comes from the `VITE_API_SERVICIOS` env var via `getApi.ts`.

### Global state (root stores in `src/stores/`)

- `authStore` — JWT token + user stored in `localStorage` via VueUse `useLocalStorage`; sets `axios.defaults.headers.common['Authorization']` on login/logout
- `dialogStore` — Promise-based modal dialog; call `dialog.show({ message, type, showCancel })` and `await` the boolean result
- `themeStore` — Bootstrap dark/light theme toggled via `data-bs-theme` on `<html>`
- `layoutStore` — sidebar/nav state (minified, mobile menu, etc.) managed by toggling CSS body classes

### Global utilities (`src/utils/`)

- `msg.ts` — thin wrapper over `dialogStore` for showing API errors, info messages, and confirmation prompts
- `excelHelper.ts` — `exportToExcel<T>()` and `readExcel<T>()` using the `xlsx` library

### UI conventions

Bootstrap classes are used throughout. The app loads an external Bootstrap-based HTML template; `useApp.ts` initializes Bootstrap tooltips, popovers, and Waves.js on mount. The `@` alias maps to `src/`.
