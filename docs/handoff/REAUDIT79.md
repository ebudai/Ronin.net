# Re-audit 79 — the named routes close, but `Positions` is not the set of value positions

> **Ledger** — `[A]` Audit of `c56ded6..bed1acc`, requested by
> `FORAUDIT79`. REAUDIT78's untyped-peer, no-typer, inferred-list, and adjacent
> lookup routes are closed. **Not signed off:** the replacement pass claims every
> action in a value position but enumerates only operation operands and square
> aggregate entries. Call arguments, omitted-return answers, round inputs, and
> untyped initializer roots remain valid source routes that compile silently.

## Audit result

The requested three routes now behave correctly. `Inadmissible` runs independently
of operator typing, so a known action reports beside an untyped peer and on either
side of `otherwise`. List and lookup positions are inspected at their source,
including lookup keys and values, so an omitted-return helper can no longer infer
and export `List(Action)` or the corresponding lookup shape from those literals.
`Operations` now leaves action classification to the admissibility pass and does
not stack `OperandType` on top.

The abstraction is still incomplete. `Inadmissible` walks every resolved node,
but `Positions(node)` recognizes only three parent shapes:

- operation → left and right;
- square list → values; and
- square lookup → keys and values.

Walking every node does not make the child set complete. A value position is a
semantic role, and several such roles are represented by `Node.Call`, round
`Node.Group`, or by the grammar context around a reading rather than by any parent
node in the resolved tree.

## Finding 1 — high — actions remain legal in call and initializer value positions

**Location:** `Compiler/Compilation.cs`, `Inadmissible` and `Positions`.
`Positions` has no `Node.Call` or round-group case, and the pass receives readings
without the grammar role needed to distinguish an initializer root from a legal
standalone action statement.

### Route A — a generic call argument

```ronin
function act (x => number) { return; }
function use (x) => number { return 1; }
var r => number = use (act 1);
```

**Actual:** `0 problems`.

**Expected:** one `ActionInValue` on `act 1`. A call argument is a value position
whether its parameter has a written type or is generic. The existing `Arguments`
pass catches the typed-parameter version only; the missing annotation changes what
can be compared, not whether an action is a value.

### Route B — an omitted-return answer

```ronin
function act (x => number) { return; }
function outer { return act 1; }
```

**Actual:** `0 problems`; `outer` is inferred as an action.

**Expected:** `ActionInValue` on `act 1`. The argument of `return (_)` is a value
position. A written return type already reaches `Disagreeing` and reports the
action, but omitting the type cannot make the same expression admissible.

Both routes are `Node.Call` arguments: ordinary function arguments in the first,
the supplied answer call's argument in the second. `Positions` currently returns
none for either call.

### Route C — an untyped initializer root

```ronin
function act (x => number) { return; }
var r = act 1;
```

The direct and round-grouped forms both compile with `0 problems`:

```ronin
var r = (act 1);
```

**Expected:** `ActionInValue`. A datum initializer is a value position even when
there is no annotation to compare against. The typed form is already caught by
`Initializers`; the untyped form is skipped there, and the resolved tree alone
does not say that its root belongs to a datum rather than being a legal standalone
action invocation.

### Round/multi-input control

The same omission is visible when an action is carried through a round input
group to a generic call:

```ronin
function act (x => number) { return; }
function use (x) => number { return 1; }
var r => number = use ((act 1, 2));
```

This also compiles cleanly. Square aggregates now expose their parts; round groups
do not. Grouping must be traversed without turning brackets themselves into a new
value or emitting both a group-level and an inner finding.

### Repair direction and regression coverage

Make admissibility a context walk rather than a cross-product of `tree.Whole` and
a partial child switch:

- every `Node.Call` argument is a value position, including `return (_)` and a
  generic parameter;
- round grouping is transparent, and every part of a multi-input value is still
  a value position;
- list and lookup entries remain value positions as now; and
- grammar-owned roots such as datum initializers must carry their role into the
  check even when no annotation supplies an expected sort.

Do not simply classify every reading root: a standalone call to an action is the
normal way to perform it and must remain legal. The pass needs the surrounding
role, not a blanket root rule. A recursive context walk also avoids duplicate
findings where a parent position contains a transparent group whose inner action
would otherwise be visited again through `tree.Whole`.

Maintain paired controls for typed and generic call parameters, written and
omitted return types, typed and untyped initializers, singleton and multi-part
round groups, and a standalone action statement that remains clean. Keep the
operation/list/lookup controls from FORAUDIT79 to pin the routes already closed.

## Disposition of REAUDIT78

| Prior route | Reassessment |
|---|---|
| A — action beside an untyped operator peer | **Closed.** Each operation operand is checked independently. |
| B — action under an operator without a typer | **Closed.** `otherwise` no longer gates admissibility. |
| C — inferred list laundering | **Closed for list literals.** The source element reports before a `List(Action)` can escape. |
| Adjacent lookup key/value laundering | **Closed.** Both evaluated halves are positions. |
| General “action in no value position” contract | **Still open.** Finding 1 names the remaining roles. |

## Verification performed

- Reviewed `FORAUDIT79`, `REAUDIT78`, the governing action-type rulings, and the
  complete production/test diff for `c56ded6..bed1acc`.
- Verified the repaired operation, list, and lookup routes through the maintained
  integration controls.
- Reproduced the generic-call, omitted-return, direct/grouped untyped-initializer,
  and multi-input routes through the public compiler; temporary sources were
  removed.
- Locked net10 restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Debug and Release suites: `1363` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- Generated-ledger check and `git diff --check`: clean before this report.

FORAUDIT79 is not signed off. Its named action routes are closed, but the
dedicated pass must model the complete semantic set of value positions before the
standing action-admissibility rule is enforced.
