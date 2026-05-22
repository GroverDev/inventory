#!/bin/bash
# Coloca este archivo en la raíz del proyecto: init-databases.sh
# Este script crea las bases de datos 'rrhh' y 'sales' automáticamente

set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Crear base de datos rrhh si no existe
    SELECT 'CREATE DATABASE rrhh'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'punto_venta_pg')\gexec

    -- Crear base de datos sales si no existe
    SELECT 'CREATE DATABASE sales'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'otros')\gexec

    -- Otorgar privilegios
    GRANT ALL PRIVILEGES ON DATABASE punto_venta_pg TO $POSTGRES_USER;
    --GRANT ALL PRIVILEGES ON DATABASE sales TO $POSTGRES_USER;
EOSQL

echo "✓ Bases de datos 'punto_venta_pg' y 'otros' creadas exitosamente"