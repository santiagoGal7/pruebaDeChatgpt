namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.Interfaces;
public sealed class DeleteCrewRoleUseCase
{
    private readonly ICrewRoleService _service;
    public DeleteCrewRoleUseCase(ICrewRoleService service) => _service = service;
    public Task ExecuteAsync(int id, CancellationToken ct = default)
        => _service.DeleteAsync(id, ct);
}
