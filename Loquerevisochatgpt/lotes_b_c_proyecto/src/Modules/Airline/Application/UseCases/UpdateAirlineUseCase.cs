namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Interfaces;
public sealed class UpdateAirlineUseCase
{
    private readonly IAirlineService _service;
    public UpdateAirlineUseCase(IAirlineService service) => _service = service;
    public Task<AirlineDto> ExecuteAsync(int id, UpdateAirlineRequest request, CancellationToken cancellationToken = default)
        => _service.UpdateAsync(id, request, cancellationToken);
}
