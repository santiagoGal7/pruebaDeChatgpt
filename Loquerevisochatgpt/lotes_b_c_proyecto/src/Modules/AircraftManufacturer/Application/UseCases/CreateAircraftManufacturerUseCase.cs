namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Interfaces;
public sealed class CreateAircraftManufacturerUseCase
{
    private readonly IAircraftManufacturerService _service;
    public CreateAircraftManufacturerUseCase(IAircraftManufacturerService service) => _service = service;
    public Task<AircraftManufacturerDto> ExecuteAsync(CreateAircraftManufacturerRequest r, CancellationToken ct = default) => _service.CreateAsync(r, ct);
}
