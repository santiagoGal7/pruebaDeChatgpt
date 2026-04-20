namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCountryUseCase
{
    private readonly ICountryRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public DeleteCountryUseCase(ICountryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CountryId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
