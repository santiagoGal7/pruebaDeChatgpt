namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.ValueObject;

public interface IAircraftRepository
{
    Task<AircraftAggregate?> GetByIdAsync(AircraftId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AircraftAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AircraftAggregate aircraft, CancellationToken cancellationToken = default);
    void Update(AircraftAggregate aircraft);
    void Delete(AircraftAggregate aircraft);
}
