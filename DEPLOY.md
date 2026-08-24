# Despliegue en VPS

**Requisitos:** Ubuntu Server con Nginx (SSL configurado) y Docker instalados.  
**Dominios:** `app.ideanueva.com` (frontend) · `api.ideanueva.com` (backend)

Los nombres de los contenedores salen de `docker-compose.yml`: `pos_db_dos`
(base), `pos_backend_dos` y `pos_frontend_dos`. La base se llama `punto_venta` y
el superusuario es el `DB_USER` del `.env`.

> **Cargar el `.env` antes de usar `$DB_USER`.** `docker compose` lo lee solo
> para sí mismo: en la shell esa variable no existe y los comandos de abajo
> quedarían con `-U` vacío (`FATAL: role "-d" does not exist`).
> ```bash
> cd /opt/punto-venta
> set -a; . ./.env; set +a
> ```

---

## Primera vez

```bash
# 1. Clonar el repositorio
git clone <tu-repo> /opt/punto-venta
cd /opt/punto-venta

# 2. Crear el archivo de variables de entorno
cp .env.example .env
nano .env

# 3. Compilar y levantar los 3 contenedores (db, backend, frontend)
docker compose up --build -d

# 4. Restaurar la base de datos (solo una vez — el volumen queda persistido)
#    La carpeta ./db del host se ve como /backups dentro del contenedor.
set -a; . ./.env; set +a
docker exec -i pos_db_dos psql -U "$DB_USER" -d punto_venta < db/pos_backup_20260810.sql
```

---

## Actualizar

```bash
cd /opt/punto-venta
git pull
docker compose up --build -d
```

---

## Backup

```bash
cd /opt/punto-venta
set -a; . ./.env; set +a
docker exec -t pos_db_dos pg_dump -U "$DB_USER" -d punto_venta -F c \
  -f "/backups/punto_venta_$(date +%Y%m%d_%H%M).dump"
```

El archivo queda en `./db/` del host (mismo directorio, montado como `/backups`).
Para restaurarlo en otra base: `pg_restore -U "$DB_USER" -d <destino> <archivo>`.

---

## Despliegue 2026-08-21 — fechas y horas

Cambia el manejo de fechas en las tres capas y trae **5 migraciones**. Las
migraciones van **antes** que el código nuevo: el backend nuevo espera
`purchase_date` como columna `date`, mientras que el backend viejo funciona
igual contra las columnas ya migradas. En ese orden no hay estado intermedio roto.

```bash
cd /opt/punto-venta
set -a; . ./.env; set +a

# 1. Traer el código nuevo (deja las migraciones en ./db/migrations, que el
#    contenedor de la base ve como /backups/migrations).
git pull

# 2. Backup ANTES de migrar. Varias reescriben columnas y datos.
docker exec -t pos_db_dos pg_dump -U "$DB_USER" -d punto_venta -F c \
  -f "/backups/pre_fechas_$(date +%Y%m%d_%H%M).dump"

# 3. Parar el backend para que nadie escriba durante la migración.
docker compose stop backend

# 4. Migraciones, en este orden. ON_ERROR_STOP corta a la primera que falle.
for m in 2026-08-21_cash_sessions_timezone_fix \
         2026-08-21_timestamps_utc_resto \
         2026-08-21_purchase_dates_a_date \
         2026-08-21_cliente_generico_nombre \
         2026-08-21_corrige_sale_date_movil ; do
  echo "── $m"
  docker exec -i pos_db_dos psql -v ON_ERROR_STOP=1 -U "$DB_USER" -d punto_venta \
    -f "/backups/migrations/$m.sql" || break
done

# 5. Levantar backend y frontend con el código nuevo.
docker compose up --build -d
```

### Qué hace cada migración

| Migración | Qué cambia |
|---|---|
| `cash_sessions_timezone_fix` | `cash_sessions` y `cash_movements` pasan a `timestamptz` |
| `timestamps_utc_resto` | Lo mismo en `stock_movements`, `sale_returns`, `sale_return_detail`, `categories`, `payment_methods`. Recrea la vista `v_mermas` |
| `purchase_dates_a_date` | Las 3 fechas de compra pasan de `timestamptz` a `date` |
| `cliente_generico_nombre` | El cliente que siembra un tenant nuevo se llama "Cliente Genérico" |
| `corrige_sale_date_movil` | Corrige las ventas que la app móvil guardó 4 h antes de ocurrir |

