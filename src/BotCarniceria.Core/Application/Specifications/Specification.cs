using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

/// <summary>
/// Base class for implementing the Specification pattern.
/// Encapsulates query logic in reusable, composable objects.
/// </summary>
/// <typeparam name="T">The entity type this specification applies to</typeparam>
public abstract class Specification<T>
{
    /// <summary>
    /// Converts this specification to a LINQ expression that can be used in queries.
    /// </summary>
    /// <returns>Expression representing the specification criteria</returns>
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Checks if an entity satisfies this specification.
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True if the entity satisfies the specification, false otherwise</returns>
    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    /// <summary>
    /// Combines this specification with another using AND logic.
    /// </summary>
    /// <param name="specification">The specification to combine with</param>
    /// <returns>A new specification representing the AND combination</returns>
    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Combines this specification with another using OR logic.
    /// </summary>
    /// <param name="specification">The specification to combine with</param>
    /// <returns>A new specification representing the OR combination</returns>
    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Negates this specification.
    /// </summary>
    /// <returns>A new specification representing the NOT of this specification</returns>
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }

    /// <summary>
    /// Implicit conversion to Expression for easy use in LINQ queries.
    /// </summary>
    public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
    {
        return specification.ToExpression();
    }
}
