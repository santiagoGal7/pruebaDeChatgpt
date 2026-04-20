namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class AircraftRepository : IAircraftRepository
{
    private readonly AppDbContext _context;
    public AircraftRepository(AppDbContext context) => _context = context;

    public async Task<AircraftAggregate?> GetByIdAsync(AircraftId id, CancellationToken ct = default)
    {
        var e = await _context.Aircrafts.AsNoTracking().FirstOrDefaultAsync(x => x.AircraftId == id.Value, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<AircraftAggregate>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _context.Aircrafts.AsNoTracking().OrderBy(x => x.RegistrationNumber).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task AddAsync(AircraftAggregate a, CancellationToken ct = default)
        => await _context.Aircrafts.AddAsync(ToEntity(a), ct);

    public void Update(AircraftAggregate a) => _context.Aircrafts.Update(ToEntity(a));
    public void Delete(AircraftAggregate a) => _context.Aircrafts.Remove(new AircraftEntity { AircraftId = a.Id.Value });

    private static AircraftAggregate Map(AircraftEntity e) =>
        AircraftAggregate.Reconstitute(e.AircraftId, e.AirlineId, e.AircraftTypeId, e.RegistrationNumber, e.ManufactureYear, e.IsActive, e.CreatedAt, e.UpdatedAt);

    private static AircraftEntity ToEntity(AircraftAggregate a) => new()
    {
        AircraftId = a.Id.Value, AirlineId = a.AirlineId, AircraftTypeId = a.AircraftTypeId,
        RegistrationNumber = a.RegistrationNumber, ManufactureYear = a.ManufactureYear,
        IsActive = a.IsActive, CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt
    };
}
