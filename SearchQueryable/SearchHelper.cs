using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SearchQueryable
{
    internal static class SearchHelper
    {
        /// <summary>
        /// A constant definition of the ToLowerInvariant method to use in expressions
        /// </summary>
        private static readonly MethodInfo LowerMethod = typeof(string).GetMethod("ToLowerInvariant", new Type[0]);

        /// <summary>
        /// A constant definition of the Contants method to use in expressions
        /// </summary>
        private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

        /// <summary>
        /// Constructs an expression that is used to filter entries and execute a search for the specified query
        /// on all string type fields of an entity
        /// </summary>
        /// <param name="searchQuery">The query to be searched for in all string fields</param>
        /// <returns>An expression that can be used in queries to the DB context</returns>
        ///
        /// Example:
        /// For an entity with string properties `Name` and `Address`, the resulting expression
        /// is something like this:
        ///
        /// `x => x.Name.ToLower().Contains(query) || x.Address.ToLower().Contains(query)`
        ///
        internal static Expression<Func<TObject, bool>> ConstructSearchPredicate<TObject>(string searchQuery, params Expression<Func<TObject, object>>[] fields)
        {
            // Create constant with query
            var constant = Expression.Constant(searchQuery);

            // Input parameter (e.g. "c => ")
            var parameter = Expression.Parameter(typeof(TObject), "c");

            // Construct expression
            Expression finalExpression = null;
            foreach (var f in fields) {
                // Visit the provided expression and replace the input parameter with the one defined above ("c")
                // e.g. (from "x.Something" we get "c.Something")
                var propertyExpression = new ExpressionParameterVisitor(f.Parameters.First(), parameter)
                    .VisitAndConvert(f.Body, nameof(ConstructSearchPredicate));

                // Get query expression
                var partialExpression = GetQueryExpression(propertyExpression, constant, propertyExpression.Type);

                // Handle case when no OR operation can be constructed
                if (finalExpression == null) {
                    finalExpression = partialExpression;
                } else {
                    finalExpression = Expression.Or(finalExpression, partialExpression);
                }
            }

            // Return the constructed expression
            return Expression.Lambda<Func<TObject, bool>>(finalExpression, parameter);
        }

        /// <summary>
        /// Constructs an expression that is used to filter entries and execute a search for the specified query
        /// on all string type fields of an entity
        /// </summary>
        /// <param name="query">The query to be searched for in all string fields</param>
        /// <returns>An expression that can be used in queries to the DB context</returns>
        ///
        /// Example:
        /// For an entity with string properties `Name` and `Address`, the resulting expression
        /// is something like this:
        ///
        /// `x => x.Name.ToLower().Contains(query) || x.Address.ToLower().Contains(query)`
        ///
        internal static Expression<Func<T, bool>> ConstructSearchPredicate<T>(string query)
        {
            // Create constant with query
            var constant = Expression.Constant(query);

            // Input parameter (e.g. "c => ")
            var parameter = Expression.Parameter(typeof(T), "c");

            // Get all object properties
            var type = typeof(T);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var propertiesOrFields = type
                .GetFields(flags).Cast<MemberInfo>()
                .Concat(type.GetProperties(flags)).ToArray();

            // Construct expression
            Expression finalExpression = null;
            foreach (var p in propertiesOrFields) {
                // Get type of p
                var pType = p.GetUnderlyingType();

                if ((p.MemberType == MemberTypes.Property || p.MemberType == MemberTypes.Field)) {
                    // Express a property (e.g. "c.<property>" )
                    Expression propertyExpression;
                    if (p.MemberType == MemberTypes.Field) {
                        propertyExpression = Expression.Field(parameter, p.Name);
                    } else {
                        propertyExpression = Expression.Property(parameter, p.Name);
                    }

                    // Get query expression
                    var partialExpression = GetQueryExpression(propertyExpression, constant, pType);

                    // Handle case when no OR operation can be constructed
                    if (finalExpression == null) {
                        finalExpression = partialExpression;
                    } else {
                        finalExpression = Expression.Or(finalExpression, partialExpression);
                    }
                }
            }


            // Return the constructed expression
            return Expression.Lambda<Func<T, bool>>(finalExpression, parameter);
        }
    
        /// <summary>
        /// Constructs an expression to query the property for the specified search term
        /// </summary>
        /// <param name="propertyExpression">The expression of the property being tested</param>
        /// <param name="queryConstant">The expression representing the search term</param>
        /// <param name="propertyType">The type of the property to be tested</param>
        /// The resulting expression will test the property for inclusion of the query
        /// (e.g. "c.<property>.ToString().ToLowerInvariant().Contains(<queryConstant>)")
        private static Expression GetQueryExpression(Expression propertyExpression, Expression queryConstant, Type propertyType)
        {
            // Check that property value is not null (or default) (e.g. "c.<property> != null")
            Expression nullCheckExpression = null;

            // Value types can safely be operated on, since they have non-null default values
            if (!propertyType.IsValueType) {
                nullCheckExpression = Expression.NotEqual(propertyExpression, Expression.Constant(null, propertyType));
            }

            // Find the ToString method that should be executed for the specific type
            var toStringMethod = propertyType.GetMethod("ToString", new Type[0]);

            // Run ToString method on property (e.g. "c.<property>.ToString()")
            var transformedProperty = Expression.Call(propertyExpression, toStringMethod);

            // Run lowercase method on property (e.g. "c.<property>.ToString().ToLowerInvariant()")
            transformedProperty = Expression.Call(transformedProperty, LowerMethod);

            // Run contains on property with provided query (e.g. "c.<property>.ToString().ToLowerInvariant().Contains(<query>)")
            transformedProperty = Expression.Call(transformedProperty, ContainsMethod, queryConstant);

            if (nullCheckExpression == null) {
                return transformedProperty;
            } else {
                return Expression.AndAlso(nullCheckExpression, transformedProperty);
            }
        }

        private static Type GetUnderlyingType(this MemberInfo member)
        {
            switch (member.MemberType) {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new ArgumentException
                    (
                     "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                    );
            }
        }
    }
}
