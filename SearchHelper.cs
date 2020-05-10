using System;
using System.Linq;
using System.Linq.Expressions;

namespace SearchQueryable
{
    public static class SearchHelper {

        /// <summary>
        /// Extensions method for filtering entries by the provided search query
        /// </summary>
        /// <param name="searchQuery">The search query by which the entries should be filtered</param>
        public static IQueryable<T> Search<T>(this IQueryable<T> data, string searchQuery)
        {
            var matches = data;
            foreach (var part in searchQuery.Split()) {
                if (!string.IsNullOrWhiteSpace(part)) {
                    matches = matches.Where(SearchHelper.ConstructSearchPredicate<T>(part.Trim()));
                }
            }

            return matches;
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
        public static Expression<Func<T, bool>> ConstructSearchPredicate<T>(string query) {
            // Create constant with query
            var constant = Expression.Constant(query);

            // Input parameter (e.g. "c => ")
            var parameter = Expression.Parameter(typeof(T), "c");

            // Find methods that will be run on each property
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var lowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);

            // Get all object properties
            var properties = typeof(T).GetProperties();

            // Construct expression
            Expression finalBody = null;
            foreach (var p in properties) {
                if (p.PropertyType == typeof(string) && p.CanWrite){
                    // Express a property (e.g. "c.<property>" )
                    var expressionProperty = Expression.Property(parameter, p.Name);

                    // Run lowercase method on property (e.g. "c.<property>.ToLowerInvariant()")
                    var transformedProperty = Expression.Call(expressionProperty, lowerMethod);

                    // Run contains on property with provided query (e.g. "c.<property>.ToLowerInvariant().Contains(<query>)")
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
    }
}
