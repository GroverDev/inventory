# Ficha de Google Play

Textos y material para la ficha de **Punto de Venta IdeaNueva**
(`com.ideanueva.puntoventa`). Copiar y pegar en Play Console.

---

## Nombre de la app (máx. 30 caracteres)

```
IdeaNueva Punto de Venta
```
*24 caracteres.*

---

## Descripción corta (máx. 80 caracteres)

```
Punto de venta, inventario y pedidos para tu comercio, desde el celular.
```
*71 caracteres.*

---

## Descripción completa (máx. 4000 caracteres)

```
IdeaNueva Punto de Venta es la aplicación móvil del sistema de gestión comercial
IdeaNueva. Permite a cajeros y encargados vender, consultar el inventario y
administrar pedidos a proveedores desde el celular, con los mismos datos y los
mismos permisos que la versión web.

PUNTO DE VENTA
• Venta con búsqueda de productos por nombre o categoría.
• Control de turno de caja: apertura con fondo inicial, movimientos y arqueo al
  cerrar.
• Cobro con varios métodos de pago en una misma venta y cálculo de vuelto.
• Descuentos por línea y sobre el total, con autorización de supervisor cuando
  superan el límite configurado.
• Acceso protegido por un PIN local, independiente de la contraseña de la cuenta.

INVENTARIO
• Consulta de productos con stock actual y aviso de stock por debajo del mínimo.
• Alta y edición de productos para quien tenga permiso; el resto los ve en modo
  consulta.

VENTAS Y DEVOLUCIONES
• Historial de ventas con su detalle completo.
• Registro de devoluciones totales o parciales.

PEDIDOS A PROVEEDORES
• Creación y seguimiento de órdenes de compra.

SEGURIDAD
• Autenticación en dos pasos (2FA) con aplicaciones como Google Authenticator o
  Microsoft Authenticator.
• Permisos por rol: cada usuario ve y hace únicamente aquello que su rol
  habilita.
• Sesión con renovación automática y cierre remoto.
• Toda la comunicación con el servidor viaja cifrada.

PERSONALIZACIÓN
• Tema claro y oscuro, o el que use el sistema.

REQUISITOS
Esta aplicación requiere una cuenta activa en el sistema IdeaNueva, que entrega
el comercio. No es una aplicación de uso individual: sin credenciales del
servicio no es posible utilizarla.
```

---

## Categoría y clasificación

- **Categoría:** Empresa (Business)
- **Etiquetas sugeridas:** punto de venta, inventario, comercio
- **Contiene anuncios:** No
- **Compras en la app:** No
- **Público objetivo:** mayores de 18 años, uso profesional

---

## Cuestionario de clasificación de contenido

Todas las respuestas son "No" (sin violencia, sexo, lenguaje, drogas, juego ni
contenido generado por usuarios). La clasificación esperada es apta para todo
público.

---

## Formulario de seguridad de los datos

Qué declarar en Play Console → Contenido de la app → Seguridad de los datos:

| Dato | ¿Se recopila? | ¿Se comparte? | Finalidad | Obligatorio |
|---|---|---|---|---|
| Nombre | Sí | No | Funcionalidad de la app | Sí |
| Correo electrónico | Sí | No | Funcionalidad de la app, autenticación | Sí |
| ID de usuario | Sí | No | Funcionalidad de la app, autenticación | Sí |
| Actividad en la app (registro de accesos) | Sí | No | Prevención de fraude y seguridad | Sí |

Declaraciones adicionales:

- Los datos se **cifran en tránsito**: sí (HTTPS).
- El usuario **puede solicitar la eliminación** de sus datos: sí, con el
  procedimiento indicado en la política de privacidad.
- No hay recopilación con fines publicitarios ni de analítica de terceros.
- No hay conexiones a terceros: la app habla solo con su propio backend. La
  tipografía viaja empaquetada (ver abajo), así que no se contacta a los
  servidores de Google.

---

## Política de privacidad

Se publica en **https://ideanueva.com/politica-de-privacidad.html**, y esa URL va
en Play Console → Contenido de la app → Política de privacidad.

Notas que antes vivían como comentario dentro del propio HTML —y por eso
terminaron publicadas, visibles en el código fuente de la página—:

- **Tiene que seguir accesible mientras la app esté publicada.** Play la revisa
  cada tanto, no solo al aprobar: si la URL muere, puede sacar la app. Elegir una
  ruta que sobreviva a un rediseño del sitio.
- **`privacidad@ideanueva.com` tiene que existir y ser leído**, porque es el canal
  por el que llegan las solicitudes de eliminación de datos.
