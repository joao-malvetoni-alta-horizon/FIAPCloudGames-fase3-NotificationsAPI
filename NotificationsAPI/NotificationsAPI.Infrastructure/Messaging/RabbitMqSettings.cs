namespace NotificationsAPI.Infrastructure.Messaging;

/// <summary>
/// Configuration settings for RabbitMQ connection.
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// RabbitMQ host name or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port number.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ username for authentication.
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Maximum number of connection retries.
    /// </summary>
    public int MaxConnectionRetries { get; set; } = 3;

    /// <summary>
    /// Delay in milliseconds between connection retries.
    /// </summary>
    public int ConnectionRetryDelayMs { get; set; } = 1000;
}
