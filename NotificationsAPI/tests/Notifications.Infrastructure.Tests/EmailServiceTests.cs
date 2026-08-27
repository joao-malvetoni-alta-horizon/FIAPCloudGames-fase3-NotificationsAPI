using FluentAssertions;
using Microsoft.Extensions.Logging;
using Notifications.Infrastructure.Email;
using NSubstitute;
using Xunit;

namespace Notifications.Infrastructure.Tests;

public class EmailServiceTests
{
    [Fact]
    public async Task Deve_Enviar_Email()
    {
        var logger = Substitute.For<ILogger<EmailService>>();

        var service = new EmailService(logger);

        Func<Task> act = () => service.SendWelcomeEmailAsync("joao@email.com", "João");

        await act.Should().NotThrowAsync();
    }
}
