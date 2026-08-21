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
