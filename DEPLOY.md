# Despliegue en VPS

**Requisitos:** Ubuntu Server con Nginx (SSL configurado) y Docker instalados.  
**Dominios:** `app.ideanueva.com` (frontend) · `api.ideanueva.com` (backend)

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
#    Usa el backup más reciente disponible en db/
docker exec -i punto_venta_db psql -U postgres punto_venta_pg < db/02062026_inventory_backup.sql
```

---

## Actualizar

```bash
cd /opt/punto-venta
git pull
docker compose up --build -d
```
