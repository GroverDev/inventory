# Estado de la Aplicación

> Última actualización: 2026-05-26

## Stack

- **Backend:** .NET 10, Clean Architecture, Dapper, FluentValidation, JWT, TOTP
- **Frontend:** Vue 3 (Composition API), Vite, TypeScript, Pinia, Vue Router 4, Axios, Bootstrap
- **Base de datos:** PostgreSQL — schemas `public` (negocio), `sec` (seguridad), `siat` (facturación electrónica Bolivia)

---

## Resumen ejecutivo

| Capa      | Progreso estimado | Notas                                           |
|-----------|------------------|-------------------------------------------------|
| Backend   | ~92%             | Caja, pagos por método, roles reales en JWT, cambio de contraseña implementados |
| Frontend  | ~92%             | POS activo con control de caja, menú filtrado por rol, gestión de usuarios completa |

---

## Backend

### Módulo Seguridad (`/api/...`)

| Recurso        | Endpoints | Estado       |
|----------------|-----------|--------------|
| Login          | POST      | ✅ Completo — ahora devuelve `RolId` y `RolName` reales desde `sec.roles` |
| Users          | CRUD + reset MFA + require MFA + cambio de contraseña (admin y propio) | ✅ Completo |
| Roles          | CRUD + asignación de Forms | ✅ Completo  |
| Forms          | CRUD      | ✅ Completo  |
| Modules        | CRUD      | ✅ Completo  |
| AccessMenu     | GET       | ✅ Completo — filtrado por usuario real (corregido hardcode `userId=1`) |
| MFA / TOTP     | setup-init, totp-verify, mfa-recover | ✅ Completo |

### Módulo Inventario (`/api/...`)

| Recurso             | Endpoints                              | Estado      |
|---------------------|----------------------------------------|-------------|
| Product             | CRUD + validate (precio/stock para POS)| ✅ Completo |
| Category            | CRUD                                   | ✅ Completo |
| Customers           | CRUD                                   | ✅ Completo |
| Laboratory          | CRUD                                   | ✅ Completo |
| Provider            | CRUD                                   | ✅ Completo |
| UnitOfMeasurement   | CRUD                                   | ✅ Completo |
| Purchases           | CRUD + reciveOrders (recepción)        | ✅ Completo |
| PurchaseStatus      | GET (enum: Pending, Delivered, etc.)   | ✅ Completo |
| Sales               | CRUD + filtro por rol (Cajero/Admin) + columna vendedor | ✅ Completo |
| CashSession         | GET active, GET list, POST open, PUT close, POST movement | ✅ Completo |
| StockMovement       | GET por producto + POST ajuste         | ✅ Completo |
| Dashboard           | GET estadísticas                       | ✅ Completo |

### Infraestructura backend

- Repositorios Dapper implementados para todas las entidades
- Validaciones FluentValidation en todos los requests
- Migraciones SQL: `migration_totp.sql`, `migration_mfa_v2.sql`, `migration_categories.sql`, `migration_stock_movements.sql`, `migration_reports_menu.sql`, `migration_payment_methods.sql`, `migration_returns.sql`
- **`migration_cash_register.sql`** — tabla `cash_sessions` (partial unique index para sesión única abierta) y `cash_movements`; columna `cash_session_id` en `sales`
- Swagger/OpenAPI configurado por grupos (SECURITY, POS)
- JWT ahora incluye el rol real del usuario (`Rol` claim) obtenido de `sec.roles`
- Clave AES-256 para cifrado TOTP corregida en `appsettings.json` (se eliminaron caracteres inválidos)

### Tablas en BD sin código backend correspondiente

Las siguientes tablas existen en el schema `public` pero **no tienen entidad, repositorio ni endpoint** implementados:

| Tabla BD                  | Descripción                                              |
|---------------------------|----------------------------------------------------------|
| `discounts`               | Descuentos por nombre/tipo/valor                        |
| `sale_detail_discounts`   | Descuentos aplicados por línea de venta                 |
| `products_providers`      | Catálogo de productos por proveedor                     |
| `sequences_key`           | Secuencias personalizadas por tabla (usada internamente) |

