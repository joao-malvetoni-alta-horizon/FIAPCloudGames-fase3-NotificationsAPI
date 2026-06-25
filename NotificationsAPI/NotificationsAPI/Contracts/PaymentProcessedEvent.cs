namespace NotificationsAPI.Contracts
{
    public class PaymentProcessedEvent
    {
        public Guid UserId { get; init; }
        public Guid GameId { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
