namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

public sealed class CancellationReasonService : ICancellationReasonService
{
    private readonly CreateCancellationReasonUseCase   _create;
    private readonly DeleteCancellationReasonUseCase   _delete;
    private readonly GetAllCancellationReasonsUseCase  _getAll;
    private readonly GetCancellationReasonByIdUseCase  _getById;
    private readonly UpdateCancellationReasonUseCase   _update;

    public CancellationReasonService(
        CreateCancellationReasonUseCase  create,
        DeleteCancellationReasonUseCase  delete,
        GetAllCancellationReasonsUseCase getAll,
        GetCancellationReasonByIdUseCase getById,
        UpdateCancellationReasonUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CancellationReasonDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new CancellationReasonDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CancellationReasonDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new CancellationReasonDto(a.Id.Value, a.Name));
    }

    public async Task<CancellationReasonDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new CancellationReasonDto(agg.Id.Value, agg.Name);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, cancellationToken);
}
