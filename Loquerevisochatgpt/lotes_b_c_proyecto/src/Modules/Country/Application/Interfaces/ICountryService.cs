namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.Interfaces;

public interface ICountryService
{
    Task<CountryDto?>             GetByIdAsync(int id,            CancellationToken cancellationToken = default);
    Task<IEnumerable<CountryDto>> GetAllAsync(                    CancellationToken cancellationToken = default);
    Task<CountryDto>              CreateAsync(string name,        CancellationToken cancellationToken = default);
    Task                          UpdateAsync(int id, string name,CancellationToken cancellationToken = default);
    Task                          DeleteAsync(int id,             CancellationToken cancellationToken = default);
}

public sealed record CountryDto(int Id, string Name);
