using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// Take() method
/// </summary>
internal class TakeFinder : ExpressionVisitor
{
    private MethodCallExpression? takeExpression;

    public MethodCallExpression? FindTake(Expression expression)
    {
        Visit(expression);
        return takeExpression;
    }

    /// <summary>
    /// Method chain builder
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    protected override Expression VisitMethodCall(MethodCallExpression expression)
    {
        if (expression.Method.Name == "Take")
            takeExpression = expression;

        Visit(expression.Arguments[0]);
        return expression;
    }
}