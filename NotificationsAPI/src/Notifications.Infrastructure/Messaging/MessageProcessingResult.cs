namespace Notifications.Infrastructure.Messaging;

/// <summary>
/// Resultado do processamento de uma mensagem consumida do RabbitMQ.
/// </summary>
public enum MessageProcessingResult
{
    /// <summary>
    /// Mensagem processada com sucesso; deve ser confirmada (ack).
    /// </summary>
    Success,

    /// <summary>
    /// Mensagem corrompida ou inválida (ex.: JSON malformado); não deve ser reenfileirada.
    /// </summary>
    PoisonMessage,

    /// <summary>
    /// Falha transitória ao processar a mensagem (ex.: banco de dados indisponível); deve ser reenfileirada.
    /// </summary>
    TransientFailure
}
