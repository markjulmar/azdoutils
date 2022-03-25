using System.Linq.Expressions;
using System.Reflection;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// WIQL expression handler
/// </summary>
/// <typeparam name="T"></typeparam>
internal class WiqlVisitor<T> : ExpressionVisitor
{
    private readonly Expression expression;
    private List<string>? values;
    private readonly Stack<string> futureComparisons = new();
    private readonly TraceHelpers? log;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="log"></param>
    /// <param name="exp"></param>
    public WiqlVisitor(TraceHelpers? log, Expression exp)
    {
        this.expression = exp;
        this.log = log;
    }

    /// <summary>
    /// Handle a Where clause
    /// </summary>
    public List<string> WhereClauses
    {
        get
        {
            if (values == null)
            {
                values = new List<string>();
                this.Visit(this.expression);
            }
            return this.values;
        }
    }

    /// <summary>
    /// Add a clause to the query.
    /// </summary>
    /// <param name="whereClause">Where clause to add</param>
    /// <exception cref="InvalidOperationException"></exception>
    void AddClause(string whereClause)
    {
        log?.WriteLine(LogLevel.LinqQuery, $"\tWiqlVisitor.AddClause \"{whereClause}\"");
        if (values == null) throw new InvalidOperationException("AddClause called before visit.");
        values.Add(whereClause);
    }

    /// <summary>
    /// Get the available properties from the type.
    /// </summary>
    PropertyInfo[] TypeProperties => typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
        .Where(prop => prop.GetCustomAttribute<AzDOFieldAttribute>() != null)
        .ToArray();

    /// <summary>
    /// Translate the property into a specific field name.
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    private string TranslateFieldName(PropertyInfo? propertyInfo)
    {
        if (propertyInfo == null) return "";

        var attr = propertyInfo.GetCustomAttribute<AzDOFieldAttribute>();
        var result = attr?.FieldName ?? throw new ArgumentOutOfRangeException(nameof(propertyInfo), $"Missing AzDOField attribute on {propertyInfo.Name}");

        log?.WriteLine(LogLevel.LinqQuery, $"\tWiqlVisitor.TranslateFieldName:{propertyInfo.Name} => {result}");

        return result;
    }

    /// <summary>
    /// Translate a value using the AzDOField attribute
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private object TranslateValue(PropertyInfo? propertyInfo, object value)
    {
        object result = value;
        var fieldInfo = propertyInfo?.GetCustomAttribute<AzDOFieldAttribute>();
        if (fieldInfo?.Converter != null)
        {
            var converter = (IFieldConverter?) Activator.CreateInstance(fieldInfo.Converter);
            if (converter == null)
                throw new InvalidOperationException($"Failed to create converter {fieldInfo.Converter.Name}");
            if (converter is not IFieldComparer)
            {
                result = converter.ConvertBack(value) ?? value;
            }
        }

        log?.WriteLine(LogLevel.LinqQuery, $"\tWiqlVisitor.TranslateValue:{propertyInfo?.Name ?? "null"}: {value} => {result}");

        return result;
    }

