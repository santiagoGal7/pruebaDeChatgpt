namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Interfaces;
public sealed class CreateAircraftTypeUseCase
{
    private readonly IAircraftTypeService _service;
    public CreateAircraftTypeUseCase(IAircraftTypeService service) => _service = service;
    public Task<AircraftTypeDto> ExecuteAsync(CreateAircraftTypeRequest r, CancellationToken ct = default) => _service.CreateAsync(r, ct);
}
