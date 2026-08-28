# Re-audit 77 — the operator foundation mishandles bottom, actions, and bracketed initializers

> **Ledger** — `[A]` Audit of `427dabb..a8e3c79`, requested by
> `FORAUDIT77`. The net10 move, relayed-document headers, arithmetic/equality
> rules, optional equality carve-out, and direct result-flow paths are sound.
> **Not signed off:** three high semantic gaps remain in the reusable foundation:
> `error` operands are rejected instead of treated as bottom; two action results
> pass `is` as values; and grouping an operation in a typed initializer suppresses
> its result mismatch.

## Audit result

The core of Slice 1(a) is correctly shaped. Operators carry their runtime,
precedence, and typing facts together; all four arithmetic operators are strictly
numeric; `is` requires compatible value types and admits the ruled one-level
`optional T is T` comparison. A valid operation result flows through direct
arguments, returns, nested operations, inferred returns, and list elements. The
single-part `Node.Group` unwrapping is correct for those resolved-tree consumers,
and invalid inner operations suppress derivative outer mismatches.

The new abstraction does not yet distinguish three states that its callers need:
a bottom value that an operator accepts, an action sort that is not a value at all,
and an operation result hidden behind the initializer's separate grammar-tree
path. Each produces a source-level contradiction despite the maintained gate.

## Finding 1 — high — `error` operands receive a second, false `OperandType`

**Location:** `Compiler/Runtime/Values.cs`, `Numeric` and `Equality`, which rely
on `Sort.Unify`; and `Compiler/Checking/Sort.cs`, where `Unify` has no bottom
case.

### Witnesses

```ronin
var e => error;
var n => number;
var r = e + n;
```

```ronin
var e => error;
var n => number;
var r = e is n;
```

**Actual:** the first reports “`+` takes two numbers”; the second reports that
`is` received two different types. Reversing the operands produces the same
false findings.

**Expected:** neither operation raises `OperandType`. `error` is the bottom and
the slice explicitly says a failure flowing into an operator is not diagnosed a
second time. Arithmetic retains its ordinary `number` result type; equality
retains `truth`.

The consultation's premise that `Sort.Error.Same` already supplies this behavior
is false: `Sort.Equals` first requires equal runtime sort classes, and `Unify`
only special-cases variables, lists, and optionals. An `Error` therefore equals
another `Error`, not a `Scalar`.

### Repair direction and regression coverage

Preserve the ruling's directionality. Do not make `Error` a symmetric “any” in
general type equality: `Error` is assignable to every expected value type, while
ordinary values are not assignable to a declared `error`. The operator layer can
treat a top-level `Error` operand as accepted (while still checking the other
operand), or consume a directional compatibility operation that states the same
rule.

Add both operand orders for arithmetic and `is`, plus a control such as
`e + textValue` showing that accepting the bottom operand does not excuse an
independently wrong non-bottom operand.

## Finding 2 — high — `is` silently admits action results as values

**Location:** `Compiler/Runtime/Values.cs`, `Equality`, and
`Compiler/Compilation.cs`, `Operated`/`Operations`. The typer receives bare
`Sort` values and calls `Unify` without first enforcing value admissibility.

### Witness

```ronin
function f (x => number) { return; }
var r => truth = f 1 is f 2;
```

Both calls return `Sort.Action`. Since two actions structurally unify,
`Equality` returns `truth`; that result agrees with the initializer, and the
whole file compiles with `0 problems`.

**Expected:** an action is admitted in no value position. Each action call used
as an operand should retain the existing `ActionInValue` classification, and the
equality operation should produce no value type from inadmissible operands.

The adjacent mixed witness exposes the same ordering error rather than silence:

```ronin
function f (x => number) { return; }
var amount => number;
var r => truth = f 1 is amount;
```

It currently reports `OperandType`, describing two value types that disagree.
The primary fault is instead that `f 1` is not a value. Admissibility precedes
the operator's behavioral/type relation.

### Repair direction and regression coverage

Check value admissibility before invoking a typer, in both result inference and
the finding pass. Preserve the action operand's source span and emit
`ActionInValue`; do not merely make `Equality` return null, which would replace
the silent success with the still-wrong `OperandType`. Cover action/action,
action/value, and an action nested through a transparent round group. Aggregate
operands should also be checked to ensure an action cannot become admissible by
being wrapped in a value container.

## Finding 3 — high — parentheses suppress an initializer result mismatch

**Location:** `Compiler/Compilation.cs`, `Initializers`/`Disagreements`. Returns
and arguments resolve through `Inferred`, including its new single-group unwrap;
the initializer's separate grammar-value path does not.

### Witness

```ronin
var a => number;
var b => number;
var r => truth = (a + b);
```

**Actual:** `0 problems`.

Without the semantically transparent parentheses, `var r => truth = a + b;`
correctly reports that the operation is a `number` where `truth` is declared.
The operation pass sees valid numeric operands in both forms, so it has no
operand finding to emit; only result flow is lost.

**Expected:** the bracketed form produces the same `TypeMismatch` as the direct
form. Round grouping changes resolution, not the value or its type.

### Repair direction and regression coverage

Make initializer references consume the same resolved-node inference used by
returns and arguments, or explicitly unwrap round singleton groups before the
structural collection path. Preserve the distinction from square list/lookup
literals. Pin one and multiple grouping levels, a correct matching initializer,
and a bracketed operation whose own operands are invalid so the operand finding
continues to suppress a derivative result mismatch.

This gap belongs to Slice 1(a): operations did not have result types before this
slice, but they do now, and adding or removing parentheses cannot decide whether
that new type reaches a typed initializer.

## Opinion on the finding kind

Keep one `OperandType` kind for arithmetic and compatible-type equality. Both
identify the same repair class—an operator received operands outside its type
relation—while `Symbol` and the message carry the operator-specific explanation.
A distinct `NotIndexable` for Slice 1(b)'s non-indexable left remains appropriate:
that is a missing capability on one value with a different repair. A wrong
lookup key or list index can still use the relational operand-type category.

## Range disposition

| Area | Reassessment |
|---|---|
| net8 → net10 project and lock-graph move | **Signed off.** All projects build and test as `net10.0`; locked restore is clean. |
| Relayed documents, headers, and ledger edges | **Signed off.** The generated ledger is reciprocal and current. |
| Numeric and ordinary `is` typing | **Holds.** Ground scalar, named, aggregate, and optional controls behave as ruled. |
| Operator result flow | **Partial.** Resolved-tree consumers hold; bracketed initializers remain silent (finding 3). |
| Special checker sorts | **Not complete.** Bottom and action policies are inverted or omitted at the typer boundary (findings 1–2). |

The noted literal/parser asymmetry is unchanged by the slice and is not an audit
finding here.

## Verification performed

- Reviewed `FORAUDIT77`, `SLICEONETYPINGS`, `CHECKERCONSULT`, the relevant
  standing rulings, and the complete production/test diff for
  `427dabb..a8e3c79`.
- Reproduced both operand orders for bottom-valued arithmetic/equality, the
  action/action silent success, the action/value misclassification, and direct
  versus grouped initializer result flow through the public command-line
  compiler; temporary sources were removed.
- Locked net10 restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Debug and Release suites: `1359` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- Generated-ledger check and `git diff --check`: clean before this report.

FORAUDIT77 is not signed off. The ordinary operator cases are a solid base, but
bottom compatibility, action admissibility, and initializer grouping must be
closed before later operator slices depend on this foundation.
