namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Interfaces;
public sealed class DeleteAircraftManufacturerUseCase
{
    private readonly IAircraftManufacturerService _service;
    public DeleteAircraftManufacturerUseCase(IAircraftManufacturerService service) => _service = service;
    public Task ExecuteAsync(int id, CancellationToken ct = default) => _service.DeleteAsync(id, ct);
}
