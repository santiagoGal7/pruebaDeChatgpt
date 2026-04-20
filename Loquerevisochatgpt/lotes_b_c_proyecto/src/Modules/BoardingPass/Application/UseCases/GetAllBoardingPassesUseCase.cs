namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;

public sealed class GetAllBoardingPassesUseCase
{
    private readonly IBoardingPassRepository _repository;

    public GetAllBoardingPassesUseCase(IBoardingPassRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BoardingPassAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
