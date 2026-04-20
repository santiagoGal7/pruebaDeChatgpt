namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.Interfaces;

public interface IBaggageAllowanceService
{
    Task<BaggageAllowanceDto?>             GetByIdAsync(int id,                                                                              CancellationToken cancellationToken = default);
    Task<IEnumerable<BaggageAllowanceDto>> GetAllAsync(                                                                                      CancellationToken cancellationToken = default);
    Task<BaggageAllowanceDto?>             GetByCabinAndFareAsync(int cabinClassId, int fareTypeId,                                          CancellationToken cancellationToken = default);
    Task<BaggageAllowanceDto>              CreateAsync(int cabinClassId, int fareTypeId, int carryOnPieces, decimal carryOnKg, int checkedPieces, decimal checkedKg, CancellationToken cancellationToken = default);
    Task                                   UpdateAsync(int id, int carryOnPieces, decimal carryOnKg, int checkedPieces, decimal checkedKg,   CancellationToken cancellationToken = default);
    Task                                   DeleteAsync(int id,                                                                               CancellationToken cancellationToken = default);
}

public sealed record BaggageAllowanceDto(
    int     Id,
    int     CabinClassId,
    int     FareTypeId,
    int     CarryOnPieces,
    decimal CarryOnKg,
    int     CheckedPieces,
    decimal CheckedKg);
