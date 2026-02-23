# Best Practices

Essential principles for effective XML documentation.

## Overview

This guide presents 12 core principles for writing clear, maintainable XML documentation. Each principle includes detailed examples and anti-patterns in separate files for easy reference.

## The 12 Principles

### 1. [Keep It Simple](examples/01-keep-it-simple.md)
Concise summaries for straightforward elements. Don't over-document simple properties.

**Key Point**: Simple properties don't need verbose documentation. The name and type convey the meaning.

---

### 2. [Add Context Where Needed](examples/02-add-context.md)
Complex concepts deserve detailed explanations in remarks.

**Key Point**: Non-obvious behaviors and relationships need explicit documentation.

---

### 3. [Think About Your Audience](examples/03-language-choice.md)
Choose language (Korean/English/Mixed) that serves your team best.

**Key Point**: Consider your primary audience - internal team, international contributors, or mixed.

---

### 4. [Be Consistent](examples/04-consistency.md)
Follow established patterns within files and across the project.

**Key Point**: Same language and style within a file, similar strategy across related components.

---

### 5. [Avoid Redundancy](examples/05-avoid-redundancy.md)
Don't repeat information obvious from method names or types.

**Key Point**: Self-documenting names reduce the need for verbose documentation.

---

### 6. [Use Structured XML](examples/06-structured-xml.md)
Leverage XML tags (`<para>`, `<br/>`, `<list>`) for complex documentation.

**Key Point**: Structure improves readability and scannability.

---

### 7. [Document Special Cases](examples/07-special-cases.md)
Magic numbers, unusual behaviors, and important notes deserve explanation.

**Key Point**: Non-obvious behaviors and thresholds need explicit documentation.

---

### 8. [Document Exceptions](examples/08-exceptions.md)
Use `<exception>` for exceptions that are part of the method's contract.

**Key Point**: Only document exceptions callers should handle, not internal implementation details.

---

### 9. [Document Enums Completely](examples/09-enums.md)
Each enum value should have clear `<summary>` describing its purpose.

**Key Point**: Enum values appear in IntelliSense and need clear descriptions.

---

### 10. [Extension Methods: Document the Extended Type](examples/10-extension-methods.md)
Always document the extended type in the `this` parameter.

**Key Point**: Makes it clear what type is being extended and how.

---

### 11. [Property Side Effects: Use `<value>` Tag](examples/11-property-side-effects.md)
Use `<value>` when getter/setter have non-obvious behavior.

**Key Point**: Developers need to know about side effects and non-obvious behaviors.

---

### 12. [Interface vs Implementation](examples/12-interface-implementation.md)
Full documentation in interface, `<inheritdoc/>` in implementation.

**Key Point**: Separate API contracts from implementation details using `<inheritdoc/>`.

---

## Quick Reference Checklist

**For every public API member:**
- [ ] Has clear `<summary>` in team's chosen language
- [ ] Complex concepts have `<remarks>` with detailed explanation
- [ ] Methods document parameters if not obvious
- [ ] Methods document return values if not obvious
- [ ] Exceptions are documented if part of contract
- [ ] Properties use `<value>` if they have side effects
- [ ] Enums document each value
- [ ] Extension methods document the extended type
- [ ] Implementation classes use `<inheritdoc/>` appropriately
- [ ] Language choice is consistent within file
- [ ] Documentation is accurate and up-to-date

## Remember

- **Keep it simple** for straightforward cases
- **Add context** where behavior is non-obvious
- **Be consistent** with language and formatting choices
- **Think about your audience** when choosing language
- **Optimize for IntelliSense** display experience
