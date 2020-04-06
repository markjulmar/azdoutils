using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AzDOUtilities.Linq
{
    internal class WiqlVisitor<T> : ExpressionVisitor
    {
        private readonly Expression expression;
        private List<string> values;
        private readonly Stack<string> futureComparisons = new Stack<string>();
        private TraceHelpers log;

        public WiqlVisitor(TraceHelpers log, Expression exp)
        {
            this.expression = exp;
            this.log = log;
        }

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

        void AddClause(string whereClause)
        {
            log?.WriteLine(LogLevel.LinqQuery, $"\tWiqlVisitor.AddClause \"{whereClause}\"");
            values.Add(whereClause);
        }

        PropertyInfo[] TypeProperties => typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                                  .Where(prop => prop.GetCustomAttribute<AzDOFieldAttribute>() != null)
                                                  .ToArray();

        string TranslateFieldName(PropertyInfo propertyInfo)
        {
            var attr = propertyInfo.GetCustomAttribute<AzDOFieldAttribute>();
            var result = attr.FieldName;

            log?.WriteLine(LogLevel.LinqQuery, $"\tWiqlVisitor.TranslateFieldName:{propertyInfo.Name} => {result}");

            return result;
        }

        object TranslateValue(PropertyInfo propertyInfo, object value)
        {
            object result = value;
            var fieldInfo = propertyInfo?.GetCustomAttribute<AzDOFieldAttribute>();
            if (fieldInfo?.Converter != null)
            {
                var converter = (IFieldConverter)Activator.CreateInstance(fieldInfo.Converter);
                if (!(converter is IFieldComparer))
                {
                    result = converter.ConvertBack(value);
                }
            }

            log?.WriteLine(LogLevel.LinqQuery, $"\tWiqlVisitor.TranslateValue:{propertyInfo?.Name}: {value} => {result}");

            return value;
        }

        public override Expression Visit(Expression node)
        {
            using var mc = log?.Enter(LogLevel.LinqExpression, node);
            return base.Visit(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            using var mc = log?.Enter(LogLevel.LinqExpression, node);
            return base.VisitBlock(node);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            using var mc = log?.Enter(LogLevel.LinqExpression, node);
            return base.VisitTypeBinary(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            using var mc = log?.Enter(LogLevel.LinqExpression, node);
            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            using var mc = log?.Enter(LogLevel.LinqExpression, node);
            return base.VisitConstant(node);
        }

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

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            using var mc = log?.Enter(LogLevel.LinqExpression, node);
            return base.VisitConditional(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            string prefix = (futureComparisons.TryPop(out string literalCompare) && literalCompare == "= 'False'") ? "NOT " : "";
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
                PropertyInfo propertyInfo = null;
                Expression valuesExpression = null;

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
                        else if (ExpressionTreeHelpers.IsSpecificMemberExpression(node.Arguments[1], typeof(T), prop.Name))
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
                    AddClause($"{prefix}[{TranslateFieldName(propertyInfo)}] CONTAINS '{ce.Value}' ");
                }
                else
                {
                    // Add each string in the collection to the list of locations to obtain data about.
                    var textValues = (IEnumerable<string>)ce.Value;
                    AddClause($"{prefix}[{TranslateFieldName(propertyInfo)}] IN ({string.Join(',', textValues.Select(s => "'" + s + "'"))}) ");
                }

                return node;
            }

            return base.VisitMethodCall(node);
        }

        Stack<ExpressionType> parentExpression = new Stack<ExpressionType>();
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
            else if (node.NodeType == ExpressionType.OrElse || node.NodeType == ExpressionType.Or)
            {
                log?.WriteLine(LogLevel.LinqQuery, $"\t[{node.NodeType}]: {node.Left.NodeType} vs. {node.Right.NodeType}");

                AddClause("(");
                base.Visit(node.Left);
                AddClause(" OR ");
                base.Visit(node.Right);
                AddClause(")");

                return node;
            }
            else if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.And)
            {
                log?.WriteLine(LogLevel.LinqQuery, $"\t[{node.NodeType}]: {node.Left.NodeType} vs. {node.Right.NodeType}");

                AddClause("(");
                base.Visit(node.Left);
                AddClause(" AND ");
                base.Visit(node.Right);
                AddClause(")");

                return node;
            }
            else
            {
                log?.WriteLine(LogLevel.LinqQuery, $"ERROR: unsupported comparison: {node.NodeType}");
                return base.VisitBinary(node);
            }
        }
    }
}