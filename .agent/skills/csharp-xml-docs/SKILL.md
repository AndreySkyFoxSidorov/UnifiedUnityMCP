---
name: csharp-xml-docs
description: C# XML documentation with on-demand Haiku→Expert Review→Final workflow. Use when documenting C# APIs, properties, methods, classes, and interfaces natively in English.
requires:
  - csharp-plugin:csharp-code-style
---

# C# XML Documentation Workflow

## Overview

This skill provides a structured workflow for generating high-quality C# XML documentation.

**Foundation Required**: `csharp-code-style` (Formatting, Naming conventions)

**Core Workflow**:
1. Draft (Haiku style)
2. Expert Review (Criteria check)
3. Final Polish

## The Workflow

When asked to document code, follow this exact sequence:

### Phase 1: Draft (Haiku Style)
Quickly generate the basic structure. Focus on "What" and "Why".
- `<summary>`: 1-2 sentences maximum. Active voice.
- `<param>`: What the parameter is and its constraints.
- `<returns>`: What comes out and when it might be null.

### Phase 2: Expert Review
Review the draft against these criteria:
- **Clarity**: Is the language unambiguous?
- **Completeness**: Are edge cases mentioned?
- **Conciseness**: Is there passive voice or redundant wording?
- **Formatting**: Are code references using `<c>` and `<see>` tags?

### Phase 3: Final Polish
Produce the final XML documentation block.

## Style Guidelines

### 1. Mandatory Tags

Every public member MUST have these tags if applicable:

- **Classes/Interfaces/Structs**: `<summary>`, `<remarks>` (if complex)
- **Methods**: `<summary>`, `<param>` x N, `<returns>` (if not void), `<exception>` x N
- **Properties**: `<summary>`, `<value>` (optional but recommended for complex properties)

### 2. Formatting

- Use `<c>text</c>` for inline code snippets, parameter names in text, or simple values (`<c>null</c>`, `<c>true</c>`).
- Use `<see cref="TypeName"/>` for referencing other classes, methods, or properties.
- Use `///` exclusively. Do not use `/** */` style blocks.

### 3. Language Rules

- **English Only**: Write all documentation in clear, professional English.
- **Active Voice**: "Calculates the total..." instead of "This method is used to calculate the total..."
- **No Redundancy**: If a property is named `TotalCount`, don't write "Gets or sets the total count." Write "The total number of processed items."

## Examples

### Method Documentation

```csharp
/// <summary>
/// Asynchronously retrieves the optimal server route for the specified player.
/// </summary>
/// <remarks>
/// This evaluates current regional latency and server load. 
/// If the primary region is down, it automatically falls back to the secondary region.
/// </remarks>
/// <param name="playerId">The unique identifier of the target player.</param>
/// <param name="forceRefresh">If <c>true</c>, bypasses the cache and queries the matchmaking service directly.</param>
/// <returns>A <see cref="ServerRoute"/> object describing the connection path, or <c>null</c> if no servers are available.</returns>
/// <exception cref="ArgumentException">Thrown when <paramref name="playerId"/> is empty.</exception>
public async Task<ServerRoute> GetOptimalRoute(string playerId, bool forceRefresh)
{
    // implementation
}
```

### Property Documentation

```csharp
/// <summary>
/// The maximum number of simultaneous connections allowed.
/// </summary>
/// <value>An integer between 1 and 100. The default is 50.</value>
public int MaxConnections { get; set; } = 50;
```

## Anti-Patterns

- **The Echo**: Writing `<summary>Gets the user ID.</summary>` for `GetUserId()`.
- **The Novel**: A `<summary>` that spans 3 paragraphs. Move details to `<remarks>`.
- **The Ghost**: Missing `<param>` tags for complex methods.
- **The Orphan Variables**: Writing "throws error if id is null" instead of using `<paramref name="id"/>`.
