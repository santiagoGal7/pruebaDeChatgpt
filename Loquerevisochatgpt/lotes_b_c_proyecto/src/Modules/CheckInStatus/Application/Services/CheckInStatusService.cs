namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

public sealed class CheckInStatusService : ICheckInStatusService
{
    private readonly CreateCheckInStatusUseCase   _create;
    private readonly DeleteCheckInStatusUseCase   _delete;
    private readonly GetAllCheckInStatusesUseCase _getAll;
    private readonly GetCheckInStatusByIdUseCase  _getById;
    private readonly UpdateCheckInStatusUseCase   _update;

    public CheckInStatusService(
        CreateCheckInStatusUseCase  create,
        DeleteCheckInStatusUseCase  delete,
        GetAllCheckInStatusesUseCase getAll,
        GetCheckInStatusByIdUseCase getById,
        UpdateCheckInStatusUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CheckInStatusDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new CheckInStatusDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CheckInStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new CheckInStatusDto(a.Id.Value, a.Name));
    }

    public async Task<CheckInStatusDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new CheckInStatusDto(agg.Id.Value, agg.Name);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, cancellationToken);
}
