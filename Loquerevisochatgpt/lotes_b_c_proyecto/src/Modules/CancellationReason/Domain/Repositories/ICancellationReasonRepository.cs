namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;

public interface ICancellationReasonRepository
{
    Task<CancellationReasonAggregate?>             GetByIdAsync(CancellationReasonId id,                      CancellationToken cancellationToken = default);
    Task<IEnumerable<CancellationReasonAggregate>> GetAllAsync(                                                CancellationToken cancellationToken = default);
    Task                                           AddAsync(CancellationReasonAggregate cancellationReason,   CancellationToken cancellationToken = default);
    Task                                           UpdateAsync(CancellationReasonAggregate cancellationReason,CancellationToken cancellationToken = default);
    Task                                           DeleteAsync(CancellationReasonId id,                        CancellationToken cancellationToken = default);
}
