---
name: dotnet-crud
description: >
  Genera un CRUD completo para una entidad .NET 10 con Clean Architecture: entidad, request+validator,
  response, IRepository, Repository (Dapper), IApplication, Application, Controller con endpoints REST.
  ÚSALO SIEMPRE que el usuario pida "crear crud", "scaffold", "generar entidad completa",
  "crear [NombreEntidad]", "implementar [NombreEntidad]" o cualquier combinación de
  "crear/generar/implementar" + nombre de entidad en un proyecto .NET / ASP.NET Core.
  También triggerea si el usuario menciona "todas las capas", "repositorio + application + controller",
  o "dapper" junto a una entidad. El argumento es el nombre de la entidad en PascalCase.
---

# Skill: dotnet-crud

Genera un CRUD completo para **$ARGUMENTS** siguiendo Clean Architecture con Dapper.

---

## Paso 1 — Inferir contexto del proyecto

Antes de crear cualquier archivo, analiza el proyecto abierto para determinar:

- **Namespace base**: busca en archivos `.cs` existentes el patrón `namespace X.Domain` o `namespace X.Application`. Usa ese `X` como namespace base. Si no encuentras nada, pregunta al usuario.
- **Ruta raíz**: detecta dónde están las carpetas `4-Domain`, `3-Infrastructure`, `2-Application`, `1-Services`. Si no existen, créalas desde la raíz del proyecto.
- **GroupName**: busca `ApiExplorerSettings(GroupName = "...")` en controllers existentes y reutiliza ese valor. Si no hay, usa `"v1"`.
- **Nombre de tabla**: convierte el nombre de la entidad a `snake_case` (ej: `ProductCategory` → `product_category`).

> Si no puedes inferir el namespace, pregunta: *"¿Cuál es el namespace base del proyecto?"* antes de continuar.

---

## Paso 2 — Crear los 8 archivos

Reemplaza en todas las plantillas:
- `$ENTITY` → nombre de la entidad en PascalCase (ej: `Product`)
- `$NS` → namespace base inferido (ej: `POS`)
- `$TABLE` → nombre en snake_case (ej: `product`)
- `$GROUP` → GroupName inferido (ej: `POS`)

---

### Archivo 1 — Entidad
**Ruta:** `4-Domain/$NS.Domain/Entities/$ENTITY.cs`

```csharp
using Common.Utilities;

namespace $NS.Domain;

public class $ENTITY : Audit
{
    public Guid Id { get; set; }

    // TODO: agregar propiedades de negocio
    // public string Name { get; set; } = string.Empty;
}
```

---

### Archivo 2 — Request + Validator
**Ruta:** `4-Domain/$NS.Domain/Entities/Requests/$ENTITYRequest.cs`

```csharp
using FluentValidation;

namespace $NS.Domain;

public class $ENTITYRequest
{
    public string Id { get; set; } = string.Empty;

    // TODO: agregar propiedades del request
    // public string Name { get; set; } = string.Empty;
}

public class $ENTITYRequestValidator : AbstractValidator<$ENTITYRequest>
{
    public $ENTITYRequestValidator()
    {
        // TODO: agregar reglas de validación. Ejemplos:

        // Texto obligatorio:
        // RuleFor(p => p.Name)
        //     .NotEmpty().WithMessage("El nombre es requerido.")
        //     .MinimumLength(3).WithMessage("El nombre no puede ser menor a {MinLength} caracteres.")
        //     .MaximumLength(100).WithMessage("El nombre no puede ser mayor a {MaxLength} caracteres.");

        // ID foráneo (string GUID):
        // RuleFor(p => p.RelatedId)
        //     .NotEmpty().WithMessage("El ID relacionado es requerido.")
        //     .Must(id => Guid.TryParse(id, out _)).WithMessage("El ID relacionado no tiene un formato válido.");

        // Número positivo:
        // RuleFor(p => p.Price)
        //     .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");
    }
}
```

---

### Archivo 3 — Response
**Ruta:** `4-Domain/$NS.Domain/Entities/Responses/$ENTITYResponse.cs`

```csharp
namespace $NS.Domain.Entities.Responses;

public class $ENTITYResponse
{
    public Guid Id { get; set; } = Guid.Empty;

    // TODO: agregar propiedades que el cliente necesita ver
    // Incluir nombres de entidades relacionadas, no solo sus IDs.
    // public string Name { get; set; } = string.Empty;
}
```

