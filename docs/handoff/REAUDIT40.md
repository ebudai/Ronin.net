# Re-audit 40 — `REAUDIT39` incorporation

**Audited:** `a415a8e`

**Date:** 2026-08-04

## Result

**The `REAUDIT39` production finding is fixed. `Best.Readings` and `Best.Pair`
both construct only the final private array and allocate 64 bytes per warmed
call in the focused probes. Ownership, ordering, and ambiguity behavior remain
correct.**

The new element-wise and mapped factories are the right structural APIs:
neither accepts caller-created destination storage, and both create the final
array inside `Kept<T>`. The removed public sequence overload had no remaining
production caller; the nested sequence factory remains correctly used by
`Copy`. No current correctness or performance defect was found.

The allocation regression test still has one low shared-oracle gap. Each
producer is compared with the same factory it invokes, so slowing the factory
internally moves the measured producer and its supposed baseline together.
Both former intermediate shapes were restored inside the factories and the
maintained test remained green. This is not an unconditional sign-off on the
safeguard.

The owner-authorized warning suppressions, authoritative-document work, and
settled hand-aligned formatter output remain outside this round. Existing
uncommitted documentation and handoff files were preserved.

## Finding

### 1. Each allocation oracle calls the implementation it is intended to protect

**Severity: low — production is optimal, but both factory-internal allocation
regressions move the baseline with it and pass the “cheapest shape” test**

The new test measures `Best.Readings` against:

```csharp
Owned.Of(order, static node => node.ToString())
```

and `Best.Pair` against:

```csharp
Owned.Of(witness[0], witness[1])
```

(`Test/Unit/ResolverCost.cs:228-275`). Those are not independent baselines.
They are the exact factories the production methods call
(`Compiler/Resolution/Resolver.cs:745-769`). If a factory regresses internally,
both sides execute the regressed code and retain the same ratio.

This was reproduced for both new factories.

#### Mapped factory

Replacing the direct array fill at `Compiler/Owned.cs:106-113` with:

```csharp
internal static Kept<T> Of<TSource>(
    IReadOnlyList<TSource> values,
    Func<TSource, T> select)
    => new([.. values.Select(select)]);
```

restores the `Select` iterator and raises the factory from 64 to 112 bytes per
call. `Best.Readings` and the `mapped` baseline both call it, so
`AndEachProducerBuildsItsValueOnceInTheCheapestShapeThatOwnsIt` still passes.

#### Two-element factory

Replacing the direct two-element construction at `Owned.cs:104` with:

```csharp
internal static Kept<T> Of(T first, T second)
    => Of((IEnumerable<T>)[first, second]);
```

restores the intermediate collection and raises the factory from 64 to 128
bytes per call. `Best.Pair` and the `elements` baseline both call it, so the same
maintained test again passes.

In each assertion the producer allocation is effectively the baseline plus a
non-allocating forwarding call. The condition

```text
producer × 2 < baseline × 3
```

therefore stays true whether the shared factory costs 64, 112, or 128 bytes.
The test protects that each producer delegates to its factory; it does not
protect the factory shape that actually delivered the `REAUDIT39` saving.

**Recommendation:** measure the two factories themselves against an independent
oracle. The simplest guard is a warmed per-call ceiling with a wide gap: both
currently cost 64 bytes, while the known regressions cost 112 and 128. A ceiling
near the midpoint retains substantial runtime margin and fails both mutations.
If a relative test is preferred, use a test-only minimal owner with the same
array-and-wrapper shape rather than calling the production factory on the
baseline side. Keep the current producer-to-factory ratios as delegation guards
if desired, but they cannot serve as factory-allocation guards.

## Status of `REAUDIT39`

| prior item | result |
|---|---|
| witness producers allocate avoidable intermediate shapes | **Fixed in production.** Element-wise and mapped factories reduce both producers to 64 bytes per call. Their internal allocation shapes remain unguarded because the test baselines call the same factories; finding 1. |

## What was rechecked without another finding

- `Owned.Of(first, second)` creates its final array inside the private
  `Kept<T>` factory; no caller storage can be retained.
- The mapped overload allocates a correctly sized final array, fills every
  element in index order, and uses a noncapturing selector in `Best.Readings`.
- `Best.Readings` receives the actual indexable cell order; no hand-built source
  path remains in production.
- `Best.Pair` still selects exactly the first two readings only when the input
  contains more than two, and preserves an already-owned short witness.
- Both producer-level measurements reproduce 64 bytes per call.
- The former `Readings` `Select` path, former ordinary-then-copy path, `Pair`
  collection expression, and `Pair.Take(2)` remain slower than production.
- The nominal `Kept<T>` return and store types continue to prevent the original
  cell bypass at compile time.
- The real resolver allocation ceiling remains green.

## Verification

Both factory-internal mutation probes were removed before the maintained gates.

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
  passed — 1024 tests, 100% line, 100% branch, 100% method
```
