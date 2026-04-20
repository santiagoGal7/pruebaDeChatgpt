namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

public sealed class DelayReasonService : IDelayReasonService
{
    private readonly CreateDelayReasonUseCase   _create;
    private readonly DeleteDelayReasonUseCase   _delete;
    private readonly GetAllDelayReasonsUseCase  _getAll;
    private readonly GetDelayReasonByIdUseCase  _getById;
    private readonly UpdateDelayReasonUseCase   _update;

    public DelayReasonService(
        CreateDelayReasonUseCase  create,
        DeleteDelayReasonUseCase  delete,
        GetAllDelayReasonsUseCase getAll,
        GetDelayReasonByIdUseCase getById,
        UpdateDelayReasonUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<DelayReasonDto> CreateAsync(
        string            name,
        string            category,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, category, cancellationToken);
        return new DelayReasonDto(agg.Id.Value, agg.Name, agg.Category);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<DelayReasonDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new DelayReasonDto(a.Id.Value, a.Name, a.Category));
    }

    public async Task<DelayReasonDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new DelayReasonDto(agg.Id.Value, agg.Name, agg.Category);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        string            category,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, category, cancellationToken);
}
