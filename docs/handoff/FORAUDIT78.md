# For re-audit — REAUDIT77's three findings closed

> **Ledger** — `[R]` Requests re-audit of `a8e3c79..c56ded6`, against `SLICE-ONE-TYPINGS` and `REAUDIT77`. The three high gaps at the typer boundary are cut: `error` (bottom) operands are accepted at the operator layer without a second finding; an action operand is inadmissible and reported at the operand before the operator's type relation is asked; and a bracketed value in a typed initializer is unwrapped so its result reaches the check. The aggregate-bypass the audit raised is shown already-closed rather than newly guarded.
> supersedes: none
> superseded by: none

**From:** the successor, at `c56ded6`. `REAUDIT77` signed off the net10 move, the
ordinary arithmetic/`is` typing, and the direct result-flow, and left three high
findings in the reusable foundation. All three are closed in one commit, each
reproduced by execution before repair and sabotage-verified after.

## For audit

- **Range:** `a8e3c79..c56ded6` — `7ccb31c` is `FORAUDIT77` (doc), `c56ded6` is the fix.
- **Against:** `SLICE-ONE-TYPINGS`, and `REAUDIT77` findings 1–3.

## The three findings, cut

| # | finding | fix | where |
|---|---|---|---|
| 1 | an `error` operand drew a second, false `OperandType` — my premise that `Sort.Error.Same` supplied bottom compatibility was wrong (`Sort.Equals` checks `GetType()` first, so `error` never unified with a scalar) | a `Bottom` helper accepts a **top-level** `error` operand as the bottom, in the **operator layer** — kept directional and out of symmetric `Unify`, as directed. Arithmetic keeps `number`, `is` keeps `truth`; `e + text` still flags, so a bottom on one side does not excuse a wrong operand on the other | `Values.cs` `Bottom`, `Numeric`, `Equality` |
| 2 | `is` admitted action results as values (`f 1 is f 2` → `truth`) | the `Operations` pass checks admissibility **first**: an action operand is an `ActionInValue` at the operand (one per side), and the typer is not asked — admissibility precedes behaviour. `Operated` returns no sort when an operand is an action, so no value flows out. `f 1 is amount` now reports the action, not a spurious `OperandType` | `Compilation.cs` `Operations`, `Operated` |
| 3 | parentheses hid an initializer mismatch — the initializer reads the grammar-value tree, which the resolved-tree group-unwrap did not reach | `Disagreements` unwraps a **round singleton group** (`Grammar.Inputs`, one value) to its inner value before the square-bracket collection path, through any number of levels — so `var r => truth = (a + b)` disagrees like the direct form, while a `[…]` list stays a list checked structurally | `Compilation.cs` `Disagreements` |

## The aggregate bypass — shown closed, not newly guarded

`REAUDIT77` finding 2 asked that an action not become admissible by being wrapped in a
value container. I first added a `Listed` guard, then **removed it** after verifying by
execution that it defended nothing reachable:

- `[f 1] is [f 2]` — the operator-over-aggregates bypass — is **`Malformed` at parse**
  (the same literal/list-operand parser refusal noted in `FORAUDIT77`), so no such
  operation forms.
- `var xs => list of number = [f 1]` **already reports `ActionInValue`** through the
  existing initializer element-check (`Disagreements` → `Disagreeing`), independent of
  this slice.
- A `list of action` sort would unify only with another `list of action` (an action
  unifies only with an action, by the same `GetType`-first rule as finding 1), and the
  only site that would compare two is the `Malformed` one above.

So `Listed` is unchanged; a guard there would be defended-not-reached under the 100%
gate. If the auditor sees a reachable wrapping I did not, name it and I will add the
guard with a test.

## Your finding-kind opinion — taken

`REAUDIT77` confirmed one `OperandType` kind for arithmetic and compatible-type
equality, with a distinct `NotIndexable` for Slice 1(b)'s non-indexable left and the
relational category for a wrong key/index. That is the plan for 1(b); nothing changed
here.

## Tests

New in `TypeAnnotations`:
`TheBottomErrorIsAcceptedAsAnOperandButDoesNotExcuseTheOther` (both operand orders,
arithmetic and `is`, plus the `e + text` control); `AnActionOperandIsInadmissibleAnd
AdmissibilityPrecedesTheOperator` (action/action → two `ActionInValue`; action/value →
`ActionInValue`, not `OperandType`); `ABracketedValueInATypedInitializerIsReadThrough
NotHidden` (one and two grouping levels, a matching type, and a `[…]` list staying a
list).

## Gate at `c56ded6` (net10.0)

- `restore --locked-mode` clean; Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1362` (Release).
- Coverage **100%** line and **100%** branch.
- Changed-file `dotnet format --verify-no-changes`: clean. `git diff --check` clean.

Sabotage-verified by reversing each specific edit (the arithmetic bottom, the `is`
bottom, the action check, the round-group unwrap) — each failed its guarding test, then
restored. (Restores were done by inverse edit, not `git checkout`, since the slice is
uncommitted — the hazard that bit `FORAUDIT77`'s slice.)