    /// <summary>Dispatches the expression to one of the more specialized visit methods in this class.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    public override Expression? Visit(Expression? node)
    {
        using var mc = log?.Enter(LogLevel.LinqExpression, node);
        return base.Visit(node);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.BlockExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitBlock(BlockExpression node)
    {
        using var mc = log?.Enter(LogLevel.LinqExpression, node);
        return base.VisitBlock(node);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.TypeBinaryExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        using var mc = log?.Enter(LogLevel.LinqExpression, node);
        return base.VisitTypeBinary(node);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MemberExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitMember(MemberExpression node)
    {
        using var mc = log?.Enter(LogLevel.LinqExpression, node);
        return base.VisitMember(node);
    }

    /// <summary>Visits the <see cref="T:System.Linq.Expressions.ConstantExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitConstant(ConstantExpression node)
    {
        using var mc = log?.Enter(LogLevel.LinqExpression, node);
        return base.VisitConstant(node);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.UnaryExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        using var mc = log?.Enter(LogLevel.LinqExpression, node);

        if (node.NodeType == ExpressionType.Not)
        {
            AddClause(" NOT ");
        }
        else if (node.NodeType == ExpressionType.Convert)
        {
            // TODO: add support for type conversion? Enums?
        }

        return base.VisitUnary(node);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.ConditionalExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitConditional(ConditionalExpression node)
    {
        using var mc = log?.Enter(LogLevel.LinqExpression, node);
        return base.VisitConditional(node);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MethodCallExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        string prefix = (futureComparisons.TryPop(out var literalCompare) && literalCompare == "= 'False'") ? "NOT " : "";
        using var mc = log?.Enter(LogLevel.LinqExpression, node);

        // member.Contains(...) || member.StartsWith(...)
        if (node.Method.DeclaringType == typeof(string) && (node.Method.Name == nameof(string.StartsWith)
                                                            || node.Method.Name == nameof(string.Contains)))
        {
            foreach (var prop in TypeProperties.Where(prop => prop.PropertyType == typeof(string)))
            {
                if (ExpressionTreeHelpers.IsSpecificMemberExpression(node.Object, typeof(T), prop.Name))
                {
                    object testValue = TranslateValue(prop, ExpressionTreeHelpers.GetValueFromExpression(node.Arguments[0]));
                    AddClause($"{prefix}[{TranslateFieldName(prop)}] CONTAINS '{testValue}' ");
                    return node;
                }
            }
        }
        // array.Contains(member) || List<string>.Contains(member)
        else if (node.Method.Name == nameof(string.Contains))
        {
            PropertyInfo? propertyInfo = null;
            Expression? valuesExpression = null;

            if (node.Method.DeclaringType == typeof(Enumerable))
            {
                foreach (var prop in TypeProperties)
                {
                    if (ExpressionTreeHelpers.IsSpecificMemberExpression(node.Arguments[0], typeof(T), prop.Name))
                    {
                        propertyInfo = prop;
                        valuesExpression = node.Arguments[1];
                        break;
                    }
                    if (ExpressionTreeHelpers.IsSpecificMemberExpression(node.Arguments[1], typeof(T), prop.Name))
                    {
                        propertyInfo = prop;
                        valuesExpression = node.Arguments[0];
                        break;
                    }
                }
            }
            else if (node.Method.DeclaringType == typeof(List<string>))
            {
                foreach (var prop in TypeProperties)
                {
                    if (ExpressionTreeHelpers.IsSpecificMemberExpression(node.Arguments[0], typeof(T), prop.Name))
                    {
                        propertyInfo = prop;
                        valuesExpression = node.Object;
                        break;
                    }
                    if (ExpressionTreeHelpers.IsSpecificMemberExpression(node.Object, typeof(T), prop.Name))
                    {
                        propertyInfo = prop;
                        valuesExpression = node.Arguments[0];
                        break;
                    }
                }
            }
            else
            {
                throw new Exception($"{node.Method.DeclaringType} must be Enumerable or List<string> to use 'Contains'");
            }

            if (valuesExpression == null || valuesExpression.NodeType != ExpressionType.Constant)
                throw new Exception($"'Contains' expression must compare to a set of literals - {valuesExpression?.NodeType}");

            ConstantExpression ce = (ConstantExpression)valuesExpression;

            if (ce.Value is string text) // single value
            {
                AddClause($"{prefix}[{TranslateFieldName(propertyInfo)}] CONTAINS '{text}' ");
            }
            else if (ce.Value is IEnumerable<string> textValues)
            {
                // Add each string in the collection to the list of locations to obtain data about.
                AddClause($"{prefix}[{TranslateFieldName(propertyInfo)}] IN ({string.Join(',', textValues.Select(s => "'" + s + "'"))}) ");
            }

            return node;
        }

        return base.VisitMethodCall(node);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.BinaryExpression" />.</summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        using var mc = log?.Enter(LogLevel.LinqExpression, node);

        if (ExpressionTreeHelpers.SupportedComparisons.ContainsKey(node.NodeType))
        {
            foreach (var prop in TypeProperties)
            {
                if (ExpressionTreeHelpers.IsMatchingMemberValueExpression(node, typeof(T), prop.Name))
                {
                    object testValue = TranslateValue(prop, ExpressionTreeHelpers.GetValueFromExpression(node, typeof(T), prop.Name));
                    AddClause($"[{TranslateFieldName(prop)}] {ExpressionTreeHelpers.SupportedComparisons[node.NodeType]} '{testValue}' ");
                    return node;
                }
            }

            if (node.Left.NodeType == ExpressionType.Call)
            {
                object testValue = TranslateValue(null, ExpressionTreeHelpers.GetValueFromExpression(node.Right));
                futureComparisons.Push($"{ExpressionTreeHelpers.SupportedComparisons[node.NodeType]} '{testValue}'");
            }

            return base.VisitBinary(node);
        }
        
        if (node.NodeType is ExpressionType.OrElse or ExpressionType.Or)
        {
            log?.WriteLine(LogLevel.LinqQuery, $"\t[{node.NodeType}]: {node.Left.NodeType} vs. {node.Right.NodeType}");

            AddClause("(");
            base.Visit(node.Left);
            AddClause(" OR ");
            base.Visit(node.Right);
            AddClause(")");

            return node;
        }
        
        if (node.NodeType is ExpressionType.AndAlso or ExpressionType.And)
        {
            log?.WriteLine(LogLevel.LinqQuery, $"\t[{node.NodeType}]: {node.Left.NodeType} vs. {node.Right.NodeType}");

            AddClause("(");
            base.Visit(node.Left);
            AddClause(" AND ");
            base.Visit(node.Right);
            AddClause(")");

            return node;
        }

        log?.WriteLine(LogLevel.LinqQuery, $"ERROR: unsupported comparison: {node.NodeType}");
        return base.VisitBinary(node);
    }
}