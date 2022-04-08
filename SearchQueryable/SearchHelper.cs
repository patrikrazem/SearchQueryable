using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace SearchQueryable;

internal static class SearchHelper
{
    /// <summary>
    /// A constant definition of the ToUpperInvariant method to use in expressions
    /// </summary>
    private static readonly MethodInfo UpperInvariantMethod = typeof(string).GetMethod("ToUpperInvariant", new Type[0])!;

    /// <summary>
    /// A constant definition of the ToUpperInvariant method to use in expressions
    /// </summary>
    private static readonly MethodInfo UpperMethod = typeof(string).GetMethod("ToUpper", new Type[0])!;

    /// <summary>
    /// A constant definition of the Contants method to use in expressions
    /// </summary>
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;

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
    internal static Expression<Func<TObject, bool>> ConstructSearchPredicate<TObject>(string searchQuery, SearchFlags flags, params Expression<Func<TObject, string?>>[] members)
    {
        // Create constant with query
        var constant = Expression.Constant(searchQuery);

        // Input parameter (e.g. "c => ")
        var parameter = Expression.Parameter(typeof(TObject), "c");

        IEnumerable<Expression> memberExpressions;
        if (!members.Any()) {
            // Get all object properties
            var type = typeof(TObject);

            // Get appropriate members to test
            memberExpressions = GetAvailableMembers(type, parameter, flags);
        } else {
            // Visit the provided expression and replace the input parameter with the one defined above ("c")
            // e.g. (from "x.Something" we get "c.Something")
            memberExpressions = members.Select(m => new ExpressionParameterVisitor(m.Parameters.First(), parameter)
                .VisitAndConvert(m.Body, nameof(ConstructSearchPredicate)));
        }

        // Construct expression
        Expression? finalExpression = null;
        foreach (var memberExpression in memberExpressions) {
            // Get query expression
            var partialExpression = GetQueryExpression(memberExpression, constant, memberExpression.Type, flags);

            // Handle case when no OR operation can be constructed
            if (finalExpression == null) {
                finalExpression = partialExpression;
            } else {
                finalExpression = Expression.OrElse(finalExpression, partialExpression);
            }
        }

        // Check that we have members
        if (finalExpression == null) {
            throw new ArgumentException("Could not determine searchable fields");
        }

        // Return the constructed expression
        return Expression.Lambda<Func<TObject, bool>>(finalExpression, parameter);
    }

    /// <summary>
    /// Constructs an expression to query the property for the specified search term
    /// </summary>
    /// <param name="propertyExpression">The expression of the property being tested</param>
    /// <param name="queryConstant">The expression representing the search term</param>
    /// <param name="propertyType">The type of the property to be tested</param>
    /// The resulting expression will test the property for inclusion of the query
    /// (e.g. "c.<property>.ToString().ToLowerInvariant().Contains(<queryConstant>)")
    private static Expression GetQueryExpression(Expression propertyExpression, Expression queryConstant, Type propertyType, SearchFlags flags)
    {
        // Check that property value is not null (or default) (e.g. "c.<property> != null")
        Expression? nullCheckExpression = null;

        // Value types can safely be operated on, since they have non-null default values
        if (!propertyType.IsValueType) {
            nullCheckExpression = Expression.NotEqual(propertyExpression, Expression.Constant(null, propertyType));
        }

        var transformedProperty = propertyExpression;

        // In lax mode, stringify all members
        if (flags.HasFlag(SearchFlags.LaxMode)) {
            // Find the ToString method that should be executed for the specific type
            var toStringMethod = propertyType.GetMethod("ToString", new Type[0])!;

            // Run ToString method on property (e.g. "c.<property>.ToString()")
            transformedProperty = Expression.Call(propertyExpression, toStringMethod);
        }

        // Uppercase everything, if specified, depending on the mode
        if (flags.HasFlag(SearchFlags.IgnoreCase)) {
            if (flags.HasFlag(SearchFlags.LaxMode)) {
                // Run uppercase method on property (e.g. "c.<property>.ToString().ToUpperInvariant()")
                transformedProperty = Expression.Call(transformedProperty, UpperInvariantMethod);
            } else {
                // Run uppercase method on property (e.g. "c.<property>.ToUpper()")
                transformedProperty = Expression.Call(transformedProperty, UpperMethod);
            }
        }

        // Run contains on property with provided query (e.g. "c.<property>.ToString().ToUpperInvariant().Contains(<query>)")
        transformedProperty = Expression.Call(transformedProperty, ContainsMethod, queryConstant);

        if (nullCheckExpression == null) {
            return transformedProperty;
        } else {
            return Expression.AndAlso(nullCheckExpression, transformedProperty);
        }
    }

    private static IEnumerable<MemberExpression> GetAvailableMembers(Type type, ParameterExpression parameter, SearchFlags flags)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

        IEnumerable<MemberInfo> members;
        if (!flags.HasFlag(SearchFlags.LaxMode)) {
            members = type
                .GetProperties(bindingFlags)
                .Where(m => m.CanWrite && m.MemberType == MemberTypes.Property)
                .Where(m => m.GetUnderlyingType() == typeof(string))
                .Select(x => x as MemberInfo)
                .ToList();
        } else {
            members = type
                .GetFields(bindingFlags).Cast<MemberInfo>()
                .Concat(type.GetProperties(bindingFlags)).ToArray()
                .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)
                .Where(m => !m.GetUnderlyingType().IsCollection())
                .Select(x => x as MemberInfo)
                .ToList();
        }

        return members.Select(x => x.MemberType == MemberTypes.Field
            ? Expression.Field(parameter, x.Name)
            : Expression.Property(parameter, x.Name));
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
            case MemberTypes.Field:
                return ((FieldInfo)member).FieldType;
            case MemberTypes.Method:
                return ((MethodInfo)member).ReturnType;
            case MemberTypes.Property:
                return ((PropertyInfo)member).PropertyType;
            default:
                throw new ArgumentException("Input MemberInfo must be of type FieldInfo, MethodInfo, or PropertyInfo");
        }
    }
}