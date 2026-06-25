using MassTransit;
using NotificationsAPI.Contracts;
using NotificationsAPI.Services;

namespace NotificationsAPI.Consumers
{
    public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
    {
        private readonly EmailService _emailService;
        public UserCreatedConsumer(EmailService emailService)
        {
            _emailService = emailService;
        }
        public async Task Consume(ConsumeContext<UserCreatedEvent> context)
        {
            await _emailService.SendWelcomeEmail(context.Message.Email, context.Message.Name);
        }
    }
}