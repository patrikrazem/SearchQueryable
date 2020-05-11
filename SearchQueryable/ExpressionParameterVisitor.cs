using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace SearchQueryable
{
    /// <summary>
    /// A visitor that programmatically walks through an expression tree and replaces the parameters
    /// </summary>
    public class ExpressionParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression from;
        private readonly ParameterExpression to;

        /// <summary>
        /// Initializes a parameter visitor with the original set of parameters and the replacement
        /// </summary>
        /// <param name="from">The original parameters to be replaced</param>
        /// <param name="to">The parameters with which to replace the original ones</param>
        public ExpressionParameterVisitor(ParameterExpression from, ParameterExpression to)
        {
            if (from == null) {
                throw new ArgumentNullException("From is required");
            }

            if (to == null) {
                throw new ArgumentNullException("to");
            }

            this.from = from;
            this.to = to;
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == from) {
                return to;
            } else {
                return node;
            }
        }
    }
}