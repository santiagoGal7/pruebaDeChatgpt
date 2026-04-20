namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class AircraftManufacturerService : IAircraftManufacturerService
{
    private readonly IAircraftManufacturerRepository _repository;
    private readonly IUnitOfWork                     _unitOfWork;

    public AircraftManufacturerService(IAircraftManufacturerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository; _unitOfWork = unitOfWork;
    }

    public async Task<AircraftManufacturerDto> CreateAsync(CreateAircraftManufacturerRequest request, CancellationToken cancellationToken = default)
    {
        var m = AircraftManufacturerAggregate.Create(request.Name, request.CountryId);
        await _repository.AddAsync(m, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Map(m);
    }

    public async Task<AircraftManufacturerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var m = await _repository.GetByIdAsync(AircraftManufacturerId.New(id), cancellationToken);
        return m is null ? null : Map(m);
    }

    public async Task<IReadOnlyList<AircraftManufacturerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<AircraftManufacturerDto> UpdateAsync(int id, UpdateAircraftManufacturerRequest request, CancellationToken cancellationToken = default)
    {
        var m = await _repository.GetByIdAsync(AircraftManufacturerId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"AircraftManufacturer {id} not found.");
        m.Update(request.Name, request.CountryId);
        _repository.Update(m);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Map(m);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var m = await _repository.GetByIdAsync(AircraftManufacturerId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"AircraftManufacturer {id} not found.");
        _repository.Delete(m);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static AircraftManufacturerDto Map(AircraftManufacturerAggregate a) =>
        new(a.Id.Value, a.Name, a.CountryId);
}
