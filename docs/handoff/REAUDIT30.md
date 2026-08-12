# Re-audit 30 — `REAUDIT29` incorporation

**Audited:** `4a2e564` through `13fdec1`

**Date:** 2026-08-03

## Result

**No sign-off. One high-severity graph-integrity defect and two medium-severity
return-surface defects remain.**

The scalar admission repair is complete: `List.Admit` now exits before creating
traversal state for every non-array value, and the maintained test measures
exactly zero allocation for a pre-existing scalar, text, error, and runtime
list. The returned-node repair is also real as far as the node itself goes:
`Node` is private and nested, `Var`, `Let`, and `When` return nothing, and
`Shadow` returns only the injected name. Values, clocks, dirty state, and the
raw-array admission bypass from `REAUDIT29` are no longer reachable.

The replacement dependency accessor nevertheless returns the private node's
live `HashSet`. A focused probe rewrote that set and made a derived value remain
permanently stale after its source changed. The new reflection test misses this
because it checks the *declared* `IReadOnlyCollection` type, not the mutable
runtime object behind it. The same shape exists in the other graph observations,
the compiler's diagnostics, and the fixed language operator registry.

The separate authoritative-document alignment remains outside this audit. Its
uncommitted files were preserved. The settled hand-aligned formatter output is
not a finding.

## Findings

### 1. `Dependencies` is a writable view of the graph's live edge set

**Severity: high — a read-only observation can permanently suppress a required
recomputation or introduce a fatal missing-node lookup**

`Graph.Dependencies` declares `IReadOnlyCollection<string>` but returns
`nodes[name].Dependencies` directly (`Compiler/Runtime/Graph.cs:469-480`). The
runtime object is the node's mutable `HashSet<string>`
(`Graph.cs:2005-2011`), so the interface is only concealment: a caller can cast
it back and modify graph-owned state.

The focused probe established this graph:

```csharp
graph.Var("source", 1d);
graph.Var("stable", 0d);
graph.Let("copy", g => g.Read("source"));
graph.Read("copy");

var exposed = (HashSet<string>)graph.Dependencies("copy");
exposed.Clear();
exposed.Add("stable");

graph.Write("source", 2d);
graph.Step();
```

`source` became `2`, but `copy` remained `1`. The original reverse edge still
made `copy` dirty; then `Settled` consulted the forged dependency, saw that
`stable` had not changed, cleared the dirty bit, and retained the stale cache
(`Graph.cs:1448-1464,1537-1552`). Nothing subsequently repairs it.

Adding a name that is not a node is worse: `Settled` calls `Read`, which returns
an error for an unknown name, and then indexes `nodes[name]` outside the body
exception boundary. That produces an unhandled `KeyNotFoundException` rather
than a runtime value.

This is the exact dependency-set mutation that `REAUDIT29` asked the new
boundary regression to cover. The test instead rejects a return type only when
its *declared generic definition* is `HashSet`, `List`, or `Dictionary`
(`Test/Unit/Admission.cs:370-392`). `IReadOnlyCollection<string>` therefore
passes without examining the object returned by the method.

**Recommendation:** return graph-independent immutable storage: for example a
frozen set or a compiler-generated/read-only snapshot of the dependency names.
Add the stale-cache reproduction above. Also regress clearing, replacing with a
different valid dependency, and inserting an unknown dependency. The test must
exercise the returned object; inspecting only its declared interface cannot
prove that writes do not reach graph storage.

### 2. Read-only interfaces still expose mutable graph and diagnostic lists

**Severity: medium — callers can erase or fabricate the runtime's account of
firings, faults, recomputations, and compile findings**

Three graph properties return private mutable lists directly:

- `Fired => fired` (`Compiler/Runtime/Graph.cs:493-494`);
- `Faults => faults` (`Graph.cs:738-744`); and
- `Trace => trace` (`Graph.cs:746-749`).

Their declared type is `IReadOnlyList<T>`, but their runtime type is
`List<T>`. Downcasting allows `Clear`, `Add`, removal, and replacement. This can
erase effect failures, falsify tracing, and change the culprit list used by the
runaway-cascade diagnostic (`Graph.cs:1386-1405`). It also directly contradicts
the maintained test's name, “nothing the graph hands back can be written
through.”

