using BotCarniceria.Infrastructure.Services.External.Printing;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Services.External.Printing;

public class EscPosCommandsTests
{
    [Fact]
    public void Initialize_ShouldMatchEscPosStandard()
    {
        EscPosCommands.Initialize.Should().Equal(new byte[] { 0x1B, 0x40 });
    }

    [Fact]
    public void AlignmentCommands_ShouldMatchEscPosStandard()
    {
        EscPosCommands.AlignLeft.Should().Equal(new byte[] { 0x1B, 0x61, 0x00 });
        EscPosCommands.AlignCenter.Should().Equal(new byte[] { 0x1B, 0x61, 0x01 });
        EscPosCommands.AlignRight.Should().Equal(new byte[] { 0x1B, 0x61, 0x02 });
    }

    [Fact]
    public void BoldCommands_ShouldMatchEscPosStandard()
    {
        EscPosCommands.BoldOn.Should().Equal(new byte[] { 0x1B, 0x45, 0x01 });
        EscPosCommands.BoldOff.Should().Equal(new byte[] { 0x1B, 0x45, 0x00 });
    }

    [Fact]
    public void FeedAndCutCommands_ShouldMatchEscPosStandard()
    {
        EscPosCommands.FeedLines.Should().Equal(new byte[] { 0x1B, 0x64, 0x03 });
        EscPosCommands.FullCut.Should().Equal(new byte[] { 0x1D, 0x56, 0x41, 0x00 });
    }
}
