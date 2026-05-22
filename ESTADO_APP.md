# Estado de la Aplicación

> Última actualización: 2026-05-22

## Stack

- **Backend:** .NET 10, Clean Architecture, Dapper, FluentValidation, JWT, TOTP
- **Frontend:** Vue 3 (Composition API), Vite, TypeScript, Pinia, Vue Router 4, Axios, Bootstrap
- **Base de datos:** PostgreSQL — schemas `public` (negocio), `sec` (seguridad), `siat` (facturación electrónica Bolivia)

---

## Resumen ejecutivo

| Capa      | Progreso estimado | Notas                                           |
|-----------|------------------|-------------------------------------------------|
| Backend   | ~70%             | CRUDs de inventario/seguridad listos; SIAT y tablas de pagos/descuentos sin implementar |
| Frontend  | ~55%             | Auth + Seguridad + Productos completados        |

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
| Customers           | CRUD                                   | ✅ Completo |
| Laboratory          | CRUD                                   | ✅ Completo |
| Provider            | CRUD                                   | ✅ Completo |
| UnitOfMeasurement   | CRUD                                   | ✅ Completo |
| Purchases           | CRUD + reciveOrders (recepción)        | ✅ Completo |
| PurchaseStatus      | GET (enum: Pending, Delivered, etc.)   | ✅ Completo |
| Sales               | CRUD                                   | ✅ Completo |

### Infraestructura backend

- Repositorios Dapper implementados para todas las entidades
- Validaciones FluentValidation en todos los requests
- Migraciones SQL: `migration_totp.sql`, `migration_mfa_v2.sql`
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
| `sequences_key`           | Secuencias personalizadas por tabla                     |

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

| Feature                         | Estado             | Notas                                                   |
|---------------------------------|--------------------|---------------------------------------------------------|
| Listado + CRUD Productos        | ✅ Completo        | Incluye selección de Lab y UOM                         |
| Dropdown Laboratory             | ✅ Parcial         | Solo búsqueda para dropdown, sin vista admin propia     |
| Dropdown UnitOfMeasurement      | ✅ Parcial         | Solo búsqueda para dropdown, sin vista admin propia     |
| Crear venta (Sales)             | ⚠️ Parcial        | `saveSaleApi` implementado, sin lista/edición/eliminación en UI |
| Admin Clientes (Customers)      | ❌ No iniciado     | Backend listo, cero frontend                           |
| Admin Proveedores (Providers)   | ❌ No iniciado     | Backend listo, cero frontend                           |
| Admin Compras (Purchases)       | ❌ No iniciado     | Backend listo (incluye recepción de pedido), cero frontend |
| Punto de Venta (POS)            | ❌ Vacío           | Rutas declaradas en router, componente `PointOfSaleView.vue` no existe |

### Módulo `reports`

| Feature   | Estado         | Notas                                        |
|-----------|----------------|----------------------------------------------|
| Reportes  | ❌ No iniciado | Mencionado en CLAUDE.md, sin rutas ni vistas |

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

Los siguientes elementos bloquean el flujo principal de negocio:

1. **Vista Punto de Venta (`PointOfSaleView.vue`)** — las rutas `/pos/` están declaradas pero el componente no existe. Sin POS no hay ventas desde caja.

2. **CRUD Clientes** — el módulo de ventas necesita seleccionar cliente; backend listo, falta:
   - `useCustomer.ts` composable
   - `CustomersAdminView.vue` + `CustomerEditView.vue`
   - Rutas en router

3. **CRUD Proveedores** — necesario para registrar compras; backend listo, falta lo mismo que clientes.

4. **CRUD Compras** — flujo de ingreso de stock al almacén; backend listo incluyendo endpoint de recepción de pedido, falta:
   - `usePurchase.ts` composable
   - `PurchasesAdminView.vue` + `PurchaseEditView.vue` + vista de recepción
   - Rutas en router

5. **UI completa de Ventas** — solo existe `saveSaleApi`; falta listado, detalle y baja de ventas.

6. **Admin de Laboratorios y Unidades de Medida** — ambos tienen CRUD completo en backend pero solo se usan como dropdowns en el formulario de producto. Sin vistas admin, el catálogo solo se puede gestionar directamente en BD.

7. **Métodos de pago y descuentos** — las tablas `payment_methods`, `discounts`, `sale_payments` y `sale_detail_discounts` existen en BD pero no tienen ningún código backend ni frontend. El POS no podrá registrar pagos con vuelto ni descuentos hasta implementarlos.

8. ~~**Módulo SIAT**~~ — fuera de alcance de este proyecto. Ver sección SIAT más arriba.

---

## Mapa endpoint → frontend

| Endpoint backend                    | Ruta frontend                         | Estado        |
|-------------------------------------|---------------------------------------|---------------|
| POST /Login                         | /auth                                 | ✅            |
| GET /AccessMenu                     | Al hacer login                        | ✅            |
| POST /Mfa/totp-setup-init           | /auth/totp-setup                      | ✅            |
| POST /Mfa/totp-verify               | /auth/totp                            | ✅            |
| POST /Mfa/mfa-recover               | /auth/totp                            | ✅            |
| CRUD /Users + MFA admin             | /account/users-admin + user-edit      | ✅            |
| CRUD /Roles + forms assign          | /account/roles-admin + role-edit      | ✅            |
| CRUD /Forms                         | /account/forms-admin + form-edit      | ✅            |
| CRUD /Modules                       | /account/modules-admin + module-edit  | ✅            |
| CRUD /Product                       | /inventory/products-admin + product-edit | ✅         |
| GET /Product/{id}/validate          | (POS, componente vacío)               | ❌            |
| GET /Laboratory (dropdown)          | Selector en product-edit              | ✅ Parcial    |
| CRUD /Laboratory (admin)            | Sin ruta/vista                        | ❌            |
| GET /UnitOfMeasurement (dropdown)   | Selector en product-edit              | ✅ Parcial    |
| CRUD /UnitOfMeasurement (admin)     | Sin ruta/vista                        | ❌            |
| CRUD /Customers                     | Sin ruta/vista                        | ❌            |
| CRUD /Provider                      | Sin ruta/vista                        | ❌            |
| CRUD /Purchases + reciveOrders      | Sin ruta/vista                        | ❌            |
| GET /PurchaseStatus                 | Sin uso                               | ❌            |
| POST /Sales (crear)                 | useSales.ts (sin vista POS)           | ⚠️ Parcial   |
| GET/PUT/DELETE /Sales               | Sin vista                             | ❌            |

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
