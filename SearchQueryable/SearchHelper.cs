using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace SearchQueryable;

internal record ExpressionScopedVariable(string Value);

internal static class SearchHelper
{
    /// <summary>
    /// A constant definition of the ToUpperInvariant method to use in expressions
    /// </summary>
    private static readonly MethodInfo UpperInvariantMethod = typeof(string).GetMethod("ToUpperInvariant", [])!;

    /// <summary>
    /// A constant definition of the ToUpperInvariant method to use in expressions
    /// </summary>
    private static readonly MethodInfo UpperMethod = typeof(string).GetMethod("ToUpper", [])!;

    /// <summary>
    /// A constant definition of the Contants method to use in expressions
    /// </summary>
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;

    /// <summary>
    /// A constant definition of the Any method to use in expressions for collections
    /// </summary>
    private static readonly MethodInfo AnyMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 2);

    /// <summary>
    /// Constructs an expression that is used to filter entries and execute a search for the specified query
    /// on all string type fields of an entity
    /// </summary>
    /// <param name="searchQuery">The query to be searched for in all string fields</param>
    /// <param name="flags">The search flags to apply</param>
    /// <param name="members">The names of members that should be queried with the specified search string (e.g. "Title", "SomeCollection.Property", etc.)</param>
    /// <returns>An expression that can be used in queries to the DB context</returns>
    ///
    /// Example:
    /// For an entity with string properties `Name` and `Orders.OrderNumber`, the resulting expression
    /// is something like this:
    /// `x => x.Name.ToLower().Contains(query) || x.Orders.Any(o => o.OrderNumber.ToLower().Contains(query))`
    internal static Expression<Func<TObject, bool>> ConstructSearchPredicate<TObject>(string searchQuery, SearchFlags flags, Expression<Func<TObject, string?>>[] memberAccessors, string[] membersNames)
    {
        // Create constant with query
        var constant = Expression.Constant(new ExpressionScopedVariable(searchQuery));
        var scopedMember = typeof(ExpressionScopedVariable).GetMember("Value")[0];
        var scopedMemberAccessor = Expression.MakeMemberAccess(constant, scopedMember!);

        // Input parameter (e.g. "c => ")
        var parameter = Expression.Parameter(typeof(TObject), "c");

        IEnumerable<Expression> memberExpressions;
        if (memberAccessors.Length == 0 && membersNames.Length == 0) {
            // Get all object properties
            var type = typeof(TObject);

            // Get appropriate members to test
            memberExpressions = GetAvailableMembers(type, parameter, flags);
        } else {
            // Based expressions on the provided memberAccessors
            IEnumerable<LambdaExpression> accessors = memberAccessors;

            // If no accessors are provided, use the member names to construct them
            if (!accessors.Any()) {
                // Convert string selectors to member expressions
                accessors = membersNames.Select(m => ConstructExpressionFromStringSelector<TObject>(parameter, m, flags)).ToList();
            }

            // Visit the provided expression and replace the input parameter with the one defined above ("c")
            // e.g. (from "x.Something" we get "c.Something")
            memberExpressions = accessors.Select(m => new ExpressionParameterVisitor(m.Parameters.First(), parameter)
                .VisitAndConvert(m.Body, nameof(ConstructSearchPredicate)));
        }

        // Construct expression
        Expression? finalExpression = null;
        foreach (var memberExpression in memberExpressions) {
            Expression partialExpression;

            if (IsCollectionAnyExpression(memberExpression)) {
                partialExpression = UpdateCollectionQueryExpression(memberExpression, scopedMemberAccessor);
            } else {
                // Get query expression for regular properties
                partialExpression = GetQueryExpression(memberExpression, scopedMemberAccessor, memberExpression.Type, flags);
            }

            // Handle case when no OR operation can be constructed (this is the first expression)
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
    /// Constructs an expression from a string selector for a specific object type.
    /// </summary>
    /// <param name="parameter">The parameter expression representing the object (e.g. "c.<property>"</param>
    /// <param name="selector">The string selector to parse (e.g. "Title", "Orders.OrderNumber", etc.).</param>
    /// <param name="flags">The search flags to apply.</param>
    /// <typeparam name="TObject">The type of the object being queried.</typeparam>
    /// <returns>The constructed expression (e.g. "c => c.Title" or "c => c.Orders.Any(o => o.OrderNumber == <value>)")</returns>
    private static LambdaExpression ConstructExpressionFromStringSelector<TObject>(ParameterExpression parameter, string selector, SearchFlags flags)
    {
        // Define binding flags to get public instance members
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        Expression currentExpression = parameter;

        // parts are the segments of the selector split by '.'
        var parts = selector.Split('.');

        // If there is only one part, it's a direct property of the parameter
        if (parts.Length == 1) {
            // Get the member
            var member = typeof(TObject).GetMember(parts[0], bindingFlags).First();

            var memberExpression = CreateMemberExpression(parameter, member!);

            return Expression.Lambda(memberExpression, parameter);
        }

        // If there's exactly two parts, we need to handle a collection property
        if (parts.Length == 2) {
            // Construct collection expression
            var collectionExpression = Expression.Property(currentExpression, parts[0]);

            var collectionSearchExpression = GetCollectionSearchExpressions(collectionExpression, collectionExpression.Type, flags);
            return Expression.Lambda(collectionSearchExpression.First(), parameter);
        }

        // If there's more than two parts, this usage is not supported
        throw new NotSupportedException("Selectors with more than two parts are not supported.");
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
            var toStringMethod = propertyType.GetMethod("ToString", [])!;

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

    /// <summary>
    /// Get all available members of the specified type that can be searched
    /// </summary>
    /// <param name="type">The type from which to retrieve members</param>
    /// <param name="parameter">The parameter expression representing the object being queried (e.g. "c.<member>")</param>
    /// <param name="flags">The search flags to apply</param>
    /// <param name="isRecursive">Whether to recurse into collection properties</param>
    /// <returns>A collection of member expressions representing the searchable members</returns>
    /// Example:
    /// For an entity with string properties `Name` and `Address`, the resulting expressions
    /// will represent `c.Name` and `c.Address`
    /// For an entity with a collection property `Orders` of type `Order` (which has a string property `Description`),
    /// the resulting expressions will also include `c.Orders.Any(o => o.Description.Contains(query))`
    private static List<Expression> GetAvailableMembers(Type type, ParameterExpression parameter, SearchFlags flags, bool isRecursive = true)
    {
        // Define binding flags to get public instance members
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;


        var members = new List<Expression>();

        if (!flags.HasFlag(SearchFlags.LaxMode)) {
            // Include only string properties in strict mode (to avoid issues with EF converting the expression to SQL)
            var stringProperties = type
                .GetProperties(bindingFlags)
                .Where(m => m.CanWrite && m.MemberType == MemberTypes.Property)
                .Where(m => m.GetUnderlyingType() == typeof(string))
                .Select(x => x as MemberInfo)
                .ToList();
            members.AddRange(stringProperties.Select(x => CreateMemberExpression(parameter, x)));
        } else {
            // Include all non-collection fields/properties
            var nonCollectionMembers = type
                .GetFields(bindingFlags).Cast<MemberInfo>()
                .Concat(type.GetProperties(bindingFlags)).ToArray()
                .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)
                .Where(m => !m.GetUnderlyingType().IsCollection())
                .ToList();

            members.AddRange(nonCollectionMembers.Select(x => CreateMemberExpression(parameter, x)));
        }

        // Find all collection properties (excluding strings, since they were already included above)
        var collectionProperties = type
            .GetProperties(bindingFlags)
            .Where(p => p.GetUnderlyingType().IsCollection())
            .Where(p => p.GetUnderlyingType() != typeof(string))
            .ToList();

        // Recurse into collections to get their searchable members
        foreach (var collectionProperty in collectionProperties) {
            var collectionExpression = Expression.Property(parameter, collectionProperty.Name);
            var collectionSearchExpressions = GetCollectionSearchExpressions(collectionExpression, collectionProperty.GetUnderlyingType(), flags);
            members.AddRange(collectionSearchExpressions);
        }

        return members;
    }

    /// <summary>
    /// Safely creates a member expression (field or property)
    /// </summary>
    /// <param name="parameter">The parameter expression</param>
    /// <param name="member">The member info</param>
    /// <returns>A member expression</returns>
    private static Expression CreateMemberExpression(ParameterExpression parameter, MemberInfo member)
    {
        try {
            return member.MemberType == MemberTypes.Field
                ? Expression.Field(parameter, member.Name)
                : Expression.Property(parameter, member.Name);
        } catch {
            // If we can't create the expression for this member, skip it by returning a constant that won't match
            return Expression.Constant(string.Empty);
        }
    }

    /// <summary>
    /// Gets the element type of a collection type
    /// </summary>
    /// <param name="collectionType">The collection type</param>
    /// <returns>The element type, or null if not a valid collection</returns>
    private static Type? GetCollectionElementType(Type collectionType)
    {
        // Handle arrays
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        // Handle generic collections (IEnumerable<T>, List<T>, etc.)
        if (collectionType.IsGenericType) {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length == 1)
                return genericArgs[0];
        }

        // Handle non-generic IEnumerable implementations
        var enumerableInterface = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface != null) {
            return enumerableInterface.GetGenericArguments()[0];
        }

        return null;
    }

    /// <summary>
    /// Gets search expressions for collection properties using the Any method
    /// </summary>
    /// <param name="collectionExpression">The expression representing the collection property</param>
    /// <param name="collectionType">The type of the collection</param>
    /// <param name="flags">Search flags</param>
    /// <returns>Collection of expressions that can search within the collection</returns>
    private static List<Expression> GetCollectionSearchExpressions(Expression collectionExpression, Type collectionType, SearchFlags flags)
    {
        var expressions = new List<Expression>();

        // Get the element type of the collection
        var elementType = GetCollectionElementType(collectionType);
        if (elementType == null) {
            return expressions;
        }

        // Create parameter for the collection element (e.g., "item" in "collection.Any(item => ...)")
        var elementParameter = Expression.Parameter(elementType, "item");

        // Get all searchable members of the element type (but don't recurse into collections to avoid infinite recursion)
        var elementMembers = GetAvailableMembers(elementType, elementParameter, flags, false);

        foreach (var memberExpression in elementMembers) {
            // Create the Any expression: collection.Any(item => item.Property.Contains(searchTerm))
            var anyExpression = CreateAnyExpression(collectionExpression, elementParameter, memberExpression, elementType, flags);
            if (anyExpression != null) {
                expressions.Add(anyExpression);
            }
        }

        return expressions;
    }

    /// <summary>
    /// Creates an Any expression for searching within a collection
    /// </summary>
    /// <param name="collectionExpression">The collection to search in</param>
    /// <param name="elementParameter">Parameter representing an element in the collection</param>
    /// <param name="memberExpression">The member expression to search on</param>
    /// <param name="elementType">The type of elements in the collection</param>
    /// <returns>An expression representing collection.Any(item => condition)</returns>
    private static BinaryExpression? CreateAnyExpression(Expression collectionExpression, ParameterExpression elementParameter, Expression memberExpression, Type elementType, SearchFlags flags)
    {
        try {
            // This will be used as a placeholder - the actual search term will be injected during query construction
            var dummySearchTerm = Expression.Constant(new ExpressionScopedVariable(""));
            var scopedMember = typeof(ExpressionScopedVariable).GetMember("Value")[0];
            var scopedMemberAccessor = Expression.MakeMemberAccess(dummySearchTerm, scopedMember!);

            // Create the condition for the Any method (e.g., item.Property.Contains(searchTerm))
            var condition = GetQueryExpression(memberExpression, scopedMemberAccessor, memberExpression.Type, flags);

            // Create lambda expression: item => condition
            var lambda = Expression.Lambda(condition, elementParameter);

            // Get the generic Any method for this element type
            var genericAnyMethod = AnyMethod.MakeGenericMethod(elementType);

            // Add null check for the collection
            var nullCheck = Expression.NotEqual(collectionExpression, Expression.Constant(null, collectionExpression.Type));

            // Create the Any call: collection.Any(item => condition)
            var anyCall = Expression.Call(genericAnyMethod, collectionExpression, lambda);

            // Combine null check with Any call: collection != null && collection.Any(item => condition)
            return Expression.AndAlso(nullCheck, anyCall);
        } catch {
            // If we can't create the Any expression for this member, skip it
            return null;
        }
    }

    /// <summary>
    /// Updates the search term in a collection Any expression
    /// </summary>
    /// <param name="anyExpression">The Any expression to update</param>
    /// <param name="newSearchTerm">The new search term</param>
    /// <returns>Updated Any expression with the new search term</returns>
    private static Expression UpdateCollectionQueryExpression(Expression anyExpression, Expression newSearchTerm)
    {
        // Handle the case where we have: collection != null && collection.Any(item => condition)
        if (anyExpression is BinaryExpression binaryExpr && binaryExpr.NodeType == ExpressionType.AndAlso) {
            var left = binaryExpr.Left; // The null check
            var right = binaryExpr.Right; // The Any call

            if (right is MethodCallExpression methodCall && methodCall.Arguments.Count == 2) {
                var collection = methodCall.Arguments[0];
                var lambda = methodCall.Arguments[1];

                // Replace the search term in the lambda expression
                var updatedLambda = new AnyExpressionParameterVisitor(newSearchTerm).Visit(lambda);

                var updatedAnyCall = Expression.Call(methodCall.Method, collection, updatedLambda);
                return Expression.AndAlso(left, updatedAnyCall);
            }
        }
        // Handle the case where we just have: collection.Any(item => condition)
        else if (anyExpression is MethodCallExpression methodCall && methodCall.Arguments.Count == 2) {
            var collection = methodCall.Arguments[0];
            var lambda = methodCall.Arguments[1];

            // Replace the search term in the lambda expression
            var updatedLambda = new AnyExpressionParameterVisitor(newSearchTerm).Visit(lambda);

            return Expression.Call(methodCall.Method, collection, updatedLambda);
        }

        return anyExpression;
    }

    /// <summary>
    /// Checks if an expression is a collection Any expression
    /// </summary>
    /// <param name="expression">The expression to check</param>
    /// <returns>True if it's a collection Any expression</returns>
    private static bool IsCollectionAnyExpression(Expression expression)
    {
        // Handle: collection != null && collection.Any(item => condition)
        if (expression is BinaryExpression binaryExpr && binaryExpr.NodeType == ExpressionType.AndAlso) {
            var right = binaryExpr.Right;
            return right is MethodCallExpression methodCall &&
                   methodCall.Method.IsGenericMethod &&
                   methodCall.Method.GetGenericMethodDefinition() == AnyMethod;
        }

        // Handle: collection.Any(item => condition)
        return expression is MethodCallExpression directMethodCall &&
               directMethodCall.Method.IsGenericMethod &&
               directMethodCall.Method.GetGenericMethodDefinition() == AnyMethod;
    }

    /// <summary>
    /// Checks if a type is a collection (implements IEnumerable)
    /// </summary>
    /// <param name="type">The type to be checked</param>
    /// <returns>Whether the type represents collection (implement IEnumerable)</returns>
    private static bool IsCollection(this Type type)
    {
        // Strings are formally collections, so we should handle this separately
        if (type == null || type == typeof(string)) {
            return false;
        }
        return typeof(IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Gets the underlying type of a member (field, property, or method return type)
    /// </summary>
    /// <param name="member">The member for which to retrieve the underlying type</param>
    /// <returns>The underlying type of the member</returns>
    private static Type GetUnderlyingType(this MemberInfo member)
    {
        return member.MemberType switch {
            MemberTypes.Field => ((FieldInfo)member).FieldType,
            MemberTypes.Method => ((MethodInfo)member).ReturnType,
            MemberTypes.Property => ((PropertyInfo)member).PropertyType,
            _ => throw new ArgumentException("Input MemberInfo must be of type FieldInfo, MethodInfo, or PropertyInfo"),
        };
    }
}
