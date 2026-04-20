namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.Interfaces;

public interface ICancellationReasonService
{
    Task<CancellationReasonDto?>             GetByIdAsync(int id,              CancellationToken cancellationToken = default);
    Task<IEnumerable<CancellationReasonDto>> GetAllAsync(                      CancellationToken cancellationToken = default);
    Task<CancellationReasonDto>              CreateAsync(string name,          CancellationToken cancellationToken = default);
    Task                                     UpdateAsync(int id, string name,  CancellationToken cancellationToken = default);
    Task                                     DeleteAsync(int id,               CancellationToken cancellationToken = default);
}

public sealed record CancellationReasonDto(int Id, string Name);
