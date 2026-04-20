namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Interfaces;
public sealed class GetAircraftTypeByIdUseCase
{
    private readonly IAircraftTypeService _service;
    public GetAircraftTypeByIdUseCase(IAircraftTypeService service) => _service = service;
    public Task<AircraftTypeDto?> ExecuteAsync(int id, CancellationToken ct = default) => _service.GetByIdAsync(id, ct);
}
