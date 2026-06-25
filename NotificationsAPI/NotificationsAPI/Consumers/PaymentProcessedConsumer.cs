using MassTransit;
using NotificationsAPI.Contracts;
using NotificationsAPI.Services;

namespace NotificationsAPI.Consumers
{
    public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
    {
        private readonly EmailService _emailService;

        public PaymentProcessedConsumer(EmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
        {
            if (context.Message.Status != "Approved")
                return;

            await _emailService.SendPurchaseConfirmation(context.Message.UserId, context.Message.GameId);
        }
    }
}
