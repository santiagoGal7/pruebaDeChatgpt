namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;

public interface IBaggageTypeRepository
{
    Task<BaggageTypeAggregate?>             GetByIdAsync(BaggageTypeId id,               CancellationToken cancellationToken = default);
    Task<IEnumerable<BaggageTypeAggregate>> GetAllAsync(                                  CancellationToken cancellationToken = default);
    Task                                    AddAsync(BaggageTypeAggregate baggageType,    CancellationToken cancellationToken = default);
    Task                                    UpdateAsync(BaggageTypeAggregate baggageType, CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(BaggageTypeId id,                 CancellationToken cancellationToken = default);
}
