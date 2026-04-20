namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.Interfaces;

public interface ICrewRoleService
{
    Task<CrewRoleDto>               CreateAsync(CreateCrewRoleRequest  request, CancellationToken ct = default);
    Task<CrewRoleDto?>              GetByIdAsync(int id,                        CancellationToken ct = default);
    Task<IReadOnlyList<CrewRoleDto>> GetAllAsync(CancellationToken ct = default);
    Task<CrewRoleDto>               UpdateAsync(int id, UpdateCrewRoleRequest request, CancellationToken ct = default);
    Task                            DeleteAsync(int id, CancellationToken ct = default);
}

public sealed record CrewRoleDto(int CrewRoleId, string Name);
public sealed record CreateCrewRoleRequest(string Name);
public sealed record UpdateCrewRoleRequest(string Name);
