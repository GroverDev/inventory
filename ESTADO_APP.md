# Estado de la Aplicación

> Última actualización: 2026-05-25

## Stack

- **Backend:** .NET 10, Clean Architecture, Dapper, FluentValidation, JWT, TOTP
- **Frontend:** Vue 3 (Composition API), Vite, TypeScript, Pinia, Vue Router 4, Axios, Bootstrap
- **Base de datos:** PostgreSQL — schemas `public` (negocio), `sec` (seguridad), `siat` (facturación electrónica Bolivia)

---

## Resumen ejecutivo

| Capa      | Progreso estimado | Notas                                           |
|-----------|------------------|-------------------------------------------------|
| Backend   | ~85%             | Categorías y movimientos de stock agregados; tablas de pagos/descuentos aún sin implementar |
| Frontend  | ~85%             | POS implementado, Compras/Clientes/Proveedores/Reportes/Stock completados |

---

## Backend

### Módulo Seguridad (`/api/...`)

| Recurso        | Endpoints | Estado       |
|----------------|-----------|--------------|
| Login          | POST      | ✅ Completo  |
| Users          | CRUD + reset MFA + require MFA | ✅ Completo |
| Roles          | CRUD + asignación de Forms | ✅ Completo |
| Forms          | CRUD      | ✅ Completo  |
| Modules        | CRUD      | ✅ Completo  |
| AccessMenu     | GET       | ✅ Completo  |
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
| Sales               | CRUD                                   | ✅ Completo |
| StockMovement       | GET por producto + POST ajuste         | ✅ Completo |
| Dashboard           | GET estadísticas                       | ✅ Completo |

### Infraestructura backend

- Repositorios Dapper implementados para todas las entidades
- Validaciones FluentValidation en todos los requests
- Migraciones SQL: `migration_totp.sql`, `migration_mfa_v2.sql`, `migration_categories.sql`, `migration_stock_movements.sql`, `migration_reports_menu.sql`
- Swagger/OpenAPI configurado por grupos (SECURITY, POS)

### Tablas en BD sin código backend correspondiente

Las siguientes tablas existen en el schema `public` pero **no tienen entidad, repositorio ni endpoint** implementados:

| Tabla BD                  | Descripción                                              |
|---------------------------|----------------------------------------------------------|
| `discounts`               | Descuentos por nombre/tipo/valor                        |
| `payment_methods`         | Métodos de pago (efectivo, tarjeta, etc.) con `requires_changes` |
| `sale_detail_discounts`   | Descuentos aplicados por línea de venta                 |
| `sale_payments`           | Pagos de una venta: método + monto entregado + vuelto   |
| `products_providers`      | Catálogo de productos por proveedor (nombre y precio propios del proveedor) |
| `sequences_key`           | Secuencias personalizadas por tabla (usada internamente por `set_sequences_key`) |

---

## SIAT — Facturación Electrónica Bolivia (fuera de alcance)

> **Este proyecto NO implementará nada relacionado con el schema `siat`. Queda reservado para una fase futura.**

El schema existe en BD con 16 tablas completamente diseñadas (empresas, sucursales, puntos de venta, CUIS/CUFD, facturas electrónicas de 48 campos, catálogo SIN, etc.), pero **no se tocará en este proyecto**. El foco de desarrollo es exclusivamente los schemas `public` y `sec`.

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

| Feature                              | Estado      |
|--------------------------------------|-------------|
| Listado + CRUD Usuarios              | ✅ Completo |
| Reset MFA / require MFA (desde admin)| ✅ Completo |
| Listado + CRUD Roles                 | ✅ Completo |
| Asignación de Forms a Rol            | ✅ Completo |
| Listado + CRUD Forms                 | ✅ Completo |
| Listado + CRUD Modules               | ✅ Completo |

### Módulo `inventory`

| Feature                              | Estado             | Notas                                                         |
|--------------------------------------|--------------------|---------------------------------------------------------------|
| Listado + CRUD Productos             | ✅ Completo        | Incluye selección de Lab, UOM y Categoría                    |
| Admin Categorías                     | ✅ Completo        | `useCategory.ts` + `CategoriesAdminView` + `CategoryEditView` |
| Admin Laboratory                     | ✅ Completo        | `useLaboratory.ts` + `LaboratoriesAdminView` + `LaboratoryEditView` |
| Admin UnitOfMeasurement              | ✅ Completo        | `useUnitOfMeasurement.ts` + `UnitOfMeasurementAdminView` + `UnitOfMeasurementEditView` |
| Admin Clientes (Customers)           | ✅ Completo        | `useCustomer.ts` + `CustomersAdminView` + `CustomerEditView` |
| Admin Proveedores (Providers)        | ✅ Completo        | `useProvider.ts` + `ProvidersAdminView` + `ProviderEditView` |
| Admin Compras (Purchases)            | ✅ Completo        | `usePurchase.ts` + `PurchasesAdminView` + `PurchaseEditView` + `PurchaseReceiveView` |
| Listado + Detalle Ventas (Sales)     | ✅ Completo        | `SalesAdminView` + `SaleDetailView`; CRUD completo en composable |
| Inventario / Stock                   | ✅ Completo        | `InventoryStockView`, `StockAdjustmentView`, `StockHistoryView`, `useStockMovement.ts` |
| Punto de Venta (POS)                 | ⚠️ Parcial        | `PointOfSaleView.vue` implementado (694 líneas, búsqueda cliente/producto, carrito); ruta comentada en router — no accesible aún |

