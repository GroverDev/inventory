# Facturación
Net 8 , Dapper, PostgreSQL


# Como compilar el docker file en imagen

docker build -t facturacion-api .
# en net 8 por defecto se compila en docker en puerto 8080

# Como crear el container con docker-compose 

docker-compose up

# Como ejecutar el containner con comando

docker run -d -p 7001:8080 facturacion-api

# Manejo en local
# Guardar una imagen en local
docker save facturacion-api > /home/grover/facturacion-api.tar

# Cargar una imagen en local
docker load < /home/grover/facturacion-api.tar