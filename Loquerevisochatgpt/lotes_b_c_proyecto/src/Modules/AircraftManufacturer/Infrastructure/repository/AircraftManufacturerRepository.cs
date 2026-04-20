namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class AircraftManufacturerRepository : IAircraftManufacturerRepository
{
    private readonly AppDbContext _context;
    public AircraftManufacturerRepository(AppDbContext context) => _context = context;

    public async Task<AircraftManufacturerAggregate?> GetByIdAsync(AircraftManufacturerId id, CancellationToken ct = default)
    {
        var e = await _context.AircraftManufacturers.AsNoTracking().FirstOrDefaultAsync(x => x.ManufacturerId == id.Value, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<AircraftManufacturerAggregate>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _context.AircraftManufacturers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task AddAsync(AircraftManufacturerAggregate m, CancellationToken ct = default)
        => await _context.AircraftManufacturers.AddAsync(ToEntity(m), ct);

    public void Update(AircraftManufacturerAggregate m) => _context.AircraftManufacturers.Update(ToEntity(m));
    public void Delete(AircraftManufacturerAggregate m) => _context.AircraftManufacturers.Remove(new AircraftManufacturerEntity { ManufacturerId = m.Id.Value });

    private static AircraftManufacturerAggregate Map(AircraftManufacturerEntity e) =>
        AircraftManufacturerAggregate.Reconstitute(e.ManufacturerId, e.Name, e.CountryId);

    private static AircraftManufacturerEntity ToEntity(AircraftManufacturerAggregate a) => new()
    {
        ManufacturerId = a.Id.Value, Name = a.Name, CountryId = a.CountryId
    };
}
