namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;

public interface IDelayReasonRepository
{
    Task<DelayReasonAggregate?>             GetByIdAsync(DelayReasonId id,               CancellationToken cancellationToken = default);
    Task<IEnumerable<DelayReasonAggregate>> GetAllAsync(                                  CancellationToken cancellationToken = default);
    Task                                    AddAsync(DelayReasonAggregate delayReason,    CancellationToken cancellationToken = default);
    Task                                    UpdateAsync(DelayReasonAggregate delayReason, CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(DelayReasonId id,                 CancellationToken cancellationToken = default);
}