---

### Archivo 4 — Interface del repositorio
**Ruta:** `3-Infrastructure/$NS.Infrastructure/Persistences/Interfaces/I$ENTITYRepository.cs`

```csharp
using $NS.Domain;
using $NS.Domain.Entities.Responses;

namespace $NS.Infrastructure;

public interface I$ENTITYRepository
{
    Task<string> Create$ENTITY($ENTITY entity);
    Task<int>    Update$ENTITY($ENTITY entity);
    Task<int>    Delete$ENTITY(Guid id, int idUserModified);
    Task<List<$ENTITYResponse>> Get$ENTITYList(string filter);
    Task<$ENTITYResponse>       Get$ENTITY(Guid id);
}
```

---

### Archivo 5 — Repositorio (Dapper)
**Ruta:** `3-Infrastructure/$NS.Infrastructure/Persistences/Repositories/$ENTITYRepository.cs`

```csharp
using Common.Utilities.Exceptions;
using Dapper;
using $NS.Domain;
using $NS.Domain.Entities.Responses;

namespace $NS.Infrastructure;

public class $ENTITYRepository(IDbContext _DbContext) : I$ENTITYRepository
{
    public async Task<string> Create$ENTITY($ENTITY entity)
    {
        using var db = _DbContext.CreateConnection;
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            entity.Id = Guid.NewGuid();

            // TODO: verificar duplicado si aplica
            // bool exists = await db.QuerySingleAsync<bool>(@"
            //     SELECT CASE WHEN EXISTS(SELECT 1 FROM $TABLE WHERE nombre = @Nombre AND state = true)
            //                 THEN CAST(1 as BIT) ELSE CAST(0 as BIT) END",
            //     new { entity.Nombre }, transaction);
            // if (exists) throw new ConflictException("El nombre ya existe.");

            await db.ExecuteAsync(@"
                INSERT INTO $TABLE (id, /* TODO: columnas */, state, created_by, created, modified_by, modified)
                VALUES (@Id, /* TODO: @Params */, @State, @CreatedBy, @Created, @ModifiedBy, @Modified)",
                entity, transaction);

            transaction.Commit();
            return entity.Id.ToString();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally { db.Close(); }
    }

    public async Task<int> Update$ENTITY($ENTITY entity)
    {
        using var db = _DbContext.CreateConnection;
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            int rows = await db.ExecuteAsync(@"
                UPDATE $TABLE SET
                    /* TODO: columna = @Propiedad, */
                    modified_by = @ModifiedBy,
                    modified    = @Modified
                WHERE id = @Id",
                entity, transaction);

            transaction.Commit();
            return rows;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally { db.Close(); }
    }

    public async Task<int> Delete$ENTITY(Guid id, int idUserModified)
    {
        using var db = _DbContext.CreateConnection;
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            int rows = await db.ExecuteAsync(@"
                UPDATE $TABLE
                SET state       = false,
                    modified_by = @ModifiedBy,
                    modified    = @Modified
                WHERE id = @Id",
                new { Id = id, ModifiedBy = idUserModified, Modified = DateTime.Now }, transaction);

            transaction.Commit();
            return rows;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally { db.Close(); }
    }

    public async Task<List<$ENTITYResponse>> Get$ENTITYList(string filter)
    {
        using var db = _DbContext.CreateConnection;
        db.Open();
        try
        {
            var result = await db.QueryAsync<$ENTITYResponse>(@"
                SELECT /* TODO: columnas */
                FROM   $TABLE
                WHERE  state = true
                -- AND nombre ILIKE @Filter
                ",
                new { Filter = $"%{filter}%" });

            return result.ToList();
        }
        finally { db.Close(); }
    }

    public async Task<$ENTITYResponse> Get$ENTITY(Guid id)
    {
        using var db = _DbContext.CreateConnection;
        db.Open();
        try
        {
            var result = await db.QueryFirstOrDefaultAsync<$ENTITYResponse>(@"
                SELECT /* TODO: columnas */
                FROM   $TABLE
                WHERE  state = true
                AND    id    = @Id",
                new { Id = id });

            return result ?? throw new NotFoundException("No existe el registro con el Id ingresado.");
        }
        finally { db.Close(); }
    }
}
```

