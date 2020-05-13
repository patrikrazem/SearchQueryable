using System;
using System.Collections;
using System.Collections.Generic;
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
        private static readonly MethodInfo UpperMethod = typeof(string).GetMethod("ToUpperInvariant", new Type[0]);

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
        internal static Expression<Func<TObject, bool>> ConstructSearchPredicate<TObject>(string searchQuery, params Expression<Func<TObject, object>>[] members)
        {
            // Create constant with query
            var constant = Expression.Constant(searchQuery);

            // Input parameter (e.g. "c => ")
            var parameter = Expression.Parameter(typeof(TObject), "c");

            // Construct expression
            Expression finalExpression = null;
            foreach (var m in members) {
                // Visit the provided expression and replace the input parameter with the one defined above ("c")
                // e.g. (from "x.Something" we get "c.Something")
                var memberExpression = new ExpressionParameterVisitor(m.Parameters.First(), parameter)
                    .VisitAndConvert(m.Body, nameof(ConstructSearchPredicate));

                // Get query expression
                var partialExpression = GetQueryExpression(memberExpression, constant, memberExpression.Type);

                // Handle case when no OR operation can be constructed
                if (finalExpression == null) {
                    finalExpression = partialExpression;
                } else {
                    finalExpression = Expression.OrElse(finalExpression, partialExpression);
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
        internal static Expression<Func<T, bool>> ConstructSearchPredicate<T>(string query, CompatiblityMode mode)
        {
            // Create constant with query
            var constant = Expression.Constant(query);

            // Get all object properties
            var type = typeof(T);

            // Input parameter (e.g. "c => ")
            var parameter = Expression.Parameter(type, "c");
            
            // Get appropriate members to test
            var members = GetAvailableMembers(type, mode);

            // Construct expression
            Expression finalExpression = null;

            foreach (var m in members) {
                // Get type of p
                var underlyingType = m.GetUnderlyingType();
                
                // In Strict mode, only string properties are taken into account, otherwise all props and fields are (except collections)
                if ((mode == CompatiblityMode.Strict && underlyingType == typeof(string)) || (mode == CompatiblityMode.All && !underlyingType.IsCollection())) {
                    // Express a member (e.g. "c.<member>" )
                    Expression memberExpression;
                    if (m.MemberType == MemberTypes.Field) {
                        memberExpression = Expression.Field(parameter, m.Name);
                    } else {
                        memberExpression = Expression.Property(parameter, m.Name);
                    }

                    // Get query expression (e.g. "c.<member>.ToString().ToUpperInvariant().Contains(<constant>)")
                    var partialExpression = GetQueryExpression(memberExpression, constant, underlyingType);

                    // Handle case when no OR operation can be constructed (it's the first condition)
                    if (finalExpression == null) {
                        finalExpression = partialExpression;
                    } else {
                        finalExpression = Expression.OrElse(finalExpression, partialExpression);
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
            transformedProperty = Expression.Call(transformedProperty, UpperMethod);

            // Run contains on property with provided query (e.g. "c.<property>.ToString().ToLowerInvariant().Contains(<query>)")
            transformedProperty = Expression.Call(transformedProperty, ContainsMethod, queryConstant);

            if (nullCheckExpression == null) {
                return transformedProperty;
            } else {
                return Expression.AndAlso(nullCheckExpression, transformedProperty);
            }
        }

        private static IEnumerable<MemberInfo> GetAvailableMembers(Type type, CompatiblityMode mode)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;

            if (mode == CompatiblityMode.Strict) {
                return type
                    .GetProperties(flags)
                    .Where(m => m.CanWrite && m.MemberType == MemberTypes.Property);
            } else {
                return type
                    .GetFields(flags).Cast<MemberInfo>()
                    .Concat(type.GetProperties(flags)).ToArray()
                    .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field);
            }   
        }

        private static bool IsCollection(this Type type)
        {
            // Strings are formally collections, so we should handle this separately
            if (type == null || type == typeof(string)) {
                return false;
            }
            return typeof(IEnumerable).IsAssignableFrom(type);
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
                    throw new ArgumentException("Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo");
            }
        }
    }
}
