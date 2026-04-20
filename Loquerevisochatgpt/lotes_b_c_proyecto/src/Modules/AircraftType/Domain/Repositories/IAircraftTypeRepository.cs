namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.ValueObject;

public interface IAircraftTypeRepository
{
    Task<AircraftTypeAggregate?> GetByIdAsync(AircraftTypeId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AircraftTypeAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AircraftTypeAggregate aircraftType, CancellationToken cancellationToken = default);
    void Update(AircraftTypeAggregate aircraftType);
    void Delete(AircraftTypeAggregate aircraftType);
}
