namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class AirlineRepository : IAirlineRepository
{
    private readonly AppDbContext _context;
    public AirlineRepository(AppDbContext context) => _context = context;

    public async Task<AirlineAggregate?> GetByIdAsync(AirlineId id, CancellationToken cancellationToken = default)
    {
        var e = await _context.Airlines.AsNoTracking().FirstOrDefaultAsync(x => x.AirlineId == id.Value, cancellationToken);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<AirlineAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _context.Airlines.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task AddAsync(AirlineAggregate airline, CancellationToken cancellationToken = default)
        => await _context.Airlines.AddAsync(ToEntity(airline), cancellationToken);

    public void Update(AirlineAggregate airline) => _context.Airlines.Update(ToEntity(airline));
    public void Delete(AirlineAggregate airline) => _context.Airlines.Remove(new AirlineEntity { AirlineId = airline.Id.Value });

    private static AirlineAggregate Map(AirlineEntity e) =>
        AirlineAggregate.Reconstitute(e.AirlineId, e.IataCode, e.Name, e.IsActive, e.CreatedAt, e.UpdatedAt);

    private static AirlineEntity ToEntity(AirlineAggregate a) => new()
    {
        AirlineId = a.Id.Value, IataCode = a.IataCode, Name = a.Name,
        IsActive = a.IsActive, CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt
    };
}
