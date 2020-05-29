using System;
using System.Linq;
using System.Linq.Expressions;

namespace SearchQueryable
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Filters entries by all their string fields
        /// </summary>
        /// <param name="searchQuery">The search query by which the entries should be filtered</param>
        /// <returns>A filtered collection of entries</returns>
        public static IQueryable<T> Search<T>(this IQueryable<T> data, string searchQuery, CompatiblityMode mode = CompatiblityMode.Strict)
        {
            // Simply return the entire collection, if no search query is specified
            if (string.IsNullOrWhiteSpace(searchQuery)) {
                return data;
            }

            var matches = data;
            // Split the search query and construct predicates for each
            foreach (var part in searchQuery.ToUpperInvariant().Split()) {
                if (!string.IsNullOrWhiteSpace(part)) {
                    matches = matches.Where(SearchHelper.ConstructSearchPredicate<T>(part.Trim(), mode));
                }
            }

            return matches;
        }

        /// <summary>
        /// Filters entries by specified fields
        /// </summary>
        /// <param name="searchQuery">The search query by which the entries should be filtered</param>
        /// <param name="fields">The fields that should be queried with the specified search string</param>
        /// <returns>A filtered collection of entries</returns>
        public static IQueryable<T> Search<T>(this IQueryable<T> data, string searchQuery, params Expression<Func<T, string>>[] fields)
        {
            // Simply return the entire collection, if no search query is specified
            if (string.IsNullOrWhiteSpace(searchQuery) || fields == null || fields.Length < 1) {
                return data;
            }

            var matches = data;
            // Split the search query and construct predicates for each
            foreach (var part in searchQuery.ToUpperInvariant().Split()) {
                if (!string.IsNullOrWhiteSpace(part)) {
                    matches = matches.Where(SearchHelper.ConstructSearchPredicate<T>(part.Trim(), fields));
                }
            }

            return matches;
        }
    }
}