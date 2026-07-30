# Estado de la Aplicación

> Última actualización: 2026-06-10

## Stack

- **Backend:** .NET 10, Clean Architecture, Dapper, FluentValidation, JWT, TOTP
- **Frontend:** Vue 3 (Composition API), Vite, TypeScript, Pinia, Vue Router 4, Axios, Bootstrap
- **Base de datos:** PostgreSQL — schemas `public` (negocio), `sec` (seguridad), `siat` (facturación electrónica Bolivia)

---

## Resumen ejecutivo

| Capa      | Progreso estimado | Notas                                           |
|-----------|------------------|-------------------------------------------------|
| Backend   | ~95%             | Pagos por método, descuentos y devoluciones implementados. Pendientes: tests, reportes server-side, paginación en ventas |
| Frontend  | ~95%             | POS con caja, pagos, descuentos y devoluciones operativos. Reportes calculados en cliente |

> Brechas funcionales del MVP previo (pagos, descuentos, devoluciones) **ya están implementadas** end-to-end. Las brechas restantes son de calidad/escalabilidad (ver "Gaps actuales").

---

## Backend

### Módulo Seguridad (`/api/...`)

| Recurso        | Endpoints | Estado       |
|----------------|-----------|--------------|
| Login          | POST      | ✅ Completo — devuelve `RolId` y `RolName` reales desde `sec.roles` |
| Users          | CRUD + roles + reset MFA + require MFA + cambio de contraseña (admin `{uuid}/password` y propio `me/password`) | ✅ Completo |
| Roles          | CRUD + asignación de Forms (`{id}/forms`) | ✅ Completo  |
| Forms          | CRUD      | ✅ Completo  |
| Modules        | CRUD      | ✅ Completo  |
| AccessMenu     | GET       | ✅ Completo — filtrado por usuario real |
| MFA / TOTP     | GET setup, POST enable, POST verify, POST verify-recovery, DELETE | ✅ Completo |

### Módulo Inventario (`/api/...`)

| Recurso             | Endpoints                              | Estado      |
|---------------------|----------------------------------------|-------------|
| Product             | CRUD + bulk (PUT) + stock + validate   | ✅ Completo |
| Category            | CRUD                                   | ✅ Completo |
| Customers           | CRUD                                   | ✅ Completo |
| Laboratory          | CRUD                                   | ✅ Completo |
| Provider            | CRUD                                   | ✅ Completo |
| UnitOfMeasurement   | CRUD                                   | ✅ Completo |
| Purchases           | CRUD + reciveOrders (recepción)        | ✅ Completo |
| PurchaseStatus      | GET (enum)                             | ✅ Completo |
| PaymentMethod       | GET                                    | ⚠️ Solo lectura — sin CRUD de administración (datos sembrados) |
| Settings            | GET pos (config del POS)               | ✅ Completo |
| Dashboard           | GET estadísticas                       | ✅ Completo |
| StockMovement       | GET por producto + POST adjust         | ✅ Completo |

### Módulo Ventas (`/api/...`)

| Recurso             | Endpoints                              | Estado      |
|---------------------|----------------------------------------|-------------|
| Sales               | CRUD + filtro por rol (Cajero/Admin). El POST acepta `Payments[]`, descuentos de cabecera y `SupervisorAuthToken` | ✅ Completo (⚠️ listado sin paginación) |
| Discounts           | CRUD                                   | ✅ Completo |
| SaleReturn          | POST (registrar devolución)            | ⚠️ Solo POST — sin listado/reporte global de devoluciones (se ven embebidas en el detalle de venta) |
| CashSession         | GET active, GET {id}, GET list, POST open, PUT {id}/close, GET {id}/sales, POST {id}/movements | ✅ Completo |

### Infraestructura backend

- Repositorios Dapper implementados para todas las entidades
- Validaciones FluentValidation en los requests
- Migraciones SQL: `migration_totp.sql`, `migration_mfa_v2.sql`, `migration_payment_methods.sql`, `migration_returns.sql`, `migration_discounts.sql`, `migration_cash_register.sql` (tabla `cash_sessions` con partial unique index para sesión única abierta, `cash_movements`, columna `cash_session_id` en `sales`)
- Swagger/OpenAPI configurado por grupos (SECURITY, POS)
- JWT incluye el rol real del usuario (`Rol` claim) obtenido de `sec.roles`
- Cifrado AES-256 para secretos TOTP

