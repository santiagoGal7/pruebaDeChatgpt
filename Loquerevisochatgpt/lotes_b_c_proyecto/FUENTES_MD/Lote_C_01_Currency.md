# Módulo: Currency
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency`  
**Raíz de archivos:** `src/Modules/Currency/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `currency_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `iso_code` | `CHAR(3) NOT NULL UNIQUE` | `string` | ISO 4217: COP, USD, EUR… Normalizado a MAYÚSCULAS |
| `name` | `VARCHAR(80) NOT NULL UNIQUE` | `string` | Nombre completo: "Colombian Peso", "US Dollar" |
| `symbol` | `VARCHAR(5) NOT NULL` | `string` | Símbolo: $, €, £ |

Tabla [TN-1] — crítica para pagos multi-moneda.  
Sin `created_at`, `updated_at` en el DDL.  
`iso_code` se normaliza a mayúsculas (`ToUpperInvariant`) para garantizar consistencia con el estándar ISO 4217.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/Currency/Domain/valueObject/CurrencyId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;

public sealed class CurrencyId
{
    public int Value { get; }

    public CurrencyId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CurrencyId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CurrencyId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/Currency/Domain/aggregate/CurrencyAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;

/// <summary>
/// Moneda ISO 4217 para pagos multi-moneda. [TN-1]
/// SQL: currency.
///
/// Invariantes:
///   - iso_code: exactamente 3 caracteres, normalizado a MAYÚSCULAS (ISO 4217).
///   - name: máximo 80 caracteres, único.
///   - symbol: máximo 5 caracteres.
/// </summary>
public sealed class CurrencyAggregate
{
    public CurrencyId Id      { get; private set; }
    public string     IsoCode { get; private set; }
    public string     Name    { get; private set; }
    public string     Symbol  { get; private set; }

    private CurrencyAggregate()
    {
        Id      = null!;
        IsoCode = null!;
        Name    = null!;
        Symbol  = null!;
    }

    public CurrencyAggregate(CurrencyId id, string isoCode, string name, string symbol)
    {
        ValidateIsoCode(isoCode);
        ValidateName(name);
        ValidateSymbol(symbol);

        Id      = id;
        IsoCode = isoCode.Trim().ToUpperInvariant();
        Name    = name.Trim();
        Symbol  = symbol.Trim();
    }

    public void Update(string isoCode, string name, string symbol)
    {
        ValidateIsoCode(isoCode);
        ValidateName(name);
        ValidateSymbol(symbol);

        IsoCode = isoCode.Trim().ToUpperInvariant();
        Name    = name.Trim();
        Symbol  = symbol.Trim();
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateIsoCode(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
            throw new ArgumentException("IsoCode cannot be empty.", nameof(isoCode));

        if (isoCode.Trim().Length != 3)
            throw new ArgumentException(
                "IsoCode must be exactly 3 characters (ISO 4217).", nameof(isoCode));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Currency name cannot be empty.", nameof(name));

        if (name.Trim().Length > 80)
            throw new ArgumentException(
                "Currency name cannot exceed 80 characters.", nameof(name));
    }

    private static void ValidateSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Currency symbol cannot be empty.", nameof(symbol));

        if (symbol.Trim().Length > 5)
            throw new ArgumentException(
                "Currency symbol cannot exceed 5 characters.", nameof(symbol));
    }
}
```

---

### RUTA: `src/Modules/Currency/Domain/Repositories/ICurrencyRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;

