using System.Text;
using BotCarniceria.Infrastructure.Services.External.Printing;
using FluentAssertions;

namespace BotCarniceria.Infrastructure.Tests.Services.External.Printing;

public class TicketBuilderTests
{
    public TicketBuilderTests()
    {
        // Register encoding provider for CP850 support
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void WriteLine_ShouldEncodeAccentsCorrectly_InCP850()
    {
        // Arrange
        var builder = new TicketBuilder();
        var text = "Barrón"; 

        // Act
        builder.WriteLine(text);
        var bytes = builder.Build();

        // Assert
        // 'ó' in CP850 is 162 (0xA2)
        // Expected bytes: B(66), a(97), r(114), r(114), ó(162), n(110), \n(10)
        bytes.Should().ContainInOrder(new byte[] { 66, 97, 114, 114, 162, 110, 10 });
    }
}
