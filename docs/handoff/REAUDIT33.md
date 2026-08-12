# Re-audit 33 — `REAUDIT32` incorporation

**Audited:** `3ff5524`

**Date:** 2026-08-04

## Result

**No sign-off. The five writable results named in `REAUDIT32` are fixed, but
one medium constructor/input-aliasing defect and one low test-safeguard defect
remain.**

`Triggers.Distinct`, `Pattern.Reads`, `Best.Pair`/`Either`, nested cascade
rings, and `Initialisation.TryOrder` now return genuinely read-only objects.
The promise discovery includes read-only `out` parameters, the explicit opener
set is compared with the assembly-wide reflection result, nested published
collections are checked recursively, and the object walk now reaches dictionary
values. Additional adjacent repairs protect `Effects`, `Resolution.Readings`,
`Declared.Words`' fallback, and a chain's counter list.

The remaining production issue is the difference between checking one specimen
and enforcing an ownership rule. `Best.Witness` copies only `List<string>` and
arrays, so another ordinary writable `IReadOnlyList` implementation passes
through. `Declared.Words` protects its fallback but trusts every value assigned
through its `init` accessor. The opener table exercises a `List` for the first
and no init value for the second, so both current writable paths pass the new
guard.

The owner-authorized XML documentation suppressions remain deferred and are not
findings in this round. The separate authoritative-document alignment also
remains outside this audit. Existing uncommitted files were preserved, and the
settled hand-aligned formatter output is not a finding.

## Findings

### 1. Representative probes do not make `Best` and `Declared` immutable for every accepted input

**Severity: medium — two public internal construction paths still retain
writable objects behind read-only properties, and later caller mutation changes
the compiler object's contents**

`Best.Witness` now tries to avoid the resolver allocation regression by copying
only two concrete writable shapes:

```csharp
Witness is List<string> or string[] ? [.. Witness] : Witness
```

(`Compiler/Resolution/Resolver.cs:688-702`). This is not the unconditional
invariant described by the surrounding comment. `Collection<string>` is a
standard writable implementation of `IReadOnlyList<string>` and is neither
listed type.

The focused probe constructed a `Best` with a `Collection<string>`. Its
`Witness` remained the same writable object; `Writable(best.Witness)` returned
true, and changing the supplied collection changed the witness held by the
`Best`. The maintained opener uses `new List<string>`
(`Test/Unit/Admission.cs:622-624`), which exercises the protected branch and
therefore cannot detect the type assumption.

`Declared.Words` has the complementary hole. Its fallback now copies the array
produced by lexing, but its `init` accessor stores the supplied
`IReadOnlyList<string>` directly (`Compiler/Diagnostics/Rules.cs:46-57`). A
probe supplied a `List<string>`, observed a writable `declared.Words`, mutated
the original list, and changed the word sequence later diagnostic rules would
read. The maintained opener constructs `new Declared(...).Words` without an
initializer (`Admission.cs:630`), so it exercises only the fallback.

The source pipeline currently gives `Declared` compiler-generated read-only
canonical lists, and the resolver currently gives `Best` its own protected
witnesses. The probes therefore did not reproduce a source-spellable failure.
The defect is at the internal construction boundary: a future compiler caller,
test helper, or joined analysis can satisfy the declared parameter type and
silently reintroduce mutable diagnostic or ambiguity state.

Positional deconstruction is not a second door. A focused probe confirmed that
both `Best.Deconstruct` and `Resolution.Deconstruct` return their protected
properties rather than their raw positional constructor inputs.

**Recommendation:** decide mutability by capability rather than the two known
concrete types. A shared helper can treat arrays as writable and inspect
`ICollection<T>.IsReadOnly`; copy only when that says the value is writable, so
the resolver's already-read-only production witnesses keep the measured
zero-copy path. Apply the same ownership rule in `Declared.Words.init`.
Regress `List<T>`, `Collection<T>`, arrays, and an already-read-only wrapper,
including mutation of the original input after construction. If only inputs
produced by the pipeline are intentionally supported, narrow the constructors and the
test's “every promise” claim instead of leaving a broader type contract.

### 2. A probe that throws is silently counted as having opened its promise

**Severity: low — the structural test can stop exercising a member and remain
green**

The explicit opener loop executes each probe through `Held` and adds its member
name to `opened` unconditionally (`Test/Unit/Admission.cs:528-535`). `Held`
catches every `Exception` and returns null (`Admission.cs:742-747`). `Deeply`
then treats null as non-writable, and the discovery equality still succeeds.

Consequently, if an opener begins throwing because its setup no longer reaches
the intended branch, the test records it as successfully opened and safe. This
undoes the important distinction the new test otherwise establishes between a
name in a ledger and a promise actually exercised. The reflective walk uses the
same helper and records a parameterless member as opened before invocation, so
getter failures have the same false-negative shape.

No current maintained opener threw during this audit; this is a safeguard
failure rather than a present compiler behavior failure.

**Recommendation:** call explicit probes directly and let any exception fail
the test. For the reflective walk, either let invocation failures fail too or
collect them with member names and assert that the failure set is empty. If a
specific inaccessible or intentionally throwing member must be skipped, make
that a named, narrowly matched exclusion rather than converting all failures to
null.

## Status of `REAUDIT32`

| prior item | result |
|---|---|
| parameterized, nested, and `out` results escape the object census | **Fixed on every named output.** All five reproduced paths now reject mutation. Discovery is joined to explicit execution, includes read-only `out` parameters, and checks nested collections. Representative constructor inputs and swallowed opener failures leave findings 1 and 2. |

## What was rechecked without another finding

- `Triggers.Distinct` and `Pattern.Reads` return read-only wrappers.
- `Best.Pair` and both branches of `Best.Either` protect ordinary list inputs;
  the resolver's 149-lexeme allocation ceiling remains green.
- Both the outer cycle result and every ring inside it are read-only;
  `Initialisation.Cycles` inherits the protected nested result.
- A successful `Initialisation.TryOrder` exposes a read-only order, and its
  `out` promise is included in reflection discovery.
- `Effects.Reads` and `Writes` are frozen copies of their constructor inputs.
- `Resolution.Readings`, call/group parts, declaration blocks, injected words,
  discovery results, graph observations, findings, and operator definitions
  remain protected.
- The authorized documentation-warning policy builds cleanly and was not
  reopened.
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
  passed — 1015 tests, 100% line, 100% branch, 100% method
```
