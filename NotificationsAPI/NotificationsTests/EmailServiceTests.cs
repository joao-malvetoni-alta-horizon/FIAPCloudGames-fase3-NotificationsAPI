using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationsAPI.Services;

namespace NotificationsTests
{
    public class EmailServiceTests
    {
        [Fact]
        public async Task Deve_Enviar_Email()
        {
            var logger = new Mock<ILogger<EmailService>>();

            var service = new EmailService(logger.Object);

            var act = () => service.SendWelcomeEmail("joao@email.com", "João");

            await act.Should().NotThrowAsync();
        }
    }
}
