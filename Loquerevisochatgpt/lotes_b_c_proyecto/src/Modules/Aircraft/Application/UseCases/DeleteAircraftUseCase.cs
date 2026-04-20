namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Interfaces;
public sealed class DeleteAircraftUseCase
{
    private readonly IAircraftService _service;
    public DeleteAircraftUseCase(IAircraftService service) => _service = service;
    public Task ExecuteAsync(int id, CancellationToken ct = default) => _service.DeleteAsync(id, ct);
}
