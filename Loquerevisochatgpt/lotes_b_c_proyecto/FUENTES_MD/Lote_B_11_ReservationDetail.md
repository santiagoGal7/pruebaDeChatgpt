# Módulo: ReservationDetail
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail`  
**Raíz de archivos:** `src/Modules/ReservationDetail/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `reservation_detail_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `reservation_id` | `INT NOT NULL FK` | `int` | FK → `reservation` |
| `passenger_id` | `INT NOT NULL FK` | `int` | FK → `passenger` |
| `flight_seat_id` | `INT NOT NULL FK` | `int` | FK → `flight_seat` |
| `fare_type_id` | `INT NOT NULL FK` | `int` | FK → `fare_type` (tarifa elegida por el pasajero) |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create |
| `updated_at` | `DATETIME NULL` | `DateTime?` | Nullable |

**UNIQUE:**
- `(reservation_id, passenger_id)` — un pasajero solo aparece una vez por reserva.
- `(reservation_id, flight_seat_id)` — un asiento solo se asigna a un pasajero por reserva.

**4NF:** `reservation_id →→ passenger_id` y `→→ flight_seat_id` NO son independientes (cada pasajero tiene UN asiento) → no viola 4NF.  
`fare_amount` fue eliminado del DDL — se obtiene vía JOIN con `flight_cabin_price`.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/ReservationDetail/Domain/valueObject/ReservationDetailId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.ValueObject;

public sealed class ReservationDetailId
{
    public int Value { get; }

    public ReservationDetailId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("ReservationDetailId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is ReservationDetailId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/ReservationDetail/Domain/aggregate/ReservationDetailAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.ValueObject;

/// <summary>
/// Una línea de reserva: un pasajero + un asiento + una tarifa dentro de una reserva.
/// SQL: reservation_detail.
///
/// 4NF: reservation_id →→ passenger_id y →→ flight_seat_id NO son independientes;
/// cada pasajero tiene UN asiento en esa reserva — no viola 4NF.
///
/// Invariantes clave:
///   - UNIQUE (reservation_id, passenger_id) — un pasajero solo una vez por reserva.
///   - UNIQUE (reservation_id, flight_seat_id) — un asiento solo a un pasajero.
///   - fare_amount fue eliminado — se obtiene vía JOIN con flight_cabin_price.
///
/// La única actualización válida es cambiar la tarifa (ChangeFareType).
/// El asiento y el pasajero son la clave de negocio y no pueden cambiarse.
/// El trigger RF-6 en la BD verifica que el asiento esté AVAILABLE antes de insertar.
/// </summary>
public sealed class ReservationDetailAggregate
{
    public ReservationDetailId Id                  { get; private set; }
    public int                 ReservationId       { get; private set; }
    public int                 PassengerId         { get; private set; }
    public int                 FlightSeatId        { get; private set; }
    public int                 FareTypeId          { get; private set; }
    public DateTime            CreatedAt           { get; private set; }
    public DateTime?           UpdatedAt           { get; private set; }

    private ReservationDetailAggregate()
    {
        Id = null!;
    }

    public ReservationDetailAggregate(
        ReservationDetailId id,
        int                 reservationId,
        int                 passengerId,
        int                 flightSeatId,
        int                 fareTypeId,
        DateTime            createdAt,
        DateTime?           updatedAt = null)
    {
        if (reservationId <= 0)
            throw new ArgumentException(
                "ReservationId must be a positive integer.", nameof(reservationId));

        if (passengerId <= 0)
            throw new ArgumentException(
                "PassengerId must be a positive integer.", nameof(passengerId));

        if (flightSeatId <= 0)
            throw new ArgumentException(
                "FlightSeatId must be a positive integer.", nameof(flightSeatId));

        if (fareTypeId <= 0)
            throw new ArgumentException(
                "FareTypeId must be a positive integer.", nameof(fareTypeId));

        Id            = id;
        ReservationId = reservationId;
        PassengerId   = passengerId;
        FlightSeatId  = flightSeatId;
        FareTypeId    = fareTypeId;
        CreatedAt     = createdAt;
        UpdatedAt     = updatedAt;
    }

    /// <summary>
    /// Cambia la tarifa seleccionada por el pasajero en esta línea de reserva.
    /// ReservationId, PassengerId y FlightSeatId son la clave de negocio — inmutables.
    /// </summary>
    public void ChangeFareType(int fareTypeId)
    {
        if (fareTypeId <= 0)
            throw new ArgumentException(
                "FareTypeId must be a positive integer.", nameof(fareTypeId));

        FareTypeId = fareTypeId;
        UpdatedAt  = DateTime.UtcNow;
    }
}
```

