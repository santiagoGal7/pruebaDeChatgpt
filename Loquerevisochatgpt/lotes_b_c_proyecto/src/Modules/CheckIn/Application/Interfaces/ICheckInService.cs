namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.Interfaces;

public interface ICheckInService
{
    Task<CheckInDto?>             GetByIdAsync(int id,                                                               CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInDto>> GetAllAsync(                                                                       CancellationToken cancellationToken = default);
    Task<CheckInDto?>             GetByTicketAsync(int ticketId,                                                     CancellationToken cancellationToken = default);
    Task<CheckInDto>              CreateAsync(int ticketId, int checkInStatusId, string? counterNumber,              CancellationToken cancellationToken = default);
    Task                          ChangeStatusAsync(int id, int checkInStatusId, string? counterNumber,             CancellationToken cancellationToken = default);
    Task                          DeleteAsync(int id,                                                               CancellationToken cancellationToken = default);
}

public sealed record CheckInDto(
    int      Id,
    int      TicketId,
    DateTime CheckInTime,
    int      CheckInStatusId,
    string?  CounterNumber);
