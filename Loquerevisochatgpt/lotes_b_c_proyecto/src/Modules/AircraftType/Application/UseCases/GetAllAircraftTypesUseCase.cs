namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Interfaces;
public sealed class GetAllAircraftTypesUseCase
{
    private readonly IAircraftTypeService _service;
    public GetAllAircraftTypesUseCase(IAircraftTypeService service) => _service = service;
    public Task<IReadOnlyList<AircraftTypeDto>> ExecuteAsync(CancellationToken ct = default) => _service.GetAllAsync(ct);
}