---

### Archivo 6 — Interface de Application
**Ruta:** `2-Application/$NS.Application/Interfaces/I$ENTITYApplication.cs`

```csharp
using $NS.Domain;
using $NS.Domain.Entities.Responses;

namespace $NS.Application;

public interface I$ENTITYApplication
{
    Task<string>              Create$ENTITY($ENTITYRequest request, int createdBy);
    Task                      Update$ENTITY($ENTITYRequest request, int modifiedBy);
    Task                      Delete$ENTITY(string id, int modifiedBy);
    Task<List<$ENTITYResponse>> Get$ENTITYList(string filter);
    Task<$ENTITYResponse>       Get$ENTITY(string id);
}
```

---

### Archivo 7 — Application Service
**Ruta:** `2-Application/$NS.Application/Services/$ENTITYApplication.cs`

```csharp
using Common.Utilities;
using Common.Utilities.Exceptions;
using Mapster;
using $NS.Domain;
using $NS.Domain.Entities.Responses;
using $NS.Infrastructure;

namespace $NS.Application;

public class $ENTITYApplication(I$ENTITYRepository _repository) : I$ENTITYApplication
{
    public async Task<string> Create$ENTITY($ENTITYRequest request, int createdBy)
    {
        // TODO: validar FKs si las hay
        // await _otherRepository.GetOther(Guid.Parse(request.OtherId));

        // TODO: normalizar campos de texto
        // request.Name = request.Name.Trim().ToUpper();

        var entity = request.Adapt<$ENTITY>();
        entity.State = true;
        AuditHelper.SetCreated(entity, createdBy);

        return await _repository.Create$ENTITY(entity);
    }

    public async Task Update$ENTITY($ENTITYRequest request, int modifiedBy)
    {
        // TODO: normalizar campos de texto
        // request.Name = request.Name.Trim().ToUpper();

        var entity = request.Adapt<$ENTITY>();
        AuditHelper.SetModified(entity, modifiedBy);

        int rows = await _repository.Update$ENTITY(entity);
        if (rows <= 0) throw new NotFoundException("No existe el registro para actualizar.");
    }

    public async Task Delete$ENTITY(string id, int modifiedBy)
    {
        int rows = await _repository.Delete$ENTITY(Guid.Parse(id), modifiedBy);
        if (rows <= 0) throw new NotFoundException("No existe el registro para eliminar.");
    }

    public async Task<List<$ENTITYResponse>> Get$ENTITYList(string filter)
        => await _repository.Get$ENTITYList(filter);

    public async Task<$ENTITYResponse> Get$ENTITY(string id)
        => await _repository.Get$ENTITY(Guid.Parse(id));
}
```

---

### Archivo 8 — Controller
**Ruta:** `1-Services/Services.Api/Controllers/$ENTITYController.cs`

```csharp
using Common.Utilities;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;
using $NS.Application;
using $NS.Domain;
using $NS.Domain.Entities.Responses;

namespace Services.Api.Controllers;

[ApiExplorerSettings(GroupName = "$GROUP")]
[Route("api/[controller]")]
[ApiController]
public class $ENTITYController(I$ENTITYApplication _application) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Response<string>>> Create([FromBody] $ENTITYRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok)
            return Unauthorized(Response<string>.Fail(MessageTypes.Warning, "Acceso no Autorizado."));

        var validation = new $ENTITYRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            var errors = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage));
            return BadRequest(Response<string>.Fail(MessageTypes.Warning, errors));
        }

        var id = await _application.Create$ENTITY(request, datos.UserId);
        return StatusCode(StatusCodes.Status201Created, Response<string>.Ok(id));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> Update(string id, [FromBody] $ENTITYRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok)
            return Unauthorized(Response<bool>.Fail(MessageTypes.Warning, "Acceso no Autorizado."));

        if (!Guid.TryParse(id, out _))
            return BadRequest(Response<bool>.Fail(MessageTypes.Warning, "El Id no tiene un formato válido."));

        var validation = new $ENTITYRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            var errors = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage));
            return BadRequest(Response<bool>.Fail(MessageTypes.Warning, errors));
        }

        request.Id = id;
        await _application.Update$ENTITY(request, datos.UserId);
        return Ok(Response<bool>.Ok(true));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> Delete(string id)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok)
            return Unauthorized(Response<bool>.Fail(MessageTypes.Warning, "Acceso no Autorizado."));

        if (!Guid.TryParse(id, out _))
            return BadRequest(Response<bool>.Fail(MessageTypes.Warning, "El Id no tiene un formato válido."));

        await _application.Delete$ENTITY(id, datos.UserId);
        return Ok(Response<bool>.Ok(true));
    }

    [HttpGet]
    public async Task<ActionResult<Response<List<$ENTITYResponse>>>> GetList([FromQuery] string filter = "")
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok)
            return Unauthorized(Response<List<$ENTITYResponse>>.Fail(MessageTypes.Warning, "Acceso no Autorizado."));

        var list = await _application.Get$ENTITYList(filter);
        return Ok(Response<List<$ENTITYResponse>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Response<$ENTITYResponse>>> GetById(string id)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok)
            return Unauthorized(Response<$ENTITYResponse>.Fail(MessageTypes.Warning, "Acceso no Autorizado."));

        if (!Guid.TryParse(id, out _))
            return BadRequest(Response<$ENTITYResponse>.Fail(MessageTypes.Warning, "El Id no tiene un formato válido."));

        var item = await _application.Get$ENTITY(id);
        return Ok(Response<$ENTITYResponse>.Ok(item));
    }
}
```

