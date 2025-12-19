namespace BotCarniceria.Core.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message)
    {
    }
}

public class InvalidDomainOperationException : DomainException
{
    public InvalidDomainOperationException(string message) : base(message)
    {
    }
}

public class EntityNotFoundDomainException : DomainException
{
    public EntityNotFoundDomainException(string entityName, object key) 
        : base($"Entity '{entityName}' with key '{key}' was not found.")
    {
    }
}
