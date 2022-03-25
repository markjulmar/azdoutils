using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// Query context interface
/// </summary>
interface IQueryContext
{
    /// <summary>
    /// Execute a query based on an expression
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="isEnumerable"></param>
    /// <returns></returns>
    object Execute(Expression expression, bool isEnumerable);
}