using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq
{
    class ExpressionTreeHelpers
    {
        internal static Dictionary<ExpressionType, string> SupportedComparisons = new Dictionary<ExpressionType, string>()
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

        internal static bool IsSpecificMemberExpression(Expression exp, Type declaringType, string memberName)
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
                MemberExpression me = (MemberExpression)be.Left;

                if (me.Member.Name == memberName)
                {
                    return GetValueFromExpression(be.Right);
                }
            }
            else if (be.Right.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression me = (MemberExpression)be.Right;

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
                return (ce.Value == null)
                    ? "" : ce.Value.ToString();
            }
            else
                throw new InvalidQueryException(
                    String.Format("The expression type {0} is not supported to obtain a value.", expression.NodeType));
        }
    }

    public class InvalidQueryException : Exception
    {
        private readonly string message;

        public InvalidQueryException(string message)
        {
            this.message = message + " ";
        }

        public override string Message => "The client query is invalid: " + message;
    }
}