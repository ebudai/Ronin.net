# Re-audit 28 — `REAUDIT27` incorporation

**Audited:** `43d2ca9` through `6236555`

**Date:** 2026-08-03

## Result

**No sign-off. Two high-severity runtime findings and two medium boundary
findings remain.**

All six findings in `REAUDIT27` have concrete repairs, and the repairs work on
their named paths. Type seeds are immutable values; declaration arguments and
results are admitted before use; null member names no longer leave partial type
state; ordinary error elements remain inside lists; excessive non-empty nesting
is refused before comparison; the cutoff regression no longer runs through a
fault; and collection brokenness is now compositional with linear measured
allocation.

The adversarial boundary sweep found one more unnormalised runtime ingress:
constants still store a host array verbatim. It also found an independent fatal
resource shape in the normaliser: a compact acyclic host DAG is expanded into an
exponential tree. Two smaller cases remain around the new depth and declaration
argument handling.

The separate authoritative-document alignment remains outside this audit. Its
uncommitted files were preserved. The settled hand-aligned formatter output is
not a finding.

## Findings

### 1. Constants are still outside the value-admission boundary

**Severity: high — a supposedly unchanging value remains mutable behind the
graph, can permanently disagree with a cached reader, and is not a runtime list**

`Graph.Constant` checks the incoming object for `Error` and then stores it
verbatim (`Compiler/Runtime/Graph.cs:805-820`). It is the remaining value ingress
with no `List.Of` call.

The focused probe supplied `new object[] { 1d }`. `Read("xs")` returned that
same `object[]`, not `Ronin.Runtime.List`; `@` therefore rejected it with
`error(«@» indexes a list)`. The caller can mutate the retained array and change
the value of a constant after declaration.

This is especially damaging for constants because reads intentionally create no
dependency edge. A derived cell can cache element `1`, the caller can mutate the
constant's array to `99`, and the derived cell has no write or clock advancement
that could ever wake it. The direct and cached readings then disagree forever.

The refusal path is missed too. A self-cyclic array passed to `Constant` is not
normalised into an `Error`, so the initialisation-failure check does not see it;
the cyclic mutable array is installed as a constant without throwing.

**Recommendation:** admit the value before checking whether a constant
initialiser failed, then store only the admitted value. Regress ordinary and
nested array mutation, indexing, cycle/depth refusal, and an ordinary error
element. More importantly, replace the manually repeated `List.Of` calls with a
named admission abstraction used by every value-bearing API. `Var`, two writes,
body results, evaluator groups, type seeds, declaration input/output, and now
constants have been found by successive sweeps; the repeated local call is the
DRY failure that allows each next door to be missed.

### 2. Shared acyclic host arrays expand exponentially during freezing

**Severity: high — a value with a few dozen input arrays can exhaust memory far
below the nesting limit**

`List.Normalise` tracks only the arrays on the current recursion path
(`Compiler/Runtime/List.cs:117-149`). That is sufficient to detect a cycle. Once
a child completes, it is removed from `inside`, so a second reference to the
same acyclic child normalises and allocates the entire subtree again.

A compact host DAG can be built with one new array per level:

```csharp
object value = new object[] { 1d };
for (...) value = new object[] { value, value };
```

The input has O(depth) arrays. Normalisation produces two copies of the prior
result at every level, or O(2^depth) runtime lists. Measured allocation, excluding
construction of the host DAG, was:

```text
depth 8:    35,504 bytes
depth 12:  557,744 bytes
```

Four more input arrays cost 15.7 times as much, the expected exponential curve.
Continuing that shape reaches multi-gigabyte allocation around only two dozen
levels, while `List.Deep` permits 256. This can end the process through ordinary
memory exhaustion even though the value is acyclic and comfortably within the
depth guard.

**Recommendation:** keep the recursion-path set for cycle detection and add a
completed-array-to-immutable-list memo for sharing. Because lists are immutable
values and identity is not their equality, reusing the completed child is
semantically invisible and removes the expansion. Then ensure `Builtin.Same`
also memoises already-compared `(left, right)` list pairs (or has an equivalent
structural digest); otherwise preserving the DAG merely moves the exponential
walk from admission to comparison when two independently admitted DAGs are
compared. Add an allocation/work-count regression using a shared acyclic child,
distinct from the cycle tests.

### 3. An empty leaf bypasses the depth check by one level

**Severity: medium correctness — the runtime admits a list deeper than the
limit that is supposed to define which values it accepts**

