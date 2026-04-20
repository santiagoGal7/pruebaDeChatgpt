namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Servicio de aplicación para el módulo City.
/// Orquesta los casos de uso delegando a repositorio y UoW.
/// </summary>
public sealed class CityService : ICityService
{
    private readonly ICityRepository _repository;
    private readonly IUnitOfWork     _unitOfWork;

    public CityService(ICityRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CityDto> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
    {
        var city = CityAggregate.Create(request.Name, request.CountryId);
        await _repository.AddAsync(city, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return MapToDto(city);
    }

    public async Task<CityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _repository.GetByIdAsync(CityId.New(id), cancellationToken);
        return city is null ? null : MapToDto(city);
    }

    public async Task<IReadOnlyList<CityDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cities = await _repository.GetAllAsync(cancellationToken);
        return cities.Select(MapToDto).ToList();
    }

    public async Task<CityDto> UpdateAsync(int id, UpdateCityRequest request, CancellationToken cancellationToken = default)
    {
        var city = await _repository.GetByIdAsync(CityId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"City with id {id} not found.");

        city.Update(request.Name, request.CountryId);
        _repository.Update(city);
        await _unitOfWork.CommitAsync(cancellationToken);
        return MapToDto(city);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _repository.GetByIdAsync(CityId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"City with id {id} not found.");

        _repository.Delete(city);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static CityDto MapToDto(CityAggregate c) =>
        new(c.Id.Value, c.Name, c.CountryId, c.CreatedAt);
}
