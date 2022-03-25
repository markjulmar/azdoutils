using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// Query provider
/// </summary>
class QueryProvider : IQueryProvider
{
    private readonly IQueryContext queryContext;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="queryContext"></param>
    public QueryProvider(IQueryContext queryContext)
    {
        this.queryContext = queryContext;
    }

    /// <summary>Constructs an <see cref="T:System.Linq.IQueryable" /> object that can evaluate the query represented by a specified expression tree.</summary>
    /// <param name="expression">An expression tree that represents a LINQ query.</param>
    /// <returns>An <see cref="T:System.Linq.IQueryable" /> that can evaluate the query represented by the specified expression tree.</returns>
    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = TypeSystem.GetElementType(expression.Type);
        try
        {
            var result = Activator.CreateInstance(typeof(Queryable<>).MakeGenericType(elementType), new object[] { this, expression });
            if (result == null) throw new InvalidQueryException("Failed to create query from expression.");
            return (IQueryable) result;
        }
        catch (System.Reflection.TargetInvocationException tie)
        {
            throw tie.InnerException!;
        }
    }

    public object Execute(Expression expression) => queryContext.Execute(expression, false);

    // Queryable's "single value" standard query operators call this method.
    // It is also called from QueryableData.GetEnumerator(). 
    public TResult Execute<TResult>(Expression expression)
    {
        bool isEnumerable = typeof(TResult).Name == "IEnumerable`1";
        return (TResult) queryContext.Execute(expression, isEnumerable);
    }

    // Queryable's collection-returning standard query operators call this method. 
    public IQueryable<T> CreateQuery<T>(Expression expression) => new Queryable<T>(this, expression);
}