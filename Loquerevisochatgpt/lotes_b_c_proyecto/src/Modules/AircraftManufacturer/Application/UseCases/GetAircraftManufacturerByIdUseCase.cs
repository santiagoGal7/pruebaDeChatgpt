namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Interfaces;
public sealed class GetAircraftManufacturerByIdUseCase
{
    private readonly IAircraftManufacturerService _service;
    public GetAircraftManufacturerByIdUseCase(IAircraftManufacturerService service) => _service = service;
    public Task<AircraftManufacturerDto?> ExecuteAsync(int id, CancellationToken ct = default) => _service.GetByIdAsync(id, ct);
}
