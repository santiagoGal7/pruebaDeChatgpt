namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;

public interface ICheckInStatusRepository
{
    Task<CheckInStatusAggregate?>             GetByIdAsync(CheckInStatusId id,                  CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInStatusAggregate>> GetAllAsync(                                       CancellationToken cancellationToken = default);
    Task                                      AddAsync(CheckInStatusAggregate checkInStatus,     CancellationToken cancellationToken = default);
    Task                                      UpdateAsync(CheckInStatusAggregate checkInStatus,  CancellationToken cancellationToken = default);
    Task                                      DeleteAsync(CheckInStatusId id,                    CancellationToken cancellationToken = default);
}