`Normalise` returns `List.Empty` before checking `depth >= Deep`
(`Compiler/Runtime/List.cs:122-124`). Wrapping an already-created depth-256
`List` is correctly refused, and a raw nest ending in a scalar is correctly
refused. A raw nest ending in `[]` takes the earlier return instead.

The probe started with `Array.Empty<object>()` and wrapped it in exactly
`List.Deep` single-element arrays. The result should be a depth-257 refusal; it
was an admitted `List` whose `Depth` is 257.

**Recommendation:** apply the depth decision before the empty-array fast path,
or compute the candidate intrinsic depth uniformly for empty and non-empty
arrays. Regress the limit and limit-plus-one with scalar, empty-list, existing-
`List`, and mixed-branch leaves.

### 4. A refused grouped argument is reclassified as an arity error

**Severity: medium diagnostic correctness at the runtime API boundary — the
actual cycle/depth failure is hidden and the message recommends the wrong repair**

`Scope.Invoke` correctly normalises each raw argument, but for a parameter block
of two or more it asks whether the result is an `IReadOnlyList<object>` before
asking whether it is an `Error` (`Compiler/Runtime/Scope.cs:136-163`). A cycle or
depth refusal is therefore treated as a scalar.

The probe passed a self-cyclic array to a two-parameter block. Expected was the
normaliser's `a list cannot contain itself` error. Actual was:

```text
«use (_)» binds 2 parameters here and was given a single argument
```

The body does not run, but the original failure is lost.

**Recommendation:** propagate an admitted `Error` before destructuring or arity
classification. Do that per argument so an earlier failure cannot be hidden by
a later argument's shape error. Regress cyclic and too-deep grouped arguments as
well as ordinary correctly-sized and incorrectly-sized groups.

## Status of `REAUDIT27`

| prior item | result |
|---|---|
| 1. type seeds and declaration values bypass normalisation | **Fixed on both named paths.** Type seeds are admitted before publication; declaration input and output are admitted at the call boundary. Constants are a third missed ingress; finding 1. |
| 2. accepted deep lists compare unequal / normalisation can overflow | **Mostly fixed.** Depth travels with admitted lists, over-deep non-empty input is refused, wrapping cannot reset the count, and comparison is honest for maintained admitted values. Empty leaves bypass by one; finding 3. Shared acyclic input has a separate exponential exhaustion path; finding 2. |
| 3. ordinary errors are mistaken for cycle sentinels | **Fixed.** The internal refusal type is distinct; an error remains an element and an error beside a cycle no longer hides the cycle. |
| 4. diagnostic preservation is super-linear | **Fixed.** Completed collections cache their subtree answer, both valid and erroneous nests have linear allocation regressions, and maintained nested diagnostics remain correct. |
| 5. ownership insertion can leave partial type state | **Fixed.** The ownership table is built during preflight; a null member throws with `Declared` unchanged and a later valid declaration succeeds. |
| 6. cutoff regression evaluates a stable fault | **Fixed.** It reads through `IReadOnlyList<object>`, asserts the element, and retains the evaluation-count assertion. |

## What was rechecked without another finding

- Failed-body instance rollback remains eager and transactional.
- Equal ordinary lists cut off recompute and do not fire `changes` reactions.
- Type seeds freeze top-level and nested arrays; immutable seed sharing across
  instances is safe; cyclic seeds become errors.
- Declaration results are lists before an enclosing `@` sees them, and direct
  declaration arguments are copied before the body observes them.
- Independently constructed admitted lists near the depth limit compare equal;
  excessive non-empty input and wrapping an at-limit list are refused.
- Ordinary errors stay list elements; a cycle beside an earlier error is still
  detected.
- Collection classification retains direct, destination, and origin errors.
  Valid and malformed nested collections now measure linear allocation.
- Valid instance ownership remains O(1), and invalid ownership input leaves no
  partial type state.

## Verification

Temporary probes were removed before the maintained gates.

```text
dotnet restore --locked-mode
  passed

dotnet build --no-restore --configuration Release -warnaserror
  passed — 0 warnings, 0 errors

dotnet test --no-build --configuration Release
  /p:CollectCoverage=true
  /p:CoverletOutputFormat=cobertura
  /p:Threshold=100
  /p:ThresholdType=line%2Cbranch
  /p:ThresholdStat=total
  passed — 987 tests, 100% line, 100% branch, 100% method
```

