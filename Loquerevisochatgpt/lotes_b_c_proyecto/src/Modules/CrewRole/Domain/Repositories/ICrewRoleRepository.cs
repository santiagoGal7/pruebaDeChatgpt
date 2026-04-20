namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.ValueObject;

public interface ICrewRoleRepository
{
    Task<CrewRoleAggregate?>          GetByIdAsync(CrewRoleId id, CancellationToken ct = default);
    Task<IReadOnlyList<CrewRoleAggregate>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(CrewRoleAggregate entity, CancellationToken ct = default);
    void Update(CrewRoleAggregate entity);
    void Delete(CrewRoleAggregate entity);
}
