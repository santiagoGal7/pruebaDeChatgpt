namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>Servicio de aplicación para el módulo Airport.</summary>
public sealed class AirportService : IAirportService
{
    private readonly IAirportRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public AirportService(IAirportRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AirportDto> CreateAsync(CreateAirportRequest request, CancellationToken cancellationToken = default)
    {
        var airport = AirportAggregate.Create(request.IataCode, request.Name, request.CityId);
        await _repository.AddAsync(airport, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return MapToDto(airport);
    }

    public async Task<AirportDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var airport = await _repository.GetByIdAsync(AirportId.New(id), cancellationToken);
        return airport is null ? null : MapToDto(airport);
    }

    public async Task<IReadOnlyList<AirportDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<AirportDto> UpdateAsync(int id, UpdateAirportRequest request, CancellationToken cancellationToken = default)
    {
        var airport = await _repository.GetByIdAsync(AirportId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Airport with id {id} not found.");

        airport.Update(request.IataCode, request.Name, request.CityId);
        _repository.Update(airport);
        await _unitOfWork.CommitAsync(cancellationToken);
        return MapToDto(airport);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var airport = await _repository.GetByIdAsync(AirportId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Airport with id {id} not found.");

        _repository.Delete(airport);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static AirportDto MapToDto(AirportAggregate a) =>
        new(a.Id.Value, a.IataCode, a.Name, a.CityId, a.CreatedAt);
}
