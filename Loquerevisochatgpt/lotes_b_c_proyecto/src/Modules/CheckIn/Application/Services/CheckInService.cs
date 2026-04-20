namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;

public sealed class CheckInService : ICheckInService
{
    private readonly CreateCheckInUseCase       _create;
    private readonly DeleteCheckInUseCase       _delete;
    private readonly GetAllCheckInsUseCase      _getAll;
    private readonly GetCheckInByIdUseCase      _getById;
    private readonly ChangeCheckInStatusUseCase _changeStatus;
    private readonly GetCheckInByTicketUseCase  _getByTicket;

    public CheckInService(
        CreateCheckInUseCase      create,
        DeleteCheckInUseCase      delete,
        GetAllCheckInsUseCase     getAll,
        GetCheckInByIdUseCase     getById,
        ChangeCheckInStatusUseCase changeStatus,
        GetCheckInByTicketUseCase getByTicket)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _changeStatus = changeStatus;
        _getByTicket  = getByTicket;
    }

    public async Task<CheckInDto> CreateAsync(
        int               ticketId,
        int               checkInStatusId,
        string?           counterNumber,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            ticketId, checkInStatusId, counterNumber, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CheckInDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<CheckInDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task ChangeStatusAsync(
        int               id,
        int               checkInStatusId,
        string?           counterNumber,
        CancellationToken cancellationToken = default)
        => await _changeStatus.ExecuteAsync(id, checkInStatusId, counterNumber, cancellationToken);

    public async Task<CheckInDto?> GetByTicketAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByTicket.ExecuteAsync(ticketId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static CheckInDto ToDto(CheckInAggregate agg)
        => new(
            agg.Id.Value,
            agg.TicketId,
            agg.CheckInTime,
            agg.CheckInStatusId,
            agg.CounterNumber);
}
