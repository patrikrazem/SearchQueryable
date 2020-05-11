using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SearchQueryable
{
    public static class SearchHelper
    {

        /// <summary>
        /// Extensions method for filtering entries by all their string fields
        /// </summary>
        /// <param name="searchQuery">The search query by which the entries should be filtered</param>
        /// <returns>A filteres collection of entries</returns>
        public static IQueryable<T> Search<T>(this IQueryable<T> data, string searchQuery)
        {
            var matches = data;
            foreach (var part in searchQuery.ToLowerInvariant().Split()) {
                if (!string.IsNullOrWhiteSpace(part)) {
                    matches = matches.Where(SearchHelper.ConstructSearchPredicate<T>(part.Trim()));
                }
            }

            return matches;
        }

        /// <summary>
        /// Extensions method for filtering entries by specified fields
        /// </summary>
        /// <param name="searchQuery">The search query by which the entries should be filtered</param>
        /// <param name="fields">The fields that should be queried with the specified search string</param>
        /// <returns>A filteres collection of entries</returns>
        public static IQueryable<T> Search<T>(this IQueryable<T> data, string searchQuery, params Expression<Func<T, string>>[] fields)
        {
            var matches = data;
            foreach (var part in searchQuery.ToLowerInvariant().Split()) {
                if (!string.IsNullOrWhiteSpace(part)) {
                    matches = matches.Where(SearchHelper.ConstructSearchPredicate<T>(part.Trim(), fields));
                }
            }

            return matches;
        }

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
        private static Expression<Func<T, bool>> ConstructSearchPredicate<T>(string searchQuery, params Expression<Func<T, string>>[] fields)
        {
            // Create constant with query
            var constant = Expression.Constant(searchQuery);

            // Input parameter (e.g. "c => ")
            var parameter = Expression.Parameter(typeof(T), "c");

            // Find methods that will be run on each property
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var lowerMethod = typeof(string).GetMethod("ToLowerInvariant", new Type[0]);

            // Construct expression
            Expression finalBody = null;
            foreach (var f in fields) {
                // Visit the provided expression and replace the input parameter with the one defined above ("c")
                // e.g. (from "x.Something" we get "c.Something")
                var propertyExpression = new ExpressionParameterVisitor(f.Parameters.First(), parameter)
                    .VisitAndConvert(f.Body, nameof(ConstructSearchPredicate));

                // Coalesce value, if null, to default (e.g. "c.<property> ?? string.)
                var transformedProperty = (Expression) Expression.Coalesce(propertyExpression, Expression.Constant(string.Empty));

                // Run lowercase method on property (e.g. "c.<property>.ToLowerInvariant()")
                transformedProperty = Expression.Call(transformedProperty, lowerMethod);

                // Run contains on property with provided query (e.g. "c.<property>.ToLowerInvariant().Contains(<query>)")
                transformedProperty = Expression.Call(transformedProperty, containsMethod, constant);

                // Handle case when no OR operation can be constructed
                if (finalBody == null) {
                    finalBody = transformedProperty;
                } else {
                    finalBody = Expression.Or(finalBody, transformedProperty);
                }
            }

            // Return the constructed expression
            return Expression.Lambda<Func<T, bool>>(finalBody, parameter);
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
        private static Expression<Func<T, bool>> ConstructSearchPredicate<T>(string query)
        {
            // Create constant with query
            var constant = Expression.Constant(query);

            // Input parameter (e.g. "c => ")
            var parameter = Expression.Parameter(typeof(T), "c");

            // Find methods that will be run on each property
            var toStringMethod = typeof(string).GetMethod("ToString", new Type[0]);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var lowerMethod = typeof(string).GetMethod("ToLowerInvariant", new Type[0]);

            // Get all object properties
            var type = typeof(T);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var propertiesOrFields = type
                .GetFields(flags).Cast<MemberInfo>()
                .Concat(type.GetProperties(flags)).ToArray();

            // Construct expression
            Expression finalBody = null;
            foreach (var p in propertiesOrFields) {
                if ((p.MemberType == MemberTypes.Property || p.MemberType == MemberTypes.Field) && p.GetUnderlyingType() == typeof(string)) {
                    // Express a property (e.g. "c.<property>" )
                    Expression expressionProperty;
                    if (p.MemberType == MemberTypes.Field) {
                        expressionProperty = Expression.Field(parameter, p.Name);
                    } else {
                        expressionProperty = Expression.Property(parameter, p.Name);
                    }

                    // Coalesce value, if null, to default (e.g. "c.<property> ?? string.)
                    var transformedProperty = (Expression) Expression.Coalesce(expressionProperty, Expression.Constant(string.Empty));

                    // Run ToString method on property (e.g. "c.<property>.ToString()")
                    transformedProperty = Expression.Call(transformedProperty, toStringMethod);

                    // Run lowercase method on property (e.g. "c.<property>.ToString().ToLowerInvariant()")
                    transformedProperty = Expression.Call(transformedProperty, lowerMethod);

                    // Run contains on property with provided query (e.g. "c.<property>.ToString().ToLowerInvariant().Contains(<query>)")
                    transformedProperty = Expression.Call(transformedProperty, containsMethod, constant);

                    // Handle case when no OR operation can be constructed
                    if (finalBody == null) {
                        finalBody = transformedProperty;
                    } else {
                        finalBody = Expression.Or(finalBody, transformedProperty);
                    }
                }
            }

            // Return the constructed expression
            return Expression.Lambda<Func<T, bool>>(finalBody, parameter);
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
