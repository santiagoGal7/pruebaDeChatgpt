namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.Interfaces;

public interface IDelayReasonService
{
    Task<DelayReasonDto?>             GetByIdAsync(int id,                           CancellationToken cancellationToken = default);
    Task<IEnumerable<DelayReasonDto>> GetAllAsync(                                   CancellationToken cancellationToken = default);
    Task<DelayReasonDto>              CreateAsync(string name, string category,      CancellationToken cancellationToken = default);
    Task                              UpdateAsync(int id, string name, string category, CancellationToken cancellationToken = default);
    Task                              DeleteAsync(int id,                            CancellationToken cancellationToken = default);
}

public sealed record DelayReasonDto(int Id, string Name, string Category);
