# Dispositivo de confianza (MFA) + Panel de sesiones activas

> 2026-08-18 — 2026-08-19

Tres features sobre el módulo de seguridad, más tres bugs preexistentes que
aparecieron al construirlas y probarlas en vivo.

---

## 1. Dispositivo de confianza ("recordar este dispositivo")

Al verificar el TOTP en el login, el usuario puede marcar "Recordar este
dispositivo por 30 días". Si lo hace, los logins siguientes desde ese mismo
navegador/dispositivo saltan el paso de TOTP.

- Tabla nueva `sec.trusted_devices` (mismo patrón que `sec.refresh_tokens`:
  solo se guarda el hash SHA-512 del token, nunca el valor en claro).
- Días configurables vía `JwtSettings:TrustedDeviceDays` (default 30).
- Web: el token viaja en una cookie HttpOnly `device_trust`
  (`Path=/api/Login`). Móvil: viaja en el body (`LoginRequest.DeviceTrustToken`
  / `LoginResponse.DeviceTrustToken`) — **el cliente Flutter todavía no fue
  actualizado para usarlo**, ver "Pendientes" abajo.
- Se revoca automáticamente ante: cambio de contraseña (propia o por admin),
  reset de MFA por admin, o deshabilitar el TOTP.
- Checkbox en `TotpView.vue`.

## 2. Panel de sesiones activas / usuarios conectados

Nueva pantalla de administración para ver quién está conectado y cerrar una
sesión ajena (o todas las de un usuario), instantáneamente.

- Reutiliza `sec.refresh_tokens` como tabla de "sesiones" (no hizo falta una
  tabla nueva).