### Módulo `reports`

| Feature               | Estado      | Notas                                        |
|-----------------------|-------------|----------------------------------------------|
| Reporte de Ventas     | ✅ Completo | `SalesReportView.vue` con filtros por fecha  |
| Reporte de Stock      | ✅ Completo | `StockReportView.vue`                        |
| Reporte de Compras    | ✅ Completo | `PurchasesReportView.vue`                    |

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

---

## Gaps críticos para MVP

Los siguientes elementos bloquean o limitan el flujo principal de negocio:

1. **Ruta POS comentada** — `PointOfSaleView.vue` está completamente implementado (búsqueda de cliente, carrito, registro de venta) pero la ruta en el router está comentada (`// path: 'point-of-sale'`). Activar la ruta es el único paso pendiente para acceder al POS.

2. **Métodos de pago y descuentos** — las tablas `payment_methods`, `discounts`, `sale_payments` y `sale_detail_discounts` existen en BD pero no tienen ningún código backend ni frontend. El POS actualmente registra ventas sin desglosar métodos de pago ni descuentos por línea.

3. ~~**Módulo SIAT**~~ — fuera de alcance de este proyecto. Ver sección SIAT más arriba.

---

## Mapa endpoint → frontend

| Endpoint backend                    | Ruta frontend                                     | Estado        |
|-------------------------------------|---------------------------------------------------|---------------|
| POST /Login                         | /auth                                             | ✅            |
| GET /AccessMenu                     | Al hacer login                                    | ✅            |
| POST /Mfa/totp-setup-init           | /auth/totp-setup                                  | ✅            |
| POST /Mfa/totp-verify               | /auth/totp                                        | ✅            |
| POST /Mfa/mfa-recover               | /auth/totp                                        | ✅            |
| CRUD /Users + MFA admin             | /account/users-admin + user-edit                  | ✅            |
| CRUD /Roles + forms assign          | /account/roles-admin + role-edit                  | ✅            |
| CRUD /Forms                         | /account/forms-admin + form-edit                  | ✅            |
| CRUD /Modules                       | /account/modules-admin + module-edit              | ✅            |
| CRUD /Product                       | /inventory/products-admin + product-edit          | ✅            |
| GET /Product/{id}/validate          | PointOfSaleView.vue (ruta comentada en router)    | ⚠️ Parcial   |
| CRUD /Category                      | /inventory/categories-admin + category-edit       | ✅            |
| CRUD /Laboratory                    | /inventory/laboratories-admin + laboratory-edit   | ✅            |
| CRUD /UnitOfMeasurement             | /inventory/uom-admin + uom-edit                   | ✅            |
| CRUD /Customers                     | /inventory/customers-admin + customer-edit        | ✅            |
| CRUD /Provider                      | /inventory/providers-admin + provider-edit        | ✅            |
| CRUD /Purchases + reciveOrders      | /inventory/purchases-admin + purchase-edit + purchase-receive | ✅ |
| GET /PurchaseStatus                 | Selector en purchase-edit                         | ✅            |
| CRUD /Sales                         | /inventory/sales-admin + sale-detail              | ✅            |
| GET /StockMovement/{productId}      | /inventory/stock-history/:id                      | ✅            |
| POST /StockMovement/adjustment      | /inventory/stock-adjustment/:id                   | ✅            |
| GET /Dashboard                      | /inventory (DashboardView)                        | ✅            |
| GET /reports/sales                  | /reports/sales                                    | ✅            |
| GET /reports/stock                  | /reports/stock                                    | ✅            |
| GET /reports/purchases              | /reports/purchases                                | ✅            |

---

## Estado de la base de datos (datos reales)

| Tabla                   | Filas | Notas                                      |
|-------------------------|-------|--------------------------------------------|
| `sec.users_login`       | 11    | Intentos de login registrados (auditoria)  |
| `sec.forms`             | 2     | Dos formularios configurados               |
| `sec.roles_forms`       | 4     | Asignaciones de formularios a roles        |
| `public.zlogs_app`      | 8     | Logs de errores de la aplicación           |
| Resto de tablas         | 0     | Sin datos — app aún no en producción       |

El log `zlogs_app` ya tiene 8 entradas, lo que indica que el backend ha sido ejecutado y probado con Serilog activo.
