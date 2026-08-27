namespace Notifications.Application.UseCases.Handlers;

/// <summary>
/// Interface genérica para manipuladores de eventos de integração.
/// </summary>
/// <typeparam name="TEvent">Tipo do evento a ser manipulado.</typeparam>
public interface IEventHandler<in TEvent>
{
    /// <summary>
    /// Manipula um evento de integração.
    /// </summary>
    /// <param name="event">O evento a ser manipulado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