- Los correos van envueltos en `<!--email_off-->`, la directiva de Cloudflare para
  desactivar la ofuscación de direcciones. Sin eso, Cloudflare los reemplaza por
  `[email protected]` y solo se resuelven con JavaScript: el canal de contacto de
  la política quedaba ilegible sin JS.
- El archivo del repo es exactamente lo que se publica. **No volver a poner notas
  internas adentro** — van acá.

---

## ID de publicidad

**Respuesta: No.** La app no usa el ID de publicidad.

Se verifica sobre el propio artefacto, no de memoria: el manifiesto fusionado del
`.aab` declara únicamente `INTERNET` y `DUMP` —este último de
`androidx.profileinstaller`—, sin `com.google.android.gms.permission.AD_ID` ni
rastro de SDKs de publicidad o analítica.

```powershell
# Permisos del manifiesto fusionado, leyendo el .aab directamente
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [System.IO.Compression.ZipFile]::OpenRead("build\app\outputs\bundle\release\app-release.aab")
$e = $z.Entries | Where-Object { $_.FullName -eq 'base/manifest/AndroidManifest.xml' }
$ms = New-Object System.IO.MemoryStream; $e.Open().CopyTo($ms)
$txt = [System.Text.Encoding]::UTF8.GetString($ms.ToArray()); $z.Dispose()
[regex]::Matches($txt, 'permission\.[A-Z_]+') | ForEach-Object { $_.Value } | Sort-Object -Unique
```

La declaración tiene que coincidir con el APK: desde Android 13 usar el ID de
publicidad exige el permiso `AD_ID` en el manifiesto, y Play rechaza la versión
si la respuesta y el artefacto no concuerdan. **Si alguna vez se agrega Firebase
Analytics, Crashlytics o un SDK de anuncios, esta respuesta deja de ser cierta**
—varios lo agregan solos, sin escribirlo— y hay que actualizarla antes de subir
esa versión. Volver a correr el comando de arriba es la forma de saberlo.

---

## Material gráfico

| Recurso | Archivo | Estado |
|---|---|---|
| Ícono 512×512 | `icon-pos-512.png` | Listo |
| Gráfico destacado 1024×500 | `feature-1024x500.png` | Listo |
| Capturas de teléfono (mín. 2) | `screenshots/` | Listo — 10 disponibles, ver su README |

Todo el material gráfico se regenera desde los fuentes de esta carpeta con
Chrome headless; los comandos están en `screenshots/README.md` y en los
comentarios de cada archivo HTML.

El ícono de la app es `icon-pos.svg`, una caja registradora dibujada desde cero.
En la carpeta quedan también `icon.svg` e `icon-1024.png`, la variante anterior
con el triángulo de la marca IdeaNueva, por si se quiere volver a ella: para
cambiar, apuntar `flutter_launcher_icons` en `pubspec.yaml` a esos archivos y
correr `dart run flutter_launcher_icons`.

---

## Notas de la versión 1.0.0

```
Primera versión.

• Punto de venta con control de caja, múltiples métodos de pago y descuentos.
• Consulta de inventario con alerta de stock mínimo.
• Ventas, devoluciones y pedidos a proveedores.
• Autenticación en dos pasos y permisos por rol.
• Tema claro y oscuro.
```

---

## Tipografía: resuelto

`google_fonts` bajaba Poppins de los servidores de Google en el primer arranque.
Eran una conexión a un tercero que declarar en el formulario de datos, y un
primer arranque con la fuente por defecto donde la conexión es mala.

Ahora **la fuente viaja dentro del `.aab`**: `google_fonts/Poppins-Regular.ttf`
declarado como asset en `pubspec.yaml`, y `GoogleFonts.config.allowRuntimeFetching
= false` en `main()` para que no exista un camino de vuelta a la red.

Se empaqueta **solo la variante regular** porque es la única que el tema pide: el
`TextTheme` se arma con peso 400 y las negritas (`FontWeight.bold`, `w600`) se
dibujan engrosando esa misma cara. Agregar `Poppins-Medium/SemiBold/Bold.ttf`
sumaría medio megabyte que nunca se usa, y cambiaría cómo se ven los títulos
respecto de las capturas ya tomadas. Si alguna vez se quieren pesos reales, es un
cambio de diseño aparte: hay que declarar la familia entera y revisar las
pantallas.

`test/bundled_font_test.dart` cubre las dos cosas que romperían esto en silencio
—renombrar el archivo y que el tema empiece a pedir otra variante—, porque el
síntoma sería la app dibujada con la fuente del sistema, sin ningún error.
