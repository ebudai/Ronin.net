# Re-audit 78 — bottom and grouping are closed; action admissibility still has reachable bypasses

> **Ledger** — `[A]` Audit of `a8e3c79..c56ded6`, requested by
> `FORAUDIT78`. REAUDIT77 findings 1 and 3 are closed: top-level bottom operands
> are accepted directionally at the operator layer, and round singleton groups
> no longer hide initializer result types. **Not signed off:** finding 2 remains
> incomplete. Action admissibility is checked only after an operator has a typer
> and both operand sorts are known, and only at the top level; valid source can
> still use an action through an untyped peer, `otherwise`, or an inferred list.

## Audit result

The bottom repair is correct. `Numeric` accepts `Sort.Error` independently on
either side while still checking the non-bottom operand, and `Equality` admits a
top-level bottom against any value. The rule remains out of symmetric `Unify`, so
an ordinary value is still not assignable to a declaration of type `error`.
Arithmetic continues to produce `number`, equality `truth`, and an action paired
with an error is still rejected as an action before the bottom shortcut is used.

The initializer repair is correct as well. `Disagreements` unwraps a round
singleton `Grammar.Inputs` before collection handling, recursively through
multiple grouping levels. A square list remains a collection, and a matching
grouped operation remains clean. This makes the grammar-tree initializer path
agree with the resolved-tree `Inferred` path.

The direct action/action and action/typed-value witnesses now produce the right
`ActionInValue` findings. The implementation calls that “admissibility first,”
but its control flow still asks three earlier questions: does the operator have a
typer, can both operands be inferred, and is the action the operand's outermost
sort? Each question has a reachable negative answer that hides a known action.

## Finding 1 — high — action admissibility remains conditional and shallow

**Location:** `Compiler/Compilation.cs`, `Operations`, where the pass skips an
operation before inspecting actions when `Typer` is null or either operand has
no inferred sort; and `Listed`, which can ground and return `List(Action)`.

### Route A — an untyped peer suppresses the known action

```ronin
function act (x => number) { return; }
var u = 5;
var r = act 1 is u;
```

`act 1` has the known `Sort.Action`; only `u` lacks a recorded sort because its
declaration is unannotated. `Operations` requires both inferred operands before
checking either for action, so the file compiles with `0 problems`. Reversing the
operands is silent too.

**Expected:** one `ActionInValue` on `act 1`. Uncertainty about the other operand
cannot make a known action into a value.

### Route B — an operator without a typer skips admissibility entirely

```ronin
function act (x => number) { return; }
var n => number;
var r => number = act 1 otherwise n;
```

`otherwise` deliberately has no typer until its later slice. `Operations` checks
that fact before inferring or classifying either operand, so this also compiles
with `0 problems`; an action on the right is silent in the same way.

**Expected:** one `ActionInValue` at the action operand. The later typing of
`otherwise` may remain deferred, but the standing rule that an action is admitted
in no value position is already available and independent of an operator's type
relation.

### Route C — the aggregate bypass is reachable through inferred returns

```ronin
function act (x => number) { return; }
function actions (x => number) { return [act x]; }
var r => truth = actions 1 is actions 2;
```

This source parses. `Listed` infers the helper's answer as `List(Action)`;
`Sort.Ground` treats that as ground, and the return inference stores it. The two
calls are then valid `is` operands whose outer sorts are lists, so the top-level
action check does not see them and list unification succeeds. The whole file
compiles with `0 problems`.

The same helper assigned to `list of number` is also silent: the mismatch cannot
spell `list of action`, so the existing comparison drops it. The invalidity
originates earlier—`act x` cannot be a list element at all—and should be reported
there.

The direct `[act 1] is [act 2]` parse failure cited by FORAUDIT78 therefore does
not prove the aggregate boundary unreachable. A list literal need not stand
directly as the operator operand; an inferred function call can carry the same
sort across that syntax boundary.

### Repair direction and regression coverage

For operations, infer and classify each operand independently. Emit a known
`ActionInValue` before consulting the peer or the operator typer; only the
operator-specific relation needs both sorts and a typer. This preserves the
correct direct behavior while covering untyped peers and deferred operators.

Separately, prevent a value container from laundering `Action` into a ground
aggregate sort. Report the action at the source value position—the `act x` list
element—rather than waiting for a later consumer to compare an unspellable
`List(Action)`. A context-aware admissibility walk or inference result that can
carry the finding is preferable to merely returning null, which would restore
silence.

Maintain controls for:

- action/typed-value and action/action (the repaired direct cases);
- action/untyped-value in both operand orders;
- action on either side of `otherwise` while it has no typer;
- an action in a typed list initializer (the existing closed route); and
- an action in a list returned by an omitted-return function, both by itself and
  when the inferred call later reaches `is`.

## Disposition of REAUDIT77

| Prior finding | Reassessment |
|---|---|
| 1 — bottom operands rejected | **Closed.** Directional operator-layer compatibility works in both orders and does not excuse the other operand. |
| 2 — action operands admitted or misclassified | **Partially closed.** Direct fully-inferred typed operators work; the three routes above remain. |
| 3 — grouped initializer mismatch hidden | **Closed.** Round singleton groups unwrap recursively; square aggregates remain distinct. |

## Verification performed

- Reviewed `FORAUDIT78`, `REAUDIT77`, the governing rulings, and the complete
  production/test diff for `a8e3c79..c56ded6`.
- Reproduced the repaired bottom, direct action, and nested initializer controls
  through the public compiler.
- Reproduced all three remaining action routes through the public compiler,
  including both `otherwise` sides and both untyped-peer orders; temporary
  sources were removed.
- Locked net10 restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Debug and Release suites: `1362` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- Generated-ledger check and `git diff --check`: clean before this report.

FORAUDIT78 is not signed off. The bottom and grouping repairs hold, but action
admissibility must be independent of typer availability, peer inference, and
outer container shape before REAUDIT77 finding 2 is closed.