> `payment_methods` y `sale_payments` ya tienen migración SQL creada (`migration_payment_methods.sql`) pero pendiente de implementación en backend/frontend.

---

## SIAT — Facturación Electrónica Bolivia (fuera de alcance)

> **Este proyecto NO implementará nada relacionado con el schema `siat`. Queda reservado para una fase futura.**

---

## Frontend

### Módulo `auth`

| Feature                        | Estado      |
|-------------------------------|-------------|
| Login (email + password + JWT)| ✅ Completo |
| TOTP verify (2FA)             | ✅ Completo |
| TOTP setup (primer uso)       | ✅ Completo |
| Redirect guard (authGuard)    | ✅ Completo |

### Módulo `user-account`

| Feature                                     | Estado      |
|---------------------------------------------|-------------|
| Listado + CRUD Usuarios                     | ✅ Completo |
| Campo `UserName` visible y editable         | ✅ Completo — antes no se mostraba ni actualizaba |
| Reset MFA / require MFA (desde admin)       | ✅ Completo — badges de estado + botones contextuales en lista |
| Cambio de contraseña por admin (desde lista)| ✅ Completo — modal con nueva contraseña + confirmación |
| Cambio de contraseña propia (header)        | ✅ Completo — opción en dropdown de perfil, requiere contraseña actual |
| Listado + CRUD Roles                        | ✅ Completo |
| Asignación de Forms a Rol                   | ✅ Completo |
| Listado + CRUD Forms                        | ✅ Completo |
| Listado + CRUD Modules                      | ✅ Completo |

### Módulo `inventory`

| Feature                              | Estado             | Notas                                                         |
|--------------------------------------|--------------------|---------------------------------------------------------------|
| Listado + CRUD Productos             | ✅ Completo        | Incluye selección de Lab, UOM y Categoría                    |
| Admin Categorías                     | ✅ Completo        |                                                               |
| Admin Laboratory                     | ✅ Completo        |                                                               |
| Admin UnitOfMeasurement              | ✅ Completo        |                                                               |
| Admin Clientes (Customers)           | ✅ Completo        |                                                               |
| Admin Proveedores (Providers)        | ✅ Completo        |                                                               |
| Admin Compras (Purchases)            | ✅ Completo        |                                                               |
| Listado Ventas (Sales)               | ✅ Completo        | Columna "Vendedor" agregada; filtro por vendedor (select dinámico); filtro por rol (Cajero ve solo sus ventas) |
| Turnos de Caja (CashSessions)        | ✅ Completo        | `CashSessionsAdminView` — filtros por fecha, tabla/cards, modal de detalle con movimientos y arqueo |
| Punto de Venta (POS)                 | ✅ Completo        | Control de caja obligatorio; bloqueo sin sesión abierta; filtro por categoría; movimientos (gasto/retiro/ingreso); arqueo al cerrar |
| Inventario / Stock                   | ✅ Completo        |                                                               |

### Módulo `reports`

| Feature               | Estado      |
|-----------------------|-------------|
| Reporte de Ventas     | ✅ Completo |
| Reporte de Stock      | ✅ Completo |
| Reporte de Compras    | ✅ Completo |

### Infraestructura frontend

| Feature                                    | Estado      |
|--------------------------------------------|-------------|
| `useApi.ts` (Axios wrapper + loading + errores) | ✅ Completo |
| `authStore` (JWT + localStorage + axios header)  | ✅ Completo |
| `dialogStore` (modales promise-based)      | ✅ Completo |
| `themeStore` (dark/light Bootstrap)        | ✅ Completo |
| `layoutStore` (sidebar/nav CSS classes)    | ✅ Completo |
| `msg.ts` (wrapper de dialogStore)          | ✅ Completo |
| `excelHelper.ts` (exportar/leer XLSX)      | ✅ Completo |
| Menú filtrado por usuario/rol real         | ✅ Completo — corregido `userId=1` hardcodeado en `AccessMenuController` |

