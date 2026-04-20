namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class AirlineService : IAirlineService
{
    private readonly IAirlineRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public AirlineService(IAirlineRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AirlineDto> CreateAsync(CreateAirlineRequest request, CancellationToken cancellationToken = default)
    {
        var airline = AirlineAggregate.Create(request.IataCode, request.Name, request.IsActive);
        await _repository.AddAsync(airline, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return MapToDto(airline);
    }

    public async Task<AirlineDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var airline = await _repository.GetByIdAsync(AirlineId.New(id), cancellationToken);
        return airline is null ? null : MapToDto(airline);
    }

    public async Task<IReadOnlyList<AirlineDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<AirlineDto> UpdateAsync(int id, UpdateAirlineRequest request, CancellationToken cancellationToken = default)
    {
        var airline = await _repository.GetByIdAsync(AirlineId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Airline with id {id} not found.");
        airline.Update(request.IataCode, request.Name, request.IsActive);
        _repository.Update(airline);
        await _unitOfWork.CommitAsync(cancellationToken);
        return MapToDto(airline);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var airline = await _repository.GetByIdAsync(AirlineId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Airline with id {id} not found.");
        _repository.Delete(airline);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static AirlineDto MapToDto(AirlineAggregate a) =>
        new(a.Id.Value, a.IataCode, a.Name, a.IsActive, a.CreatedAt, a.UpdatedAt);
}
