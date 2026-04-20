namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.Interfaces;
public sealed class CreateCrewRoleUseCase
{
    private readonly ICrewRoleService _service;
    public CreateCrewRoleUseCase(ICrewRoleService service) => _service = service;
    public Task<CrewRoleDto> ExecuteAsync(CreateCrewRoleRequest request, CancellationToken ct = default)
        => _service.CreateAsync(request, ct);
}
