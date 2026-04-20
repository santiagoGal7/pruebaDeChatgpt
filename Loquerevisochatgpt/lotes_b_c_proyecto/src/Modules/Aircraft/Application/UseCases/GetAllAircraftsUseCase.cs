namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Interfaces;
public sealed class GetAllAircraftsUseCase
{
    private readonly IAircraftService _service;
    public GetAllAircraftsUseCase(IAircraftService service) => _service = service;
    public Task<IReadOnlyList<AircraftDto>> ExecuteAsync(CancellationToken ct = default) => _service.GetAllAsync(ct);
}
