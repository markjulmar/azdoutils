using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// Expression tree modifier for LINQ query context
/// </summary>
/// <typeparam name="T"></typeparam>
internal class ExpressionTreeModifier<T> : ExpressionVisitor
{
    private readonly IQueryable<T> queryableItems;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="items"></param>
    internal ExpressionTreeModifier(IQueryable<T> items)
    {
        this.queryableItems = items;
    }

    /// <summary>Visits the <see cref="T:System.Linq.Expressions.ConstantExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitConstant(ConstantExpression node)
    {
        return node.Type == typeof(Queryable<T>) 
            ? Expression.Constant(this.queryableItems) 
            : node;
    }
}