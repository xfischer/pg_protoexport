using NSubstitute;

namespace pg_protoexport.tests
{
    public class PcapToLatexOptionsTests
    {
        [Fact]
        public void AddTemplateProvider_ShouldSetCustomTemplateProvider()
        {
            // Arrange
            var options = new PcapToLatexOptions();
            Func<PostgresMessageBase, ITextTransformer?> customTemplateProvider = message => Substitute.For<ITextTransformer>();

            // Act
            options.AddTemplateProvider(customTemplateProvider);

            // Assert
            Assert.Equal(customTemplateProvider, options.CustomTemplateProvider);
        }

        [Fact]
        public void AddCustomHeader_ShouldSetCustomHeaderProvider()
        {
            // Arrange
            var options = new PcapToLatexOptions();
            Func<string?, GenerationState, ITextTransformer?> customHeaderProvider = (header, state) => Substitute.For<ITextTransformer>();

            // Act
            options.AddCustomHeader(customHeaderProvider);

            // Assert
            Assert.Equal(customHeaderProvider, options.CustomHeaderProvider);
        }
    }
}
