# Módulo: BoardingPass
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass`  
**Raíz de archivos:** `src/Modules/BoardingPass/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `boarding_pass_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-1] |
| `check_in_id` | `INT NOT NULL UNIQUE FK` | `int` | FK → `check_in`. UNIQUE: un pase por check-in |
| `gate_id` | `INT NULL FK` | `int?` | FK → `gate`. Nullable (puede cambiar de última hora) |
| `boarding_group` | `VARCHAR(10) NULL` | `string?` | Grupo de embarque, nullable |
| `flight_seat_id` | `INT NOT NULL FK` | `int` | FK → `flight_seat`. [IR-4] reemplazó seat_confirmed VARCHAR libre |

**UNIQUE:** `check_in_id` — un boarding pass por check-in.  
**[IR-4]:** `flight_seat_id` FK garantiza referencia real al asiento confirmado.  
`gate_id` puede diferir del gate del vuelo (cambios de última hora).  
Sin `created_at`, `updated_at` en el DDL.  
`Update()`: modifica `gate_id` y `boarding_group` (ambos pueden cambiar).

---

## 1. DOMAIN

---

### RUTA: `src/Modules/BoardingPass/Domain/valueObject/BoardingPassId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;

public sealed class BoardingPassId
{
    public int Value { get; }

