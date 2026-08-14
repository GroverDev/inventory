# Pruebas de aislamiento multi-tenant

Verifican que una farmacia no pueda ver ni tocar los datos de otra. Es lo único
del proyecto cuyo modo de falla es **silencioso**: si el aislamiento se rompe, la
aplicación sigue funcionando con normalidad y nadie se entera hasta que un cliente
ve datos ajenos.

## Cómo correrlas

Necesitan un PostgreSQL con la base de desarrollo migrada.

```sh
TEST_PG_ADMIN="Host=localhost;Port=5432;Username=postgres;Password=TU_PASSWORD;Database=postgres" \
  dotnet test backend/5-Tests/MultiTenancy.Tests
```

| Variable | Por defecto | Para qué |
|---|---|---|
| `TEST_PG_ADMIN` | `Host=localhost;Port=5432;Username=postgres;Database=postgres` | Conexión de superusuario. Crea y borra la base desechable. |
| `TEST_PG_TEMPLATE` | `punto_venta` | Base de la que se copia el esquema. |

Los tests de `RolePolicyTests` no necesitan base y corren siempre.

## Cómo funcionan

Cada corrida copia la base de desarrollo a una desechable
(`punto_venta_test_<aleatorio>`), crea allí la segunda farmacia con la provisión
real, ejecuta las pruebas y borra todo. Copia del esquema **actual** a propósito:
lo que se quiere detectar es que alguien agregue una tabla sin política.

Usa su propio rol `app_pos_test` en vez de `app_pos`, así no necesita conocer la
contraseña de desarrollo ni puede romperla. Las políticas no nombran ningún rol,
de modo que aplican a cualquiera que no sea superusuario ni dueño de las tablas.

Si la base de desarrollo tiene clientes conectados —un DBeaver abierto es lo
habitual— el fixture cierra las conexiones **ociosas** y reintenta. No toca las
que están ejecutando algo o con una transacción a medias: ahí avisa y se detiene.

## Qué cubren

**Comportamiento** (`AislamientoTests`) — ninguna de estas consultas lleva
`WHERE tenant_id`; el filtrado lo pone PostgreSQL:

- Una farmacia no ve los productos de otra, y sí los propios
- Sin tenant fijado no se ve nada (falla cerrado)
- No se puede insertar marcando otra farmacia (`WITH CHECK`)
- El `INSERT` toma el tenant de la sesión, que es lo que permite que las 219
  consultas del backend sigan sin tocar
- `UPDATE` y `DELETE` sobre filas ajenas afectan cero filas
- Una clave foránea no puede cruzar farmacias

**Configuración** (`PoliticasTests`) — guardarraíles contra regresiones futuras:

- Toda tabla con `tenant_id` tiene RLS, `FORCE` y política, o figura como
  excepción documentada en `SinRlsAdrede`
- Toda clave foránea entre tablas por tenant incluye `tenant_id`, o figura en
  `FkSimplesAdrede`
- El rol de la aplicación no es superusuario ni tiene `BYPASSRLS` — los dos
  atributos que dejarían las políticas de adorno
- El rol de la aplicación no puede alterar el esquema
- La búsqueda de autenticación funciona sin tenant y lo deja fijado
- Una farmacia desactivada no puede entrar

Los dos primeros son los que más valen: se ejecutan sobre `pg_class` y
`pg_constraint`, así que **detectan tablas y claves nuevas automáticamente**, sin
que nadie tenga que acordarse de agregar un caso. La migración 07 existe porque
esa prueba encontró ocho claves foráneas del schema `sec` que la 06 había pasado
por alto.

**Regla de roles** (`RolePolicyTests`) — sin base de datos. Cubre «gana el más
privilegiado»: un usuario queda restringido solo si `Cajero` es su único rol.
