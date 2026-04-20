namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Interfaces;
public sealed class CreateAircraftUseCase
{
    private readonly IAircraftService _service;
    public CreateAircraftUseCase(IAircraftService service) => _service = service;
    public Task<AircraftDto> ExecuteAsync(CreateAircraftRequest r, CancellationToken ct = default) => _service.CreateAsync(r, ct);
}
