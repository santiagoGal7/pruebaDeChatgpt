namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.Interfaces;
public sealed class UpdateCrewRoleUseCase
{
    private readonly ICrewRoleService _service;
    public UpdateCrewRoleUseCase(ICrewRoleService service) => _service = service;
    public Task<CrewRoleDto> ExecuteAsync(int id, UpdateCrewRoleRequest request, CancellationToken ct = default)
        => _service.UpdateAsync(id, request, ct);
}
