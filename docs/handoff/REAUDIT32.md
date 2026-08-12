# Re-audit 32 — `REAUDIT31` incorporation

**Audited:** `ebc997b` through `41f2447`

**Date:** 2026-08-04

## Result

**No sign-off. All three `REAUDIT31` findings are fixed, but one medium-severity
gap remains in the new return-boundary safeguard.**

`Injection.Words` and both discovery collections now copy their inputs and
publish genuine read-only wrappers. `Finding.Related` caches one live wrapper
and repeated access allocates nothing. Both malformed XML nests are repaired;
XML documentation is now generated in the maintained build, and malformed XML
warning CS1570 is promoted to an error.

The authorized suppressions for missing comments, unmatched parameter tags,
unresolved or ambiguous references, and invalid `paramref` names are explicitly
documented in `Compiler.csproj`. They are deferred by the project owner and are
not findings in this round.

The replacement assembly-wide ledger discovers every method whose *return
type* promises a read-only collection. Its object walk, however, invokes only
parameterless members. Of the parameterized entries in the ledger it probes
only two by hand, does not inspect read-only `out` parameters, and checks only
the outer object of a nested collection. Focused probes found five current
paths that still return writable objects behind read-only types.

The separate authoritative-document alignment remains outside this audit. Its
uncommitted files were preserved. The settled hand-aligned formatter output is
not a finding.

## Finding

### 1. Parameterized, nested, and `out` read-only results escape the object census

**Severity: medium — the new structural guarantee is green while several
compiler-built results still permit exactly the write-through it claims to
exclude**

The assembly ledger correctly includes parameterized methods such as
`Best.Pair`, `Cascades.Cycles`, `Pattern.Reads`, and `Triggers.Distinct`
(`Test/Unit/Admission.cs:502-533`). The object walk invokes only zero-parameter
members (`Admission.cs:468-477`). Its entire explicit parameterized coverage is:

```csharp
Assert.False(Writable(graph.Dependencies("copy")));
Assert.False(Writable(Injection.Shadow.Of(["x"])));
```

(`Admission.cs:497-499`). A name in the ledger therefore records that somebody
saw a declaration; it does not establish that the returned object was tested.

Focused probes found these current counterexamples:

- `Triggers.Distinct` builds a `List<string>` and returns it directly as
  `IReadOnlyList<string>` (`Compiler/Resolution/Triggers.cs:51-62`);
- `Pattern.Reads` returns the parser's mutable `List<string>` as
  `IReadOnlyList<string>` (`Compiler/Resolution/Resolver.cs:887-910,930-931`);
- `Best.Pair`, and therefore the short-input path through `Best.Either`, returns
  the caller's list unchanged when it contains at most two readings
  (`Resolver.cs:703-708`);
- `Cascades.Cycles` protects its outer collection, but each ring inside it is
  the mutable `List<string>` returned by `Ring`
  (`Compiler/Runtime/Cascades.cs:90-113,127-157`); and
- `Initialisation.TryOrder` assigns its mutable `List<string>` to an
  `out IReadOnlyList<string>` (`Compiler/Runtime/Initialisation.cs:36-83`).

The first probe reported all three parameterized results writable. The nested
probe found the outer cycle collection read-only and `cycles[0]` writable. The
`TryOrder` probe found its successful `order` writable.

`Initialisation.Cycles` inherits the mutable inner-ring result from
`Cascades.Cycles`. It appears in the method ledger, but nested mutability is
missed because the walk applies `Writable` only to a member's immediate return;
enumerated child collections are queued for traversal but are never themselves
checked as published results. `TryOrder` is absent from the ledger altogether
because its method returns `bool`; `Promises` does not inspect `out` or `ref`
parameter element types.

These probes did not reproduce graph corruption or process-global mutation.
Most objects are newly created results whose only owner is the caller, so the
immediate risk is a false API invariant and mutable diagnostic/analysis output,
not the high-severity hidden-state corruption from `REAUDIT30` and `31`.
Nevertheless, the maintained test explicitly adopts the stronger rule that
nothing the compiler builds is writable behind its read-only type, and all five
paths violate it.

**Recommendation:** connect discovery to execution rather than maintaining a
ledger that can acknowledge an unprobed member. Track which promised members
the object walk actually invokes, require an explicit factory/probe for every
remaining parameterized member, and fail when the union does not equal the
reflection result. Include `out`/`ref` element types in promise discovery.
When a published result is a collection of collections, propagate its
“published” provenance and check every nested collection rather than only the
outer wrapper.

For the current production paths, return immutable/read-only results from
`Distinct`, `Reads`, `Pair`/`Either`, `Ring`, and `TryOrder`. If the stronger
runtime-immutability rule is not intended for caller-owned transient results,
narrow the test and its comments explicitly instead; the current implementation
and stated invariant disagree.

## Status of `REAUDIT31`

| prior item | result |
|---|---|
| 1. selected-owner census misses injection and discovery collections | **Fixed on the named paths.** Injection words and discovery results copy and wrap their inputs. The owner selection was replaced by an assembly-wide declaration ledger and object walk; its remaining coverage gap is finding 1. |
| 2. `Finding.Related` allocates a wrapper per read | **Fixed.** One lazily created live wrapper is cached, and 1,000 warmed reads allocate exactly zero bytes. |
| 3. malformed XML documentation | **Fixed.** Both nests are valid. Documentation generation is enabled and CS1570 is gated as an error. The owner-authorized warning suppressions are deferred, not findings here. |

## What was rechecked without another finding

- Attempted array casts and item assignment cannot mutate `Injection.Words`;
  `Words`, `Prefix`, both `Of` overloads, and `SymbolTable.Old` agree.
- `Discovered.Files` and `Unreadable` do not retain or expose their input lists.
- `Finding.Related` remains a live view of labels added through `Alongside`,
  rejects mutation, and allocates no wrapper after first access.
- `Resolution.Readings` now copies and wraps its positional constructor input;
  resolved and ambiguous results remain deterministic.
- The graph dependency snapshot, event views, diagnostic collections, and
  frozen operator registry remain protected.
- The prior admission, DAG/equality, cutoff, rollback, ownership, diagnostic,
  and deferral regressions remain green.

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
  passed — 1016 tests, 100% line, 100% branch, 100% method
```
