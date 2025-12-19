using System.Text;

namespace BotCarniceria.Infrastructure.Services.External.Printing;

public class TicketBuilder
{
    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;
    private readonly Encoding _encoding;

    public TicketBuilder()
    {
        _stream = new MemoryStream();
        _encoding = Encoding.GetEncoding(850); // CP850 supports Western European accents (e.g. á, é, ñ) common in thermal printers.
        _writer = new BinaryWriter(_stream, _encoding);
    }

    public TicketBuilder Initialize()
    {
        _writer.Write(EscPosCommands.Initialize);
        return this;
    }

    public TicketBuilder AlignCenter()
    {
        _writer.Write(EscPosCommands.AlignCenter);
        return this;
    }

    public TicketBuilder AlignLeft()
    {
        _writer.Write(EscPosCommands.AlignLeft);
        return this;
    }

    public TicketBuilder BoldOn()
    {
        _writer.Write(EscPosCommands.BoldOn);
        return this;
    }

    public TicketBuilder BoldOff()
    {
        _writer.Write(EscPosCommands.BoldOff);
        return this;
    }

    public TicketBuilder WriteLine(string text)
    {
        var bytes = _encoding.GetBytes(text + "\n");
        _writer.Write(bytes);
        return this;
    }

    public TicketBuilder WriteSeparator()
    {
        return WriteLine("================================");
    }

    public TicketBuilder FeedAndCut()
    {
        _writer.Write(EscPosCommands.FeedLines); // Using the predefined 3-line feed
        _writer.Write(EscPosCommands.FullCut);
        return this;
    }

    public byte[] Build()
    {
        _writer.Flush();
        return _stream.ToArray();
    }
}
