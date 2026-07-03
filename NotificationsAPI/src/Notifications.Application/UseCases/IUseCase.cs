namespace Notifications.Application.UseCases;

/// <summary>
/// Interface genérica para caso de uso que processa um comando e retorna um resultado.
/// </summary>
/// <typeparam name="TRequest">Tipo da requisição/comando.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta/resultado.</typeparam>
public interface IUseCase<in TRequest, TResponse>
{
    /// <summary>
    /// Executa o caso de uso.
    /// </summary>
    /// <param name="request">Requisição/comando a ser processado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resposta/resultado da execução.</returns>
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
