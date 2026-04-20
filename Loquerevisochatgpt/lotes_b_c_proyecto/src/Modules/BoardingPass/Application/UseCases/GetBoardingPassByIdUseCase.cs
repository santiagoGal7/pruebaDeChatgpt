namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;

public sealed class GetBoardingPassByIdUseCase
{
    private readonly IBoardingPassRepository _repository;

    public GetBoardingPassByIdUseCase(IBoardingPassRepository repository)
    {
        _repository = repository;
    }

    public async Task<BoardingPassAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new BoardingPassId(id), cancellationToken);
}
