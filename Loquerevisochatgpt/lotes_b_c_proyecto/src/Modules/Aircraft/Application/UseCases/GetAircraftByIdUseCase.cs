namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Interfaces;
public sealed class GetAircraftByIdUseCase
{
    private readonly IAircraftService _service;
    public GetAircraftByIdUseCase(IAircraftService service) => _service = service;
    public Task<AircraftDto?> ExecuteAsync(int id, CancellationToken ct = default) => _service.GetByIdAsync(id, ct);
}
