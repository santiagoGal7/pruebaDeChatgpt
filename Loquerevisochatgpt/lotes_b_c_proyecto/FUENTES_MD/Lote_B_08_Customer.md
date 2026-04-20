# Módulo: Customer
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer`  
**Raíz de archivos:** `src/Modules/Customer/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `customer_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `person_id` | `INT NOT NULL UNIQUE FK` | `int` | FK → `person`. Un cliente = una persona |
| `phone` | `VARCHAR(30) NULL` | `string?` | Nullable |
| `email` | `VARCHAR(120) NULL UNIQUE` | `string?` | Nullable, UNIQUE no null, CHECK REGEXP validado en dominio |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create |
| `updated_at` | `DATETIME NULL` | `DateTime?` | Nullable |

**CHECK SQL:** `email IS NULL OR email REGEXP '^[^@]+@[^@]+\\.[^@]+$'` → validado en el agregado con `System.Text.RegularExpressions`.  
**UNIQUE:** `(email)` — un email solo puede pertenecer a un cliente.  
`person_id` también UNIQUE: una persona solo puede ser cliente una vez.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/Customer/Domain/valueObject/CustomerId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;

public sealed class CustomerId
{
    public int Value { get; }

    public CustomerId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CustomerId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CustomerId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/Customer/Domain/aggregate/CustomerAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;

using System.Text.RegularExpressions;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;

/// <summary>
/// Rol cliente: la persona que compra o realiza reservas.
/// SQL: customer. person_id UNIQUE — una persona es cliente como máximo una vez.
///
/// Invariantes:
///   - email nullable pero, si se provee, debe respetar el formato
///     '^[^@]+@[^@]+\.[^@]+$' (espejo del CHECK SQL).
///   - email UNIQUE en la BD — la unicidad se garantiza a nivel de base de datos.
///   - phone nullable, máximo 30 caracteres.
///   - El email se normaliza a minúsculas para evitar duplicados por capitalización.
/// </summary>
public sealed class CustomerAggregate
{
    // Espejo del CHECK SQL: ^[^@]+@[^@]+\.[^@]+$
    private static readonly Regex EmailRegex =
        new(@"^[^@]+@[^@]+\.[^@]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CustomerId Id        { get; private set; }
    public int        PersonId  { get; private set; }
    public string?    Phone     { get; private set; }
    public string?    Email     { get; private set; }
    public DateTime   CreatedAt { get; private set; }
    public DateTime?  UpdatedAt { get; private set; }

    private CustomerAggregate()
    {
        Id = null!;
    }

    public CustomerAggregate(
        CustomerId id,
        int        personId,
        string?    phone,
        string?    email,
        DateTime   createdAt,
        DateTime?  updatedAt = null)
    {
        if (personId <= 0)
            throw new ArgumentException("PersonId must be a positive integer.", nameof(personId));

        ValidatePhone(phone);
        ValidateEmail(email);

        Id        = id;
        PersonId  = personId;
        Phone     = phone?.Trim();
        Email     = email?.Trim().ToLowerInvariant();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Actualiza teléfono y email del cliente.
    /// PersonId no es modificable: identifica a quién es el cliente.
    /// </summary>
    public void UpdateContact(string? phone, string? email)
    {
        ValidatePhone(phone);
        ValidateEmail(email);

        Phone     = phone?.Trim();
        Email     = email?.Trim().ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidatePhone(string? phone)
    {
        if (phone is not null && phone.Trim().Length > 30)
            throw new ArgumentException(
                "Phone cannot exceed 30 characters.", nameof(phone));
    }

    private static void ValidateEmail(string? email)
    {
        if (email is null)
            return;

        var trimmed = email.Trim();

        if (trimmed.Length > 120)
            throw new ArgumentException(
                "Email cannot exceed 120 characters.", nameof(email));

        if (!EmailRegex.IsMatch(trimmed))
            throw new ArgumentException(
                "Email format is invalid. Expected: local@domain.tld", nameof(email));
    }
}
```

---

### RUTA: `src/Modules/Customer/Domain/Repositories/ICustomerRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;

