namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Interfaces;
public sealed class GetAllAircraftManufacturersUseCase
{
    private readonly IAircraftManufacturerService _service;
    public GetAllAircraftManufacturersUseCase(IAircraftManufacturerService service) => _service = service;
    public Task<IReadOnlyList<AircraftManufacturerDto>> ExecuteAsync(CancellationToken ct = default) => _service.GetAllAsync(ct);
}
