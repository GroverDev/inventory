---
name: project-discount-system
description: Sistema de descuentos completo implementado en el POS — backend, frontend, autorización y reportes
metadata:
  type: project
---

Sistema de descuentos implementado en tres fases más pantalla de gestión.

**Why:** El POS no tenía soporte de descuentos. Se implementó desde cero con backend + frontend.

**How to apply:** Al continuar trabajo en descuentos, toda la lógica ya está en producción. No reimplementar nada de esto.

## Fase 1 — Backend (Clean Architecture)
- `Discount.cs` entidad, `DiscountRequest`, `DiscountResponse`, `PosSettingsResponse`
- `IDiscountRepository` + `DiscountRepository` (Dapper, tabla `discounts`)
- `IDiscountApplication` + `DiscountApplication` (CRUD + validaciones)
- `DiscountsController` — 5 endpoints REST en grupo "POS"
- `SalesApplication.CreateSale()` recalcula descuentos server-side, valida límites por rol
- `SalesDetailRepository` inserta en `sale_detail_discounts` cuando aplica
- Migración: `migration_discounts.sql`

## Fase 2 — Frontend POS
- `discount.model.ts`, `useDiscount.ts`
- Modal de descuento en POS: tabs Predefinido (catálogo) / Manual (% o monto fijo)
- Descuento por línea: botón % verde en cada ítem del carrito
- Descuento global: botón en sección de totales
- `recalcLine()` recalcula descuento % cuando cambia cantidad
- Modelos `SaleDetail` y `Sale` extendidos con campos de descuento

## Fase 3 — Autorización supervisor
- Límites configurables en `appsettings.json` → `PosSettings`: `MaxCashierDiscountPct=15`, `MaxCashierDiscountAmount=50`
- `PosSettings.cs` + `IOptions<PosSettings>` en `SalesApplication`
- `GET /Settings/pos` → expone límites al frontend
- `usePosSettings.ts` composable
- Modal supervisor en POS: cajero ingresa credenciales de supervisor
- `verifySupervisor()` llama POST /Login sin tocar la sesión activa
- Token del supervisor viaja en `SaleRequest.SupervisorAuthToken`
- `SalesController.ValidateSupervisorToken()` valida firma JWT + extrae rol
- `supervisorApproved` parámetro en `CreateSale` omite validación de límites

## Pantalla de gestión de descuentos
- `DiscountsAdminView.vue` — tabla desktop + cards mobile, modal inline crear/editar
- Ruta `/discounts-admin` registrada en router

## Reportes de ventas
- `SalesAdminView.vue`: 4 KPI cards (Ventas, Subtotal, Descuentos con %, Total cobrado), tfoot con totales por columna, descuentos en cards mobile, exportar Excel
- `SaleDetailView.vue`: columnas Subtotal/Descuento por línea en tabla productos, tfoot con Subtotal tras desc. línea + Desc. global + TOTAL, desglose en resumen ②
- `SaleProductResponse` y queries `GetSale`/`GetSales` retornan `HeaderDiscountAmount`

## Bugs corregidos
- `Unrecognized Guid format` en `DiscountApplication` y `SalesApplication` → normalizar `""` a `Guid.Empty.ToString()` antes de `Adapt<>()`
- Supervisor aprueba en frontend pero backend rechaza → `SupervisorAuthToken` en request
- Texto blanco ilegible en filas devueltas dark mode → `data-bs-theme="light"` en esas filas
- thead/tfoot gris en light mode → quitadas clases de fondo, usar `border-top`
