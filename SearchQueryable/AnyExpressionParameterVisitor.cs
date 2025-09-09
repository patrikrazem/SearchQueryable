using System.Linq.Expressions;

namespace SearchQueryable;

/// <summary>
/// Expression visitor to replace search terms in collection Any expressions
/// </summary>
internal class AnyExpressionParameterVisitor(Expression newSearchTerm) : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        // Replace references to ExpressionScopedVariable.Value with the new search term
        if (node.Member.DeclaringType == typeof(ExpressionScopedVariable) &&
            node.Member.Name == "Value") {
            return newSearchTerm;
        }

        return base.VisitMember(node);
    }
}
