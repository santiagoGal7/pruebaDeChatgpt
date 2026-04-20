namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCurrencyUseCase
{
    private readonly ICurrencyRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public DeleteCurrencyUseCase(ICurrencyRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CurrencyId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
