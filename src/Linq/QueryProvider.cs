using System;
using System.Linq;
using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq
{
    class QueryProvider : IQueryProvider
    {
        private readonly IQueryContext queryContext;

        public QueryProvider(IQueryContext queryContext)
        {
            this.queryContext = queryContext;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable) Activator.CreateInstance(typeof(Queryable<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public object Execute(Expression expression) => queryContext.Execute(expression, false);

        // Queryable's "single value" standard query operators call this method.
        // It is also called from QueryableData.GetEnumerator(). 
        public TResult Execute<TResult>(Expression expression)
        {
            bool IsEnumerable = typeof(TResult).Name == "IEnumerable`1";
            return (TResult)queryContext.Execute(expression, IsEnumerable);
        }

        // Queryable's collection-returning standard query operators call this method. 
        public IQueryable<T> CreateQuery<T>(Expression expression) => new Queryable<T>(this, expression);
    }
}