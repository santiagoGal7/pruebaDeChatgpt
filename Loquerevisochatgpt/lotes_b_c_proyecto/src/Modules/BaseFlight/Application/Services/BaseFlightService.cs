namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

public sealed class BaseFlightService : IBaseFlightService
{
    private readonly CreateBaseFlightUseCase   _create;
    private readonly DeleteBaseFlightUseCase   _delete;
    private readonly GetAllBaseFlightsUseCase  _getAll;
    private readonly GetBaseFlightByIdUseCase  _getById;
    private readonly UpdateBaseFlightUseCase   _update;

    public BaseFlightService(
        CreateBaseFlightUseCase   create,
        DeleteBaseFlightUseCase   delete,
        GetAllBaseFlightsUseCase  getAll,
        GetBaseFlightByIdUseCase  getById,
        UpdateBaseFlightUseCase   update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<BaseFlightDto> CreateAsync(
        string flightCode,
        int    airlineId,
        int    routeId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(flightCode, airlineId, routeId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<BaseFlightDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<BaseFlightDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int    id,
        string flightCode,
        int    airlineId,
        int    routeId,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, flightCode, airlineId, routeId, cancellationToken);

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static BaseFlightDto ToDto(
        Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate.BaseFlightAggregate agg)
        => new(agg.Id.Value, agg.FlightCode, agg.AirlineId, agg.RouteId, agg.CreatedAt, agg.UpdatedAt);
}
