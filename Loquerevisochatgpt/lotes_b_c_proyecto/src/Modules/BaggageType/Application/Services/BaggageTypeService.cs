namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

public sealed class BaggageTypeService : IBaggageTypeService
{
    private readonly CreateBaggageTypeUseCase   _create;
    private readonly DeleteBaggageTypeUseCase   _delete;
    private readonly GetAllBaggageTypesUseCase  _getAll;
    private readonly GetBaggageTypeByIdUseCase  _getById;
    private readonly UpdateBaggageTypeUseCase   _update;

    public BaggageTypeService(
        CreateBaggageTypeUseCase  create,
        DeleteBaggageTypeUseCase  delete,
        GetAllBaggageTypesUseCase getAll,
        GetBaggageTypeByIdUseCase getById,
        UpdateBaggageTypeUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<BaggageTypeDto> CreateAsync(
        string            name,
        decimal           maxWeightKg,
        decimal           extraFee,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, maxWeightKg, extraFee, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<BaggageTypeDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<BaggageTypeDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        decimal           maxWeightKg,
        decimal           extraFee,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, maxWeightKg, extraFee, cancellationToken);

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static BaggageTypeDto ToDto(
        Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate.BaggageTypeAggregate agg)
        => new(agg.Id.Value, agg.Name, agg.MaxWeightKg, agg.ExtraFee);
}