- **Cierre instantáneo**: revocar el refresh token en base solo impide
  *renovar* la sesión — el access token (JWT autocontenido) ya emitido seguía
  sirviendo hasta vencer solo. Se agregó `SessionRevocationRegistry`, un
  singleton en memoria (mismo patrón que `TurnstileCircuitBreaker`) más
  `SessionRevocationMiddleware`, que rechaza con 401 cualquier request cuyo
  `SessionId` haya sido cerrado — sin esperar el vencimiento del token.
  - Para que esto funcione por dispositivo puntual (no solo "todo el
    usuario"), se agregó la columna `sec.refresh_tokens.session_id`, que
    enlaza cada refresh token con el `SessionId` (`sec.users_login.id`)
    vigente al emitirlo.
  - **Riesgo aceptado y acotado**: el registro vive en memoria de un solo
    proceso. Un reinicio del backend "olvida" las revocaciones ya hechas; el
    usuario afectado podría seguir entrando con el access token que ya tenía
    hasta que venciera solo. Por eso también se bajó `TimeTokenRefreshable`
    de 60 a **30 minutos** — acota esa ventana de riesgo residual.
- Endpoints nuevos (`SessionsController`, todos gateados por permiso):
  `GET /api/Sessions/connected`, `GET /api/Sessions/user/{uuid}`,
  `DELETE /api/Sessions/{id}`, `DELETE /api/Sessions/user/{uuid}`.
- Permiso nuevo: formulario `active-sessions` en `sec.forms`, con
  `can_read`/`can_delete` otorgado a los roles que ya tenían `users-admin`
  (**Administrador** y **SuperAdmin** en este tenant) — no un chequeo de rol
  hardcodeado, sigue el sistema de permisos granulares existente
  (`HasFormPermission`).
- Frontend: `SessionsAdminView.vue` (panel "Usuarios Conectados", ruta
  `active-sessions`) + sección "Sesiones Activas" dentro de
  `UserEditView.vue` (ficha de cada usuario).

## 3. Autogestión de dispositivos de confianza

El propio usuario ahora puede ver y "olvidar" sus dispositivos recordados,
sin depender de un admin ni de la revocación automática total.

- Endpoints nuevos en `MfaController` (autenticado, cada uno restringido al
  propio usuario — `GetByIdForUser` verifica dueño antes de revocar):
  `GET /api/Mfa/devices`, `DELETE /api/Mfa/devices/{id}`,
  `DELETE /api/Mfa/devices` (todos de una vez).
- Frontend: modal "Mis dispositivos de confianza" en el dropdown de perfil de
  `HeaderComponent.vue`, junto al de "Cambiar contraseña" (mismo patrón:
  modal disparado desde el dropdown, no una pantalla aparte).
- Probado en vivo: listar, olvidar uno puntual (el siguiente login desde ese
  dispositivo vuelve a pedir TOTP) y olvidar todos.

---

## Tests automatizados (backend)

Se evaluó agregar tests en backend y frontend; se decidió **solo backend**
por ahora — el frontend no tiene ningún framework de testing instalado hoy
(ni vitest ni nada), así que hubiera significado levantar esa infraestructura
desde cero, no solo agregar un archivo.

- El proyecto de tests (`backend/5-Tests/MultiTenancy.Tests`, xUnit contra
  una base Postgres descartable) **no referenciaba el módulo Seguridad**
  — solo Inventory. Se agregaron las referencias a `Seguridad.Domain` y
  `Seguridad.Infrastructure`, más `TenantDatabaseFixture.ContextoAppSeguridad(tenantId)`
  (gemelo de `ContextoApp`, para poder instanciar los repositorios de
  Seguridad contra la base de prueba).
- Archivos nuevos: `SessionRevocationRegistryTests.cs` (unitario puro, sin
  DB), `TrustedDeviceRepositoryTests.cs`, `RefreshTokenRepositoryTests.cs`
  (aislamiento entre tenants del panel de sesiones — el caso que habría
  atrapado el bug #3 de abajo antes de llegar a producción),
  `ActiveSessionsPermissionTests.cs` (el seed del permiso `active-sessions`
  sigue existiendo y otorgado a Administrador).
- **Al correr la suite completa por primera vez con estos cambios, un
  guardrail ya existente (`PoliticasTests.Toda_tabla_con_tenant_id_tiene_RLS_o_una_excepcion_documentada`)
  falló**: exige que toda tabla con `tenant_id` tenga RLS o una excepción
  documentada, y `sec.trusted_devices` no estaba en esa lista. Se agregó,
  con el mismo motivo que `sec.refresh_tokens` (maquinaria de autenticación,
  se consulta antes de tener tenant resuelto).
- Estado final: **85/85 tests pasan** (71 preexistentes + 14 nuevos).
- Para correr la suite localmente hace falta `TEST_PG_ADMIN` con la password
  real del superusuario de Postgres (el default sin password solo funciona
  si `pg_hba.conf` tiene `trust` para `127.0.0.1`/`::1`, que no es el caso
  acá):
  ```
  TEST_PG_ADMIN='Host=localhost;Port=5432;Username=postgres;Password=...;Database=postgres' dotnet test
  ```

---

## Bugs preexistentes encontrados y corregidos en el camino

1. **RLS rompía la verificación de TOTP para cualquier usuario.**
   `MfaRepository.GetTotpMfa` hacía `JOIN sec.users` en una conexión sin
   tenant (correcto para ese punto del login), pero `sec.users` sí tiene RLS
   → el join descartaba la fila entera silenciosamente. Nadie con 2FA activo
   podía completar el login desde que se activó RLS multi-tenant
   (2026-08-14). No se había notado porque hoy nadie en producción tiene MFA
   habilitado. Fix: `JOIN` → `LEFT JOIN` (el email que trae de más no se usa
   en la verificación).
2. **Refresh token expuesto en el body al completar login vía TOTP.**
   `MfaController.IssueTokens` no distinguía web/móvil como sí lo hace
   `LoginController`: el refresh token viajaba siempre en el JSON, legible
   por JS (y por un XSS), en vez de ir en cookie HttpOnly. Corregido para
   seguir la misma regla que el login directo.
3. **`sec.refresh_tokens.tenant_id` y `sec.trusted_devices.tenant_id` nunca
   se escribían** — todo caía en el default estático (tenant 1) sin importar
   el tenant real. No rompía nada mientras nadie filtrara por tenant, pero
   el panel de "usuarios conectados" sí necesita hacerlo. Se corrigió para
   que el código las escriba explícitas.

---

## Migraciones a correr en otros entornos

En orden, contra la base de cada entorno (`db/migrations/`):

1. `2026-08-18_trusted_devices.sql`
2. `2026-08-18_sessions_columns.sql`
3. `2026-08-18_active_sessions_menu.sql`

Todas son idempotentes (se pueden correr más de una vez sin efecto
secundario).

## Configuración a revisar antes de desplegar

- `JwtSettings:TimeTokenRefreshable` → **30** (antes 60 en `appsettings.json`
  local, 240 en `docker-compose.yml`/`.env.example` de producción). Ya
  actualizado en ambos archivos.
- `JwtSettings:TrustedDeviceDays` → nuevo, default 30, en `appsettings.json`.
  Falta agregar `JwtSettings__TrustedDeviceDays` al `docker-compose.yml` de
  producción si se quiere otro valor (si no se define, usa el default del
  código).
- **`appsettings.json` local quedó con secretos reales** (`JwtSettings:Secret`,
  `MfaSettings:EncryptionKeyHex`, password de `app_pos` en
  `ConnectionStrings`) para poder probar el flujo completo en este entorno de
  desarrollo. Son válidos solo acá — revisar/rotar antes de usar ese archivo
  como base de otro entorno.

---

## Pendientes (no incluido en esta pasada)

- **App móvil (Flutter)**: no manda ni guarda `DeviceTrustToken`. El backend
  ya lo soporta.
- **Tests de frontend**: no se agregaron — requiere instalar y configurar
  Vitest (u otro) desde cero, el frontend no tiene tooling de testing hoy.
- **Tests de endpoints/controllers** (capa HTTP): los tests nuevos cubren
  repositorios y lógica pura; no hay tests de integración que golpeen
  `SessionsController`/`MfaController` vía HTTP end-to-end (se probó eso a
  mano con curl durante la sesión, no quedó automatizado).
- Si se llega a correr más de una instancia del backend a la vez, el
  `SessionRevocationRegistry` (en memoria) deja de alcanzar y habría que
  moverlo a un almacén compartido (Redis).

---

## Archivos principales

**Backend — nuevos**
`SessionsController.cs`, `SessionRevocationMiddleware.cs`,
`Common.Utilities/Security/SessionRevocationRegistry.cs`,
`ITrustedDeviceRepository.cs` + `TrustedDeviceRepository.cs`,
`TrustedDevice.cs`, `SessionResponse.cs` (+ `ConnectedUserResponse`),
`TrustedDeviceResponse.cs`.

**Backend — modificados**
`LoginController.cs`, `MfaController.cs`, `Program.cs`, `appsettings.json`,
`JwtSettings.cs`, `IAuthenticationApplication.cs` / `AuthenticationApplication.cs`,
`IUsersApplication.cs` / `UsersApplication.cs`, `MfaApplication.cs`,
`IAuthenticationRepository.cs` / `AuthenticationRepository.cs`,
`IRefreshTokenRepository.cs` / `RefreshTokenRepository.cs`, `MfaRepository.cs`,
`RefreshToken.cs`, `LoginRequest.cs`, `LoginResponse.cs`,
`TotpVerifyRequest.cs`, `MfaRecoveryRequest.cs`,
`InjectionExtensionsSecurityInfrastructure.cs`.

**Frontend — nuevos**
`SessionsAdminView.vue`, `useSessions.ts`, `session.model.ts`.

**Frontend — modificados**
`TotpView.vue`, `useTotp.ts`, `UserEditView.vue`, `user-account/router/index.ts`,
`HeaderComponent.vue`.

**DB**
`2026-08-18_trusted_devices.sql`, `2026-08-18_sessions_columns.sql`,
`2026-08-18_active_sessions_menu.sql`.

**Tests — nuevos**
`SessionRevocationRegistryTests.cs`, `TrustedDeviceRepositoryTests.cs`,
`RefreshTokenRepositoryTests.cs`, `ActiveSessionsPermissionTests.cs`.

**Tests — modificados**
`MultiTenancy.Tests.csproj` (referencias a Seguridad.*),
`TenantDatabaseFixture.cs` (+ `ContextoAppSeguridad`), `PoliticasTests.cs`
(+ excepción documentada para `sec.trusted_devices`).
