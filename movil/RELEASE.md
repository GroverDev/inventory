# Publicación en Google Play

App: **Inventario Móvil** · `com.ideanueva.puntoventa`

> El `applicationId` es definitivo. Una vez subida la primera versión no se
> puede cambiar: cambiarlo crea una app distinta, sin usuarios ni reseñas.

---

## Una sola vez

### 1. Generar la clave de subida

```bash
keytool -genkey -v -keystore %USERPROFILE%\upload-puntodeventa.p12 ^
  -storetype PKCS12 -keyalg RSA -keysize 2048 -validity 10000 -alias upload
```

Guardalo **fuera del repositorio** y respaldalo (gestor de contraseñas o disco
cifrado). Anotá la contraseña.

> **PKCS12, no JKS.** Con `-storetype JKS`, `keytool` avisa que es un formato
> propietario y recomienda migrar. Las dos formas funcionan igual —Gradle y Play
> aceptan ambas, el formato es solo cómo se guarda el archivo en disco—, pero
> PKCS12 es el estándar y evita el aviso.
>
> Si ya tenés un `.jks`, se convierte sin perder la clave; el certificado y su
> fingerprint no cambian:
>
> ```bash
> keytool -importkeystore -srckeystore upload-puntodeventa.jks ^
>   -destkeystore upload-puntodeventa.p12 -deststoretype pkcs12
> ```
>
> Dos cosas después de migrar: en PKCS12 **la contraseña de la clave es la misma
> que la del keystore** (si eran distintas, queda la del keystore, y en
> `key.properties` van iguales), y el `.jks` viejo sigue conteniendo la clave
> privada, así que borralo o guardalo con el mismo cuidado que el nuevo.

### 2. Crear `android/key.properties`

No se versiona (está en `.gitignore`). Cada máquina que publique necesita el
suyo:

```properties
storePassword=<contraseña del keystore>
keyPassword=<la misma, si el keystore es PKCS12>
keyAlias=upload
storeFile=C:/Users/<usuario>/upload-puntodeventa.p12
```

Usá barras normales (`/`) en `storeFile`, incluso en Windows.

Sin este archivo el proyecto compila igual, pero firma con la clave de debug y
**Play rechaza el artefacto**. Es a propósito: permite `flutter run --release`
en cualquier máquina de desarrollo.

### 3. Activar Play App Signing

Al crear la app en Play Console, dejá activada la firma gestionada por Google
(viene por defecto). Google custodia la clave real de firma y vos solo manejás
la de subida.

Importa porque **la clave de subida sí se puede resetear** si la perdés,
abriendo un caso con soporte. Sin Play App Signing, perder el `.jks` significa
no poder volver a actualizar la app nunca.

---

## Compilar una versión

1. Subir la versión en `pubspec.yaml`. El formato es `versionName+versionCode`:

   ```yaml
   version: 1.0.1+2
   ```

   El número después del `+` (`versionCode`) **debe crecer en cada subida**;
   Play rechaza uno repetido o menor. El de antes del `+` es el que ve el
   usuario.

2. Compilar el App Bundle:

   ```bash
   flutter build appbundle --release
   ```

   El archivo queda en `build/app/outputs/bundle/release/app-release.aab`.

   No hace falta pasar `--dart-define=API_URL`: `AppConfig.apiBaseUrl` ya apunta
   a `https://api.ideanueva.com/` por defecto. Solo se usa para apuntar a otro
   backend:

   ```bash
   flutter build appbundle --release --dart-define=API_URL=https://otro-api.com/
   ```

3. Subir el `.aab` en Play Console → Producción → Crear nueva versión.

### Sobre el tamaño

El `.aab` pesa unos 51 MB, pero **eso no es lo que descarga el usuario**:

- ~70 MB son símbolos de depuración nativos, metadatos que Play usa para
  descifrar los reportes de fallos. No se entregan al dispositivo.
- Las librerías nativas están las tres arquitecturas (arm64, armeabi-v7a,
  x86_64); Play envía solo la que corresponde al teléfono.

La descarga real ronda los 10 MB. El valor exacto lo muestra Play Console en el
detalle de la versión.

---

## Requisitos de la ficha de Play

Lo que hay que tener preparado para una publicación pública:

- **Política de privacidad en una URL pública.** Obligatoria: la app maneja
  credenciales y datos personales.
- **Formulario de seguridad de los datos.** Se declara que se recolectan correo
  y nombre, que viajan cifrados (HTTPS) y cómo se solicita su eliminación.
- **Ícono** de 512×512 px.
- **Gráfico destacado** de 1024×500 px.
- **Capturas**: mínimo 2 de teléfono.
- **Textos**: nombre (30 caracteres), descripción corta (80) y completa (4000).
- **Clasificación de contenido**: cuestionario en el panel.
- **Público objetivo** y declaración de anuncios (no tiene).
- **Acceso a la app**: credenciales de demostración para el revisor. Es
  obligatorio acá porque sin una cuenta del sistema la app no pasa del login, y
  Google la prueba a mano: sin esto la rechaza. La cuenta tiene que funcionar
  contra la API de producción y **no tener 2FA activado** —el revisor no puede
  resolver un segundo factor—, o hay que explicarle el procedimiento en las
  instrucciones de esa misma sección.

---

## Checklist de cada versión

- [ ] `version` incrementada en `pubspec.yaml` (el `+N` siempre mayor).
- [ ] `flutter analyze` sin errores.
- [ ] `flutter test` en verde.
- [ ] Probado contra la API de producción.
- [ ] `flutter build appbundle --release`.
- [ ] Notas de la versión escritas en Play Console.

---

## Permisos declarados

La app pide **solo `INTERNET`**. No declara `CAMERA`: se sacó porque todavía no
hay lectura de códigos de barras y un permiso sin uso hay que justificarlo ante
Play. Cuando se agregue el escáner, hay que volver a declararlo.

El tráfico HTTP en claro (`usesCleartextTraffic`) quedó **solo en la variante de
debug**, para poder apuntar al backend local desde el emulador. La build de
producción admite únicamente HTTPS.

> El permiso `android.permission.DUMP` que aparece en el manifiesto fusionado no
> lo pide la app: es la librería `androidx.profileinstaller` protegiendo su
> propio receptor. Es estándar en cualquier app Flutter moderna.
