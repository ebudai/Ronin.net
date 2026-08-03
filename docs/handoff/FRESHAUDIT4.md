# Fresh audit 4 — `otherwise`

**Audited:** `1c0404e` through `da3f9d9`

**Date:** 2026-08-02

## Result

**Three correctness findings: two high, one medium.  No sign-off.**

The word operator resolves and the good-left short circuit works, but the new
evaluator path is not joined to the graph's existing error-handling mechanism.
As a result, the source form cannot catch an error read from another cell—the
ordinary case the runtime's `Handling` API was built for.  Faults have the
opposite problem and evaluate an arm they can never use.  Separately, the
chosen binding power puts `otherwise` above open-ended pattern calls, silently
guarding their final argument instead of their result on the left and refusing
them entirely on the right.

All three were reproduced through real lexer/resolver trees and the evaluator;
the maintained suite remains green because its fallback error is manufactured
inside the same expression rather than read from a dependency, its fault tests
exercise `Builtin.Otherwise` directly, and it has no pattern-call operands.

## Findings

### 1. An error read from a cell defeats source-level `otherwise`

**Severity: high — the advertised error handler returns the error it was asked
to replace**

`Evaluator.Apply` evaluates the left operand with an ordinary call to
`Evaluate` (`Compiler/Runtime/Evaluator.cs:115-125`).  If that walk reaches a
name, `Graph.Read` records any `Error` it returns for adoption
(`Compiler/Runtime/Graph.cs:517-532`).  At the end of the derived cell's
recompute, the adopted error replaces whatever the body returned.

That is normally the graph's central propagation guarantee.  `otherwise` is
the explicit exception, and the runtime already has the exact boundary for it:
`Graph.Handling` evaluates an expression without arming adoption in the current
body (`Graph.cs:537-562`).  The new evaluator never calls it.

#### Full resolver/evaluator witness

The temporary probe resolved `failed otherwise standby` against two real names,
then evaluated the resulting tree as a `Graph.Let` body:

```text
failed  = error(bad input)
standby = 5

expected guarded: 5
actual guarded:   error(bad input)
```

`Otherwise` itself selected `5`; recompute then discarded that result and
reapplied the error inherited by the plain read.

The maintained failing-left test does not cover this join.  It evaluates
`reading / missing otherwise standby`, where division creates an `Error`
locally.  No `Graph.Read` returned that error, so adoption never armed and the
test passes.  Existing runtime tests explicitly write the missing operation:

```csharp
Builtin.Otherwise(scope.Handling(() => scope.Read("parsed")), 0d)
```

They therefore prove the graph API and expose what the evaluator omitted.

**Recommendation:** make the source operator evaluate the expression it handles
through the graph's per-frame handling boundary.  Do not make suppression
graph-wide; the existing nested-recompute tests explain why it belongs only to
the current adoption frame.  Add evaluator-tree regressions for:

- an `Error` held directly by a var;
- an error returned by a dirty derived cell;
- recovery from error to a good left value; and
- the fallback dependency appearing only while the left is failing.

### 2. A fault evaluates the fallback even though faults cannot be caught

**Severity: medium — unused dependencies and potentially unused effects occur
on the defect path**

The two definitions of the decision disagree:

```csharp
Needs = value => value is Error or Nothing
```

matches `Fault`, because `Fault` derives from `Error`
(`Compiler/Runtime/Values.cs:102-114`).  But `Builtin.Otherwise` explicitly
returns a `Fault` unchanged before considering ordinary errors
(`Values.cs:145-153`).

The value therefore remains a fault, which lets a result-only test pass, but
the evaluator first evaluates the right operand.  A focused graph/evaluator
probe produced:

```text
guarded result:       fault(InvalidOperationException: defect)
guarded dependencies: buggy, standby
```

`standby` should not be there.  Changing it can now dirty and recompute
`guarded` even though no possible value of the fallback can replace the fault.
Outside a pure `let`, the same erroneous evaluation can invoke an effectful
right-hand call.

This is a concrete DRY failure: whether the left value needs a fallback is
stored separately from the function that decides whether the fallback wins,
and `Fault` has already made them diverge.

**Recommendation:** give short-circuiting one authoritative decision that can
distinguish “return the left now” from “evaluate and apply the right,” or at
least derive both `Needs` and `Otherwise` from one fault-aware predicate.  Add a
test that asserts the fallback body was not evaluated and is absent from the
dependency set when the left is a fault; asserting only the final `Fault` is
vacuous for this defect.

### 3. Binding above pattern calls makes ordinary call operands misparse or fail

**Severity: high — a fallback around a function call silently guards the wrong
expression**

Open-ended word-pattern calls bind at `Resolver.PatternBindingPower`, currently
7.  They are available only where the requested minimum is at most that level
(`Compiler/Resolution/Resolver.cs:84-90, 466-470`).  The new operator binds at
9 (`Compiler/Runtime/Values.cs:102-114`).

Consequently, for a declared pattern `parse (_)` and names `input` and
`standby`:

```ronin
parse input otherwise standby
```

resolves silently as:

```text
parse («input» otherwise «standby»)
```

It supplies a fallback to `input` and then calls `parse`; it does **not** catch
an error returned by `parse`.  The mirror image:

```ronin
input otherwise parse standby
```

returns `NoParse`, because the right side's minimum excludes the open-ended
call entirely.

Both operands are values under the authoritative fallback grammar
(`docs/spec/grammatical-structure.md:499-511`).  The resolver's own contract
also says pattern calls sit above “plumbing operators”; power 9 puts
`otherwise` on the arithmetic side of that boundary instead
(`Resolver.cs:84-90`).

The stated allocation reason for choosing 9 does not require this semantic
cost.  A temporary binding-power-6 control:

- resolved both call forms with the call as the whole operand;
- preserved all four maintained precedence/associativity readings;
- passed the resolver allocation ceiling; and
- added one reachable DP minimum by borrowing the already-present pattern level
  7, exactly as power 9 borrows arithmetic level 10.

The control run had ten passing focused tests; its only failure was the test
that hard-codes `[9, 10, 20]` as the operator powers.

**Recommendation:** put `otherwise` below the open-pattern binding level (the
measured adjacent level 6 is sufficient at the current level 7) and add both
left-call and right-call resolver tests.  Include an evaluator regression where
the call itself returns an error, proving the fallback guards the call's result
rather than its last argument.  If brackets are instead intended to be
mandatory around call operands, that is a different language rule and must be
stated in the spec; the current value/value grammar and plumbing precedence do
not say it.

The nearby `SymbolTable.Operators` remarks should also be updated when this is
touched: they still describe every operator as a symbol produced by
`Symbol.Lex`, which became false when the table gained the word `otherwise`.

## Open design item, not counted as a finding

The declared-name takeover (`x otherwise y` becoming one declared name) is
implemented exactly as the new spec describes and is pinned by a test.  It is a
real compatibility choice, but the spec explicitly leaves reservation open, so
this audit does not silently decide it for the designer or count the documented
current behavior as an implementation defect.

## Verification

- Direct error-cell evaluator probe: failed `expected 5`, actual
  `error(bad input)`.
- Fault short-circuit probe: failed because dependency `standby` was present.
- Open-call resolver probes: left silently read as `parse (input otherwise
  standby)`; right returned `NoParse`.
- Binding-power-6 control: both call probes and the maintained fallback readings
  passed; resolver allocation remained below its ceiling; only the hard-coded
  power inventory assertion failed.
- Every temporary source and control edit was removed before repository gates.
- Debug: **901 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **901 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check 1c0404e..da3f9d9`: clean.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.
