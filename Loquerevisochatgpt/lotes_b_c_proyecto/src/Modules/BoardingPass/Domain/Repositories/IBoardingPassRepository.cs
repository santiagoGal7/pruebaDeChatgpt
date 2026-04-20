namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;

public interface IBoardingPassRepository
{
    Task<BoardingPassAggregate?>             GetByIdAsync(BoardingPassId id,                      CancellationToken cancellationToken = default);
    Task<IEnumerable<BoardingPassAggregate>> GetAllAsync(                                          CancellationToken cancellationToken = default);
    Task<BoardingPassAggregate?>             GetByCheckInAsync(int checkInId,                      CancellationToken cancellationToken = default);
    Task                                     AddAsync(BoardingPassAggregate boardingPass,          CancellationToken cancellationToken = default);
    Task                                     UpdateAsync(BoardingPassAggregate boardingPass,       CancellationToken cancellationToken = default);
    Task                                     DeleteAsync(BoardingPassId id,                        CancellationToken cancellationToken = default);
}
