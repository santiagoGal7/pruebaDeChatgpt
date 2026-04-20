namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.Interfaces;
public sealed class GetCrewRoleByIdUseCase
{
    private readonly ICrewRoleService _service;
    public GetCrewRoleByIdUseCase(ICrewRoleService service) => _service = service;
    public Task<CrewRoleDto?> ExecuteAsync(int id, CancellationToken ct = default)
        => _service.GetByIdAsync(id, ct);
}
