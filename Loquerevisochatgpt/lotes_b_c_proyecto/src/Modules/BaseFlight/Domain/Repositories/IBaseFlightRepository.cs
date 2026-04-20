namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;

public interface IBaseFlightRepository
{
    Task<BaseFlightAggregate?>             GetByIdAsync(BaseFlightId id,              CancellationToken cancellationToken = default);
    Task<IEnumerable<BaseFlightAggregate>> GetAllAsync(                               CancellationToken cancellationToken = default);
    Task                                   AddAsync(BaseFlightAggregate baseFlight,   CancellationToken cancellationToken = default);
    Task                                   UpdateAsync(BaseFlightAggregate baseFlight,CancellationToken cancellationToken = default);
    Task                                   DeleteAsync(BaseFlightId id,               CancellationToken cancellationToken = default);
}