---

## Gaps críticos para MVP

1. **Métodos de pago en POS** — la migración SQL `migration_payment_methods.sql` ya existe pero no tiene backend ni frontend. El POS registra ventas sin desglosar método de pago (efectivo, tarjeta, etc.).

2. **Descuentos por línea** — tablas `discounts` y `sale_detail_discounts` sin implementar.

---

## Mapa endpoint → frontend

| Endpoint backend                      | Ruta frontend                                     | Estado |
|---------------------------------------|---------------------------------------------------|--------|
| POST /Login                           | /auth                                             | ✅     |
| GET /AccessMenu                       | Al hacer login (filtrado por usuario real)        | ✅     |
| POST /Mfa/totp-setup-init             | /auth/totp-setup                                  | ✅     |
| POST /Mfa/totp-verify                 | /auth/totp                                        | ✅     |
| POST /Mfa/mfa-recover                 | /auth/totp                                        | ✅     |
| CRUD /Users                           | /account/users-admin + user-edit                  | ✅     |
| PUT /Users/{uuid}/password            | Modal en listado de usuarios (admin)              | ✅     |
| PUT /Users/me/password                | Dropdown de perfil en header (todos los roles)    | ✅     |
| POST /Users/{uuid}/mfa/reset          | Botón en listado de usuarios                      | ✅     |
| PUT /Users/{uuid}/mfa/required        | Botón en listado de usuarios                      | ✅     |
| DELETE /Users/{uuid}/mfa/required     | Botón en listado de usuarios                      | ✅     |
| CRUD /Roles + forms assign            | /account/roles-admin + role-edit                  | ✅     |
| CRUD /Forms                           | /account/forms-admin + form-edit                  | ✅     |
| CRUD /Modules                         | /account/modules-admin + module-edit              | ✅     |
| CRUD /Product                         | /inventory/products-admin + product-edit          | ✅     |
| GET /Product/{id}/validate            | PointOfSaleView.vue                               | ✅     |
| CRUD /Category                        | /inventory/categories-admin + category-edit       | ✅     |
| CRUD /Laboratory                      | /inventory/laboratories-admin + laboratory-edit   | ✅     |
| CRUD /UnitOfMeasurement               | /inventory/uom-admin + uom-edit                   | ✅     |
| CRUD /Customers                       | /inventory/customers-admin + customer-edit        | ✅     |
| CRUD /Provider                        | /inventory/providers-admin + provider-edit        | ✅     |
| CRUD /Purchases + reciveOrders        | /inventory/purchases-admin + purchase-edit + purchase-receive | ✅ |
| GET /PurchaseStatus                   | Selector en purchase-edit                         | ✅     |
| CRUD /Sales (filtrado por rol)        | /inventory/sales-admin + sale-detail              | ✅     |
| GET /CashSession/active               | PointOfSaleView.vue (al montar)                   | ✅     |
| GET /CashSession?dateFrom=&dateTo=    | /inventory/cash-sessions                          | ✅     |
| POST /CashSession/open                | Modal "Abrir caja" en POS                         | ✅     |
| PUT /CashSession/{id}/close           | Modal "Cerrar caja" en POS                        | ✅     |
| POST /CashSession/{id}/movements      | Modal "Registrar movimiento" en POS               | ✅     |
| GET /StockMovement/{productId}        | /inventory/stock-history/:id                      | ✅     |
| POST /StockMovement/adjustment        | /inventory/stock-adjustment/:id                   | ✅     |
| GET /Dashboard                        | /inventory (DashboardView)                        | ✅     |
| GET /reports/sales                    | /reports/sales                                    | ✅     |
| GET /reports/stock                    | /reports/stock                                    | ✅     |
| GET /reports/purchases                | /reports/purchases                                | ✅     |
