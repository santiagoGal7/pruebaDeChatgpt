namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.Interfaces;

public interface IBaseFlightService
{
    Task<BaseFlightDto?>             GetByIdAsync(int id,                                            CancellationToken cancellationToken = default);
    Task<IEnumerable<BaseFlightDto>> GetAllAsync(                                                    CancellationToken cancellationToken = default);
    Task<BaseFlightDto>              CreateAsync(string flightCode, int airlineId, int routeId,      CancellationToken cancellationToken = default);
    Task                             UpdateAsync(int id, string flightCode, int airlineId, int routeId, CancellationToken cancellationToken = default);
    Task                             DeleteAsync(int id,                                             CancellationToken cancellationToken = default);
}

public sealed record BaseFlightDto(
    int      Id,
    string   FlightCode,
    int      AirlineId,
    int      RouteId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
