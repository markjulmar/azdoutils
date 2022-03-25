using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// LINQ expression helpers
/// </summary>
static class ExpressionTreeHelpers
{
    internal static readonly Dictionary<ExpressionType, string> SupportedComparisons = new()
    {
        { ExpressionType.Equal, "=" },
        { ExpressionType.NotEqual, "<>" },
        { ExpressionType.GreaterThan, ">" },
        { ExpressionType.GreaterThanOrEqual, ">=" },
        { ExpressionType.LessThan, "<" },
        { ExpressionType.LessThanOrEqual, "<=" }
    };

    internal static bool IsMatchingMemberValueExpression(Expression exp, Type declaringType, string memberName)
    {
        if (!SupportedComparisons.ContainsKey(exp.NodeType))
            return false;

        BinaryExpression be = (BinaryExpression) exp;

        if (IsSpecificMemberExpression(be.Left, declaringType, memberName) &&
            IsSpecificMemberExpression(be.Right, declaringType, memberName))
            throw new Exception("Cannot have 'member' on both sides of an expression.");

        return IsSpecificMemberExpression(be.Left, declaringType, memberName) ||
               IsSpecificMemberExpression(be.Right, declaringType, memberName);
    }

    internal static bool IsSpecificMemberExpression(Expression? exp, Type? declaringType, string memberName)
    {
        if (exp is MemberExpression mexp)
        {
            while (declaringType != null)
            {
                if (mexp.Member.DeclaringType == declaringType
                    && mexp.Member.Name == memberName)
                    return true;
                declaringType = declaringType.BaseType;
            }
        }

        return false;
    }

    internal static string GetValueFromExpression(BinaryExpression be, Type memberDeclaringType, string memberName)
    {
        if (!SupportedComparisons.ContainsKey(be.NodeType))
            throw new Exception($"Unsupported comparison type {be.NodeType}");

        if (be.Left.NodeType == ExpressionType.MemberAccess)
        {
            var me = (MemberExpression)be.Left;
            if (me.Member.Name == memberName)
            {
                return GetValueFromExpression(be.Right);
            }
        }
        else if (be.Right.NodeType == ExpressionType.MemberAccess)
        {
            var me = (MemberExpression)be.Right;
            if (me.Member.Name == memberName)
            {
                return GetValueFromExpression(be.Left);
            }
        }

        // We should have returned by now. 
        throw new Exception("There is a bug in this program. (#2)");
    }

    internal static string GetValueFromExpression(Expression expression)
    {
        if (expression.NodeType == ExpressionType.Constant)
        {
            var ce = (ConstantExpression)expression;
            return ce.Value?.ToString() ?? "";
        }
        throw new InvalidQueryException($"The expression type {expression.NodeType} is not supported to obtain a value.");
    }
}