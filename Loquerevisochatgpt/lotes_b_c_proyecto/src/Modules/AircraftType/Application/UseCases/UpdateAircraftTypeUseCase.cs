namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Interfaces;
public sealed class UpdateAircraftTypeUseCase
{
    private readonly IAircraftTypeService _service;
    public UpdateAircraftTypeUseCase(IAircraftTypeService service) => _service = service;
    public Task<AircraftTypeDto> ExecuteAsync(int id, UpdateAircraftTypeRequest r, CancellationToken ct = default) => _service.UpdateAsync(id, r, ct);
}
