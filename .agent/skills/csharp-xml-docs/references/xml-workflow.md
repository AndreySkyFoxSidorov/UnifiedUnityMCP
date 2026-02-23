# XML Documentation Workflow

## Overview

XML documentation generation follows a controlled 3-step process to ensure quality and consistency. **XML is NEVER auto-applied** - only generated when explicitly requested.

## 3-Step Process

### Step 1: Claude-Haiku (Initial Draft)

**When**: User requests XML documentation for specific code

**Model**: `claude-haiku-4-5`

**Input Requirements**:
- Target code (property, method, class, interface, etc.)
- Project context (language preference: Korean/English/Mixed)
- Relevant patterns from [Pattern Library](pattern-library.md)

**Output**: Initial XML documentation draft

**Example**:
```csharp
// User requests: "Add XML documentation for PlayAnimation method"

// Claude-Haiku generates:
/// <summary>
/// Plays animation asynchronously
/// </summary>
/// <param name="animationPath">Animation path to play</param>
/// <param name="wrapMode">Playback mode (Loop/Once/PingPong)</param>
/// <returns>
/// true: Playback start succeeded<br/>
/// false: Playback start failed
/// </returns>
```

**Haiku Advantages**:
- ✅ Fast turnaround for initial draft
- ✅ Efficient token usage
- ✅ Follows established patterns well
- ✅ Good baseline quality

---

### Step 2: Gemini or Codex CLI (Expert Review)

**When**: After Claude-Haiku draft is generated

**Tool Selection**:
- **Use Gemini**: For complex, nuanced documentation requiring sophisticated analysis
- **Use Codex CLI**: For quick pattern validation and style consistency checks

**Input**:
- Haiku-generated draft
- Target code context
- Pattern references

**Review Checklist**:
- ✓ Pattern adherence (reference Pattern Library)
- ✓ Language consistency (Korean/English/Mixed)
- ✓ Completeness (all required tags present)
- ✓ Clarity (simple, understandable language)
- ✓ Redundancy check (no unnecessary verbosity)
- ✓ Interface vs Implementation distinction (if applicable)
- ✓ Exception documentation (if relevant)

**Output**: Refined, validated XML documentation

**Example Review**:
```
Haiku Draft:
/// <summary>
/// Plays animation asynchronously
/// </summary>

Gemini Review Feedback:
- ✓ Summary is clear and concise
- ✓ Follows Pattern Library "Complex concept" pattern
- ✓ Proper use of UniTask notation (async pattern)
- Suggestion: Add remarks for preconditions
- Suggestion: Document main failure modes

Final Result:
/// <summary>
/// Plays animation asynchronously
/// </summary>
/// <remarks>
/// <strong>Preconditions:</strong><br/>
/// - Context must be ready (IsReady = true)<br/>
/// - animationPath must not be null<br/>
/// <br/>
/// <strong>Failure Modes:</strong><br/>
/// - Unknown animation path<br/>
/// - System activation failure
/// </remarks>
/// <param name="animationPath">Animation path to play</param>
/// <param name="wrapMode">Playback mode (Loop/Once/PingPong)</param>
/// <returns>
/// true: Playback start succeeded<br/>
/// false: Playback start failed
/// </returns>
```

**Review Benefits**:
- 🔍 Cross-validates against patterns
- 📚 Ensures consistency with codebase style
- ✨ Enhances clarity and completeness
- 🎯 Professional quality assurance

---

### Step 3: Final Approval & Integration

**When**: After expert review is complete

**Decision Point**:
- ✅ **Approve**: Accept reviewed documentation and integrate into code
- 🔄 **Revise**: Request additional changes before approval
- ❌ **Reject**: Return to Step 1 with new requirements

**Integration**:
```csharp
public partial class VRMController : IVTuberAnimationController
{
    /// <summary>
    /// Plays animation asynchronously
    /// </summary>
    /// <remarks>
    /// <strong>Preconditions:</strong><br/>
    /// - Context must be ready (IsReady = true)<br/>
    /// - animationPath must not be null<br/>
    /// <br/>
    /// <strong>Failure Modes:</strong><br/>
    /// - Unknown animation path<br/>
    /// - System activation failure
    /// </remarks>
    /// <param name="animationPath">Animation path to play</param>
    /// <param name="wrapMode">Playback mode (Loop/Once/PingPong)</param>
    /// <returns>
    /// true: Playback start succeeded<br/>
    /// false: Playback start failed
    /// </returns>
    public async UniTask<bool> PlayAnimation(string animationPath, WrapMode wrapMode)
    {
        Debug.Assert(animationPath != null);
        // Implementation...
    }
}
```

