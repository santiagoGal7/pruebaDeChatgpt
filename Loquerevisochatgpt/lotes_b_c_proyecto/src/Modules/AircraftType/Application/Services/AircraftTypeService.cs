namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class AircraftTypeService : IAircraftTypeService
{
    private readonly IAircraftTypeRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public AircraftTypeService(IAircraftTypeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository; _unitOfWork = unitOfWork;
    }

    public async Task<AircraftTypeDto> CreateAsync(CreateAircraftTypeRequest request, CancellationToken cancellationToken = default)
    {
        var at = AircraftTypeAggregate.Create(request.ManufacturerId, request.Model, request.TotalSeats, request.CargoCapacityKg);
        await _repository.AddAsync(at, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Map(at);
    }

    public async Task<AircraftTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var at = await _repository.GetByIdAsync(AircraftTypeId.New(id), cancellationToken);
        return at is null ? null : Map(at);
    }

    public async Task<IReadOnlyList<AircraftTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<AircraftTypeDto> UpdateAsync(int id, UpdateAircraftTypeRequest request, CancellationToken cancellationToken = default)
    {
        var at = await _repository.GetByIdAsync(AircraftTypeId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"AircraftType {id} not found.");
        at.Update(request.ManufacturerId, request.Model, request.TotalSeats, request.CargoCapacityKg);
        _repository.Update(at);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Map(at);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var at = await _repository.GetByIdAsync(AircraftTypeId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"AircraftType {id} not found.");
        _repository.Delete(at);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static AircraftTypeDto Map(AircraftTypeAggregate a) =>
        new(a.Id.Value, a.ManufacturerId, a.Model, a.TotalSeats, a.CargoCapacityKg);
}
