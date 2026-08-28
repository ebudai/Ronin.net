# Re-audit 76 — lookup strictness and reaction ownership are complete; signed off

> **Ledger** — `[A]` Audit of `f1a9931..9fc8b20`, requested by
> `FORAUDIT76`. Both high findings are closed: keyed groups are walked in the
> evaluator's key-before-value order, and reaction ownership follows transparent
> scopes while stopping at nested callables. The direct cases, deeper
> compositions, and opposite-boundary controls all hold. **Signed off with no
> findings.**

## Audit result

The one-commit repair closes both findings at their source rather than
special-casing the witnesses.

For REAUDIT74, `Reach` now derives keyedness from `Node.Group.Kind` and visits a
key before its value. That matches the group invariant and the runtime
evaluator: a lookup evaluates both halves of each entry in that order. A return
in either half therefore keeps the enclosing call context, while ordinary lists
and groups continue to walk values only. The nested-call-in-key control confirms
that the key subtree is genuinely traversed and that the nearest dead call is
reported.

For REAUDIT75, recursive scope construction now inherits `reacting` through
transparent blocks and resets it when `body.Delegate` opens a new callable. The
same propagated fact feeds both `Exits` and the `Unreachable` guard, so a nested
block cannot again retain the derivative finding while losing
`AnsweringReaction`. Named callable bodies already take the separate recursion
path with their own `body.Reacts` value, and ancillary delegates likewise begin
with their own reaction state, so functions and parameter-default delegates
retain the same boundary.

## Boundary checks

- `send [return 5 = 1]` and `send [1 = return 5]` each report the enclosing
  `send`; `send [send return 5 = 1]` reports the inner `send` reached through the
  key.
- Multiple nested transparent scopes preserve reaction ownership: a return
  under `when` → `if` → `while` reports `AnsweringReaction` and no
  `Unreachable`.
- A delegate under a reaction resets ownership, including when another
  transparent block sits inside the delegate: its legal value-return produces
  `Unreachable` and no `AnsweringReaction`.
- A nested named function resets the reaction boundary as well and retains its
  legal dead-call finding.

## Disposition of the prior findings

| Prior finding | Reassessment |
|---|---|
| REAUDIT74 — lookup keys skipped by `Reach` | **Closed.** Keyed groups visit key then value using the declared kind. |
| REAUDIT75 — nested blocks lose reaction ownership | **Closed.** Transparent blocks inherit; delegates and named callables reset. |

## Verification performed

- Reviewed `FORAUDIT76` and the complete production/test diff for
  `f1a9931..9fc8b20` against `UNRESOLVEDRETURNAMENDMENT` §3 and
  `FINDINGCOMPOSITION` §2–§3.
- Reproduced the repaired lookup-key witness through the public compiler.
- Executed deeper reaction nesting, nested-delegate-plus-block, and nested
  named-function probes through the public compiler; temporary sources were
  removed.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Debug and Release suites: `1354` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- `git diff --check`: clean before this report.

FORAUDIT76 is signed off. REAUDIT74 and REAUDIT75 are closed, and the
`Unreachable` finding now respects both aggregate evaluation and reaction
ownership across the tested scope boundaries.
