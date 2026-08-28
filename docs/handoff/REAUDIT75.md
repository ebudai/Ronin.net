# Re-audit 75 — reaction suppression is lost in transparent nested blocks

> **Ledger** — `[A]` Audit of `954b49d..9b33d52`, requested by
> `FORAUDIT75`. Re-anchoring `Unreachable` on the call is correct, and direct
> reaction bodies now report `AnsweringReaction` alone. **Not signed off:** the
> suppression follows only the current syntactic scope's `Reacts` bit. An `if`,
> loop, or other transparent block inside a `when` resets that bit, so the
> derivative `Unreachable` survives there while the governing
> `AnsweringReaction` is itself lost.

## Audit result

The finding now names what it is about. Direct `send return 5` is reported at
the `send` call, nested calls blame the nearest call whose argument exits, and
multiple return arguments to one call fold to one dead-call finding. The direct
`when` controls also implement FINDINGCOMPOSITION: a value-return is
inadmissible, so `AnsweringReaction` remains and `Unreachable` steps aside.

The implementation records `Checking.Reacting` from the scope currently being
gathered, however, and recursive scope construction passes `body.Reacts` rather
than the enclosing reaction context. That is exact for the `when` body itself
and false for its nested `if`/loop/block bodies. Those bodies are transparent to
the callable exit—the return still belongs to the reaction—but the two checks
now classify them as non-reacting.

## Finding 1 — high — a nested block in a `when` reports the derivative finding instead of the inadmissible return

**Location:** `Compiler/Compilation.cs`: `Scope` stores the local `reacting`
argument in `Checking`; its recursive `Body` calls pass `body.Reacts`, and the
check phase suppresses `Unreachables` only when that local value is true.

### Witness

```ronin
function send (x => number) => number { return x; }
var ready => number;
when ready { if ready { send return 1; } }
```

The `if` is transparent: `return 1` still attempts to answer the enclosing
reaction, where a value-return is inadmissible. Evaluating it as `send`'s
argument also prevents `send` from running, but FINDINGCOMPOSITION says that
derivative behavior must step aside.

**Actual:** one `Unreachable` on `send`, and no `AnsweringReaction`.

**Expected:** one `AnsweringReaction` on `return 1`, and no `Unreachable`.

This is more than incomplete suppression: it reverses the ruling's priority and
leaves only the finding that admissibility was supposed to subsume. The same
context loss applies to loops and ordinary transparent blocks within a
reaction. A delegate nested in a `when` is the opposite boundary and must not be
blanket-suppressed: it is its own callable, so a legal
`() => { send return 1; }` should retain `Unreachable`.

### Repair direction and regression coverage

Carry semantic reaction ownership through transparent scopes, and reset it on
entry to a nested callable (delegate or function), just as return ownership is
already separated. `AnsweringReaction` and `Unreachable` suppression should
consult the same ownership fact; otherwise they can disagree again.

Add a maintained nested-`if` witness asserting the exact pair above, a loop or
plain-block control, and a nested-delegate control asserting that the delegate's
legal dead call still reports `Unreachable`.

## Disposition of REAUDIT74

| Prior item | Reassessment |
|---|---|
| Per-callable `Unanswered` suppression | **Holds.** This range does not disturb owner filtering. |
| Direct/list/nested `Unreachable` cases | **Holds with the new call anchor.** |
| Lookup-key traversal finding | **Still open.** The revised `Reach` continues to visit only `part.Value`. |

## Verification performed

- Reviewed `FORAUDIT75`, `FINDINGCOMPOSITION`, and the production/test diff for
  `954b49d..9b33d52` after FORAUDIT74.
- Reproduced the nested-`if` inversion through the public command-line compiler;
  the direct reaction and nested-delegate boundary were checked alongside it,
  and temporary source files were removed.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Debug and Release suites: `1352` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Generated-ledger check and `git diff --check`: clean.

FORAUDIT75 is not fully signed off. Its direct reaction composition and call
anchor are correct, but suppression must follow the reaction through transparent
scope boundaries before the ruling is complete.
