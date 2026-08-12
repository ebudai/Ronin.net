# Re-audit 29 — `REAUDIT28` incorporation

**Audited:** `6236555` through `0b96533`

**Date:** 2026-08-03

## Result

**No sign-off. One high-severity graph-integrity finding and one medium hot-path
pessimization remain.**

All four `REAUDIT28` findings are fixed on their named paths. Constants now
cross admission before their failure check; acyclic host DAGs retain sharing;
equality proves a shared pair once and still detects unequal branches; empty
leaves obey the depth limit; and refused grouped arguments preserve their real
failure. The new admission census is a worthwhile structural improvement over
the prior hand-kept set of calls.

The census is not yet the full boundary it claims to be. It examines value-
bearing parameters on `Graph` and `Scope`, but those APIs also return live
`Node` objects whose public setters mutate graph storage without admission,
clock advancement, or dirty propagation. Separately, the newly universal
`Admit` call eagerly allocates two collections before discovering that almost
every runtime value is not an array.

The separate authoritative-document alignment remains outside this audit. Its
uncommitted files were preserved. The settled hand-aligned formatter output is
not a finding.

## Findings

### 1. Returned `Node` objects bypass both admission and graph mutation semantics

**Severity: high — a caller can install the removed array representation or
change a source without waking its dependents, producing permanently
contradictory reads**

`Node.Value` has a public setter (`Compiler/Runtime/Graph.cs:49-68`). Live nodes
are returned by `Graph.Var`, `Let`, `When`, `Shadow`, and the graph indexer; the
indexer alone is at `Graph.cs:503-515`.

The new census checks public method *parameters* on only `Graph` and `Scope`
(`Test/Unit/Admission.cs:266-299`). It therefore reports every known admission
door closed while omitting the object-valued setter reachable through the
return value of several of those methods.

Two focused probes reproduced both failure modes.

First, direct mutation bypasses the graph clock:

```csharp
var source = graph.Var("x", 1d);
graph.Let("copy", g => g.Read("x"));
graph.Read("copy");                 // 1

source.Value = 2d;
```

Afterward `Read("x")` returned `2`, while `Read("copy")` remained `1`. No write
advanced `Changed`, no dependent was dirtied, and nothing can repair the cached
answer until an unrelated event happens to wake it.

Second, assigning `new object[] { 2d }` through the same setter made
`Read("x")` return the raw `object[]`. The caller's array was installed without
`List.Admit`, restoring both silent mutation and rejection by `@` after the
eight method-parameter doors had been closed.

The exposure is broader than `Value`: `Dirty`, `Evaluating`, `Changed`, and
`Evaluated` also have public setters, while `Dependencies` and `Dependents`
return mutable `HashSet<string>` instances. A caller can therefore corrupt the
graph's scheduling and edge invariants directly as well as its value.

**Recommendation:** do not return the mutable runtime node. Return a read-only
diagnostic view/snapshot containing only the observations tests and tooling
need, and keep `Node` plus all mutation reachable only from `Graph`—nesting the
implementation is one enforceable route in C#. Extend the structural safeguard
from input parameters to writable state reachable from public return types and
properties; at minimum assert that no returned graph view exposes a public
setter or mutable dependency collection. Regress scalar mutation, raw-array
injection, dirty propagation, and dependency-set mutation.

### 2. `Admit` allocates on every scalar and already-admitted value

**Severity: medium hot-path pessimization — every scalar recompute, write, and
declaration crossing now creates garbage for list machinery it never uses**

`List.Admit` eagerly constructs a `HashSet<object>` and a reference-keyed
`Dictionary<object, object>` before `Normalise` checks whether the value is an
array (`Compiler/Runtime/List.cs:90-94,134-138`). Non-arrays are returned
unchanged, but not allocation-free.

That path is the overwhelmingly common one and is deliberately universal now:
scalar `Var` and both writes, every graph-body result, declaration arguments and
results, constants, and other ordinary values all call `Admit`. In particular,
every scalar recompute allocates even when cutoff immediately discovers the
value did not change.

The probe pre-boxed its scalar so the measurement did not count boxing, admitted
one `List` before the measurement, and then made 10,000 calls for each value:

```text
20,000 no-op admissions allocated 2,880,000 bytes
                              = 144 bytes per call
```

At one million ordinary admissions this is roughly 144 MB of avoidable garbage
and corresponding GC work in the reactive hot path.

**Recommendation:** fast-path the public boundary before allocating traversal
state. At depth zero, an admitted `List` and every non-`object[]` value can be
returned directly; only a raw array needs the path set and completed-node map.
Alternatively allocate those structures lazily on the first array. Add an
allocation-count regression asserting zero for a pre-boxed scalar, text,
`Error`, and an existing runtime `List`, while retaining the linear-allocation
DAG regression for raw arrays.

## Status of `REAUDIT28`

| prior item | result |
|---|---|
| 1. constants bypass value admission | **Fixed.** Constants are admitted before the top-level failure check; arrays are immutable/indexable, cycles stop initialisation, and ordinary error elements remain values. The broader admission claim is still defeated by mutable returned nodes; finding 1. |
| 2. shared acyclic arrays expand exponentially | **Fixed.** Completed arrays are memoised by identity and retained as shared immutable lists. Equal DAG comparison is linear in distinct pairs, and an adversarial unequal unshared branch still returns false. |
| 3. empty leaves bypass depth by one | **Fixed.** The depth check precedes the empty singleton, with scalar, empty, and existing-list boundary regressions. |
| 4. refused grouped argument becomes an arity error | **Fixed.** Each admitted argument propagates an `Error` before shape classification; valid and wrong-size arity diagnostics remain intact. |

## What was rechecked without another finding

- Constants freeze top-level and nested arrays, support `@`, reject cycles at
  initialisation, and retain ordinary error elements.
- Shared acyclic input allocates linearly and preserves immutable sharing.
- Pair-memoised equality is linear for equal shared DAGs and does not hide a
  difference where one side shares and the other has a distinct unequal branch.
- Scalar, empty-list, and already-admitted-list leaves agree at the depth
  boundary; limit-plus-one values are refused.
- Cyclic and too-deep grouped arguments preserve their refusal, while admitted
  scalar/wrong-count groups keep the existing arity diagnostics.
- Type-seed, declaration, cutoff, rollback, ownership, and compositional
  diagnostic repairs from the preceding rounds remain green.

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
  passed — 1003 tests, 100% line, 100% branch, 100% method
```

