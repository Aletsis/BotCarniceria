namespace BotCarniceria.Infrastructure.Services.External.Printing;

public static class EscPosCommands
{
    public static readonly byte[] Initialize = { 0x1B, 0x40 };
    public static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };
    public static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 };
    public static readonly byte[] AlignRight = { 0x1B, 0x61, 0x02 };
    public static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };
    public static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };
    public static readonly byte[] FeedLines = { 0x1B, 0x64, 0x03 }; // Feed 3 lines default
    public static readonly byte[] FullCut = { 0x1D, 0x56, 0x41, 0x00 };
}
