using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AzDOUtilities.Linq
{
    public class Queryable<T> : IOrderedQueryable<T>
    {
        public IQueryProvider Provider { get; private set; }
        public Expression Expression { get; private set; }
        public Type ElementType => typeof(T);

        public Queryable(IAzureDevOpsService service, string project)
        {
            Provider = new QueryProvider(new QueryContext<T>(service, project));
            Expression = Expression.Constant(this);
        }

        // Called from QueryProvider.CreateQuery through reflection.
        public Queryable(IQueryProvider provider, Expression expression)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));

            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
                throw new ArgumentOutOfRangeException(nameof(expression));
        }

        public IEnumerator<T> GetEnumerator() => Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Provider.Execute<IEnumerable>(Expression).GetEnumerator();
    }
}