### Tablas en BD sin código backend correspondiente

| Tabla BD                  | Descripción                                              |
|---------------------------|----------------------------------------------------------|
| `products_providers`      | Catálogo de productos por proveedor                     |
| `sequences_key`           | Secuencias personalizadas por tabla (uso interno)        |

> `discounts`, `sale_detail_discounts`, `payment_methods`, `sale_payments`, devoluciones y caja **ya están implementadas** (entidad + repositorio + Application + Controller + frontend).

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
| Listado + CRUD Usuarios (incl. `UserName`)  | ✅ Completo |
| Reset MFA / require MFA (desde admin)       | ✅ Completo |
| Cambio de contraseña por admin (desde lista)| ✅ Completo |
| Cambio de contraseña propia (header)        | ✅ Completo |
| Listado + CRUD Roles + asignación de Forms  | ✅ Completo |
| Listado + CRUD Forms                        | ✅ Completo |
| Listado + CRUD Modules                      | ✅ Completo |

### Módulo `inventory`

| Feature                              | Estado             | Notas                                                         |
|--------------------------------------|--------------------|---------------------------------------------------------------|
| Listado + CRUD Productos             | ✅ Completo        | Selección de Lab, UOM y Categoría; listado paginado          |
| Admin Categorías                     | ✅ Completo        |                                                               |
| Admin Laboratory                     | ✅ Completo        |                                                               |
| Admin UnitOfMeasurement              | ✅ Completo        |                                                               |
| Admin Clientes (Customers)           | ✅ Completo        |                                                               |
| Admin Proveedores (Providers)        | ✅ Completo        |                                                               |
| Admin Compras (Purchases)            | ✅ Completo        | Incluye recepción de órdenes                                 |
| Admin Descuentos (Discounts)         | ✅ Completo        | `DiscountsAdminView`                                          |
| Listado Ventas (Sales)               | ✅ Completo        | Columna "Vendedor", filtro por vendedor y por rol            |
| Detalle de Venta + Devoluciones      | ✅ Completo        | `SaleDetailView` — registra devoluciones y muestra histórico embebido (`sale.Returns`) |
| Turnos de Caja (CashSessions)        | ✅ Completo        | Filtros por fecha, tabla/cards, modal de detalle con movimientos y arqueo |
| Punto de Venta (POS)                 | ✅ Completo        | Control de caja obligatorio; métodos de pago; descuentos (con autorización de supervisor); movimientos; arqueo al cerrar |
| Inventario / Stock                   | ✅ Completo        | Historial y ajuste de stock                                   |

### Módulo `reports`

| Feature               | Estado      | Notas |
|-----------------------|-------------|-------|
| Reporte de Ventas     | ✅ Funcional | Calculado en cliente (reutiliza `useSales`) |
| Reporte de Stock      | ✅ Funcional | Calculado en cliente (reutiliza `useProduct`) |
| Reporte de Compras    | ✅ Funcional | Calculado en cliente (reutiliza `usePurchase`) |

> ⚠️ No existe `ReportsController` en el backend. Los reportes agregan datos en el frontend cargando listas completas; no hay agregación ni paginación server-side.

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
| Menú filtrado por usuario/rol real         | ✅ Completo |
| `usePosStore` / `useCashSessionStore`      | ✅ Completo |

---

## Gaps actuales

### Calidad / escalabilidad
1. **Sin tests automatizados** — no hay proyectos de test en backend (.NET) ni specs en frontend. Es el hueco más relevante para mantenibilidad.
2. **Reportes sin backend** — no hay `ReportsController`; se calculan en cliente cargando datasets completos. No escala; falta agregación/paginación server-side.
3. **Listado de ventas sin paginación** — `SalesRepository` consulta `FROM sales` sin `LIMIT/OFFSET` (Productos sí está paginado).
4. **Devoluciones sin listado global** — `SaleReturnController` solo expone `POST`; solo se visualizan dentro del detalle de cada venta.
5. **PaymentMethod sin CRUD** — solo `GET`; los métodos de pago no son gestionables desde la UI.

