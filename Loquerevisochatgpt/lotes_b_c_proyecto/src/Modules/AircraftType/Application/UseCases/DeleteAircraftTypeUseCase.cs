namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Interfaces;
public sealed class DeleteAircraftTypeUseCase
{
    private readonly IAircraftTypeService _service;
    public DeleteAircraftTypeUseCase(IAircraftTypeService service) => _service = service;
    public Task ExecuteAsync(int id, CancellationToken ct = default) => _service.DeleteAsync(id, ct);
}
