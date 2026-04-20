namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Interfaces;

/// <summary>Caso de uso: Crear aeropuerto.</summary>
public sealed class CreateAirportUseCase
{
    private readonly IAirportService _service;
    public CreateAirportUseCase(IAirportService service) => _service = service;

    public Task<AirportDto> ExecuteAsync(CreateAirportRequest request, CancellationToken cancellationToken = default)
        => _service.CreateAsync(request, cancellationToken);
}
