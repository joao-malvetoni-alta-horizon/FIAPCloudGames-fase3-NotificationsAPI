namespace NotificationsAPI.Domain.Notifications;

using Shared;
using System.Text.RegularExpressions;

/// <summary>
/// Notification aggregate root. Represents a notification sent to a user.
/// </summary>
public class Notification : Entity
{
    private Notification()
    {
        // For EF Core
    }

    /// <summary>
    /// User ID who should receive this notification.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Type of notification.
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string? RecipientName { get; private set; }

    /// <summary>
    /// Notification subject line.
    /// </summary>
    public string? Subject { get; private set; }

    /// <summary>
    /// Notification message body.
    /// </summary>
    public string? Body { get; private set; }

    /// <summary>
    /// ID of the originating integration event (for idempotency).
    /// </summary>
    public Guid? EventId { get; private set; }

    /// <summary>
    /// Number of times this notification has been retried.
    /// </summary>
    public int RetryCount { get; private set; } = 0;

    /// <summary>
    /// Last error message if sending failed.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Factory method to create a new notification.
    /// </summary>
    /// <param name="userId">User who should receive the notification.</param>
    /// <param name="type">Type of notification.</param>
    /// <param name="recipientEmail">Email address to send to.</param>
    /// <param name="recipientName">Name of the recipient.</param>
    /// <param name="eventId">ID of the originating event.</param>
    /// <returns>A new Notification instance.</returns>
    /// <exception cref="NotificationDomainException">Thrown if parameters are invalid.</exception>
    public static Notification Create(
        Guid userId,
        NotificationType type,
        string recipientEmail,
        string? recipientName = null,
        Guid? eventId = null)
    {
        if (userId == Guid.Empty)
            throw new NotificationDomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new NotificationDomainException("RecipientEmail cannot be empty.");

        if (!IsValidEmail(recipientEmail))
            throw new NotificationDomainException("RecipientEmail is not in a valid format.");

        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            RecipientEmail = recipientEmail.Trim(),
            RecipientName = recipientName?.Trim(),
            Status = NotificationStatus.Pending,
            EventId = eventId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks this notification as successfully sent.
    /// </summary>
    /// <exception cref="NotificationDomainException">Thrown if notification is not in Pending status.</exception>
    public void MarkAsSent()
    {
        if (Status != NotificationStatus.Pending)
            throw new NotificationDomainException(
                $"Only Pending notifications can be marked as Sent. Current status: {Status}");

        Status = NotificationStatus.Sent;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    /// <summary>
    /// Marks this notification as failed.
    /// </summary>
    /// <param name="errorMessage">Error message describing why it failed.</param>
    /// <exception cref="NotificationDomainException">Thrown if notification is not in Pending status.</exception>
    public void MarkAsFailed(string errorMessage)
    {
        if (Status != NotificationStatus.Pending)
            throw new NotificationDomainException(
                $"Only Pending notifications can be marked as Failed. Current status: {Status}");

        Status = NotificationStatus.Failed;
        LastError = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this notification as delivered.
    /// </summary>
    /// <exception cref="NotificationDomainException">Thrown if notification is not in Sent status.</exception>
    public void MarkAsDelivered()
    {
        if (Status != NotificationStatus.Sent)
            throw new NotificationDomainException(
                $"Only Sent notifications can be marked as Delivered. Current status: {Status}");

        Status = NotificationStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the retry count.
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets the notification to Pending status for retry.
    /// </summary>
    public void ResetForRetry()
    {
        if (RetryCount >= 3)
            throw new NotificationDomainException("Maximum retry attempts exceeded.");

        Status = NotificationStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates if the given email address is in a valid format.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            return Regex.IsMatch(email, emailPattern);
        }
        catch
        {
            return false;
        }
    }
}
