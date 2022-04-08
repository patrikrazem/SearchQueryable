namespace SearchQueryable;

[Flags]
public enum SearchFlags
{
    /// <summary>
    /// Will search string properties/fields with strict mode
    /// </summary>
    Default = 0,

    /// <summary>
    /// Attempts to search all properties/fields regardless of their type
    /// </summary>
    LaxMode = 1,

    /// <summary>
    /// Calls ToUpperCaseInvariant()/ToUpper() on all search fields and query expression
    /// </summary>
    IgnoreCase = 2
}