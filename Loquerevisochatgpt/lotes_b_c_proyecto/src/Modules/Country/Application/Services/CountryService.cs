namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.UseCases;

public sealed class CountryService : ICountryService
{
    private readonly CreateCountryUseCase   _create;
    private readonly DeleteCountryUseCase   _delete;
    private readonly GetAllCountriesUseCase _getAll;
    private readonly GetCountryByIdUseCase  _getById;
    private readonly UpdateCountryUseCase   _update;

    public CountryService(
        CreateCountryUseCase   create,
        DeleteCountryUseCase   delete,
        GetAllCountriesUseCase getAll,
        GetCountryByIdUseCase  getById,
        UpdateCountryUseCase   update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CountryDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new CountryDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CountryDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new CountryDto(a.Id.Value, a.Name));
    }

    public async Task<CountryDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new CountryDto(agg.Id.Value, agg.Name);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, cancellationToken);
}