### Funcional
6. **`products_providers`** — catálogo producto×proveedor sin implementar.
7. **`sequences_key`** — secuencias personalizadas por tabla sin implementar.
8. **Ventas suspendidas (POS móvil)** — hoy una venta en curso solo se puede *descartar*. Falta poder **aparcarla y retomarla**: el caso real es el cliente que se va a buscar plata y vuelve en cinco minutos, mientras el cajero atiende a los que esperan. Requiere persistir el carrito (líneas + descuentos de línea y de cabecera), una pantalla de ventas suspendidas y atarlas al turno de caja. Es lo que hacen Square, Lightspeed y Odoo; se pospuso por costo hasta que los cajeros lo pidan.

### DevOps / repositorio
9. **Migraciones manuales** — sin runner ni orden garantizado; varios `migration_*.sql` y `docker-compose.yml` sin commitear.
10. **Backups de BD en el repo** — ~9 MB de `db/*.sql` versionados (+ `futbol_backup.sql` vacío huérfano). Deberían salir de git.
11. **Sin CI/CD** configurado.

---

## Mapa endpoint → frontend

| Endpoint backend                      | Ruta frontend                                     | Estado |
|---------------------------------------|---------------------------------------------------|--------|
| POST /Login                           | /auth                                             | ✅     |
| GET /AccessMenu                       | Al hacer login (filtrado por usuario real)        | ✅     |
| GET /Mfa/setup                        | /auth/totp-setup                                  | ✅     |
| POST /Mfa/verify                      | /auth/totp                                        | ✅     |
| POST /Mfa/verify-recovery             | /auth/totp                                        | ✅     |
| CRUD /Users (+ roles)                 | /account/users-admin + user-edit                  | ✅     |
| PUT /Users/{uuid}/password            | Modal en listado de usuarios (admin)              | ✅     |
| PUT /Users/me/password                | Dropdown de perfil en header                      | ✅     |
| POST /Users/{uuid}/mfa/reset          | Botón en listado de usuarios                      | ✅     |
| PUT·DELETE /Users/{uuid}/mfa/required | Botones en listado de usuarios                    | ✅     |
| CRUD /Roles (+ {id}/forms)            | /account/roles-admin + role-edit                  | ✅     |
| CRUD /Forms                           | /account/forms-admin + form-edit                  | ✅     |
| CRUD /Modules                         | /account/modules-admin + module-edit              | ✅     |
| CRUD /Product (+ bulk, stock, validate)| /inventory/products-admin + product-edit         | ✅     |
| CRUD /Category                        | /inventory/categories-admin + category-edit       | ✅     |
| CRUD /Laboratory                      | /inventory/laboratories-admin + laboratory-edit   | ✅     |
| CRUD /UnitOfMeasurement               | /inventory/uom-admin + uom-edit                   | ✅     |
| CRUD /Customers                       | /inventory/customers-admin + customer-edit        | ✅     |
| CRUD /Provider                        | /inventory/providers-admin + provider-edit        | ✅     |
| CRUD /Purchases + reciveOrders        | /inventory/purchases-admin + purchase-edit + purchase-receive | ✅ |
| GET /PurchaseStatus                   | Selector en purchase-edit                         | ✅     |
| GET /PaymentMethod                    | PointOfSaleView.vue                               | ✅     |
| GET /Settings/pos                     | PointOfSaleView.vue                               | ✅     |
| CRUD /Discounts                       | /inventory/discounts-admin                        | ✅     |
| CRUD /Sales (con Payments + descuentos)| /inventory/sales-admin + sale-detail + POS       | ✅     |
| POST /SaleReturn                      | SaleDetailView.vue (modal de devolución)          | ✅     |
| GET /CashSession/active               | PointOfSaleView.vue (al montar)                   | ✅     |
| GET /CashSession + {id} + {id}/sales  | /inventory/cash-sessions                          | ✅     |
| POST /CashSession/open                | Modal "Abrir caja" en POS                         | ✅     |
| PUT /CashSession/{id}/close           | Modal "Cerrar caja" en POS                        | ✅     |
| POST /CashSession/{id}/movements      | Modal "Registrar movimiento" en POS               | ✅     |
| GET /StockMovement/{productId}        | /inventory/stock-history/:id                      | ✅     |
| POST /StockMovement/adjust            | /inventory/stock-adjustment/:id                   | ✅     |
| GET /Dashboard                        | /inventory (DashboardView)                        | ✅     |
| (sin endpoint) Reportes               | /reports/sales · /reports/stock · /reports/purchases (cálculo en cliente) | ⚠️ |
