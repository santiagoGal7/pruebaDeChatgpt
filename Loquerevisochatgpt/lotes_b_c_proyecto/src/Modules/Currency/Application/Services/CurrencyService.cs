namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

public sealed class CurrencyService : ICurrencyService
{
    private readonly CreateCurrencyUseCase   _create;
    private readonly DeleteCurrencyUseCase   _delete;
    private readonly GetAllCurrenciesUseCase _getAll;
    private readonly GetCurrencyByIdUseCase  _getById;
    private readonly UpdateCurrencyUseCase   _update;

    public CurrencyService(
        CreateCurrencyUseCase  create,
        DeleteCurrencyUseCase  delete,
        GetAllCurrenciesUseCase getAll,
        GetCurrencyByIdUseCase getById,
        UpdateCurrencyUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CurrencyDto> CreateAsync(
        string            isoCode,
        string            name,
        string            symbol,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(isoCode, name, symbol, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CurrencyDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<CurrencyDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        string            isoCode,
        string            name,
        string            symbol,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, isoCode, name, symbol, cancellationToken);

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static CurrencyDto ToDto(
        Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate.CurrencyAggregate agg)
        => new(agg.Id.Value, agg.IsoCode, agg.Name, agg.Symbol);
}
