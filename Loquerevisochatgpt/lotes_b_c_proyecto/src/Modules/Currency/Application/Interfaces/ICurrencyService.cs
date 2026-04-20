namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.Interfaces;

public interface ICurrencyService
{
    Task<CurrencyDto?>             GetByIdAsync(int id,                                         CancellationToken cancellationToken = default);
    Task<IEnumerable<CurrencyDto>> GetAllAsync(                                                  CancellationToken cancellationToken = default);
    Task<CurrencyDto>              CreateAsync(string isoCode, string name, string symbol,       CancellationToken cancellationToken = default);
    Task                           UpdateAsync(int id, string isoCode, string name, string symbol, CancellationToken cancellationToken = default);
    Task                           DeleteAsync(int id,                                           CancellationToken cancellationToken = default);
}

public sealed record CurrencyDto(int Id, string IsoCode, string Name, string Symbol);
