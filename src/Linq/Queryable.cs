using System.Collections;
using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// Class to implement queryable support for our Azure DevOps queries.
/// </summary>
/// <typeparam name="T">Type we are building queryable support for</typeparam>
public class Queryable<T> : IOrderedQueryable<T>
{
    /// <summary>
    /// The query provider
    /// </summary>
    public IQueryProvider Provider { get; }

    /// <summary>
    /// The expression
    /// </summary>
    public Expression Expression { get; }
    
    /// <summary>
    /// The type
    /// </summary>
    public Type ElementType => typeof(T);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="service"></param>
    /// <param name="project"></param>
    public Queryable(IAzureDevOpsService service, string? project)
    {
        Provider = new QueryProvider(new QueryContext<T>(service, project));
        Expression = Expression.Constant(this);
    }

    /// <summary>
    /// Constructor - called from QueryProvider.CreateQuery through reflection.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="expression"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Queryable(IQueryProvider provider, Expression expression)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));

        if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            throw new ArgumentOutOfRangeException(nameof(expression));
    }

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<T> GetEnumerator() => Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();

    /// <summary>
    /// Enumerator
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator() => Provider.Execute<IEnumerable>(Expression).GetEnumerator();
}