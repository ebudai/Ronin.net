# Re-audit 41 — `REAUDIT40` incorporation

**Audited:** `1e7e9b5`

**Date:** 2026-08-04

## Result

**Sign-off. The `REAUDIT40` safeguard gap is fixed, all four relevant
allocation mutations fail, and this re-examination found no new issue.**

The existing producer-to-factory comparisons remain as delegation guards. The
new `Barely` oracle independently models the minimum instance shape—one final
array and one wrapper object—without calling any compiler implementation. The
factory-to-oracle comparisons therefore remain fixed when a production factory
slows down, closing the shared-oracle failure from the previous round.

The production factories themselves were unchanged and remain correct:
`Best.Readings` and `Best.Pair` each allocate 64 bytes per warmed call, build
only final private storage, preserve witness order, and retain the nominal
ownership guarantees.

No correctness bug, ownership regression, additional allocation pessimization,
DRY failure, or unhandled scenario was found in the audited incorporation.

The owner-authorized warning suppressions and authoritative-document alignment
remain deferred and are not findings here. The settled hand-aligned formatter
output was not raised again. Existing uncommitted documentation and handoff
files were preserved.

## Findings

None.

## Mutation verification

The maintained test now has two independent layers
(`Test/Unit/ResolverCost.cs:228-326`):

1. producer against factory, protecting delegation;
2. factory against `Barely`, protecting the factory's own allocation shape.

Each mutation was applied separately against the maintained test.

| mutation | expected guard | result |
|---|---|---|
| `Best.Readings` builds an ordinary collection and calls `Owned.Copy` | producer → mapped factory | **failed as intended** at the producer assertion |
| `Best.Pair` builds an ordinary two-element collection and calls `Owned.Copy` | producer → element factory | **failed as intended** at the producer assertion |
| mapped factory restores `values.Select(select)` | mapped factory → `Barely.Mapping` | **failed as intended**; oracle remained 64, factory rose to 112 bytes per call |
| two-element factory restores an intermediate enumerable | element factory → `Barely.Two` | **failed as intended**; oracle remained 64, factory rose to 128 bytes per call |

The two factory mutations are the exact shared-oracle cases that passed
`REAUDIT40`. Both now fail without relying on a resolver run, a production
counter, or a baseline that reaches `Owned`.

## Safeguard review

- `Work` creates the delegate and closure before measurement, warms each path
  for 200 calls, then measures 1,000 synchronous calls with
  `GC.GetAllocatedBytesForCurrentThread`.
- `Barely.Mapping` duplicates only the irreducible mechanics: allocate a final
  array, fill it in index order, and store it in one object. It does not invoke
  `Owned`, `Kept`, `Best`, or the resolver.
- `Barely.Two` likewise constructs one final two-element array and one wrapper.
- `Barely` retains its array in a field, so the oracle has the same per-instance
  reference-field shape rather than allowing the input to become dead setup.
- The 3:2 ratio leaves margin above the current 64-byte floor while remaining
  below the known 112- and 128-byte regressions.
- Test failure messages identify whether delegation, mapped-factory shape, or
  element-factory shape diverged.
- The test-only oracle is deliberately duplicated rather than factored through
  production; independence is the property it supplies.

## Status of `REAUDIT40`

| prior item | result |
|---|---|
| allocation oracle calls the factory it is intended to protect | **Fixed.** Independent `Barely` baselines remain at the minimum allocation shape while either production factory is mutated, and both regressions now fail. |

## What was rechecked without a finding

- `Owned.Of(first, second)` and the mapped `Owned.Of` overload still create
  final storage inside `Kept<T>`; caller-owned destination storage cannot enter.
- `Best.Readings` uses the mapped factory with a noncapturing selector and the
  real cell's indexable order.
- `Best.Pair` uses the element factory only for inputs longer than two and
  preserves a short owned witness without allocating.
- The nominal `Kept<T>` return/store types still reject the original cell
  collection-expression bypass at compile time.
- Witness contents, deterministic ordering, ambiguity propagation, and rendered
  diagnostics are unchanged.
- The 149-lexeme resolver allocation ceiling remains green.

## Verification

All four temporary mutations were removed before the maintained gates.

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
