using System.Linq;
using System.Linq.Expressions;

namespace AzDOUtilities.Linq
{
    internal class ExpressionTreeModifier<T> : ExpressionVisitor
    {
        private IQueryable<T> queryableItems;

        internal ExpressionTreeModifier(IQueryable<T> items)
        {
            this.queryableItems = items;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            return c.Type == typeof(Queryable<T>) ? Expression.Constant(this.queryableItems) : c;
        }
    }
}