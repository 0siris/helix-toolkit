---
type: Policy
title: CSharp Coding Conventions
description: Adopted C# and .NET coding, logging, guard, testing, and migration rules for this repository.
tags: [csharp, dotnet, coding-standards, logging, assertions, testing]
generated: { by: process:conventions-adoption, at: 2026-08-01T00:00:00Z }
status: draft
verified: 2026-09-07
---

# Purpose

This policy defines the C#/.NET conventions used by the repository. The policy is applied in phases: agent guidance first, library integration second, analyzer configuration third, and source migration only after the compatibility gates pass.

# Code Style

- Four-space indentation for C# and two-space indentation for MSBuild/XML.
- Approximately 120 characters per line where practical.
- Nullable reference types enabled and redundant null checks removed.
- File-scoped namespaces for single-namespace files; block-scoped namespaces require a documented technical reason.
- Braces omitted only for clear single-statement control-flow bodies.
- Collection expressions, simple extension methods, immutable semantic value types, and expression-bodied members are used when they preserve readability and behavior.
- Comments and technical documentation are written in English.

Property XML documentation may omit the `<summary>` element when the entire documentation fits on one line. For example, instead of:

```csharp
/// <summary>Gets the URL scheme.</summary>
public string Scheme => Uri.Scheme;
```

use:

```csharp
/// Gets the URL scheme
public string Scheme => Uri.Scheme;
```

Overrides and interface implementations do not require their own XML documentation when the corresponding base or
interface member is documented. This applies to methods, properties, events, and indexers. Omit `<inheritdoc />` because
Rider displays the inherited documentation automatically. For example, instead of:

```csharp
/// <inheritdoc />
public string ModuleId => ModuleName;
```

use:

```csharp
public string ModuleId => ModuleName;
```

If the base or interface member is not documented, document the implementing member directly. This rule does not apply
to constructors, new members, or members that merely hide or share a name with another member. Revisit this convention
if XML documentation files are generated or distributed because the compiler does not copy inherited documentation
without `<inheritdoc />`.

# Logging

`LoggerLib` is the logging implementation after Phase 2. Existing `HelixToolkit.Logger.LogManager` public signatures remain stable through an adapter. New and migrated calls use structured templates, PascalCase properties, caller metadata supplied by the compiler, no interpolation, and no trailing periods. Secrets and credentials are never logged.

# Assertions

`ValidSphere` supplies fluent BCL guards. Argument validation uses `GuardNotNull()` or `Guard().Range(...)` and
preserves `ArgumentNullException`/`ArgumentOutOfRangeException` contracts. Internal invariants use
`AssertNotNull().Value` or another `Is()` assertion only where `AssertException` is the correct failure type.
Assertions remain chainable and carry compiler-provided caller context.

# Async and API Design

- No `.Result`, `.Wait()`, or `async void` except framework-required event handlers.
- `CancellationToken cancellationToken = default` is the last parameter of new async APIs.
- Task-returning methods use the `Async` suffix with documented framework entry-point exceptions.
- New DI services may use primary constructors; new DTO/request properties may use `required` when WPF, EF, serialization, and multi-constructor constraints permit.
- Public collection boundaries use read-only collection interfaces; `List<T>` does not cross module boundaries.

# Testing

Tests use the existing xUnit v3 projects and categories. Names follow `MethodName_StateUnderTest_ExpectedBehavior`. Tests use Arrange-Act-Assert, verify one behavior, are deterministic, avoid shared mutable state, `Thread.Sleep`, and `DateTime.Now`, and mock interfaces rather than concrete classes. Small fakes are preferred for non-trivial dependencies.

# Compatibility

Submodule revisions are pinned by the main repository. Existing Helix public members, namespaces, parameters, and
behavior remain unchanged when a submodule introduces a breaking API revision; migrate target-side calls, preserve
their exception contracts, and cover the integration with tests.

# Phase Gates

1. Agent and OKF policy gate.
2. Submodule restore/build, ProjectReference, adapter, integration-test, and API-compatibility gate.
3. `.editorconfig`/analyzer gate.
4. Source migration and regression gate.
5. Full solution, explicit renderer smoke, documentation, and runtime name-hygiene gate.
