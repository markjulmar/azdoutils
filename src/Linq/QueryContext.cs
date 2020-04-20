using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq
{
    interface IQueryContext
    {
        object Execute(Expression expression, bool IsEnumerable);
    }

    class QueryContext<T> : IQueryContext
    {
        readonly string project;
        readonly IAzureDevOpsService service;

        public QueryContext(IAzureDevOpsService service, string project)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.project = project;
        }

        public object Execute(Expression expression, bool IsEnumerable)
        {
            // The expression must represent a query over the data source. 
            if (!IsQueryOverDataSource(expression))
                throw new InvalidProgramException("No query over the data source was specified.");

            // Find the call to Where() and get the lambda expression predicate.
            InnermostWhereFinder whereFinder = new InnermostWhereFinder();
            MethodCallExpression whereExpression = whereFinder.GetInnermostWhere(expression);
            LambdaExpression lambdaExpression = (LambdaExpression)((UnaryExpression)(whereExpression.Arguments[1])).Operand;

            // Send the lambda expression through the partial evaluator.
            lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

            // Get the WHERE clause query Azure DevOps with.
            var lf = new WiqlVisitor<T>(((AzDOService)service).log,lambdaExpression.Body);
            var values = lf.WhereClauses;
            if (values.Count == 0)
                throw new InvalidQueryException("You must specify at least one place name in your query.");

            // Execute the query.
            var foundItems = ExecuteQuery(values).ToArray();

            // Create a Queryable<T> from the array of objects.
            IQueryable<T> queryableItems = foundItems.AsQueryable();

            // Copy the expression tree that was passed in, changing only the first 
            // argument of the innermost MethodCallExpression. This will essentially create a LINQ expression
            // over the returned array that evaluates the entire expression _client-side_ to catch anything
            // not supported on the server call. It's not efficient, but it's easy.
            var treeCopier = new ExpressionTreeModifier<T>(queryableItems);
            Expression newExpressionTree = treeCopier.Visit(expression);

            newExpressionTree = Evaluator.PartialEval(newExpressionTree).Reduce();

            ((AzDOService)service).log?.WriteLine(LogLevel.LinqQuery, $"QueryContext.Execute => {newExpressionTree} on {foundItems.Length} items.");

            // This step creates an IQueryable that executes by replacing Queryable methods with Enumerable methods. 
            return IsEnumerable
                ? queryableItems.Provider.CreateQuery(newExpressionTree)
                : queryableItems.Provider.Execute(newExpressionTree);
        }

        private IEnumerable<T> ExecuteQuery(List<string> whereClause)
        {
            string query = $"SELECT [System.Id] FROM WorkItems WHERE ";

            if (!string.IsNullOrWhiteSpace(project))
            {
                query += $"[System.TeamProject] = '{project}' AND ";
            }

            if (typeof(T) != typeof(WorkItem))
            {
                var record = ReflectionHelpers.RegisteredTypes.SingleOrDefault(kvp => kvp.Value == typeof(T));
                if (record.Key != null)
                {
                    query += $"[System.WorkItemType] = '{record.Key}' AND ";
                }
            }

            foreach (var item in whereClause)
                query += item;

            var items = service.QueryAsync(query).Result;
            return items.Cast<T>();
        }

        private static bool IsQueryOverDataSource(Expression expression)
        {
            // If expression represents an unqueried IQueryable data source instance, 
            // expression is of type ConstantExpression, not MethodCallExpression. 
            return expression is MethodCallExpression;
        }
    }
}