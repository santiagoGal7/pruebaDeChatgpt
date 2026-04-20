namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.Interfaces;
public sealed class GetAllCrewRolesUseCase
{
    private readonly ICrewRoleService _service;
    public GetAllCrewRolesUseCase(ICrewRoleService service) => _service = service;
    public Task<IReadOnlyList<CrewRoleDto>> ExecuteAsync(CancellationToken ct = default)
        => _service.GetAllAsync(ct);
}
