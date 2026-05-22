# Use una imagen base con .NET Core SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Establecer el directorio de trabajo en /app
WORKDIR /app

# Copiar los archivos de proyecto y restaurar las dependencias
# COPY *.csproj ./

# Copiar los archivos de proyecto de cada proyecto y restaurar las dependencias
COPY 1-Services/Services.Api/Services.Api.csproj 1-Services/Services.Api/
COPY 2-Application/Facturacion.Application/Facturacion.Application.csproj 2-Application/Facturacion.Application/
COPY 2-Application/Inventory.Application/Inventory.Application.csproj 2-Application/Inventory.Application/
COPY 2-Application/Siat.Application/Siat.Application.csproj 2-Application/Siat.Application/
COPY 2-Application/Seguridad.Application/Seguridad.Application.csproj 2-Application/Seguridad.Application/
COPY 3-Infrastructure/Facturacion.Infrastructure/Facturacion.Infrastructure.csproj 3-Infrastructure/Facturacion.Infrastructure/
COPY 3-Infrastructure/Inventory.Infrastructure/Inventory.Infrastructure.csproj 3-Infrastructure/Inventory.Infrastructure/
COPY 3-Infrastructure/Seguridad.Infrastructure/Seguridad.Infrastructure.csproj 3-Infrastructure/Seguridad.Infrastructure/
COPY 4-Domain/Facturacion.Domain/Facturacion.Domain.csproj 4-Domain/Facturacion.Domain/
COPY 4-Domain/Inventory.Domain/Inventory.Domain.csproj 4-Domain/Inventory.Domain/
COPY 4-Domain/Seguridad.Domain/Seguridad.Domain.csproj 4-Domain/Seguridad.Domain/
COPY Common.Utilities/Common.Utilities.csproj Common.Utilities/

RUN dotnet restore 1-Services/Services.Api/Services.Api.csproj

# Copiar el resto de los archivos y compilar la aplicación
COPY . .
RUN dotnet publish 1-Services/Services.Api/Services.Api.csproj -c Release -r linux-x64 -o /app/publish

# Crear una imagen más pequeña para ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Configurar el punto de entrada para ejecutar la aplicación
ENTRYPOINT ["dotnet", "Services.Api.dll"]

# Despues ejecutar para verificar la compilacion
# docker build -t punto-venta-api .