using System;
using System.Linq;
using System.Linq.Expressions;

namespace SearchQueryable
{
    public static class QueryableExtensions
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
        public static IQueryable<T> Search<T>(this IQueryable<T> data, string searchQuery, params Expression<Func<T, object>>[] fields)
        {
            var matches = data;
            foreach (var part in searchQuery.ToLowerInvariant().Split()) {
                if (!string.IsNullOrWhiteSpace(part)) {
                    matches = matches.Where(SearchHelper.ConstructSearchPredicate<T>(part.Trim(), fields));
                }
            }

            return matches;
        }
    }
}