# SearchQueryable

[![NuGet](https://img.shields.io/nuget/v/SearchQueryable.svg)](https://www.nuget.org/packages/SearchQueryable)

A simple search extension for `IQueryable` collections.


## Usage

### Strict mode

By default, the search will work with a compatibility mode that is very strict, so as to support queries through EF. This mode will search all public `string` properties on each element contained in the collection:

```cs
myCollection.Search("term");
```

Additionally, a list of members can be provided to which to limit the search:

```cs
myCollection.Search(
    "term", 
    x => x.FirstName, 
    x => x.LastName);
```

In this case, the `term` will only be searched withing the `FirstName` nad `LastName` members.

### All mode

An `All` compatibility mode is also avaialble, which will try to retrieve string representations of all members and fields of *any* type. This is usually not compatible with most EF providers, since `.ToString()` in most cases can't be translated into a meaningful SQL query. It is however useful for querying in-memory collections:

```cs
myInMemoryCollection.Search("term", CompatibilityMode.All);
```

A version of this with provided member predicates is also available:

```cs
myInMemoryCollection.Search(
    "term", 
    CompatibilityMode.All,
    x => x.FirstName,               // String public property
    x => x.Age,                     // int private field
    x => x.Address.Postcode,        // Nested property of a child entity
    x => x.Employer                 // A reference type that has a meaningful .ToString() implementation
);
```

