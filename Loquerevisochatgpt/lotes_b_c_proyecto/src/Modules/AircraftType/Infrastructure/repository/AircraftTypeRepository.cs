namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class AircraftTypeRepository : IAircraftTypeRepository
{
    private readonly AppDbContext _context;
    public AircraftTypeRepository(AppDbContext context) => _context = context;

    public async Task<AircraftTypeAggregate?> GetByIdAsync(AircraftTypeId id, CancellationToken ct = default)
    {
        var e = await _context.AircraftTypes.AsNoTracking().FirstOrDefaultAsync(x => x.AircraftTypeId == id.Value, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<AircraftTypeAggregate>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _context.AircraftTypes.AsNoTracking().OrderBy(x => x.Model).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task AddAsync(AircraftTypeAggregate at, CancellationToken ct = default)
        => await _context.AircraftTypes.AddAsync(ToEntity(at), ct);

    public void Update(AircraftTypeAggregate at) => _context.AircraftTypes.Update(ToEntity(at));
    public void Delete(AircraftTypeAggregate at) => _context.AircraftTypes.Remove(new AircraftTypeEntity { AircraftTypeId = at.Id.Value });

    private static AircraftTypeAggregate Map(AircraftTypeEntity e) =>
        AircraftTypeAggregate.Reconstitute(e.AircraftTypeId, e.ManufacturerId, e.Model, e.TotalSeats, e.CargoCapacityKg);

    private static AircraftTypeEntity ToEntity(AircraftTypeAggregate a) => new()
    {
        AircraftTypeId = a.Id.Value, ManufacturerId = a.ManufacturerId,
        Model = a.Model, TotalSeats = a.TotalSeats, CargoCapacityKg = a.CargoCapacityKg
    };
}
