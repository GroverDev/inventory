# Capturas para Google Play

Tomadas del emulador `Pixel_10` a **1080×1920** (9:16). La resolución física del
emulador es 1080×2424, que da una relación de 2,24:1 y Play rechaza por encima
de 2:1; por eso se fuerza el tamaño antes de capturar.

## Archivos

| Archivo | Pantalla | Tema |
|---|---|---|
| `00-login.png` | Inicio de sesión | claro |
| `01-inicio.png` | Módulos | claro |
| `02-productos.png` | Inventario con stock y alerta en rojo | claro |
| `03-punto-de-venta.png` | POS, grilla de productos | claro |
| `03b-pos-carrito.png` | POS con carrito cargado | claro |
| `04-cobrar.png` | Cobro con cliente, líneas y total | claro |
| `05-ventas.png` | Historial de ventas del mes | claro |
| `06-ajustes.png` | Ajustes: tema, PIN, sesión | claro |
| `07-ajustes-oscuro.png` | Ajustes | oscuro |
| `08-pos-oscuro.png` | POS con carrito | oscuro |

Play admite hasta 8. Selección sugerida: `03-punto-de-venta`, `04-cobrar`,
`02-productos`, `05-ventas`, `08-pos-oscuro`, `06-ajustes`.

## De dónde salen los datos

**No son datos de producción.** Se restauró `db/pos_backup_20260708.sql` en una
base local aparte llamada `punto_venta_demo` y se la preparó para demostración:

- El único cliente real se reemplazó por *Cliente Mostrador*, y se agregaron
  *María Fernández* y *Carlos Rojas*, ficticios.
- Se crearon categorías (Analgésicos, Antibióticos, Dermatología, Vitaminas,
  Respiratorio, Gastrointestinal, Medicamentos) y se asignaron a los 1180
  productos por patrón de nombre, porque el backup los tenía sin categoría.
- Las 48 ventas se repartieron en los últimos 12 días para que los filtros
  Hoy / Semana / Mes muestren datos; las que tenían total nulo se recalcularon
  desde su detalle y las que quedaron sin detalle se corrieron fuera del mes.
- La contraseña del usuario `grover@ideanueva.com` se igualó a la de la base de
  desarrollo (se copió el hash) para poder entrar.

Los **nombres y precios de los productos sí provienen del catálogo real**. Para
una farmacia son datos que cualquiera ve en la góndola, pero conviene revisarlo
antes de publicar: las capturas de la ficha son públicas e indexables.

## Cómo repetirlas

```bash
# 1. API contra la base de demostración
cd backend/1-Services/Services.Api
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Database=punto_venta_demo;Username=postgres;Password=***;Port=5432;" \
dotnet run --no-launch-profile

# 2. App en debug apuntando al host desde el emulador
cd movil
flutter build apk --debug --dart-define=API_URL=http://10.0.2.2:6001/
adb install -r build/app/outputs/flutter-apk/app-debug.apk

# 3. Resolución de captura
adb shell wm size 1080x1920      # al terminar:  adb shell wm size reset

# 4. Capturar
adb exec-out screencap -p > store/screenshots/NN-pantalla.png
```

Tiene que ser build **debug**: la de release ya no admite tráfico HTTP en claro,
así que no puede hablar con la API local por `10.0.2.2`.
