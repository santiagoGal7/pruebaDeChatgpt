namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Interfaces;

/// <summary>Caso de uso: Obtener todos los aeropuertos.</summary>
public sealed class GetAllAirportsUseCase
{
    private readonly IAirportService _service;
    public GetAllAirportsUseCase(IAirportService service) => _service = service;

    public Task<IReadOnlyList<AirportDto>> ExecuteAsync(CancellationToken cancellationToken = default)
        => _service.GetAllAsync(cancellationToken);
}
