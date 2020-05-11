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
        /// Types of properties/fields that can be searched
        /// </summary>
        private static Type[] AvailableTypes = new Type[] {
            typeof(string),
            typeof(int),
            typeof(decimal)
        };

        /// <summary>
        /// A constant definition of the ToLowerInvariant method to use in expressions
        /// </summary>
        private static readonly MethodInfo LowerMethod = typeof(string).GetMethod("ToLowerInvariant", new Type[0]);

        /// <summary>
        /// A constant definition of the Contants method to use in expressions
        /// </summary>
        private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

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
        public static IQueryable<T> Search<T, U>(this IQueryable<T> data, string searchQuery, params Expression<Func<T, U>>[] fields)
        {
            var matches = data;
            foreach (var part in searchQuery.ToLowerInvariant().Split()) {
                if (!string.IsNullOrWhiteSpace(part)) {
                    matches = matches.Where(SearchHelper.ConstructSearchPredicate<T, U>(part.Trim(), fields));
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
        private static Expression<Func<TObject, bool>> ConstructSearchPredicate<TObject, TMember>(string searchQuery, params Expression<Func<TObject, TMember>>[] fields)
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

                // Construct an expression that will check that the value is not null
                var nullCheckExpression = GetNullCheckExpression<TMember>(propertyExpression);

                // Construct an expression that will query the value
                var queryExpression = GetQueryExpression<TMember>(propertyExpression, constant);

                // Combine the null checking expression, if needed (reference types)
                var partialExpression = queryExpression;
                if (nullCheckExpression != null) {
                    partialExpression = Expression.AndAlso(nullCheckExpression, queryExpression);
                }

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
        private static Expression<Func<T, bool>> ConstructSearchPredicate<T>(string query)
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

                if ((p.MemberType == MemberTypes.Property || p.MemberType == MemberTypes.Field) && AvailableTypes.Contains(pType)) {
                    // Express a property (e.g. "c.<property>" )
                    Expression expressionProperty;
                    if (p.MemberType == MemberTypes.Field) {
                        expressionProperty = Expression.Field(parameter, p.Name);
                    } else {
                        expressionProperty = Expression.Property(parameter, p.Name);
                    }
                    
                    // Check that property value is not null (or default) (e.g. "c.<property> != null")
                    var nullCheckExpression = GetNullCheckExpression(expressionProperty, pType);

                    // Get query expression
                    var queryExpression = GetQueryExpression(expressionProperty, constant, pType);

                    var partialExpression = queryExpression;
                    if (nullCheckExpression != null) {
                        partialExpression = Expression.AndAlso(nullCheckExpression, queryExpression);
                    }

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
        /// Constructs an expression to check that the property is not null, if needed
        /// </summary>
        /// <param name="propertyExpression">The expression of the property</param>
        /// The resulting expression will check that the property is not null (e.g. "c.<property> != null")
        /// This is only required for reference types, since value types have non-null default values
        private static Expression GetNullCheckExpression<TMember>(Expression propertyExpression)
            => GetNullCheckExpression(propertyExpression, typeof(TMember));
        

        /// <summary>
        /// Constructs an expression to check that the property is not null, if needed
        /// </summary>
        /// <param name="propertyExpression">The expression of the property</param>
        /// <param name="propertyType">The type of the property</param>
        /// The resulting expression will check that the property is not null (e.g. "c.<property> != null")
        /// This is only required for reference types, since value types have non-null default values
        private static Expression GetNullCheckExpression(Expression propertyExpression, Type propertyType)
        {
            // Value types have non-null defaults and can be safely operated on
            if (propertyType.IsValueType) {
                return null;
            }

            // Check that property value is not null (or default) (e.g. "c.<property> != null")
            var nullCheckExpression = Expression.NotEqual(propertyExpression, Expression.Constant(null, propertyType));

            return nullCheckExpression;
        }

        /// <summary>
        /// Constructs an expression to query the property for the specified search term
        /// </summary>
        /// <param name="propertyExpression">The expression of the property being tested</param>
        /// <param name="queryConstant">The expression representing the search term</param>
        /// The resulting expression will test the property for inclusion of the query
        /// c.<property>.ToString().ToLowerInvariant().Contains(<queryConstant>)
        private static Expression GetQueryExpression<TMember>(Expression propertyExpression, Expression queryConstant)
            =>  GetQueryExpression(propertyExpression, queryConstant, typeof(TMember));
        
        /// <summary>
        /// Constructs an expression to query the property for the specified search term
        /// </summary>
        /// <param name="propertyExpression">The expression of the property being tested</param>
        /// <param name="queryConstant">The expression representing the search term</param>
        /// <param name="propertyType">The type of the property to be tested</param>
        /// The resulting expression will test the property for inclusion of the query
        /// c.<property>.ToString().ToLowerInvariant().Contains(<queryConstant>)
        private static Expression GetQueryExpression(Expression propertyExpression, Expression queryConstant, Type propertyType)
        {
            // Find methods that will be run on each property
            var toStringMethod = propertyType.GetMethod("ToString", new Type[0]);

            // Run ToString method on property (e.g. "c.<property>.ToString()")
            var transformedProperty = Expression.Call(propertyExpression, toStringMethod);

            // Run lowercase method on property (e.g. "c.<property>.ToString().ToLowerInvariant()")
            transformedProperty = Expression.Call(transformedProperty, LowerMethod);

            // Run contains on property with provided query (e.g. "c.<property>.ToString().ToLowerInvariant().Contains(<query>)")
            transformedProperty = Expression.Call(transformedProperty, ContainsMethod, queryConstant);

            return transformedProperty;
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
