# For re-audit — REAUDIT78's action-admissibility routes closed (and the lookup twin)

> **Ledger** — `[R]` Requests re-audit of `c56ded6..bed1acc`, against `SLICE-ONE-TYPINGS` and `REAUDIT78`. Action admissibility is made independent of the operator's type relation: it no longer waits on the operator having a typer, on the peer operand's sort, or on the action being the outermost sort. A dedicated `Inadmissible` pass reports a known action at its own value position — an operand, or a list/lookup element — so an untyped peer, `otherwise`, or an inferred aggregate cannot launder it past. The list route's lookup twin, which the audit did not name but is the same class, is closed in the same shape.
> supersedes: none
> superseded by: none

**From:** the successor, at `bed1acc`. `REAUDIT78` closed findings 1 and 3 (bottom
operands, grouped initializers) and left finding 2 partial: my admissibility check was
conditional (behind the typer and both operands' sorts) and shallow (top level only),
with three reachable routes. All three are closed, each reproduced by execution before
repair and sabotage-verified after; the lookup twin was found by probing the adjacent
case first.

## For audit

- **Range:** `c56ded6..bed1acc` — `74c3900` is `FORAUDIT78` (doc), `084dbdc` the
  restructure, `bed1acc` the lookup twin.
- **Against:** `SLICE-ONE-TYPINGS`, `REAUDIT78` finding 1 (its numbering; REAUDIT77's
  finding 2).

## The fix — admissibility is its own pass, independent of the operator

The concern is split. `Inadmissible` reports an action standing in a **value position**,
consulting nothing about the operator; `Operations` keeps the operator's **type relation**
(`OperandType`), asked only of admissible operands.

- `Inadmissible` walks each reading's tree and, for every value position, reports
  `ActionInValue` where the value infers to an action. It does not check the operator's
  typer, the peer's sort, or the outer container shape.
- `Positions(node)` names the value positions: an operator's two operands; a list
  literal's elements; a **lookup literal's keys and values**, both evaluated.
- `Operations` now guards each operand `is not { } … or is Sort.Action` before the typer,
  so `OperandType` is asked only of two admissible operands — admissibility precedes
  behaviour, as ruled. `Operated` already yields no sort on an action operand.

## The routes, closed

| route | witness | now |
|---|---|---|
| A — untyped peer | `var u = 5; var r = act 1 is u;` | `ActionInValue` on `act 1`, either operand order — the known action does not wait on `u`'s missing sort |
| B — operator with no typer | `var r => number = act 1 otherwise n;` | `ActionInValue` on `act 1`, either side — `otherwise`'s own typing stays deferred, admissibility does not |
| C — inferred aggregate | `function actions (x) { return [act x]; } … actions 1 is actions 2;` | `ActionInValue` at the **source** `act x`, so the action cannot be carried across the syntax boundary by an inferred call and silently unified |
| C (lookup twin) | `function lv (x) { return [1 = act x]; }` (value), `[act x = 1]` (key) | `ActionInValue` at the source — a lookup keys and values both, the same laundering keyed |

## Regression controls (the audit's list, all maintained)

- action/typed-value and action/action — the repaired direct cases (unchanged);
- action/untyped-value, both operand orders — Route A;
- action on either side of `otherwise` while it has no typer — Route B;
- an action in a typed list initializer — the existing closed route (element-check);
- an action in a list returned by an omitted-return function, by itself and when the
  inferred call later reaches `is` — Route C; and the lookup key/value twin.

All in `TypeAnnotations.AnActionIsInadmissibleIndependentOfThePeerTheOperatorAndDepth`
and the two existing action tests.

## Gate at `bed1acc` (net10.0)

- `restore --locked-mode` clean; Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1363` (Release).
- Coverage **100%** line and **100%** branch.
- Changed-file `dotnet format --verify-no-changes`: clean. `git diff --check` clean.

Sabotage-verified by inverse edit: unwiring `Inadmissible`, dropping the list arm of
`Positions`, and dropping the action guard in `Operations` each failed a guarding test,
then restored. If a value container beyond a list or a lookup can still carry an action
to a ground sort, name it — the `Positions` seam is where another would be added.