    public BoardingPassId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("BoardingPassId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is BoardingPassId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/BoardingPass/Domain/aggregate/BoardingPassAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;

/// <summary>
/// Pase de abordar emitido tras el check-in. [NC-1] id renombrado.
/// SQL: boarding_pass.
///
/// [IR-4] flight_seat_id FK → flight_seat (reemplazó seat_confirmed VARCHAR).
/// UNIQUE: check_in_id — un boarding pass por check-in.
///
/// gate_id: puede diferir del gate del vuelo (cambios de puerta de última hora).
/// boarding_group: grupo de embarque (ej. A, B, 1, 2), nullable.
///
/// check_in_id y flight_seat_id son la clave operacional — inmutables.
/// Update(): modifica gate_id y boarding_group (datos operativos mutables).
/// </summary>
public sealed class BoardingPassAggregate
{
    public BoardingPassId Id             { get; private set; }
    public int            CheckInId      { get; private set; }
    public int?           GateId         { get; private set; }
    public string?        BoardingGroup  { get; private set; }
    public int            FlightSeatId   { get; private set; }

    private BoardingPassAggregate()
    {
        Id = null!;
    }

    public BoardingPassAggregate(
        BoardingPassId id,
        int            checkInId,
        int?           gateId,
        string?        boardingGroup,
        int            flightSeatId)
    {
        if (checkInId <= 0)
            throw new ArgumentException(
                "CheckInId must be a positive integer.", nameof(checkInId));

        if (gateId.HasValue && gateId.Value <= 0)
            throw new ArgumentException(
                "GateId must be a positive integer when provided.", nameof(gateId));

        if (flightSeatId <= 0)
            throw new ArgumentException(
                "FlightSeatId must be a positive integer.", nameof(flightSeatId));

        ValidateBoardingGroup(boardingGroup);

        Id            = id;
        CheckInId     = checkInId;
        GateId        = gateId;
        BoardingGroup = boardingGroup?.Trim();
        FlightSeatId  = flightSeatId;
    }

    /// <summary>
    /// Actualiza la puerta de embarque y/o el grupo de embarque.
    /// Ambos campos pueden cambiar en operaciones de última hora.
    /// check_in_id y flight_seat_id son inmutables.
    /// </summary>
    public void Update(int? gateId, string? boardingGroup)
    {
        if (gateId.HasValue && gateId.Value <= 0)
            throw new ArgumentException(
                "GateId must be a positive integer when provided.", nameof(gateId));

        ValidateBoardingGroup(boardingGroup);

        GateId        = gateId;
        BoardingGroup = boardingGroup?.Trim();
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateBoardingGroup(string? boardingGroup)
    {
        if (boardingGroup is not null && boardingGroup.Trim().Length > 10)
            throw new ArgumentException(
                "BoardingGroup cannot exceed 10 characters.", nameof(boardingGroup));
    }
}
```

---

### RUTA: `src/Modules/BoardingPass/Domain/Repositories/IBoardingPassRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;

public interface IBoardingPassRepository
{
    Task<BoardingPassAggregate?>             GetByIdAsync(BoardingPassId id,                      CancellationToken cancellationToken = default);
    Task<IEnumerable<BoardingPassAggregate>> GetAllAsync(                                          CancellationToken cancellationToken = default);
    Task<BoardingPassAggregate?>             GetByCheckInAsync(int checkInId,                      CancellationToken cancellationToken = default);
    Task                                     AddAsync(BoardingPassAggregate boardingPass,          CancellationToken cancellationToken = default);
    Task                                     UpdateAsync(BoardingPassAggregate boardingPass,       CancellationToken cancellationToken = default);
    Task                                     DeleteAsync(BoardingPassId id,                        CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/BoardingPass/Application/Interfaces/IBoardingPassService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.Interfaces;

public interface IBoardingPassService
{
    Task<BoardingPassDto?>             GetByIdAsync(int id,                                                           CancellationToken cancellationToken = default);
    Task<IEnumerable<BoardingPassDto>> GetAllAsync(                                                                   CancellationToken cancellationToken = default);
    Task<BoardingPassDto?>             GetByCheckInAsync(int checkInId,                                               CancellationToken cancellationToken = default);
    Task<BoardingPassDto>              CreateAsync(int checkInId, int? gateId, string? boardingGroup, int flightSeatId, CancellationToken cancellationToken = default);
    Task                               UpdateAsync(int id, int? gateId, string? boardingGroup,                       CancellationToken cancellationToken = default);
    Task                               DeleteAsync(int id,                                                           CancellationToken cancellationToken = default);
}

public sealed record BoardingPassDto(
    int     Id,
    int     CheckInId,
    int?    GateId,
    string? BoardingGroup,
    int     FlightSeatId);
```

---

### RUTA: `src/Modules/BoardingPass/Application/UseCases/CreateBoardingPassUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateBoardingPassUseCase
{
    private readonly IBoardingPassRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public CreateBoardingPassUseCase(IBoardingPassRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BoardingPassAggregate> ExecuteAsync(
        int               checkInId,
        int?              gateId,
        string?           boardingGroup,
        int               flightSeatId,
        CancellationToken cancellationToken = default)
    {
        // BoardingPassId(1) es placeholder; EF Core asigna el Id real al insertar.
        var boardingPass = new BoardingPassAggregate(
            new BoardingPassId(1),
            checkInId, gateId, boardingGroup, flightSeatId);

        await _repository.AddAsync(boardingPass, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return boardingPass;
    }
}
```

---

### RUTA: `src/Modules/BoardingPass/Application/UseCases/DeleteBoardingPassUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteBoardingPassUseCase
{
    private readonly IBoardingPassRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public DeleteBoardingPassUseCase(IBoardingPassRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new BoardingPassId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/BoardingPass/Application/UseCases/GetAllBoardingPassesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;

public sealed class GetAllBoardingPassesUseCase
{
    private readonly IBoardingPassRepository _repository;

    public GetAllBoardingPassesUseCase(IBoardingPassRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BoardingPassAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/BoardingPass/Application/UseCases/GetBoardingPassByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;

public sealed class GetBoardingPassByIdUseCase
{
    private readonly IBoardingPassRepository _repository;

    public GetBoardingPassByIdUseCase(IBoardingPassRepository repository)
    {
        _repository = repository;
    }

    public async Task<BoardingPassAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new BoardingPassId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/BoardingPass/Application/UseCases/UpdateBoardingPassUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza la puerta y/o grupo de embarque del boarding pass.
/// Necesario para gestionar cambios de última hora (gate changes).
/// check_in_id y flight_seat_id son inmutables.
/// </summary>
public sealed class UpdateBoardingPassUseCase
{
    private readonly IBoardingPassRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public UpdateBoardingPassUseCase(IBoardingPassRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int?              gateId,
        string?           boardingGroup,
        CancellationToken cancellationToken = default)
    {
        var boardingPass = await _repository.GetByIdAsync(new BoardingPassId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"BoardingPass with id {id} was not found.");

        boardingPass.Update(gateId, boardingGroup);
        await _repository.UpdateAsync(boardingPass, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/BoardingPass/Application/UseCases/GetBoardingPassByCheckInUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;

/// <summary>
/// Obtiene el boarding pass asociado a un check-in.
/// La UNIQUE sobre check_in_id garantiza como máximo un resultado.
/// Caso de uso clave para mostrar el boarding pass al pasajero.
/// </summary>
public sealed class GetBoardingPassByCheckInUseCase
{
    private readonly IBoardingPassRepository _repository;

    public GetBoardingPassByCheckInUseCase(IBoardingPassRepository repository)
    {
        _repository = repository;
    }

    public async Task<BoardingPassAggregate?> ExecuteAsync(
        int               checkInId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByCheckInAsync(checkInId, cancellationToken);
}
```

---

### RUTA: `src/Modules/BoardingPass/Application/Services/BoardingPassService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;

public sealed class BoardingPassService : IBoardingPassService
{
    private readonly CreateBoardingPassUseCase      _create;
    private readonly DeleteBoardingPassUseCase      _delete;
    private readonly GetAllBoardingPassesUseCase    _getAll;
    private readonly GetBoardingPassByIdUseCase     _getById;
    private readonly UpdateBoardingPassUseCase      _update;
    private readonly GetBoardingPassByCheckInUseCase _getByCheckIn;

    public BoardingPassService(
        CreateBoardingPassUseCase       create,
        DeleteBoardingPassUseCase       delete,
        GetAllBoardingPassesUseCase     getAll,
        GetBoardingPassByIdUseCase      getById,
        UpdateBoardingPassUseCase       update,
        GetBoardingPassByCheckInUseCase getByCheckIn)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _update       = update;
        _getByCheckIn = getByCheckIn;
    }

    public async Task<BoardingPassDto> CreateAsync(
        int               checkInId,
        int?              gateId,
        string?           boardingGroup,
        int               flightSeatId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            checkInId, gateId, boardingGroup, flightSeatId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<BoardingPassDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<BoardingPassDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        int?              gateId,
        string?           boardingGroup,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, gateId, boardingGroup, cancellationToken);

    public async Task<BoardingPassDto?> GetByCheckInAsync(
        int               checkInId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByCheckIn.ExecuteAsync(checkInId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static BoardingPassDto ToDto(BoardingPassAggregate agg)
        => new(agg.Id.Value, agg.CheckInId, agg.GateId, agg.BoardingGroup, agg.FlightSeatId);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/BoardingPass/Infrastructure/entity/BoardingPassEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Infrastructure.Entity;

public sealed class BoardingPassEntity
{
    public int     Id            { get; set; }
    public int     CheckInId     { get; set; }
    public int?    GateId        { get; set; }
    public string? BoardingGroup { get; set; }
    public int     FlightSeatId  { get; set; }
}
```

---

### RUTA: `src/Modules/BoardingPass/Infrastructure/entity/BoardingPassEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BoardingPassEntityConfiguration : IEntityTypeConfiguration<BoardingPassEntity>
{
    public void Configure(EntityTypeBuilder<BoardingPassEntity> builder)
    {
        builder.ToTable("boarding_pass");

        builder.HasKey(e => e.Id);

        // PK en SQL es boarding_pass_id [NC-1]
        builder.Property(e => e.Id)
               .HasColumnName("boarding_pass_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.CheckInId)
               .HasColumnName("check_in_id")
               .IsRequired();

        // UNIQUE (check_in_id) — un boarding pass por check-in
        builder.HasIndex(e => e.CheckInId)
               .IsUnique()
               .HasDatabaseName("uq_boarding_pass_check_in");

        builder.Property(e => e.GateId)
               .HasColumnName("gate_id")
               .IsRequired(false);

        builder.Property(e => e.BoardingGroup)
               .HasColumnName("boarding_group")
               .IsRequired(false)
               .HasMaxLength(10);

        // [IR-4] FK → flight_seat (reemplazó seat_confirmed VARCHAR)
        builder.Property(e => e.FlightSeatId)
               .HasColumnName("flight_seat_id")
               .IsRequired();
    }
}
```

---

### RUTA: `src/Modules/BoardingPass/Infrastructure/repository/BoardingPassRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class BoardingPassRepository : IBoardingPassRepository
{
    private readonly AppDbContext _context;

    public BoardingPassRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static BoardingPassAggregate ToDomain(BoardingPassEntity entity)
        => new(
            new BoardingPassId(entity.Id),
            entity.CheckInId,
            entity.GateId,
            entity.BoardingGroup,
            entity.FlightSeatId);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<BoardingPassAggregate?> GetByIdAsync(
        BoardingPassId    id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BoardingPasses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<BoardingPassAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.BoardingPasses
            .AsNoTracking()
            .OrderBy(e => e.CheckInId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<BoardingPassAggregate?> GetByCheckInAsync(
        int               checkInId,
        CancellationToken cancellationToken = default)
    {
        // check_in_id es UNIQUE — FirstOrDefault es correcto.
        var entity = await _context.BoardingPasses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CheckInId == checkInId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        BoardingPassAggregate boardingPass,
        CancellationToken     cancellationToken = default)
    {
        var entity = new BoardingPassEntity
        {
            CheckInId     = boardingPass.CheckInId,
            GateId        = boardingPass.GateId,
            BoardingGroup = boardingPass.BoardingGroup,
            FlightSeatId  = boardingPass.FlightSeatId
        };
        await _context.BoardingPasses.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        BoardingPassAggregate boardingPass,
        CancellationToken     cancellationToken = default)
    {
        var entity = await _context.BoardingPasses
            .FirstOrDefaultAsync(e => e.Id == boardingPass.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BoardingPassEntity with id {boardingPass.Id.Value} not found.");

        // Solo GateId y BoardingGroup son mutables (cambios operativos de última hora).
        // CheckInId y FlightSeatId son inmutables.
        entity.GateId        = boardingPass.GateId;
        entity.BoardingGroup = boardingPass.BoardingGroup;

        _context.BoardingPasses.Update(entity);
    }

    public async Task DeleteAsync(
        BoardingPassId    id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BoardingPasses
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BoardingPassEntity with id {id.Value} not found.");

        _context.BoardingPasses.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/BoardingPass/UI/BoardingPassConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.Interfaces;

public sealed class BoardingPassConsoleUI
{
    private readonly IBoardingPassService _service;

    public BoardingPassConsoleUI(IBoardingPassService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== BOARDING PASS MODULE ==========");
            Console.WriteLine("1. List all boarding passes");
            Console.WriteLine("2. Get boarding pass by ID");
            Console.WriteLine("3. Get boarding pass by check-in");
            Console.WriteLine("4. Issue boarding pass");
            Console.WriteLine("5. Update gate and boarding group");
            Console.WriteLine("6. Delete boarding pass");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();         break;
                case "2": await GetByIdAsync();         break;
                case "3": await GetByCheckInAsync();    break;
                case "4": await IssueAsync();           break;
                case "5": await UpdateAsync();          break;
                case "6": await DeleteAsync();          break;
                case "0": running = false;              break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var passes = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Boarding Passes ---");
        foreach (var p in passes) PrintPass(p);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter boarding pass ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var p = await _service.GetByIdAsync(id);
        if (p is null) Console.WriteLine($"Boarding pass with ID {id} not found.");
        else           PrintPass(p);
    }

    private async Task GetByCheckInAsync()
    {
        Console.Write("Enter Check-In ID: ");
        if (!int.TryParse(Console.ReadLine(), out int checkInId))
        { Console.WriteLine("Invalid ID."); return; }

        var p = await _service.GetByCheckInAsync(checkInId);
        if (p is null)
            Console.WriteLine($"No boarding pass for check-in {checkInId} yet.");
        else
            PrintPass(p);
    }

    private async Task IssueAsync()
    {
        Console.Write("Check-In ID: ");
        if (!int.TryParse(Console.ReadLine(), out int checkInId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Flight Seat ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightSeatId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Gate ID (optional): ");
        var gateInput = Console.ReadLine()?.Trim();
        int? gateId = int.TryParse(gateInput, out int gParsed) ? gParsed : null;

        Console.Write("Boarding group (optional, e.g. A, B, 1): ");
        var groupInput = Console.ReadLine()?.Trim();
        string? group = string.IsNullOrWhiteSpace(groupInput) ? null : groupInput;

        try
        {
            var created = await _service.CreateAsync(checkInId, gateId, group, flightSeatId);
            Console.WriteLine($"Boarding pass issued: [{created.Id}]");
            PrintPass(created);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Boarding pass ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("New Gate ID (optional, Enter to clear): ");
        var gateInput = Console.ReadLine()?.Trim();
        int? gateId = int.TryParse(gateInput, out int gParsed) ? gParsed : null;

        Console.Write("New boarding group (optional, Enter to clear): ");
        var groupInput = Console.ReadLine()?.Trim();
        string? group = string.IsNullOrWhiteSpace(groupInput) ? null : groupInput;

        try
        {
            await _service.UpdateAsync(id, gateId, group);
            Console.WriteLine("Boarding pass updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Boarding pass ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Boarding pass deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintPass(BoardingPassDto p)
        => Console.WriteLine(
            $"  [{p.Id}] CheckIn:{p.CheckInId} | Seat:{p.FlightSeatId} | " +
            $"Gate:{(p.GateId.HasValue ? p.GateId.ToString() : "N/A")} | " +
            $"Group:{p.BoardingGroup ?? "N/A"}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── BoardingPass Module ───────────────────────────────────────────────────────
builder.Services.AddScoped<IBoardingPassRepository, BoardingPassRepository>();
builder.Services.AddScoped<CreateBoardingPassUseCase>();
builder.Services.AddScoped<DeleteBoardingPassUseCase>();
builder.Services.AddScoped<GetAllBoardingPassesUseCase>();
builder.Services.AddScoped<GetBoardingPassByIdUseCase>();
builder.Services.AddScoped<UpdateBoardingPassUseCase>();
builder.Services.AddScoped<GetBoardingPassByCheckInUseCase>();
builder.Services.AddScoped<IBoardingPassService, BoardingPassService>();
builder.Services.AddScoped<BoardingPassConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/BoardingPass/
├── Application/
│   ├── Interfaces/
│   │   └── IBoardingPassService.cs
│   ├── Services/
│   │   └── BoardingPassService.cs
│   └── UseCases/
│       ├── CreateBoardingPassUseCase.cs
│       ├── DeleteBoardingPassUseCase.cs
│       ├── GetAllBoardingPassesUseCase.cs
│       ├── GetBoardingPassByCheckInUseCase.cs
│       ├── GetBoardingPassByIdUseCase.cs
│       └── UpdateBoardingPassUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── BoardingPassAggregate.cs
│   ├── Repositories/
│   │   └── IBoardingPassRepository.cs
│   └── valueObject/
│       └── BoardingPassId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── BoardingPassEntity.cs
│   │   └── BoardingPassEntityConfiguration.cs
│   └── repository/
│       └── BoardingPassRepository.cs
└── UI/
    └── BoardingPassConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo BoardingPass_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
