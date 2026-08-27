using System.Net;
using System.Net.Http.Json;
using Notifications.API.Models;
using Notifications.Domain.Notifications;
using Shouldly;

namespace Notifications.Tests.Integration.Api;

/// <summary>
/// Testes de integração ponta a ponta dos endpoints de notificação, subindo a API real
/// (Program.cs, DI, middlewares e roteamento) contra um PostgreSQL real via TestContainers.
/// </summary>
public class NotificationEndpointsTests(NotificationApiFactory factory)
    : IClassFixture<NotificationApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateNotification_WithValidRequest_ReturnsCreated()
    {
        var request = new CreateNotificationRequest(
            Guid.NewGuid(),
            NotificationType.WelcomeEmail,
            "user@example.com",
            "Test User");

        var response = await _client.PostAsJsonAsync("/notifications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        body.ShouldNotBeNull();
        body.RecipientEmail.ShouldBe("user@example.com");
        body.Status.ShouldBe(NotificationStatus.Pending);
    }

    [Fact]
    public async Task CreateNotification_WithInvalidEmail_ReturnsBadRequest()
    {
        var request = new CreateNotificationRequest(
            Guid.NewGuid(),
            NotificationType.WelcomeEmail,
            "email-invalido");

        var response = await _client.PostAsJsonAsync("/notifications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNotificationById_WithExistingId_ReturnsNotification()
    {
        var created = await CreateNotificationAsync();

        var response = await _client.GetAsync($"/notifications/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        body.ShouldNotBeNull();
        body.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task GetNotificationById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/notifications/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllNotifications_ReturnsCreatedNotification()
    {
        var created = await CreateNotificationAsync();

        var response = await _client.GetAsync("/notifications");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        body.ShouldNotBeNull();
        body.ShouldContain(n => n.Id == created.Id);
    }

    [Fact]
    public async Task GetNotificationsByUserId_ReturnsOnlyThatUsersNotifications()
    {
        var userId = Guid.NewGuid();
        var created = await CreateNotificationAsync(userId: userId);
        await CreateNotificationAsync();

        var response = await _client.GetAsync($"/notifications/user/{userId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        body.ShouldNotBeNull();
        body.ShouldAllBe(n => n.UserId == userId);
        body.ShouldContain(n => n.Id == created.Id);
    }

    [Fact]
    public async Task GetNotificationsByStatus_WithValidStatus_ReturnsMatchingNotifications()
    {
        var created = await CreateNotificationAsync();

        var response = await _client.GetAsync("/notifications/status/Pending");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        body.ShouldNotBeNull();
        body.ShouldContain(n => n.Id == created.Id);
        body.ShouldAllBe(n => n.Status == NotificationStatus.Pending);
    }

    [Fact]
    public async Task GetNotificationsByStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/notifications/status/EstadoQueNaoExiste");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateNotification_WithValidStatus_UpdatesAndReturnsOk()
    {
        var created = await CreateNotificationAsync();
        var request = new UpdateNotificationRequest(NotificationStatus.Sent);

        var response = await _client.PutAsJsonAsync($"/notifications/{created.Id}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        body.ShouldNotBeNull();
        body.Status.ShouldBe(NotificationStatus.Sent);
    }

    [Fact]
    public async Task UpdateNotification_WithNonExistentId_ReturnsNotFound()
    {
        var request = new UpdateNotificationRequest(NotificationStatus.Sent);

        var response = await _client.PutAsJsonAsync($"/notifications/{Guid.NewGuid()}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNotification_WithExistingId_RemovesNotification()
    {
        var created = await CreateNotificationAsync();

        var deleteResponse = await _client.DeleteAsync($"/notifications/{created.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/notifications/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNotification_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/notifications/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<NotificationResponse> CreateNotificationAsync(Guid? userId = null)
    {
        var request = new CreateNotificationRequest(
            userId ?? Guid.NewGuid(),
            NotificationType.WelcomeEmail,
            "user@example.com",
            "Test User");

        var response = await _client.PostAsJsonAsync("/notifications", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        return body!;
    }
}
