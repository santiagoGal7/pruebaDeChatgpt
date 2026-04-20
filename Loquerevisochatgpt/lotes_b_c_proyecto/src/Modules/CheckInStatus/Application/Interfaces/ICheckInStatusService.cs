namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.Interfaces;

public interface ICheckInStatusService
{
    Task<CheckInStatusDto?>             GetByIdAsync(int id,            CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInStatusDto>> GetAllAsync(                    CancellationToken cancellationToken = default);
    Task<CheckInStatusDto>              CreateAsync(string name,        CancellationToken cancellationToken = default);
    Task                                UpdateAsync(int id, string name,CancellationToken cancellationToken = default);
    Task                                DeleteAsync(int id,             CancellationToken cancellationToken = default);
}

public sealed record CheckInStatusDto(int Id, string Name);
