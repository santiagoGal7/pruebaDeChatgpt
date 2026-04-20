namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.ValueObject;

public interface IAirlineRepository
{
    Task<AirlineAggregate?> GetByIdAsync(AirlineId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AirlineAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AirlineAggregate airline, CancellationToken cancellationToken = default);
    void Update(AirlineAggregate airline);
    void Delete(AirlineAggregate airline);
}
