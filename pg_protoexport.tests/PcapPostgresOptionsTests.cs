using NSubstitute;

namespace pg_protoexport.tests
{
    public class PcapPostgresOptionsTests
    {
        [Fact]
        public void MessageCatalog_ShouldBeInitialized()
        {
            // Arrange & Act
            var options = new PcapPostgresOptions();

            // Assert
            Assert.NotNull(options.MessageCatalog);
        }

        [Fact]
        public void CustomMessageProcessor_ShouldBeNullByDefault()
        {
            // Arrange & Act
            var options = new PcapPostgresOptions();

            // Assert
            Assert.Null(options.CustomMessageProcessor);
        }

        [Fact]
        public void AddDefaultPostgresMessages_ShouldAddMessagesToCatalog()
        {
            // Arrange
            var options = new PcapPostgresOptions();
            var messageCatalog = Substitute.For<IPostgresMessageRegistry>();
            options.MessageCatalog = messageCatalog;

            // Act
            options.AddDefaultPostgresMessages();

            // Assert
            messageCatalog.Received().AddOrReplaceBackendMessage(Arg.Is<PostgresMessageDescriptor>(m => m.Code == 'R' && m.Name == "AuthenticationRequest" && !m.IsFrontEnd));
            messageCatalog.Received().AddOrReplaceFrontendMessage(Arg.Is<PostgresMessageDescriptor>(m => m.Code == 'D' && m.Name == "Describe" && m.IsFrontEnd));
            // Add more assertions as needed for other messages
        }

        [Fact]
        public void DefaultServiceInstance_ShouldHaveDefaultPostgresMessages()
        {
            // Arrange/Act
            var service = PcapService.Create();
            
            // Assert
            Assert.IsType<PcapService>(service);
            var typedService = (PcapService)service;
            Assert.NotNull(typedService.Options.MessageCatalog.GetMessage('R',frontEnd:false));
            Assert.NotNull(typedService.Options.MessageCatalog.GetMessage('D', frontEnd: true));

            // Add more assertions as needed for other messages
        }
    }
}
