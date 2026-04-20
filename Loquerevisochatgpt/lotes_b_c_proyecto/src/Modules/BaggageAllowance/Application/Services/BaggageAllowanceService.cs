namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;

public sealed class BaggageAllowanceService : IBaggageAllowanceService
{
    private readonly CreateBaggageAllowanceUseCase             _create;
    private readonly DeleteBaggageAllowanceUseCase             _delete;
    private readonly GetAllBaggageAllowancesUseCase            _getAll;
    private readonly GetBaggageAllowanceByIdUseCase            _getById;
    private readonly UpdateBaggageAllowanceUseCase             _update;
    private readonly GetBaggageAllowanceByCabinAndFareUseCase  _getByCabinAndFare;

    public BaggageAllowanceService(
        CreateBaggageAllowanceUseCase            create,
        DeleteBaggageAllowanceUseCase            delete,
        GetAllBaggageAllowancesUseCase           getAll,
        GetBaggageAllowanceByIdUseCase           getById,
        UpdateBaggageAllowanceUseCase            update,
        GetBaggageAllowanceByCabinAndFareUseCase getByCabinAndFare)
    {
        _create            = create;
        _delete            = delete;
        _getAll            = getAll;
        _getById           = getById;
        _update            = update;
        _getByCabinAndFare = getByCabinAndFare;
    }

    public async Task<BaggageAllowanceDto> CreateAsync(
        int               cabinClassId,
        int               fareTypeId,
        int               carryOnPieces,
        decimal           carryOnKg,
        int               checkedPieces,
        decimal           checkedKg,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            cabinClassId, fareTypeId, carryOnPieces, carryOnKg, checkedPieces, checkedKg, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<BaggageAllowanceDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<BaggageAllowanceDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        int               carryOnPieces,
        decimal           carryOnKg,
        int               checkedPieces,
        decimal           checkedKg,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, carryOnPieces, carryOnKg, checkedPieces, checkedKg, cancellationToken);

    public async Task<BaggageAllowanceDto?> GetByCabinAndFareAsync(
        int               cabinClassId,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByCabinAndFare.ExecuteAsync(cabinClassId, fareTypeId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static BaggageAllowanceDto ToDto(BaggageAllowanceAggregate agg)
        => new(
            agg.Id.Value,
            agg.CabinClassId,
            agg.FareTypeId,
            agg.CarryOnPieces,
            agg.CarryOnKg,
            agg.CheckedPieces,
            agg.CheckedKg);
}
