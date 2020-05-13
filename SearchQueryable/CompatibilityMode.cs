namespace SearchQueryable
{
    /// <summary>
    /// Compatibility mode with which all search expressions will be generated
    /// </summary>
    public enum CompatiblityMode
    {
        /// <summary>
        /// Will only test public string properties (appropriate for EF interactions)
        /// </summary>
        Strict = 1,

        /// <summary>
        /// Will test all properties and fields (useful when querying in-memory)
        /// </summary>
        All = 2
    }
}