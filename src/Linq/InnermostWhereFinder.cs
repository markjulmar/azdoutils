using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq
{
    internal class TakeFinder : ExpressionVisitor
    {
        private MethodCallExpression takeExpression;

        public MethodCallExpression FindTake(Expression expression)
        {
            Visit(expression);
            return takeExpression;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (expression.Method.Name == "Take")
                takeExpression = expression;

            Visit(expression.Arguments[0]);

            return expression;
        }
    }

    internal class InnermostWhereFinder : ExpressionVisitor
    {
        private MethodCallExpression innermostWhereExpression;

        public MethodCallExpression GetInnermostWhere(Expression expression)
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
}