public interface ICustomerRepository
{
    Task<CustomerAggregate?>             GetByIdAsync(CustomerId id,               CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerAggregate>> GetAllAsync(                              CancellationToken cancellationToken = default);
    Task                                 AddAsync(CustomerAggregate customer,      CancellationToken cancellationToken = default);
    Task                                 UpdateAsync(CustomerAggregate customer,   CancellationToken cancellationToken = default);
    Task                                 DeleteAsync(CustomerId id,                CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/Customer/Application/Interfaces/ICustomerService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerDto?>             GetByIdAsync(int id,                                        CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDto>> GetAllAsync(                                                CancellationToken cancellationToken = default);
    Task<CustomerDto>              CreateAsync(int personId, string? phone, string? email,     CancellationToken cancellationToken = default);
    Task                           UpdateAsync(int id, string? phone, string? email,           CancellationToken cancellationToken = default);
    Task                           DeleteAsync(int id,                                         CancellationToken cancellationToken = default);
}

public sealed record CustomerDto(
    int      Id,
    int      PersonId,
    string?  Phone,
    string?  Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

---

### RUTA: `src/Modules/Customer/Application/UseCases/CreateCustomerUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCustomerUseCase
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public CreateCustomerUseCase(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerAggregate> ExecuteAsync(
        int               personId,
        string?           phone,
        string?           email,
        CancellationToken cancellationToken = default)
    {
        // CustomerId(1) es placeholder; EF Core asigna el Id real al insertar.
        var customer = new CustomerAggregate(
            new CustomerId(1),
            personId,
            phone,
            email,
            DateTime.UtcNow);

        await _repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return customer;
    }
}
```

---

### RUTA: `src/Modules/Customer/Application/UseCases/DeleteCustomerUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCustomerUseCase
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public DeleteCustomerUseCase(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CustomerId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Customer/Application/UseCases/GetAllCustomersUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;

public sealed class GetAllCustomersUseCase
{
    private readonly ICustomerRepository _repository;

    public GetAllCustomersUseCase(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CustomerAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/Customer/Application/UseCases/GetCustomerByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;

public sealed class GetCustomerByIdUseCase
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdUseCase(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<CustomerAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CustomerId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/Customer/Application/UseCases/UpdateCustomerUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza los datos de contacto del cliente (phone, email).
/// PersonId no es modificable — define a quién identifica el cliente.
/// </summary>
public sealed class UpdateCustomerUseCase
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public UpdateCustomerUseCase(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string?           phone,
        string?           email,
        CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(new CustomerId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Customer with id {id} was not found.");

        customer.UpdateContact(phone, email);
        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Customer/Application/Services/CustomerService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;

public sealed class CustomerService : ICustomerService
{
    private readonly CreateCustomerUseCase   _create;
    private readonly DeleteCustomerUseCase   _delete;
    private readonly GetAllCustomersUseCase  _getAll;
    private readonly GetCustomerByIdUseCase  _getById;
    private readonly UpdateCustomerUseCase   _update;

    public CustomerService(
        CreateCustomerUseCase  create,
        DeleteCustomerUseCase  delete,
        GetAllCustomersUseCase getAll,
        GetCustomerByIdUseCase getById,
        UpdateCustomerUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CustomerDto> CreateAsync(
        int               personId,
        string?           phone,
        string?           email,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(personId, phone, email, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CustomerDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<CustomerDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        string?           phone,
        string?           email,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, phone, email, cancellationToken);

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static CustomerDto ToDto(CustomerAggregate agg)
        => new(agg.Id.Value, agg.PersonId, agg.Phone, agg.Email, agg.CreatedAt, agg.UpdatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/Customer/Infrastructure/entity/CustomerEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Infrastructure.Entity;

public sealed class CustomerEntity
{
    public int       Id        { get; set; }
    public int       PersonId  { get; set; }
    public string?   Phone     { get; set; }
    public string?   Email     { get; set; }
    public DateTime  CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

---

### RUTA: `src/Modules/Customer/Infrastructure/entity/CustomerEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CustomerEntityConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customer");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("customer_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.PersonId)
               .HasColumnName("person_id")
               .IsRequired();

        // person_id UNIQUE — una persona solo puede ser cliente una vez.
        builder.HasIndex(e => e.PersonId)
               .IsUnique()
               .HasDatabaseName("uq_customer_person_id");

        builder.Property(e => e.Phone)
               .HasColumnName("phone")
               .IsRequired(false)
               .HasMaxLength(30);

        builder.Property(e => e.Email)
               .HasColumnName("email")
               .IsRequired(false)
               .HasMaxLength(120);

        // UNIQUE (email). En MySQL múltiples NULL ya son permitidos,
        // por lo que NO se usa HasFilter(...).
        builder.HasIndex(e => e.Email)
               .IsUnique()
               .HasDatabaseName("uq_customer_email");

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);
    }
}
```

---

### RUTA: `src/Modules/Customer/Infrastructure/repository/CustomerRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static CustomerAggregate ToDomain(CustomerEntity entity)
        => new(
            new CustomerId(entity.Id),
            entity.PersonId,
            entity.Phone,
            entity.Email,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<CustomerAggregate?> GetByIdAsync(
        CustomerId        id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CustomerAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Customers
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CustomerAggregate customer,
        CancellationToken cancellationToken = default)
    {
        var entity = new CustomerEntity
        {
            PersonId  = customer.PersonId,
            Phone     = customer.Phone,
            Email     = customer.Email,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
        await _context.Customers.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CustomerAggregate customer,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Customers
            .FirstOrDefaultAsync(e => e.Id == customer.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CustomerEntity with id {customer.Id.Value} not found.");

        // PersonId no se modifica — es la clave de negocio.
        entity.Phone     = customer.Phone;
        entity.Email     = customer.Email;
        entity.UpdatedAt = customer.UpdatedAt;

        _context.Customers.Update(entity);
    }

    public async Task DeleteAsync(
        CustomerId        id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Customers
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CustomerEntity with id {id.Value} not found.");

        _context.Customers.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/Customer/UI/CustomerConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.Interfaces;

public sealed class CustomerConsoleUI
{
    private readonly ICustomerService _service;

    public CustomerConsoleUI(ICustomerService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CUSTOMER MODULE ==========");
            Console.WriteLine("1. List all customers");
            Console.WriteLine("2. Get customer by ID");
            Console.WriteLine("3. Create customer");
            Console.WriteLine("4. Update contact info");
            Console.WriteLine("5. Delete customer");
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
        var customers = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Customers ---");

        foreach (var c in customers)
            PrintCustomer(c);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var customer = await _service.GetByIdAsync(id);

        if (customer is null)
            Console.WriteLine($"Customer with ID {id} not found.");
        else
            PrintCustomer(customer);
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter Person ID: ");
        if (!int.TryParse(Console.ReadLine(), out int personId))
        {
            Console.WriteLine("Invalid Person ID.");
            return;
        }

        Console.Write("Phone (optional — press Enter to skip): ");
        var phoneInput = Console.ReadLine()?.Trim();
        string? phone  = string.IsNullOrWhiteSpace(phoneInput) ? null : phoneInput;

        Console.Write("Email (optional — press Enter to skip): ");
        var emailInput = Console.ReadLine()?.Trim();
        string? email  = string.IsNullOrWhiteSpace(emailInput) ? null : emailInput;

        try
        {
            var created = await _service.CreateAsync(personId, phone, email);
            Console.WriteLine(
                $"Customer created: [{created.Id}] PersonId: {created.PersonId} | " +
                $"Email: {created.Email ?? "—"} | Phone: {created.Phone ?? "—"}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter customer ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("New phone (optional — press Enter to clear): ");
        var phoneInput = Console.ReadLine()?.Trim();
        string? phone  = string.IsNullOrWhiteSpace(phoneInput) ? null : phoneInput;

        Console.Write("New email (optional — press Enter to clear): ");
        var emailInput = Console.ReadLine()?.Trim();
        string? email  = string.IsNullOrWhiteSpace(emailInput) ? null : emailInput;

        try
        {
            await _service.UpdateAsync(id, phone, email);
            Console.WriteLine("Customer contact info updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter customer ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Customer deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintCustomer(CustomerDto c)
        => Console.WriteLine(
            $"  [{c.Id}] PersonId: {c.PersonId} | " +
            $"Phone: {c.Phone ?? "—"} | Email: {c.Email ?? "—"} | " +
            $"Created: {c.CreatedAt:yyyy-MM-dd HH:mm}" +
            (c.UpdatedAt.HasValue ? $" | Updated: {c.UpdatedAt:yyyy-MM-dd HH:mm}" : string.Empty));
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── Customer Module ───────────────────────────────────────────────────────────
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<CreateCustomerUseCase>();
builder.Services.AddScoped<DeleteCustomerUseCase>();
builder.Services.AddScoped<GetAllCustomersUseCase>();
builder.Services.AddScoped<GetCustomerByIdUseCase>();
builder.Services.AddScoped<UpdateCustomerUseCase>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<CustomerConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/Customer/
├── Application/
│   ├── Interfaces/
│   │   └── ICustomerService.cs
│   ├── Services/
│   │   └── CustomerService.cs
│   └── UseCases/
│       ├── CreateCustomerUseCase.cs
│       ├── DeleteCustomerUseCase.cs
│       ├── GetAllCustomersUseCase.cs
│       ├── GetCustomerByIdUseCase.cs
│       └── UpdateCustomerUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── CustomerAggregate.cs
│   ├── Repositories/
│   │   └── ICustomerRepository.cs
│   └── valueObject/
│       └── CustomerId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── CustomerEntity.cs
│   │   └── CustomerEntityConfiguration.cs
│   └── repository/
│       └── CustomerRepository.cs
└── UI/
    └── CustomerConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo Customer_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
