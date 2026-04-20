namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Interfaces;

/// <summary>Caso de uso: Eliminar aeropuerto.</summary>
public sealed class DeleteAirportUseCase
{
    private readonly IAirportService _service;
    public DeleteAirportUseCase(IAirportService service) => _service = service;

    public Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => _service.DeleteAsync(id, cancellationToken);
}
