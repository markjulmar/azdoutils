using System.Linq.Expressions;

namespace Julmar.AzDOUtilities.Linq;

/// <summary>
/// LINQ evaluator
/// </summary>
static class Evaluator
{
    /// <summary> 
    /// Performs evaluation and replacement of independent sub-trees 
    /// </summary> 
    /// <param name="expression">The root of the expression tree.</param>
    /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns> 
    public static Expression? PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated) 
        => new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);

    /// <summary> 
    /// Performs evaluation and replacement of independent sub-trees 
    /// </summary> 
    /// <param name="expression">The root of the expression tree.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns> 
    public static Expression? PartialEval(Expression expression) => PartialEval(expression, CanBeEvaluatedLocally);

    private static bool CanBeEvaluatedLocally(Expression expression) => expression.NodeType != ExpressionType.Parameter;

    /// <summary> 
    /// Evaluates and replaces sub-trees when first candidate is reached (top-down) 
    /// </summary> 
    class SubtreeEvaluator : ExpressionVisitor
    {
        private readonly HashSet<Expression> candidates;

        internal SubtreeEvaluator(HashSet<Expression> candidates)
        {
            this.candidates = candidates;
        }

        internal Expression? Eval(Expression? exp) => this.Visit(exp);

        public override Expression? Visit(Expression? exp)
        {
            return exp == null
                ? null
                : this.candidates.Contains(exp)
                    ? Evaluate(exp)
                    : base.Visit(exp);
        }

        private static Expression Evaluate(Expression e)
        {
            if (e.NodeType == ExpressionType.Constant)
                return e;

            var lambda = Expression.Lambda(e);
            var fn = lambda.Compile();
            return Expression.Constant(fn.DynamicInvoke(null), e.Type);
        }
    }

    /// <summary> 
    /// Performs bottom-up analysis to determine which nodes can possibly 
    /// be part of an evaluated sub-tree. 
    /// </summary> 
    class Nominator : ExpressionVisitor
    {
        private readonly Func<Expression, bool> fnCanBeEvaluated;
        private readonly HashSet<Expression> candidates;
        private bool cannotBeEvaluated;

        internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
        {
            this.fnCanBeEvaluated = fnCanBeEvaluated;
            this.candidates = new HashSet<Expression>();
        }

        internal HashSet<Expression> Nominate(Expression? expression)
        { 
            candidates.Clear();
            Visit(expression);
            return candidates;
        }

        public override Expression? Visit(Expression? expression)
        {
            if (expression != null)
            {
                bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                this.cannotBeEvaluated = false;
                base.Visit(expression);
                if (!this.cannotBeEvaluated)
                {
                    if (this.fnCanBeEvaluated(expression))
                    {
                        this.candidates.Add(expression);
                    }
                    else
                    {
                        this.cannotBeEvaluated = true;
                    }
                }
                this.cannotBeEvaluated |= saveCannotBeEvaluated;
            }
            return expression;
        }
    }
}