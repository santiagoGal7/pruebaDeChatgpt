namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Interfaces;
public sealed class GetAirlineByIdUseCase
{
    private readonly IAirlineService _service;
    public GetAirlineByIdUseCase(IAirlineService service) => _service = service;
    public Task<AirlineDto?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => _service.GetByIdAsync(id, cancellationToken);
}
