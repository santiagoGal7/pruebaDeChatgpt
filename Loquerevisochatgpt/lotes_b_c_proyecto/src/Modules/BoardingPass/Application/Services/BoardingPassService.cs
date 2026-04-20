namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;

public sealed class BoardingPassService : IBoardingPassService
{
    private readonly CreateBoardingPassUseCase      _create;
    private readonly DeleteBoardingPassUseCase      _delete;
    private readonly GetAllBoardingPassesUseCase    _getAll;
    private readonly GetBoardingPassByIdUseCase     _getById;
    private readonly UpdateBoardingPassUseCase      _update;
    private readonly GetBoardingPassByCheckInUseCase _getByCheckIn;

    public BoardingPassService(
        CreateBoardingPassUseCase       create,
        DeleteBoardingPassUseCase       delete,
        GetAllBoardingPassesUseCase     getAll,
        GetBoardingPassByIdUseCase      getById,
        UpdateBoardingPassUseCase       update,
        GetBoardingPassByCheckInUseCase getByCheckIn)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _update       = update;
        _getByCheckIn = getByCheckIn;
    }

    public async Task<BoardingPassDto> CreateAsync(
        int               checkInId,
        int?              gateId,
        string?           boardingGroup,
        int               flightSeatId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            checkInId, gateId, boardingGroup, flightSeatId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<BoardingPassDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<BoardingPassDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        int?              gateId,
        string?           boardingGroup,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, gateId, boardingGroup, cancellationToken);

    public async Task<BoardingPassDto?> GetByCheckInAsync(
        int               checkInId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByCheckIn.ExecuteAsync(checkInId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static BoardingPassDto ToDto(BoardingPassAggregate agg)
        => new(agg.Id.Value, agg.CheckInId, agg.GateId, agg.BoardingGroup, agg.FlightSeatId);
}
