# Re-audit 31 — `REAUDIT30` incorporation

**Audited:** `4cf2801`

**Date:** 2026-08-04

## Result

**No sign-off. The three `REAUDIT30` defects are fixed on their named surfaces,
but one medium boundary defect and two low-severity maintenance/pessimization
findings remain.**

The graph now snapshots dependencies, exposes cached read-only wrappers for its
live firing/fault/trace accounts, and retains no write-through route to node
state. Compilation findings and related labels reject mutation. The fixed
operator definition is now a `FrozenDictionary`, while each `SymbolTable`
still receives its deliberately mutable local copy.

The new object-level test is a real improvement over the declared-type test it
replaces. It is not yet the census its comments claim: its six owner types are
written by hand. `Injection`, omitted from that list, exposes the backing array
of another process-wide language definition. Mutating it separates the word
used by reservation and diagnostics from the prefix actually injected. The
discovery result is omitted too and returns two ordinary `List` instances.

Separately, `Finding.Related` creates a new read-only wrapper on every read, and
the operator repair introduced malformed XML documentation. Neither is a
runtime correctness defect.

The separate authoritative-document alignment remains outside this audit. Its
uncommitted files were preserved. The settled hand-aligned formatter output is
not a finding.

## Findings

### 1. The hand-selected return census misses a writable global injection descriptor

**Severity: medium — an internal caller can split one compiler-generated name
definition into contradictory words and prefixes for the rest of the process**

`Injection` receives its words as a `params string[]`, stores that array
directly, and publishes it as `IReadOnlyList<string>`
(`Compiler/Diagnostics/Injection.cs:28-41`). The runtime object remains a
`string[]`, so a cast recovers element assignment.

This is not merely a mutable presentation. Different consumers deliberately
read different derived parts of the descriptor:

- `SymbolTable.Old` reads `Injection.Shadow.Words[0]` dynamically;
- `Injection.Of(IReadOnlyList<string>)` reads `Words` dynamically; while
- `SymbolTable.Shadowed`, `Injection.Of(string)`, and `Shape` use `Prefix`,
  which was computed once in the constructor.

The focused probe cast `Injection.Shadow.Words`, changed `"old"` to `"prior"`,
and restored it in `finally`. During the mutation, `SymbolTable.Old` and the
canonical-word overload said `prior`, while the generated runtime name and
prefix still said `old`. This reintroduces exactly the independent-definition
failure the comments at `Compiler/Resolution/Resolver.cs:1285-1299` say the
descriptor eliminated: reservation/diagnostic analysis and actual injected
names can disagree.

The new census cannot discover this. It reflects only `Graph`, `Scope`,
`Compilation`, `Finding`, `Builtin`, and `SymbolTable`
(`Test/Unit/Admission.cs:451-464`), then compares those owners against a
second hand-written array of live objects. Adding `Injection.Words` changes
neither list, so the claimed structural safeguard stays green.

There is a lower-impact existing example of the same omission. `Sources.Under`
builds mutable `List<FileInfo>` and `List<string>` objects and places them
directly in `Discovered.Files` and `Unreadable`, both declared read-only
(`Compiler/Sources.cs:10-13,39-49`). The probe confirmed that both returned
objects report `ICollection<T>.IsReadOnly == false`. This does not corrupt the
walker after it returns, but it shows that the test is not a project-wide return
boundary.

**Recommendation:** copy/freeze `Injection.Words` at construction and return
read-only discovery collections. More importantly, derive the promised-member
set from every applicable type in the compiler assembly instead of a selected
owner array. Where obtaining a live instance cannot be automatic, make the
reflection result the required manifest and require a factory/probe for each
entry; a newly introduced promise must then fail until it receives an explicit
immutability decision. Regress the `Words`/`Prefix` agreement after an attempted
mutation, not only the cast type.

### 2. Every read of `Finding.Related` allocates another wrapper

**Severity: low — a repaired observation adds avoidable garbage per diagnostic
access**

`Finding.Related` is implemented as:

```csharp
public IReadOnlyList<Labelled> Related => related.AsReadOnly();
```

(`Compiler/Diagnostics/Finding.cs:127-142`). `AsReadOnly` creates a new
`ReadOnlyCollection<Labelled>` each time. It protects the backing list, but the
wrapper itself does not need to change: a single wrapper remains a live view as
`Alongside` adds labels.

After warming the property, a focused allocation probe read it 10,000 times and
measured exactly 240,000 bytes, or 24 bytes per read. That is small next to
parsing and normally happens only once during rendering, so this is not a hot
path issue. It is nevertheless unnecessary and differs from `Graph` and
`Compilation`, which cache their wrappers once.

**Recommendation:** construct one read-only view per `Finding` and return it
from the property. Add an exact-zero repeated-read allocation regression, as
used for scalar admission, while retaining the mutation rejection test.

### 3. XML documentation contains malformed element nesting

**Severity: low — documentation generation emits structural warnings, including
one introduced by this repair**

The new frozen-registry paragraph begins after the existing `</remarks>` and is
followed by a second `</remarks>` (`Compiler/Runtime/Values.cs:116-132`). A
documentation build reports CS1570: “End tag was not expected at this
location.” The paragraph should be inside the original remarks element.

The same optional check exposed an older malformed block at
`Compiler/Diagnostics/Rules.cs:175-195`: the paragraph opened at line 185 is
not closed before another `<para>` begins at line 190. That produces the other
two CS1570 warnings.

The maintained build does not generate an XML documentation file, so neither
warning appears in the ordinary warning-as-error gate and neither affects the
compiler binary.

**Recommendation:** repair the two element nests. If code XML documentation is
intended to remain machine-readable, add a narrow documentation validation gate;
the current optional build also reports unrelated missing-parameter/cref
warnings, so enabling every documentation warning as an error would require a
separate cleanup decision.

## Status of `REAUDIT30`

| prior item | result |
|---|---|
| 1. `Dependencies` exposes the graph's live edge set | **Fixed.** It returns an independent read-only snapshot. Cast, clear, add, stale-cache, and subsequent dependency observations are regressed. |
| 2. graph and diagnostic lists expose mutable backing storage | **Fixed for the named objects.** Graph wrappers are cached and live; compilation and related-label views reject mutation. `Related` has the allocation issue in finding 2. |
| 3. fixed operator registry is globally writable | **Fixed.** The global table is frozen, mutation is rejected, and local symbol-table extension remains isolated. |

## What was rechecked without another finding

- A retained dependency snapshot cannot be cast to `HashSet`, and mutations
  through `ICollection<string>` throw without altering the graph.
- Changing a source after observing dependencies recomputes the dependent and
  refreshes the next snapshot correctly.
- `Fired`, `Faults`, and `Trace` remain live observations as the graph changes,
  but no caller can write through their cached wrappers.
- Compilation findings and related labels remain visible as they are built and
  cannot be cleared or reordered through their published views.
- The frozen operator table rejects both concrete downcast and dictionary
  removal. Extending one `SymbolTable` does not affect a later one.
- The prior admission, DAG/equality, cutoff, diagnostic, rollback, ownership,
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
  passed — 1013 tests, 100% line, 100% branch, 100% method
```

An additional `GenerateDocumentationFile=true` build succeeded but reported 33
documentation warnings. Three were CS1570 malformed-XML warnings from the two
locations in finding 3; the others were pre-existing missing parameter or
unresolved-reference documentation warnings and were not elevated into this
re-audit's scope.
