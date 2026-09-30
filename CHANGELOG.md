# Changelog

## Unreleased

- Report invalid endpoint-policy HTTP method selectors instead of silently ignoring them, preventing invalid authorization declarations from being mistaken for active policy.
- Restrict configured success response statuses to 200–299 so generated server responses agree with the client's success-status handling.

## 2.0.0-preview.2

This replaces the withdrawn `2.0.0-preview.1` GitHub release, whose tag incorrectly pointed to the 1.0.0 source commit. No 2.0 package was published for that release.

- Add endpoint metadata attributes for response/request types and media types, OpenAPI tags and text, endpoint names, host constraints, route order, form limits, and other routing behavior. Attributes can target all operations on a contract or select an HTTP method.
- Add typed convention builders for every mapped remote endpoint, an individual service, or one HTTP operation. Each scope accepts ASP.NET Core endpoint filters and common native endpoint conventions such as `Produces`, `Accepts`, request timeouts, and routing short-circuiting.
- Inherit endpoint metadata, authorization/policy attributes, XML summaries, and parameter-binding attributes from base service interfaces. Diagnose conflicting HTTP-operation declarations inherited from unrelated interfaces.
- Diagnose duplicate endpoint names across generated services at compile time.
- Support duration-based request timeout attributes and per-HTTP-method policy selection.
- Support ASP.NET Core 10 endpoint validation and cookie-redirect conventions. Using these attributes with .NET 8 or .NET 9 produces a generator diagnostic instead of generated code that cannot compile.
- Keep the three public package IDs and target .NET 8, .NET 9, and .NET 10.

### Breaking changes

- `RemoteMethodAttribute.SuccessStatusCode` is now an `int` with `0` meaning “use the default”. Nullable types are not valid CLR attribute named-argument types, so the previous `int?` property could not be set in attribute usage.
- `RemoteServiceMethodConventionBuilder.HttpMethod` and `RemoteServiceEndpointConventionBuilder<TService>.Operation(method, httpMethod)` now return `RemoteServiceOperationConventionBuilder` so operation-scoped extensions can be called fluently. Rebuild consumers of these public APIs when upgrading.
- Remove the advanced, undocumented `CompositeEndpointConventionBuilder`; use the registry returned from `MapRemoteServices()` for global conventions.
