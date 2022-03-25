using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// Where() handler
/// </summary>
internal class InnermostWhereFinder : ExpressionVisitor
{
    private MethodCallExpression? innermostWhereExpression;

    public MethodCallExpression? GetInnermostWhere(Expression expression)
    {
        Visit(expression);
        return innermostWhereExpression;
    }

    protected override Expression VisitMethodCall(MethodCallExpression expression)
    {
        if (expression.Method.Name == "Where")
            innermostWhereExpression = expression;

        Visit(expression.Arguments[0]);

        return expression;
    }
}