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
            int? takeValue = null;
            List<string> values = null;

            if (IsQueryOverDataSource(expression))
            {
                // See if we have a TAKE expression.
                var takeExpression = new TakeFinder().FindTake(expression);
                if (takeExpression != null)
                {
                    ConstantExpression ce = (ConstantExpression) takeExpression.Arguments[1];
                    takeValue = Convert.ToInt32(ce.Value);
                }

                // Find the call to Where() and get the lambda expression predicate.
                var whereExpression = new InnermostWhereFinder().GetInnermostWhere(expression);
                if (whereExpression != null)
                {
                    LambdaExpression lambdaExpression = (LambdaExpression)((UnaryExpression)(whereExpression.Arguments[1])).Operand;
                    if (lambdaExpression != null)
                    {
                        // Send the lambda expression through the partial evaluator.
                        lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

                        // Get the WHERE clause query Azure DevOps with.
                        var lf = new WiqlVisitor<T>(((AzDOService)service).log, lambdaExpression.Body);
                        values = lf.WhereClauses;
                    }
                }
            }

            // Execute the query.
            var foundItems = ExecuteQuery(values, takeValue).ToArray();

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

        private IEnumerable<T> ExecuteQuery(List<string> whereClause, int? take)
        {
            bool addedWhere = false;
            const string WhereClause = " WHERE ";
            const string AndClause = " AND ";

            string query = $"SELECT [System.Id] FROM WorkItems";

            if (!string.IsNullOrWhiteSpace(project))
            {
                addedWhere = true;
                query += WhereClause;
                query += $"[System.TeamProject] = '{project}'";
            }

            if (typeof(T) != typeof(WorkItem))
            {
                var record = ReflectionHelpers.RegisteredTypes.SingleOrDefault(kvp => kvp.Value == typeof(T));
                if (record.Key != null)
                {
                    if (!addedWhere)
                    {
                        addedWhere = true;
                        query += WhereClause;
                    }
                    else
                    {
                        query += AndClause;
                    }

                    query += $"[System.WorkItemType] = '{record.Key}'";
                }
            }

            if (whereClause?.Count > 0)
            {
                if (!addedWhere)
                {
                    addedWhere = true;
                    query += WhereClause;
                }
                else
                {
                    query += AndClause;
                }

                foreach (var item in whereClause)
                    query += item;
            }

            var items = service.QueryAsync(query, take).Result;
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