using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// Query context implementation
/// </summary>
/// <typeparam name="T"></typeparam>
internal class QueryContext<T> : IQueryContext
{
    private readonly string? project;
    private readonly AzDOService service;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="service"></param>
    /// <param name="project"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public QueryContext(IAzureDevOpsService service, string? project)
    {
        this.service = service as AzDOService ?? throw new ArgumentNullException(nameof(service));
        this.project = project;
    }

    public object Execute(Expression expression, bool isEnumerable)
    {
        int? takeValue = null;
        List<string>? values = null;

        if (IsQueryOverDataSource(expression))
        {
            // See if we have a TAKE expression.
            var takeExpression = new TakeFinder().FindTake(expression);
            if (takeExpression != null)
            {
                var ce = (ConstantExpression) takeExpression.Arguments[1];
                takeValue = Convert.ToInt32(ce.Value);
            }

            // Find the call to Where() and get the lambda expression predicate.
            var whereExpression = new InnermostWhereFinder().GetInnermostWhere(expression);
            
            var lambdaExpression = (LambdaExpression?) ((UnaryExpression?) whereExpression?.Arguments[1])?.Operand;
            if (lambdaExpression != null)
            {
                // Send the lambda expression through the partial evaluator.
                lambdaExpression = (LambdaExpression?) Evaluator.PartialEval(lambdaExpression);
                if (lambdaExpression != null)
                {
                    // Get the WHERE clause query Azure DevOps with.
                    var lf = new WiqlVisitor<T>(service.log, lambdaExpression.Body);
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
        var newExpressionTree = treeCopier.Visit(expression);

        newExpressionTree = Evaluator.PartialEval(newExpressionTree)?.Reduce();
        if (newExpressionTree == null) throw new InvalidQueryException("Unable to evaluate expression.");

        service.log?.WriteLine(LogLevel.LinqQuery, $"QueryContext.Execute => {newExpressionTree} on {foundItems.Length} items.");

        // This step creates an IQueryable that executes by replacing Queryable methods with Enumerable methods. 
        return isEnumerable
            ? queryableItems.Provider.CreateQuery(newExpressionTree)
            : queryableItems.Provider.Execute(newExpressionTree)!;
    }

    /// <summary>
    /// Executes a query
    /// </summary>
    /// <param name="whereClause"></param>
    /// <param name="take"></param>
    /// <returns></returns>
    private IEnumerable<T> ExecuteQuery(List<string>? whereClause, int? take)
    {
        bool addedWhere = false;
        const string WhereClause = " WHERE ";
        const string AndClause = " AND ";

        string query = $"SELECT * FROM WorkItems";

        if (!string.IsNullOrWhiteSpace(project))
        {
            addedWhere = true;
            query += WhereClause;
            query += $"[System.TeamProject] = '{project}'";
        }

        if (typeof(T) != typeof(WorkItem))
        {
            var record = ReflectionHelpers.RegisteredTypes.SingleOrDefault(kvp => kvp.Value == typeof(T));
            if (!string.IsNullOrEmpty(record.Key))
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

        var items = service.QueryForTypeAsync(typeof(T), query, take, 
                                        timePrecision:true, CancellationToken.None).Result;
        return items.Cast<T>();
    }

    private static bool IsQueryOverDataSource(Expression expression)
    {
        // If expression represents an unqueried IQueryable data source instance, 
        // expression is of type ConstantExpression, not MethodCallExpression. 
        return expression is MethodCallExpression;
    }
}