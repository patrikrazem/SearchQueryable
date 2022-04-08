using System.Linq.Expressions;

namespace SearchQueryable;

public static class QueryableExtensions
{
    /// <summary>
    /// Filters entries by specified fields
    /// </summary>
    /// <param name="searchQuery">The search query by which the entries should be filtered</param>
    /// <param name="fields">The fields that should be queried with the specified search string</param>
    /// <returns>A filtered collection of entries</returns>
    public static IQueryable<T> Search<T>(this IQueryable<T> data, string searchQuery, params Expression<Func<T, string?>>[] fields)
    {
        return data.Search(searchQuery, SearchFlags.Default, fields);
    }

    /// <summary>
    /// Filters entries by specified fields
    /// </summary>
    /// <param name="searchQuery">The search query by which the entries should be filtered</param>
    /// <param name="flags">Search flags</param>
    /// <param name="fields">The fields that should be queried with the specified search string</param>
    /// <returns>A filtered collection of entries</returns>
    public static IQueryable<T> Search<T>(this IQueryable<T> data, string searchQuery, SearchFlags flags, params Expression<Func<T, string?>>[] fields)
    {
        var matches = data;

        if (flags.HasFlag(SearchFlags.IgnoreCase)) {
            searchQuery = searchQuery.ToUpperInvariant();
        }

        // Split the search query and construct predicates for each
        foreach (var part in searchQuery.Split(" ", StringSplitOptions.RemoveEmptyEntries)) {
            matches = matches.Where(SearchHelper.ConstructSearchPredicate<T>(part.Trim(), flags, fields));
        }

        return matches;
    }
}