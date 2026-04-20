namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Interfaces;
public sealed class UpdateAircraftUseCase
{
    private readonly IAircraftService _service;
    public UpdateAircraftUseCase(IAircraftService service) => _service = service;
    public Task<AircraftDto> ExecuteAsync(int id, UpdateAircraftRequest r, CancellationToken ct = default) => _service.UpdateAsync(id, r, ct);
}
