namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Interfaces;
public sealed class UpdateAircraftManufacturerUseCase
{
    private readonly IAircraftManufacturerService _service;
    public UpdateAircraftManufacturerUseCase(IAircraftManufacturerService service) => _service = service;
    public Task<AircraftManufacturerDto> ExecuteAsync(int id, UpdateAircraftManufacturerRequest r, CancellationToken ct = default) => _service.UpdateAsync(id, r, ct);
}
