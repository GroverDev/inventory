# ETAPA 1: BUILD (Compilación)
# Usa la imagen específica de Node.js v22.11.0 basada en Alpine (más ligera)
FROM node:22.11.0-alpine as build-stage 
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build # Este comando genera la carpeta `dist`

# --- (El resto del Dockerfile no cambia, ya que usa Nginx) ---

# ETAPA 2: PRODUCTION (Servidor Nginx)
FROM nginx:alpine as production-stage
COPY default.conf /etc/nginx/conf.d/default.conf
COPY --from=build-stage /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]