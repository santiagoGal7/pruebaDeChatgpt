namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Interfaces;
public sealed class CreateAirlineUseCase
{
    private readonly IAirlineService _service;
    public CreateAirlineUseCase(IAirlineService service) => _service = service;
    public Task<AirlineDto> ExecuteAsync(CreateAirlineRequest request, CancellationToken cancellationToken = default)
        => _service.CreateAsync(request, cancellationToken);
}
