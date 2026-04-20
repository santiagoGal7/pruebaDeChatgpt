namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.Interfaces;

public interface IBaggageTypeService
{
    Task<BaggageTypeDto?>             GetByIdAsync(int id,                                                    CancellationToken cancellationToken = default);
    Task<IEnumerable<BaggageTypeDto>> GetAllAsync(                                                            CancellationToken cancellationToken = default);
    Task<BaggageTypeDto>              CreateAsync(string name, decimal maxWeightKg, decimal extraFee,        CancellationToken cancellationToken = default);
    Task                              UpdateAsync(int id, string name, decimal maxWeightKg, decimal extraFee, CancellationToken cancellationToken = default);
    Task                              DeleteAsync(int id,                                                     CancellationToken cancellationToken = default);
}

public sealed record BaggageTypeDto(int Id, string Name, decimal MaxWeightKg, decimal ExtraFee);