public interface ICurrencyRepository
{
    Task<CurrencyAggregate?>             GetByIdAsync(CurrencyId id,              CancellationToken cancellationToken = default);
    Task<IEnumerable<CurrencyAggregate>> GetAllAsync(                             CancellationToken cancellationToken = default);
    Task                                 AddAsync(CurrencyAggregate currency,     CancellationToken cancellationToken = default);
    Task                                 UpdateAsync(CurrencyAggregate currency,  CancellationToken cancellationToken = default);
    Task                                 DeleteAsync(CurrencyId id,               CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/Currency/Application/Interfaces/ICurrencyService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.Interfaces;

public interface ICurrencyService
{
    Task<CurrencyDto?>             GetByIdAsync(int id,                                         CancellationToken cancellationToken = default);
    Task<IEnumerable<CurrencyDto>> GetAllAsync(                                                  CancellationToken cancellationToken = default);
    Task<CurrencyDto>              CreateAsync(string isoCode, string name, string symbol,       CancellationToken cancellationToken = default);
    Task                           UpdateAsync(int id, string isoCode, string name, string symbol, CancellationToken cancellationToken = default);
    Task                           DeleteAsync(int id,                                           CancellationToken cancellationToken = default);
}

public sealed record CurrencyDto(int Id, string IsoCode, string Name, string Symbol);
```

---

### RUTA: `src/Modules/Currency/Application/UseCases/CreateCurrencyUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCurrencyUseCase
{
    private readonly ICurrencyRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public CreateCurrencyUseCase(ICurrencyRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CurrencyAggregate> ExecuteAsync(
        string            isoCode,
        string            name,
        string            symbol,
        CancellationToken cancellationToken = default)
    {
        // CurrencyId(1) es placeholder; EF Core asigna el Id real al insertar.
        var currency = new CurrencyAggregate(new CurrencyId(1), isoCode, name, symbol);

        await _repository.AddAsync(currency, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return currency;
    }
}
```

---

### RUTA: `src/Modules/Currency/Application/UseCases/DeleteCurrencyUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCurrencyUseCase
{
    private readonly ICurrencyRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public DeleteCurrencyUseCase(ICurrencyRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CurrencyId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Currency/Application/UseCases/GetAllCurrenciesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;

public sealed class GetAllCurrenciesUseCase
{
    private readonly ICurrencyRepository _repository;

    public GetAllCurrenciesUseCase(ICurrencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CurrencyAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/Currency/Application/UseCases/GetCurrencyByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;

public sealed class GetCurrencyByIdUseCase
{
    private readonly ICurrencyRepository _repository;

    public GetCurrencyByIdUseCase(ICurrencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<CurrencyAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CurrencyId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/Currency/Application/UseCases/UpdateCurrencyUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateCurrencyUseCase
{
    private readonly ICurrencyRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public UpdateCurrencyUseCase(ICurrencyRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            isoCode,
        string            name,
        string            symbol,
        CancellationToken cancellationToken = default)
    {
        var currency = await _repository.GetByIdAsync(new CurrencyId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Currency with id {id} was not found.");

        currency.Update(isoCode, name, symbol);
        await _repository.UpdateAsync(currency, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Currency/Application/Services/CurrencyService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

public sealed class CurrencyService : ICurrencyService
{
    private readonly CreateCurrencyUseCase   _create;
    private readonly DeleteCurrencyUseCase   _delete;
    private readonly GetAllCurrenciesUseCase _getAll;
    private readonly GetCurrencyByIdUseCase  _getById;
    private readonly UpdateCurrencyUseCase   _update;

    public CurrencyService(
        CreateCurrencyUseCase  create,
        DeleteCurrencyUseCase  delete,
        GetAllCurrenciesUseCase getAll,
        GetCurrencyByIdUseCase getById,
        UpdateCurrencyUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CurrencyDto> CreateAsync(
        string            isoCode,
        string            name,
        string            symbol,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(isoCode, name, symbol, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CurrencyDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<CurrencyDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        string            isoCode,
        string            name,
        string            symbol,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, isoCode, name, symbol, cancellationToken);

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static CurrencyDto ToDto(
        Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate.CurrencyAggregate agg)
        => new(agg.Id.Value, agg.IsoCode, agg.Name, agg.Symbol);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/Currency/Infrastructure/entity/CurrencyEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Infrastructure.Entity;

public sealed class CurrencyEntity
{
    public int    Id      { get; set; }
    public string IsoCode { get; set; } = null!;
    public string Name    { get; set; } = null!;
    public string Symbol  { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/Currency/Infrastructure/entity/CurrencyEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CurrencyEntityConfiguration : IEntityTypeConfiguration<CurrencyEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyEntity> builder)
    {
        builder.ToTable("currency");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("currency_id")
               .ValueGeneratedOnAdd();

        // CHAR(3) — código ISO 4217
        builder.Property(e => e.IsoCode)
               .HasColumnName("iso_code")
               .IsRequired()
               .HasMaxLength(3)
               .IsFixedLength();

        builder.HasIndex(e => e.IsoCode)
               .IsUnique()
               .HasDatabaseName("uq_currency_iso_code");

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(80);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_currency_name");

        builder.Property(e => e.Symbol)
               .HasColumnName("symbol")
               .IsRequired()
               .HasMaxLength(5);
    }
}
```

---

### RUTA: `src/Modules/Currency/Infrastructure/repository/CurrencyRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CurrencyRepository : ICurrencyRepository
{
    private readonly AppDbContext _context;

    public CurrencyRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static CurrencyAggregate ToDomain(CurrencyEntity entity)
        => new(new CurrencyId(entity.Id), entity.IsoCode, entity.Name, entity.Symbol);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<CurrencyAggregate?> GetByIdAsync(
        CurrencyId        id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CurrencyAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Currencies
            .AsNoTracking()
            .OrderBy(e => e.IsoCode)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CurrencyAggregate currency,
        CancellationToken cancellationToken = default)
    {
        var entity = new CurrencyEntity
        {
            IsoCode = currency.IsoCode,
            Name    = currency.Name,
            Symbol  = currency.Symbol
        };
        await _context.Currencies.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CurrencyAggregate currency,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Currencies
            .FirstOrDefaultAsync(e => e.Id == currency.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CurrencyEntity with id {currency.Id.Value} not found.");

        entity.IsoCode = currency.IsoCode;
        entity.Name    = currency.Name;
        entity.Symbol  = currency.Symbol;

        _context.Currencies.Update(entity);
    }

    public async Task DeleteAsync(
        CurrencyId        id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Currencies
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CurrencyEntity with id {id.Value} not found.");

        _context.Currencies.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/Currency/UI/CurrencyConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.Interfaces;

public sealed class CurrencyConsoleUI
{
    private readonly ICurrencyService _service;

    public CurrencyConsoleUI(ICurrencyService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CURRENCY MODULE ==========");
            Console.WriteLine("1. List all currencies");
            Console.WriteLine("2. Get currency by ID");
            Console.WriteLine("3. Create currency");
            Console.WriteLine("4. Update currency");
            Console.WriteLine("5. Delete currency");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();  break;
                case "2": await GetByIdAsync();  break;
                case "3": await CreateAsync();   break;
                case "4": await UpdateAsync();   break;
                case "5": await DeleteAsync();   break;
                case "0": running = false;       break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var currencies = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Currencies ---");

        foreach (var c in currencies)
            Console.WriteLine($"  [{c.Id}] {c.IsoCode} — {c.Name} ({c.Symbol})");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter currency ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var currency = await _service.GetByIdAsync(id);

        if (currency is null)
            Console.WriteLine($"Currency with ID {id} not found.");
        else
            Console.WriteLine($"  [{currency.Id}] {currency.IsoCode} — {currency.Name} ({currency.Symbol})");
    }

    private async Task CreateAsync()
    {
        Console.Write("ISO code (3 chars, e.g. COP, USD, EUR): ");
        var isoCode = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(isoCode))
        { Console.WriteLine("ISO code cannot be empty."); return; }

        Console.Write("Name (e.g. Colombian Peso): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("Symbol (e.g. $, €, £): ");
        var symbol = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(symbol))
        { Console.WriteLine("Symbol cannot be empty."); return; }

        try
        {
            var created = await _service.CreateAsync(isoCode, name, symbol);
            Console.WriteLine($"Currency created: [{created.Id}] {created.IsoCode} — {created.Name} ({created.Symbol})");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter currency ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New ISO code (3 chars): ");
        var isoCode = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(isoCode)) { Console.WriteLine("ISO code cannot be empty."); return; }

        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("New symbol: ");
        var symbol = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(symbol)) { Console.WriteLine("Symbol cannot be empty."); return; }

        try
        {
            await _service.UpdateAsync(id, isoCode, name, symbol);
            Console.WriteLine("Currency updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter currency ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Currency deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── Currency Module ───────────────────────────────────────────────────────────
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<CreateCurrencyUseCase>();
builder.Services.AddScoped<DeleteCurrencyUseCase>();
builder.Services.AddScoped<GetAllCurrenciesUseCase>();
builder.Services.AddScoped<GetCurrencyByIdUseCase>();
builder.Services.AddScoped<UpdateCurrencyUseCase>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<CurrencyConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/Currency/
├── Application/
│   ├── Interfaces/
│   │   └── ICurrencyService.cs
│   ├── Services/
│   │   └── CurrencyService.cs
│   └── UseCases/
│       ├── CreateCurrencyUseCase.cs
│       ├── DeleteCurrencyUseCase.cs
│       ├── GetAllCurrenciesUseCase.cs
│       ├── GetCurrencyByIdUseCase.cs
│       └── UpdateCurrencyUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── CurrencyAggregate.cs
│   ├── Repositories/
│   │   └── ICurrencyRepository.cs
│   └── valueObject/
│       └── CurrencyId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── CurrencyEntity.cs
│   │   └── CurrencyEntityConfiguration.cs
│   └── repository/
│       └── CurrencyRepository.cs
└── UI/
    └── CurrencyConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo Currency_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
