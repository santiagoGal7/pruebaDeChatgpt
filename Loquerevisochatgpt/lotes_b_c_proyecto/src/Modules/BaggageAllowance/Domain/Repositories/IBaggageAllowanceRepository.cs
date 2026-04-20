namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;

public interface IBaggageAllowanceRepository
{
    Task<BaggageAllowanceAggregate?>             GetByIdAsync(BaggageAllowanceId id,                            CancellationToken cancellationToken = default);
    Task<IEnumerable<BaggageAllowanceAggregate>> GetAllAsync(                                                    CancellationToken cancellationToken = default);
    Task<BaggageAllowanceAggregate?>             GetByCabinAndFareAsync(int cabinClassId, int fareTypeId,        CancellationToken cancellationToken = default);
    Task                                         AddAsync(BaggageAllowanceAggregate baggageAllowance,           CancellationToken cancellationToken = default);
    Task                                         UpdateAsync(BaggageAllowanceAggregate baggageAllowance,        CancellationToken cancellationToken = default);
    Task                                         DeleteAsync(BaggageAllowanceId id,                             CancellationToken cancellationToken = default);
}
