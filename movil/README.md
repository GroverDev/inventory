# Inventario Móvil (Flutter)

App móvil que consume **el backend .NET existente** (`../backend`) para:

- **Productos** — buscar, crear, editar y eliminar (incluye stock, precio, categoría, laboratorio, unidad).
- **Punto de venta (POS)** — apertura de caja, búsqueda de productos, carrito y cobro.
- **Pedidos** — listar y crear compras a proveedores.

No se modificó nada del backend ni del frontend: la app reutiliza los mismos endpoints REST y el mismo login JWT.

## Stack

- Flutter (Material 3) + Dart
- `dio` (HTTP + interceptor Bearer), `provider` (estado), `flutter_secure_storage` (token), `intl`, `mobile_scanner`.

## Estructura

```
lib/
  core/        config, red (ApiClient + ApiResponse), almacenamiento, tema
  models/      espejo de los DTOs del backend (PascalCase)
  services/    una clase por área: auth, product, sale, purchase, catalog
  providers/   estado: AuthProvider, CartProvider
  features/    auth, home, products, pos, orders
```

## Configuración

### 1. Generar las carpetas nativas (android/ios)

Este repo trae solo el código Dart (`lib/`, `pubspec.yaml`). Genera los proyectos
nativos **sin sobrescribir** lo existente:

```sh
cd movil
flutter create --platforms=android,ios --org com.inventory --project-name inventory_movil .
flutter pub get
```

> En WSL usa el Flutter de Linux. El Flutter de Windows (`/mnt/c/...`) falla bajo
> bash por los saltos de línea CRLF de sus scripts.

### 2. URL del backend

La URL base se inyecta al compilar (debe terminar en `/`):

```sh
# Emulador Android -> host local (valor por defecto)
flutter run --dart-define=API_URL=http://10.0.2.2:5000/

# Dispositivo físico (misma red): usa la IP de tu PC
flutter run --dart-define=API_URL=http://192.168.1.50:5000/

# Producción
flutter run --dart-define=API_URL=https://api.tudominio.com/
```

### 3. Permitir HTTP en Android (solo si el backend NO usa HTTPS)

Para desarrollo contra `http://`, agrega en
`android/app/src/main/AndroidManifest.xml`, dentro de `<application ...>`:

```xml
android:usesCleartextTraffic="true"
```

En producción usa siempre HTTPS y elimina esa línea.

## Notas de integración con el backend

- Respuestas envueltas en `Response<T>` con **PascalCase** (el backend usa
  `PropertyNamingPolicy = null`). Los modelos Dart respetan ese casing.
- Login: `POST /api/Login` → se guarda `Token` y se envía como `Bearer` en cada request.
- Endpoints usados: `api/Product`, `api/Product/stock`, `api/Sales`,
  `api/CashSession/active|open`, `api/Purchases`, y catálogos
  `api/Category`, `api/Laboratory`, `api/UnitOfMeasurement`, `api/Provider`,
  `api/Customers`, `api/PaymentMethod`, `api/PurchaseStatus`.
- Algunas rutas de catálogos siguen la convención `api/[controller]`; si alguna
  difiere en tu backend, ajústala en `lib/services/catalog_service.dart`.
- MFA/TOTP no está implementado en móvil: una cuenta con TOTP obligatorio será
  rechazada en el login con un mensaje claro.

## CORS

Para que la app móvil consuma la API conviene restringir CORS (hoy `WithOrigins("*")`).
Recuerda: el JWT es la barrera real para apps nativas; CORS solo afecta a navegadores.