---

### RUTA: `src/Modules/ReservationDetail/Domain/Repositories/IReservationDetailRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.ValueObject;

public interface IReservationDetailRepository
{
    Task<ReservationDetailAggregate?>             GetByIdAsync(ReservationDetailId id,                      CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDetailAggregate>> GetAllAsync(                                               CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDetailAggregate>> GetByReservationAsync(int reservationId,                   CancellationToken cancellationToken = default);
    Task                                          AddAsync(ReservationDetailAggregate reservationDetail,     CancellationToken cancellationToken = default);
    Task                                          UpdateAsync(ReservationDetailAggregate reservationDetail,  CancellationToken cancellationToken = default);
    Task                                          DeleteAsync(ReservationDetailId id,                        CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/ReservationDetail/Application/Interfaces/IReservationDetailService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.Interfaces;

public interface IReservationDetailService
{
    Task<ReservationDetailDto?>             GetByIdAsync(int id,                                                              CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDetailDto>> GetAllAsync(                                                                      CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDetailDto>> GetByReservationAsync(int reservationId,                                          CancellationToken cancellationToken = default);
    Task<ReservationDetailDto>              CreateAsync(int reservationId, int passengerId, int flightSeatId, int fareTypeId,  CancellationToken cancellationToken = default);
    Task                                    ChangeFareTypeAsync(int id, int fareTypeId,                                       CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(int id,                                                               CancellationToken cancellationToken = default);
}

public sealed record ReservationDetailDto(
    int      Id,
    int      ReservationId,
    int      PassengerId,
    int      FlightSeatId,
    int      FareTypeId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

---

### RUTA: `src/Modules/ReservationDetail/Application/UseCases/CreateReservationDetailUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateReservationDetailUseCase
{
    private readonly IReservationDetailRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public CreateReservationDetailUseCase(IReservationDetailRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReservationDetailAggregate> ExecuteAsync(
        int               reservationId,
        int               passengerId,
        int               flightSeatId,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
    {
        // ReservationDetailId(1) es placeholder; EF Core asigna el Id real al insertar.
        // NOTA: el trigger RF-6 de la BD verifica que el asiento esté AVAILABLE.
        var detail = new ReservationDetailAggregate(
            new ReservationDetailId(1),
            reservationId,
            passengerId,
            flightSeatId,
            fareTypeId,
            DateTime.UtcNow);

        await _repository.AddAsync(detail, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return detail;
    }
}
```

---

### RUTA: `src/Modules/ReservationDetail/Application/UseCases/DeleteReservationDetailUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteReservationDetailUseCase
{
    private readonly IReservationDetailRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public DeleteReservationDetailUseCase(IReservationDetailRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new ReservationDetailId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/ReservationDetail/Application/UseCases/GetAllReservationDetailsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Repositories;

public sealed class GetAllReservationDetailsUseCase
{
    private readonly IReservationDetailRepository _repository;

    public GetAllReservationDetailsUseCase(IReservationDetailRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReservationDetailAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/ReservationDetail/Application/UseCases/GetReservationDetailByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.ValueObject;

public sealed class GetReservationDetailByIdUseCase
{
    private readonly IReservationDetailRepository _repository;

    public GetReservationDetailByIdUseCase(IReservationDetailRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReservationDetailAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new ReservationDetailId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/ReservationDetail/Application/UseCases/UpdateReservationDetailUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Cambia la tarifa seleccionada por el pasajero en esta línea de reserva.
/// ReservationId, PassengerId y FlightSeatId son la clave de negocio — inmutables.
/// </summary>
public sealed class UpdateReservationDetailUseCase
{
    private readonly IReservationDetailRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public UpdateReservationDetailUseCase(IReservationDetailRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _repository.GetByIdAsync(new ReservationDetailId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"ReservationDetail with id {id} was not found.");

        detail.ChangeFareType(fareTypeId);
        await _repository.UpdateAsync(detail, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/ReservationDetail/Application/UseCases/GetReservationDetailsByReservationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Repositories;

/// <summary>
/// Obtiene todas las líneas (pasajero + asiento + tarifa) de una reserva.
/// Caso de uso clave para mostrar el detalle completo de una reserva.
/// </summary>
public sealed class GetReservationDetailsByReservationUseCase
{
    private readonly IReservationDetailRepository _repository;

    public GetReservationDetailsByReservationUseCase(IReservationDetailRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReservationDetailAggregate>> ExecuteAsync(
        int               reservationId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByReservationAsync(reservationId, cancellationToken);
}
```

---

### RUTA: `src/Modules/ReservationDetail/Application/Services/ReservationDetailService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Aggregate;

public sealed class ReservationDetailService : IReservationDetailService
{
    private readonly CreateReservationDetailUseCase              _create;
    private readonly DeleteReservationDetailUseCase              _delete;
    private readonly GetAllReservationDetailsUseCase             _getAll;
    private readonly GetReservationDetailByIdUseCase             _getById;
    private readonly UpdateReservationDetailUseCase              _update;
    private readonly GetReservationDetailsByReservationUseCase   _getByReservation;

    public ReservationDetailService(
        CreateReservationDetailUseCase            create,
        DeleteReservationDetailUseCase            delete,
        GetAllReservationDetailsUseCase           getAll,
        GetReservationDetailByIdUseCase           getById,
        UpdateReservationDetailUseCase            update,
        GetReservationDetailsByReservationUseCase getByReservation)
    {
        _create           = create;
        _delete           = delete;
        _getAll           = getAll;
        _getById          = getById;
        _update           = update;
        _getByReservation = getByReservation;
    }

    public async Task<ReservationDetailDto> CreateAsync(
        int               reservationId,
        int               passengerId,
        int               flightSeatId,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            reservationId, passengerId, flightSeatId, fareTypeId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<ReservationDetailDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<ReservationDetailDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task ChangeFareTypeAsync(
        int               id,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, fareTypeId, cancellationToken);

    public async Task<IEnumerable<ReservationDetailDto>> GetByReservationAsync(
        int               reservationId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByReservation.ExecuteAsync(reservationId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static ReservationDetailDto ToDto(ReservationDetailAggregate agg)
        => new(
            agg.Id.Value,
            agg.ReservationId,
            agg.PassengerId,
            agg.FlightSeatId,
            agg.FareTypeId,
            agg.CreatedAt,
            agg.UpdatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/ReservationDetail/Infrastructure/entity/ReservationDetailEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Infrastructure.Entity;

public sealed class ReservationDetailEntity
{
    public int       Id                  { get; set; }
    public int       ReservationId       { get; set; }
    public int       PassengerId         { get; set; }
    public int       FlightSeatId        { get; set; }
    public int       FareTypeId          { get; set; }
    public DateTime  CreatedAt           { get; set; }
    public DateTime? UpdatedAt           { get; set; }
}
```

---

### RUTA: `src/Modules/ReservationDetail/Infrastructure/entity/ReservationDetailEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ReservationDetailEntityConfiguration : IEntityTypeConfiguration<ReservationDetailEntity>
{
    public void Configure(EntityTypeBuilder<ReservationDetailEntity> builder)
    {
        builder.ToTable("reservation_detail");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("reservation_detail_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.ReservationId)
               .HasColumnName("reservation_id")
               .IsRequired();

        builder.Property(e => e.PassengerId)
               .HasColumnName("passenger_id")
               .IsRequired();

        builder.Property(e => e.FlightSeatId)
               .HasColumnName("flight_seat_id")
               .IsRequired();

        builder.Property(e => e.FareTypeId)
               .HasColumnName("fare_type_id")
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);

        // UNIQUE (reservation_id, passenger_id) — espejo de uq_rd_passenger
        builder.HasIndex(e => new { e.ReservationId, e.PassengerId })
               .IsUnique()
               .HasDatabaseName("uq_rd_passenger");

        // UNIQUE (reservation_id, flight_seat_id) — espejo de uq_rd_seat
        builder.HasIndex(e => new { e.ReservationId, e.FlightSeatId })
               .IsUnique()
               .HasDatabaseName("uq_rd_seat");
    }
}
```

---

### RUTA: `src/Modules/ReservationDetail/Infrastructure/repository/ReservationDetailRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class ReservationDetailRepository : IReservationDetailRepository
{
    private readonly AppDbContext _context;

    public ReservationDetailRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static ReservationDetailAggregate ToDomain(ReservationDetailEntity entity)
        => new(
            new ReservationDetailId(entity.Id),
            entity.ReservationId,
            entity.PassengerId,
            entity.FlightSeatId,
            entity.FareTypeId,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<ReservationDetailAggregate?> GetByIdAsync(
        ReservationDetailId id,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.ReservationDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<ReservationDetailAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ReservationDetails
            .AsNoTracking()
            .OrderBy(e => e.ReservationId)
            .ThenBy(e => e.PassengerId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<ReservationDetailAggregate>> GetByReservationAsync(
        int               reservationId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ReservationDetails
            .AsNoTracking()
            .Where(e => e.ReservationId == reservationId)
            .OrderBy(e => e.PassengerId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        ReservationDetailAggregate reservationDetail,
        CancellationToken          cancellationToken = default)
    {
        var entity = new ReservationDetailEntity
        {
            ReservationId = reservationDetail.ReservationId,
            PassengerId   = reservationDetail.PassengerId,
            FlightSeatId  = reservationDetail.FlightSeatId,
            FareTypeId    = reservationDetail.FareTypeId,
            CreatedAt     = reservationDetail.CreatedAt,
            UpdatedAt     = reservationDetail.UpdatedAt
        };
        await _context.ReservationDetails.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        ReservationDetailAggregate reservationDetail,
        CancellationToken          cancellationToken = default)
    {
        var entity = await _context.ReservationDetails
            .FirstOrDefaultAsync(e => e.Id == reservationDetail.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"ReservationDetailEntity with id {reservationDetail.Id.Value} not found.");

        // Solo FareTypeId y UpdatedAt son mutables.
        // ReservationId, PassengerId y FlightSeatId forman la clave de negocio.
        entity.FareTypeId = reservationDetail.FareTypeId;
        entity.UpdatedAt  = reservationDetail.UpdatedAt;

        _context.ReservationDetails.Update(entity);
    }

    public async Task DeleteAsync(
        ReservationDetailId id,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.ReservationDetails
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"ReservationDetailEntity with id {id.Value} not found.");

        _context.ReservationDetails.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/ReservationDetail/UI/ReservationDetailConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationDetail.Application.Interfaces;

public sealed class ReservationDetailConsoleUI
{
    private readonly IReservationDetailService _service;

    public ReservationDetailConsoleUI(IReservationDetailService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== RESERVATION DETAIL MODULE ==========");
            Console.WriteLine("1. List all reservation details");
            Console.WriteLine("2. Get detail by ID");
            Console.WriteLine("3. List details by reservation");
            Console.WriteLine("4. Add passenger to reservation");
            Console.WriteLine("5. Change fare type");
            Console.WriteLine("6. Remove detail");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();              break;
                case "2": await GetByIdAsync();              break;
                case "3": await ListByReservationAsync();    break;
                case "4": await AddPassengerAsync();         break;
                case "5": await ChangeFareTypeAsync();       break;
                case "6": await RemoveAsync();               break;
                case "0": running = false;                   break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var details = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Reservation Details ---");
        foreach (var d in details) PrintDetail(d);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter reservation detail ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var detail = await _service.GetByIdAsync(id);
        if (detail is null) Console.WriteLine($"Reservation detail with ID {id} not found.");
        else                PrintDetail(detail);
    }

    private async Task ListByReservationAsync()
    {
        Console.Write("Enter Reservation ID: ");
        if (!int.TryParse(Console.ReadLine(), out int reservationId))
        { Console.WriteLine("Invalid ID."); return; }

        var details = await _service.GetByReservationAsync(reservationId);
        Console.WriteLine($"\n--- Details for Reservation {reservationId} ---");
        foreach (var d in details) PrintDetail(d);
    }

    private async Task AddPassengerAsync()
    {
        Console.Write("Reservation ID: ");
        if (!int.TryParse(Console.ReadLine(), out int reservationId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Passenger ID: ");
        if (!int.TryParse(Console.ReadLine(), out int passengerId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Flight Seat ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightSeatId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Fare Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int fareTypeId))
        { Console.WriteLine("Invalid ID."); return; }

        var created = await _service.CreateAsync(
            reservationId, passengerId, flightSeatId, fareTypeId);

        Console.WriteLine(
            $"Detail added: [{created.Id}] Passenger {created.PassengerId} → " +
            $"Seat {created.FlightSeatId} | Fare: {created.FareTypeId}");
    }

    private async Task ChangeFareTypeAsync()
    {
        Console.Write("Reservation detail ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New Fare Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int fareTypeId))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.ChangeFareTypeAsync(id, fareTypeId);
        Console.WriteLine("Fare type updated successfully.");
    }

    private async Task RemoveAsync()
    {
        Console.Write("Reservation detail ID to remove: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Reservation detail removed successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintDetail(ReservationDetailDto d)
        => Console.WriteLine(
            $"  [{d.Id}] Reservation: {d.ReservationId} | " +
            $"Passenger: {d.PassengerId} | Seat: {d.FlightSeatId} | " +
            $"FareType: {d.FareTypeId} | Created: {d.CreatedAt:yyyy-MM-dd HH:mm}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── ReservationDetail Module ──────────────────────────────────────────────────
builder.Services.AddScoped<IReservationDetailRepository, ReservationDetailRepository>();
builder.Services.AddScoped<CreateReservationDetailUseCase>();
builder.Services.AddScoped<DeleteReservationDetailUseCase>();
builder.Services.AddScoped<GetAllReservationDetailsUseCase>();
builder.Services.AddScoped<GetReservationDetailByIdUseCase>();
builder.Services.AddScoped<UpdateReservationDetailUseCase>();
builder.Services.AddScoped<GetReservationDetailsByReservationUseCase>();
builder.Services.AddScoped<IReservationDetailService, ReservationDetailService>();
builder.Services.AddScoped<ReservationDetailConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/ReservationDetail/
├── Application/
│   ├── Interfaces/
│   │   └── IReservationDetailService.cs
│   ├── Services/
│   │   └── ReservationDetailService.cs
│   └── UseCases/
│       ├── CreateReservationDetailUseCase.cs
│       ├── DeleteReservationDetailUseCase.cs
│       ├── GetAllReservationDetailsUseCase.cs
│       ├── GetReservationDetailByIdUseCase.cs
│       ├── GetReservationDetailsByReservationUseCase.cs
│       └── UpdateReservationDetailUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── ReservationDetailAggregate.cs
│   ├── Repositories/
│   │   └── IReservationDetailRepository.cs
│   └── valueObject/
│       └── ReservationDetailId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── ReservationDetailEntity.cs
│   │   └── ReservationDetailEntityConfiguration.cs
│   └── repository/
│       └── ReservationDetailRepository.cs
└── UI/
    └── ReservationDetailConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo ReservationDetail_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
