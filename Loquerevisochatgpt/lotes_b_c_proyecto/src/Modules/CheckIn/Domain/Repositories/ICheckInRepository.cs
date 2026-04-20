namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;

public interface ICheckInRepository
{
    Task<CheckInAggregate?>             GetByIdAsync(CheckInId id,                         CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInAggregate>> GetAllAsync(                                        CancellationToken cancellationToken = default);
    Task<CheckInAggregate?>             GetByTicketAsync(int ticketId,                      CancellationToken cancellationToken = default);
    Task                                AddAsync(CheckInAggregate checkIn,                  CancellationToken cancellationToken = default);
    Task                                UpdateAsync(CheckInAggregate checkIn,               CancellationToken cancellationToken = default);
    Task                                DeleteAsync(CheckInId id,                           CancellationToken cancellationToken = default);
}