The same defect survives in the diagnostics layer:

- `Compilation.Findings` returns its private `List<Finding>` directly
  (`Compiler/Compilation.cs:60,597`); and
- `Finding.Related` returns its private `List<Labelled>` directly
  (`Compiler/Diagnostics/Finding.cs:127-138`).

A focused probe compiled malformed input, downcast `Findings`, cleared it, and
made the same `Compilation` report no findings. `Program` uses that count to
choose success or failure (`Compiler/Program.cs:94-103`), so the collection is
not merely cosmetic output. Related labels can likewise be removed or reordered
before rendering.

These are not source-spellable attacks today; they are integrity failures at
internal host/tooling boundaries. They matter here because the newly added
structural test is intended to make this whole class impossible, yet every one
passes it.

**Recommendation:** expose immutable snapshots or cached read-only wrappers,
not a mutable collection typed as a read-only interface. Add write-through
regressions for every observation and for both diagnostic collections. A useful
general guard is to reject a returned `ICollection<T>` whose `IsReadOnly` is
false and then attempt a representative mutation, while retaining explicit
semantic tests that the graph or diagnostic owner is unchanged.

### 3. The one fixed operator registry is globally writable through its view

**Severity: medium — any internal consumer can redefine the language for all
subsequent resolvers, defeating the single-source-of-truth invariant**

`Builtin.Operators` is documented as the language's one operator table and is
declared `IReadOnlyDictionary<string, Operator>`, but it is constructed as and
returned as a mutable `Dictionary` (`Compiler/Runtime/Values.cs:115-125`). Every
new `SymbolTable` copies whatever happens to be in that live dictionary
(`Compiler/Resolution/Resolver.cs:1150-1171`).

The probe downcast the property, removed `+`, and constructed a new
`SymbolTable`; `+` was absent. The entry was restored in `finally`, but without
that cleanup the mutation is process-wide. Replacing an entry can change both
binding power and implementation for later compilations, while the statically
derived operator-word diagnostics may already have captured the old key set.

This is distinct from `SymbolTable.Operators`, whose mutability is deliberate
for a scope and its resolver tests. The comments explicitly say a scope must
not add to the language definition; the global table currently permits exactly
that by downcast.

**Recommendation:** store the language definition in a `FrozenDictionary` (or
an equivalent truly immutable dictionary) and keep the mutable copy only on
`SymbolTable`. Regress removal and replacement through the published property,
then confirm separately that per-symbol-table extension remains possible and
does not affect a subsequently constructed table.

## Status of `REAUDIT29`

| prior item | result |
|---|---|
| 1. returned nodes bypass admission and graph mutation semantics | **Partially fixed.** The node, its value, clocks, and dirty state are private now. The replacement dependency observation still returns the node's writable edge set and reproduces the stale-cache failure; finding 1. |
| 2. `Admit` allocates for scalars and admitted values | **Fixed.** The type fast path precedes both traversal allocations, and exact-zero allocation regressions cover all named non-array cases. |

## What was rechecked without another finding

- `Var`, `Let`, and `When` no longer return nodes; all maintained call sites
  compile against their `void` surface.
- `Shadow` returns only the generated name, and its maintained tests obtain the
  value through `Read`.
- `Node` is nested and private, so its setters and reverse-edge collection are
  no longer nameable outside `Graph`.
- Scalar, text, `Error`, and existing runtime-list admissions perform no
  traversal allocation. Raw arrays still take the cycle/depth/DAG path.
- The declaration block copies use the compiler's read-only array wrapper. A
  focused mutation probe confirmed that this superficially similar
  `IReadOnlyList` does not expose writable storage.
- The previous constant, DAG-sharing, pair-equality, depth, grouped-argument,
  compositional-diagnostic, rollback, ownership, and cutoff regressions remain
  green.

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
  passed — 1008 tests, 100% line, 100% branch, 100% method
```
