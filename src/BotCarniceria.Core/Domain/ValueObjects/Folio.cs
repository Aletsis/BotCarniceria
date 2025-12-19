namespace BotCarniceria.Core.Domain.ValueObjects;

public sealed class Folio : ValueObject
{
    public string Value { get; private set; }
    
    private Folio(string value) => Value = value;
    
    // Required for EF Core
    private Folio() { }

    private static readonly Random _random = new Random();
    private static readonly object _syncLock = new object();

    public static Folio Generate()
    {
        var fecha = DateTime.UtcNow.ToString("yyyyMMdd");
        string randomStr;
        lock (_syncLock)
        {
            randomStr = _random.Next(1, 9999).ToString("D4");
        }
        return new Folio($"CAR-{fecha}-{randomStr}");
    }
    
    public static Folio From(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Folio required");
        return new Folio(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value!;
    }
    
    public override string ToString() => Value;
}
