using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NotificationsAPI.Services;

namespace NotificationsTests;

public class EmailServiceTests
{
    [Fact]
    public async Task Deve_Enviar_Email()
    {
        var logger = Substitute.For<ILogger<EmailService>>();

        var service = new EmailService(logger);

        Func<Task> act = () => service.SendWelcomeEmail("joao@email.com", "João");

        await act.Should().NotThrowAsync();
    }
}
