# Re-audit 24 — `FRESHAUDIT4` incorporation

**Audited:** `da3f9d9` through `8798edc`

**Date:** 2026-08-02

## Result

**Sign-off for the `FRESHAUDIT4` incorporation.  No findings.**

All three `otherwise` defects are fixed through the real resolver/evaluator
path.  The graph handling boundary is now joined where an error-valued operand
is inspected, fault exclusion and fallback selection share one predicate, and
the operator binds below open pattern calls without adding another resolver
table level.  The new regressions observe the previously silent properties
rather than merely the final value.

This sign-off covers the `otherwise` implementation and its incorporation.  It
does not decide the separately documented open question of whether the word
should be protected from declared-name takeover, nor does it promote previously
disclosed unfinished pipeline work to complete.

## What was rechecked

### Error adoption is suppressed at the correct boundary

`Evaluator.Apply` now distinguishes ordinary eager operators from operators
that inspect and may replace their left value.  Only the latter evaluate their
left expression through `Graph.Handling`; the right expression is evaluated
normally and only when the shared predicate says it is needed.

That placement preserves all three parts of the runtime's error model:

- an error read by the handled left expression is available to `otherwise`
  instead of being adopted over its answer;
- a dirty derived cell recomputed during that read retains its own adoption
  frame, so suppression does not leak into nested bodies; and
- an error read from the selected fallback is not handled and therefore
  propagates normally.

The maintained tests now cover both an error held directly by a var and one
computed by a dirty derived cell.  The recovery test also observes the dynamic
edge: `standby` is a dependency while the left is failing, disappears when the
left recovers, and changing it afterwards does not recompute the guarded cell.
Those assertions close the one-join-short test gap from `FRESHAUDIT4`.

### Fault short-circuiting has one decision

`Builtin.Replaces` is now the sole predicate for whether a left value is
replaced.  Both `Operator.Catches` and `Builtin.Otherwise` consume it, and its
fault exclusion is explicit:

```csharp
value is Error and not Fault or Nothing
```

The regression asserts both the returned `Fault` and the absence of `standby`
from the dependency set.  It therefore fails if the evaluator goes back to
evaluating and discarding the fallback.

An additional temporary full evaluator probe used an effectful, closed-pattern
fallback call on the right of a fault.  The result remained a fault and the call
count remained zero.  This confirms that the fix prevents observable execution
outside a pure `let`, not only an unnecessary graph edge.

### Fallback now sits below calls

Binding power 6 is below the default open-pattern level 7.  Both directions now
resolve as whole-value operands:

```text
(parse «reading» otherwise «standby»)
(«reading» otherwise parse «standby»)
```

The value-level regression also makes `parse` itself return an error and
observes `standby`, proving the first reading is not merely rendered correctly
while still evaluating the old tree.

Power 6 borrows the already-reachable pattern minimum 7, just as the old power
9 borrowed arithmetic minimum 10.  `ResolverCost` consequently retains one
added column and the same allocation ceiling.  The operator-table remarks were
also corrected to account for word lexemes rather than claiming every entry is
produced by `Symbol.Lex`.

### Independent adjacent probes

Temporary probes, removed before repository gates, checked two paths not
maintained directly beside the fix:

- `first otherwise second otherwise backup`, with both earlier cells holding
  errors, returned `backup` and recorded all three dependencies; and
- `buggy otherwise fallback now`, where `buggy` is a fault and `fallback now`
  is effectful, returned the fault without invoking the fallback.

Both passed.  No adoption-scope leak, nested-chain error, or unused right-hand
execution was found.

## Status of `FRESHAUDIT4`

| prior item | result |
|---|---|
| an error read from a cell defeats `otherwise` | **Fixed.** The evaluator uses the graph's per-frame handling boundary. |
| a fault evaluates an unusable fallback | **Fixed.** One fault-aware predicate controls both evaluation and selection. |
| open calls misparse or fail at binding power 9 | **Fixed.** Power 6 places calls wholly on either side and retains the same table cost. |
| stale symbol-only operator remarks | **Fixed.** The documentation distinguishes word and symbol entries. |

## Verification

- Independent nested-handler and effectful-fault probes: **2 passed**, then
  removed.
- Debug: **908 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **908 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check da3f9d9..8798edc`: clean.
- The worktree was clean before this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.
