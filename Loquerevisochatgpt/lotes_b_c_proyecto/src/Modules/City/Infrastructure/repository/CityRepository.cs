namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

/// <summary>
/// Implementación del repositorio de Ciudad usando EF Core + AppDbContext.
/// </summary>
public sealed class CityRepository : ICityRepository
{
    private readonly AppDbContext _context;

    public CityRepository(AppDbContext context) => _context = context;

    public async Task<CityAggregate?> GetByIdAsync(CityId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CityId == id.Value, cancellationToken);

        return entity is null ? null : MapToAggregate(entity);
    }

    public async Task<IReadOnlyList<CityAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Cities
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToAggregate).ToList();
    }

    public async Task AddAsync(CityAggregate city, CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(city);
        await _context.Cities.AddAsync(entity, cancellationToken);
    }

    public void Update(CityAggregate city)
    {
        var entity = MapToEntity(city);
        _context.Cities.Update(entity);
    }

    public void Delete(CityAggregate city)
    {
        var entity = new CityEntity { CityId = city.Id.Value };
        _context.Cities.Remove(entity);
    }

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static CityAggregate MapToAggregate(CityEntity e) =>
        CityAggregate.Reconstitute(e.CityId, e.Name, e.CountryId, e.CreatedAt);

    private static CityEntity MapToEntity(CityAggregate a) => new()
    {
        CityId    = a.Id.Value,
        Name      = a.Name,
        CountryId = a.CountryId,
        CreatedAt = a.CreatedAt
    };
}