### Verificación

```bash
set -a; . ./.env; set +a

# a) Solo debe quedar zlogs_app.raise_date sin zona horaria (es de Serilog).
docker exec -it pos_db_dos psql -U "$DB_USER" -d punto_venta -c "
SELECT table_name, column_name FROM information_schema.columns
 WHERE data_type='timestamp without time zone' AND table_schema IN ('public','sec');"

# b) v_mermas debe conservar security_invoker: sin él la vista corre como su
#    dueño (con BYPASSRLS) y una farmacia vería las mermas de las demás.
docker exec -it pos_db_dos psql -U "$DB_USER" -d punto_venta -c "
SELECT reloptions FROM pg_class WHERE relname='v_mermas';"

# c) Ninguna venta debe quedar con ~4 h de diferencia contra su 'created'.
docker exec -it pos_db_dos psql -U "$DB_USER" -d punto_venta -c "
SELECT count(*) AS ventas_corridas FROM sales
 WHERE EXTRACT(EPOCH FROM (created - sale_date)) BETWEEN 14100 AND 14700;"
```

En la app: abrir **Turnos de Caja** y **Registro de Ventas** y confirmar que las
horas coinciden con la hora local, y que un turno abierto figura aunque se haya
abierto fuera del rango de fechas filtrado.

### Después del despliegue

`corrige_sale_date_movil` arregla el historial, pero **los celulares que todavía
no actualizaron la app siguen enviando la hora mal**. Es idempotente y se puede
volver a correr cuando la nueva versión esté difundida:

```bash
docker exec -i pos_db_dos psql -v ON_ERROR_STOP=1 -U "$DB_USER" -d punto_venta \
  -f /backups/migrations/2026-08-21_corrige_sale_date_movil.sql
```

### Si algo sale mal

Restaurar el backup del paso 2 sobre una base nueva y comparar antes de pisar la
que está en producción:

```bash
docker exec -it pos_db_dos psql -U "$DB_USER" -d postgres -c 'CREATE DATABASE punto_venta_rollback;'
docker exec -i  pos_db_dos pg_restore -U "$DB_USER" -d punto_venta_rollback /backups/pre_fechas_<fecha>.dump
```

---

## Despliegue 2026-08-24 — devoluciones, neto de ventas y arqueo

Trae tres cambios que tocan plata, y **4 migraciones de esquema** (más una de
datos que se re-ejecuta y una que es solo documentación):

1. **Devoluciones sobre el precio efectivamente cobrado.** Antes se reembolsaba
   `cantidad * precio de lista`, sin descontar los descuentos: una venta de 84.00
   con 55.00 de descuento, cobrada en 29.00, devolvía 84.00. Ahora el importe lo
   calcula el servidor desde la venta y el `UnitPrice` que manda el cliente se
   ignora.
2. **Ventas netas de devoluciones.** La vista `v_sales_net` centraliza el neto y
   la leen el listado de ventas, el reporte, el dashboard y el detalle.
3. **Arqueo de caja real.** El efectivo esperado suma solo lo cobrado por medios
   que entran al cajón (`payment_methods.affects_cash`) y resta las devoluciones
   reintegradas en efectivo, que ahora generan un `cash_movements` de tipo
   `return`.

Las migraciones van **antes** que el código: todas agregan columnas con default o
crean objetos nuevos, así que el backend viejo sigue funcionando contra la base ya
migrada. No hay estado intermedio roto.

> **Antes de empezar: el lote del 2026-08-21 tiene que estar aplicado.** Si ese
> despliegue nunca se hizo en el VPS, correrlo primero (sección anterior) y recién
> después este. Cómo saber en qué estado está la base:
> ```bash
> docker exec -it pos_db_dos psql -U "$DB_USER" -d punto_venta -c "
> SELECT column_name, data_type FROM information_schema.columns
>  WHERE table_name = 'purchases' AND column_name = 'purchase_date';"
> ```
> Si devuelve `date`, el lote del 21 ya está. Si devuelve `timestamp with time
> zone`, falta.

