namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.Interfaces;

public interface IBoardingPassService
{
    Task<BoardingPassDto?>             GetByIdAsync(int id,                                                           CancellationToken cancellationToken = default);
    Task<IEnumerable<BoardingPassDto>> GetAllAsync(                                                                   CancellationToken cancellationToken = default);
    Task<BoardingPassDto?>             GetByCheckInAsync(int checkInId,                                               CancellationToken cancellationToken = default);
    Task<BoardingPassDto>              CreateAsync(int checkInId, int? gateId, string? boardingGroup, int flightSeatId, CancellationToken cancellationToken = default);
    Task                               UpdateAsync(int id, int? gateId, string? boardingGroup,                       CancellationToken cancellationToken = default);
    Task                               DeleteAsync(int id,                                                           CancellationToken cancellationToken = default);
}

public sealed record BoardingPassDto(
    int     Id,
    int     CheckInId,
    int?    GateId,
    string? BoardingGroup,
    int     FlightSeatId);
