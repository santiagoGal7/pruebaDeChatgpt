namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Interfaces;

/// <summary>Caso de uso: Obtener aeropuerto por ID.</summary>
public sealed class GetAirportByIdUseCase
{
    private readonly IAirportService _service;
    public GetAirportByIdUseCase(IAirportService service) => _service = service;

    public Task<AirportDto?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => _service.GetByIdAsync(id, cancellationToken);
}