```bash
cd /opt/punto-venta
set -a; . ./.env; set +a

# 1. Traer el código nuevo (deja las migraciones en ./db/migrations, que el
#    contenedor de la base ve como /backups/migrations).
git pull

# 2. Backup ANTES de migrar.
docker exec -t pos_db_dos pg_dump -U "$DB_USER" -d punto_venta -F c \
  -f "/backups/pre_devoluciones_$(date +%Y%m%d_%H%M).dump"

# 3. Parar el backend para que nadie escriba durante la migración.
docker compose stop backend

# 4. Migraciones, en este orden.
for m in 2026-08-24_devoluciones_precio_efectivo \
         2026-08-24_vista_ventas_neto \
         2026-08-24_caja_solo_efectivo \
         2026-08-24_devoluciones_a_caja \
         2026-08-21_corrige_sale_date_movil ; do
  echo "── $m"
  docker exec -i pos_db_dos psql -v ON_ERROR_STOP=1 -U "$DB_USER" -d punto_venta \
    -f "/backups/migrations/$m.sql" || break
done

# 5. Levantar backend y frontend con el código nuevo.
docker compose up --build -d
```

### Qué hace cada migración

| Migración | Qué cambia |
|---|---|
| `devoluciones_precio_efectivo` | `sale_return_detail.discount_share`: los descuentos que corresponden a lo devuelto. `line_total` pasa a ser el importe realmente reembolsado |
| `vista_ventas_neto` | Crea `v_sales_net` (total, devuelto, neto y estado por venta) e indexa `sale_returns` por venta |
| `caja_solo_efectivo` | `payment_methods.affects_cash` + siembra de tenant nuevo con la bandera correcta |
| `devoluciones_a_caja` | `sale_returns.cash_session_id` y `payment_method_id`; `cash_movements` acepta el tipo `return` |
| `corrige_sale_date_movil` | Re-ejecución: corrige las ventas que la app móvil vieja guardó 4 h antes |

`2026-08-24_notas_historico_ventas.sql` **no se ejecuta**: no cambia nada, solo
deja documentadas tres inconsistencias históricas que se decidió no corregir.

### Verificación

```bash
set -a; . ./.env; set +a

# a) La vista tiene que existir CON security_invoker: sin él corre como su dueño
#    y el aislamiento por tenant deja de aplicarse.
docker exec -it pos_db_dos psql -U "$DB_USER" -d punto_venta -c "
SELECT reloptions FROM pg_class WHERE relname='v_sales_net';"

# b) El rol de la aplicación tiene que poder leerla.
docker exec -it pos_db_dos psql -U "$APP_DB_USER" -d punto_venta -c "
SELECT count(*) FROM v_sales_net;"

# c) Efectivo por método: Efectivo en true, QR y Tarjeta en false.
docker exec -it pos_db_dos psql -U "$DB_USER" -d punto_venta -c "
SELECT name, requires_changes, affects_cash FROM payment_methods ORDER BY name;"

# d) Ninguna venta con ~4 h de diferencia contra su 'created'.
docker exec -it pos_db_dos psql -U "$DB_USER" -d punto_venta -c "
SELECT count(*) AS ventas_corridas FROM sales
 WHERE EXTRACT(EPOCH FROM (created - sale_date)) BETWEEN 14100 AND 14700;"
```

En la app, sobre una venta con devolución: **Registro de Ventas** debe mostrar la
columna *Devuelto* y el total tachado con el neto al lado, y los KPIs del período
deben cerrar (facturado − devuelto = neto). En **Turnos de Caja**, una sesión con
venta por QR debe mostrar "en efectivo" menor que "Ventas".

Probar además los tres caminos de una devolución nueva: en efectivo con caja
abierta (aparece el movimiento *Devolución* y baja el esperado), en efectivo sin
caja abierta (debe rechazarla), y por QR (no toca la caja).

### Después del despliegue

**Publicar la app móvil.** Los celulares con la versión vieja siguen mandando la
hora local sin zona (ventas 4 h antes) y no mandan el medio de reintegro, así que
el servidor lo deduce del pago de la venta. Cuando la nueva versión esté
difundida, volver a correr:

```bash
docker exec -i pos_db_dos psql -v ON_ERROR_STOP=1 -U "$DB_USER" -d punto_venta \
  -f /backups/migrations/2026-08-21_corrige_sale_date_movil.sql
```
