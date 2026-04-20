namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CrewRoleRepository : ICrewRoleRepository
{
    private readonly AppDbContext _context;
    public CrewRoleRepository(AppDbContext context) => _context = context;

    public async Task<CrewRoleAggregate?> GetByIdAsync(CrewRoleId id, CancellationToken ct = default)
    {
        var e = await _context.CrewRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CrewRoleId == id.Value, ct);
        return e is null ? null : ToAggregate(e);
    }

    public async Task<IReadOnlyList<CrewRoleAggregate>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _context.CrewRoles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return list.Select(ToAggregate).ToList();
    }

    public async Task AddAsync(CrewRoleAggregate entity, CancellationToken ct = default)
        => await _context.CrewRoles.AddAsync(ToEntity(entity), ct);

    public void Update(CrewRoleAggregate entity) => _context.CrewRoles.Update(ToEntity(entity));

    public void Delete(CrewRoleAggregate entity)
        => _context.CrewRoles.Remove(new CrewRoleEntity { CrewRoleId = entity.Id.Value });

    private static CrewRoleAggregate ToAggregate(CrewRoleEntity e) =>
        CrewRoleAggregate.Reconstitute(e.CrewRoleId, e.Name);

    private static CrewRoleEntity ToEntity(CrewRoleAggregate a) =>
        new() { CrewRoleId = a.Id.Value, Name = a.Name };
}