**Post-Integration**:
- ✅ Code review (if part of your process)
- ✅ IntelliSense validation in IDE
- ✅ Consistency check against similar methods

---

## Decision Tree: When to Document

```
Is XML documentation explicitly requested?
│
├─ YES → Proceed with Step 1 (Claude-Haiku)
│         │
│         └─ Generate draft
│            │
│            └─ Step 2 (Gemini/Codex Review)
│               │
│               └─ Step 3 (Final Approval)
│
└─ NO → Skip documentation
         (Do NOT auto-apply)
```

---

## Tool Selection Guide

| Scenario | Best Tool | Why |
|----------|-----------|-----|
| Public interface method | Gemini | Complex contracts need sophisticated analysis |
| Simple property documentation | Codex CLI | Quick pattern validation sufficient |
| Factory method with special semantics | Gemini | Nuanced explanation beneficial |
| Exception documentation | Codex CLI | Pattern matching for exception formats |
| Multi-method documentation batch | Codex CLI | Efficiency for similar patterns |
| First time documenting new component | Gemini | Comprehensive baseline quality |

---

## Quality Standards

### Haiku Draft Quality
- Must follow [Pattern Library](pattern-library.md) structure
- Must include essential tags (`<summary>`, required `<param>`, `<returns>`)
- Language choice must be consistent with project

### Post-Review Quality
- ✅ Patterns fully adhered to
- ✅ All edge cases documented
- ✅ Language clear and consistent
- ✅ No redundancy
- ✅ Proper use of formatting tags (`<remarks>`, `<para>`, `<br/>`, etc.)

### Approval Criteria
- Documentation passes both Haiku baseline AND expert review
- Adheres to project language policy
- Matches existing code documentation style
- Clear enough for IDE IntelliSense display

---

## Common Patterns in Workflow

### Pattern: Interface Full Documentation + Implementation `<inheritdoc/>`

```
Request: "Document IAnimationController interface and VRMController implementation"

Step 1 (Claude-Haiku):
- Generate full documentation for interface
- Generate <inheritdoc/> + implementation notes for class

Step 2 (Gemini Review):
- Validate interface captures complete contract
- Verify implementation notes add value without redundancy
- Check ISP compliance (interface segregation)

Step 3 (Final):
- Approve and integrate both interface and implementation docs
```

### Pattern: Batch Documentation

```
Request: "Add documentation for 5 similar action handler methods"

Step 1 (Claude-Haiku):
- Generate all 5 documentation drafts
- Ensure consistency across drafts

Step 2 (Codex CLI - efficient for batch):
- Validate pattern consistency across batch
- Flag any inconsistencies or missing details

Step 3 (Final):
- Single approval for consistent batch
- Integrate all 5 together
```

---

## Integration with csharp-xml-docs Skill

This workflow complements the [csharp-xml-docs](../SKILL.md) skill by:

1. **Pattern Reference**: Uses [Pattern Library](pattern-library.md) as baseline
2. **Language Flexibility**: Supports all language options from main skill
3. **Quality Assurance**: Two-step validation (Haiku → Expert Review)
4. **On-Demand Principle**: Never auto-applies, only when explicitly requested

---

## Common Questions

**Q: Why not just use Gemini directly?**
A: Claude-Haiku provides fast baseline generation. Gemini then adds expert refinement. Combined approach is more efficient than Gemini alone.

**Q: Can I skip the expert review step?**
A: Technically yes, but not recommended. Review catches issues that Haiku might miss and improves overall quality.

**Q: What if Gemini/Codex strongly disagrees with Haiku?**
A: Return to Step 1 with new requirements and have Haiku regenerate, then review again. This is normal iteration.

**Q: How long does the full process take?**
A: Typically 5-10 minutes per component (1-2 min Haiku + 2-5 min review + 1-2 min approval).

**Q: Can I batch multiple reviews?**
A: Yes! Process multiple Haiku drafts together, then review all in one Gemini/Codex session.

---

## See Also

- [Pattern Library](pattern-library.md) - Complete documentation patterns
- [Formatting Guidelines](formatting-guidelines.md) - XML tag usage reference
- [Best Practices](best-practices.md) - Essential documentation principles
