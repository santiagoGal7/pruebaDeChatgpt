namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Interfaces;
public sealed class DeleteAirlineUseCase
{
    private readonly IAirlineService _service;
    public DeleteAirlineUseCase(IAirlineService service) => _service = service;
    public Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => _service.DeleteAsync(id, cancellationToken);
}
