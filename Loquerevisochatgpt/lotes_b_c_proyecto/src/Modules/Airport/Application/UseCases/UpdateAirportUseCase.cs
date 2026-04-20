namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Interfaces;

/// <summary>Caso de uso: Actualizar aeropuerto.</summary>
public sealed class UpdateAirportUseCase
{
    private readonly IAirportService _service;
    public UpdateAirportUseCase(IAirportService service) => _service = service;

    public Task<AirportDto> ExecuteAsync(int id, UpdateAirportRequest request, CancellationToken cancellationToken = default)
        => _service.UpdateAsync(id, request, cancellationToken);
}