---

## Paso 3 — Confirmar al usuario

Después de crear los archivos muestra este resumen:

```
✅ CRUD generado para $ENTITY

📁 Archivos creados:
  4-Domain/$NS.Domain/Entities/$ENTITY.cs
  4-Domain/$NS.Domain/Entities/Requests/$ENTITYRequest.cs
  4-Domain/$NS.Domain/Entities/Responses/$ENTITYResponse.cs
  3-Infrastructure/$NS.Infrastructure/Persistences/Interfaces/I$ENTITYRepository.cs
  3-Infrastructure/$NS.Infrastructure/Persistences/Repositories/$ENTITYRepository.cs
  2-Application/$NS.Application/Interfaces/I$ENTITYApplication.cs
  2-Application/$NS.Application/Services/$ENTITYApplication.cs
  1-Services/Services.Api/Controllers/$ENTITYController.cs

⚠️  Pasos manuales:
  1. Completa los TODO en los archivos (columnas SQL y propiedades).
  2. Registra en DI:
       services.AddScoped<I$ENTITYApplication, $ENTITYApplication>();
       services.AddScoped<I$ENTITYRepository, $ENTITYRepository>();
  3. Agrega el mapping en Mapster:
       config.NewConfig<$ENTITYRequest, $ENTITY>()
           .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id));
```

---

## Reglas de arquitectura que Claude debe respetar

### Repository
- Un solo `catch` con `throw`. Sin try/catch anidados.
- `QueryFirstOrDefaultAsync` + `?? throw new NotFoundException(...)` para lecturas por ID.
- `ConflictException` para duplicados. `NotFoundException` para no encontrados.
- Nunca DELETE físico — siempre soft delete (`state = false`).
- Queries de lista siempre con `WHERE state = true`.
- Pasar `transaction` a todos los métodos de escritura.
- `snake_case` → `PascalCase` automático via `DefaultTypeMap.MatchNamesWithUnderscores = true`.

### Application
- Sin try/catch. Sin `Response<T>`. Solo lógica de negocio.
- `Trim().ToUpper()` en campos de texto en Create y Update.
- `AuditHelper.SetCreated` al crear, `AuditHelper.SetModified` al modificar.
- Si `rowsAffected <= 0` → `throw new NotFoundException(...)`.

### Controller
- `TokenData.GetData(HttpContext)` una sola vez por acción.
- `Response<T>` solo en el Controller, nunca en capas inferiores.
- Sin try/catch. El `GlobalExceptionHandler` maneja todo.
- POST → `StatusCode(201, ...)`. PUT / DELETE / GET → `Ok(...)`.

### Excepciones

| Excepción | Cuándo | HTTP |
|---|---|---|
| `NotFoundException` | Registro no encontrado | 404 |
| `ConflictException` | Duplicado | 409 |
| `CustomException` | Regla de negocio | 400 |
| `Exception` inesperada | BD caída, null ref… | 500 + log |
