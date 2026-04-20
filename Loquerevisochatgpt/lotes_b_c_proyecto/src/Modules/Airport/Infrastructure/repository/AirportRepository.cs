namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

/// <summary>Repositorio EF Core para Airport.</summary>
public sealed class AirportRepository : IAirportRepository
{
    private readonly AppDbContext _context;
    public AirportRepository(AppDbContext context) => _context = context;

    public async Task<AirportAggregate?> GetByIdAsync(AirportId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Airports
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.AirportId == id.Value, cancellationToken);
        return entity is null ? null : MapToAggregate(entity);
    }

    public async Task<IReadOnlyList<AirportAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Airports
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
        return entities.Select(MapToAggregate).ToList();
    }

    public async Task AddAsync(AirportAggregate airport, CancellationToken cancellationToken = default)
        => await _context.Airports.AddAsync(MapToEntity(airport), cancellationToken);

    public void Update(AirportAggregate airport)
        => _context.Airports.Update(MapToEntity(airport));

    public void Delete(AirportAggregate airport)
        => _context.Airports.Remove(new AirportEntity { AirportId = airport.Id.Value });

    private static AirportAggregate MapToAggregate(AirportEntity e) =>
        AirportAggregate.Reconstitute(e.AirportId, e.IataCode, e.Name, e.CityId, e.CreatedAt);

    private static AirportEntity MapToEntity(AirportAggregate a) => new()
    {
        AirportId = a.Id.Value,
        IataCode  = a.IataCode,
        Name      = a.Name,
        CityId    = a.CityId,
        CreatedAt = a.CreatedAt
    };
}
