namespace NotificationsAPI.Services
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendWelcomeEmail(string email, string name)
        {
            _logger.LogInformation("E-mail de boas vindas enviado para {Name} ({Email})", name, email);
            return Task.CompletedTask;
        }

        public Task SendPurchaseConfirmation(Guid userId, Guid gameId)
        {
            _logger.LogInformation("Compra confirmada. UserId: {UserId}, GameId: {GameId}", userId, gameId);
            return Task.CompletedTask;
        }
    }
}