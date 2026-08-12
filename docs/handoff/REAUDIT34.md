# Re-audit 34 — `REAUDIT33` incorporation

**Audited:** `cbf826c`

**Date:** 2026-08-04

## Result

**No sign-off. The concrete `List`, `Collection`, and array aliases from
`REAUDIT33` are fixed, and throwing probes now fail, but one medium ownership
defect and one low safeguard defect remain.**

`Best.Witness` and both paths into `Declared.Words` now share one capability-
based helper. Ordinary writable lists, collections, arrays, and opaque
`IReadOnlyList` implementations are copied; already-read-only objects are kept
to preserve the resolver's allocation ceiling. The maintained tests cover
those branches and mutation of directly writable inputs. Explicit probes are
called directly, while reflection failures are collected and asserted empty.

The ownership decision still confuses “this view refuses writes” with “nobody
can write the storage.” `ReadOnlyCollection<T>` and `ArraySegment<T>` both
report `ICollection<T>.IsReadOnly == true` while retaining caller-owned backing
storage. Focused probes mutated those backing objects and changed both compiler
objects after construction.

The owner-authorized XML documentation suppressions remain deferred and are not
findings in this round. The separate authoritative-document alignment also
remains outside this audit. Existing uncommitted files were preserved, and the
settled hand-aligned formatter output is not a finding.

## Findings

### 1. `IsReadOnly` does not establish ownership of a read-only view's backing storage

**Severity: medium — caller mutation still changes `Best.Witness` and
`Declared.Words` after both objects accepted and retained a read-only view**

`Owned.Copy` keeps any non-array `IReadOnlyList<T>` that implements
`ICollection<T>` with `IsReadOnly == true` (`Compiler/Owned.cs:33-47`). That
property means only that mutation is unavailable *through that interface*. It
does not say whether another reference can mutate the same storage.

Two ordinary framework types demonstrate the distinction:

```csharp
var backing = new List<string> { "one", "two" };
IReadOnlyList<string> supplied = new ReadOnlyCollection<string>(backing);
```

and:

```csharp
var backing = new[] { "one", "two" };
IReadOnlyList<string> supplied = new ArraySegment<string>(backing);
```

Both satisfy the helper's “keep” branch. The focused probes constructed a
`Best` and a `Declared` from each view, mutated the separately retained backing
list or array, and expected the compiler objects to remain at `"one"`. Both
instead changed to `"changed"`.

The maintained `"wrapped"` case uses
`new ReadOnlyCollection<string>(["one", "two"])`
(`Test/Unit/Admission.cs:595-637`). Its backing collection is created inline and
no reference is retained, so the test proves only that writes through the view
are unavailable. It then asserts the wrapper was deliberately retained. It
does not exercise the ownership claim the helper is responsible for.

This defeats the comments on both consumers. `Declared.Words` says its sequence
must not change because the caller kept a reference
(`Compiler/Diagnostics/Rules.cs:46-55`), while `Best.Witness` describes its
protection as unconditional (`Compiler/Resolution/Resolver.cs:688-702`). As in
the previous round, production pipeline callers currently provide compiler-
owned wrappers, so no source-spellable mutation was reproduced; the accepted
internal input contract remains broader than the protection.

**Recommendation:** distinguish compiler-owned immutable storage from arbitrary
read-only views. `IsReadOnly` cannot prove that distinction. One structural
route is an internal owned-list type/marker produced by the compiler's
collection builders: `Owned.Copy` may retain only that trusted representation
and must copy every external `IReadOnlyList`, including `ReadOnlyCollection` and
`ArraySegment`. Another is an immutable collection type whose backing cannot be
aliased. Preserve the resolver ceiling by ensuring its witness producers create
the trusted representation once, rather than heuristically trusting any
read-only interface.

Regress a read-only wrapper with a separately retained mutable list, an
`ArraySegment` with a retained array, direct writable collections, an opaque
list, and the trusted compiler-owned representation. Every external backing
mutation must leave the stored values unchanged; only the trusted owned value
should retain reference identity.

### 2. A null opener is still recorded as successfully exercised

**Severity: low — an explicit probe can stop producing its promised object and
the structural test remains green**

The main opener loop now correctly calls each probe directly, so exceptions
fail. It still passes a null result through `Deeply`, regards it as non-writable,
and records the name in `opened` (`Test/Unit/Admission.cs:544-560`).

The dedicated regression intended to prevent a stopped probe is:

```csharp
Assert.NotNull($"{member}{probe()}");
```

(`Admission.cs:653-665`). String interpolation produces at least `member` even
when `probe()` returns null, so the assertion can never detect a null result.
This is not hypothetical setup shape: the `Identifier.TryPattern` opener calls
`Blocks()`, which explicitly returns null when the chosen identifier no longer
parses as a pattern (`Admission.cs:786-787`).

No current opener returned null during this audit. The throwing-probe finding
from `REAUDIT33` is fixed; this is the remaining non-throwing false-negative
path.

**Recommendation:** capture each probe's raw result, assert that result itself
is non-null, then inspect and record it. The separate interpolated assertion is
unnecessary once the main opener loop owns all three steps. If a promise may
legitimately return null, its probe should be a named special case that also
exercises a non-null result, rather than allowing null to count as the promised
collection.

## Status of `REAUDIT33`

| prior item | result |
|---|---|
| 1. `Best` and `Declared` retain writable constructor/init inputs | **Partially fixed.** Direct lists, `Collection<T>`, arrays, and opaque inputs are copied through one rule. Read-only views over caller-owned mutable backing storage are retained and remain aliased; finding 1. |
| 2. throwing probes are counted as opened | **Fixed for exceptions.** Explicit probes now throw through, and reflective failures are asserted. A null return is still counted as opened; finding 2. |

## What was rechecked without another finding

- Direct `List<T>`, `Collection<T>`, and array inputs are copied, and later
  mutation of those inputs does not change `Best` or `Declared`.
- Opaque `IReadOnlyList` implementations are conservatively copied.
- Positional deconstruction of `Best` and `Resolution` continues to return the
  protected properties.
- Every current maintained explicit opener completes without throwing, and
  parameterless reflection failures are reported rather than swallowed.
- The named parameterized, nested, and `out` results from `REAUDIT32` remain
  protected.
- The resolver allocation ceiling, authorized documentation-warning policy,
  and all prior runtime/diagnostic regressions remain green.

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
  passed — 1021 tests, 100% line, 100% branch, 100% method
```
