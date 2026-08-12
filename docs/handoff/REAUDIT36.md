# Re-audit 36 — `REAUDIT35` incorporation

**Audited:** `77a1703`

**Date:** 2026-08-04

## Result

**The `REAUDIT35` production finding is fixed. Both resolver witness producers
now create or preserve the private owned representation, the allocation
improvement reproduces through the real resolver, and no current correctness
or performance defect was found in the implementation. One low safeguard gap
remains, so this is not an unconditional sign-off.**

`Owned.Of` gives a producer a structurally safe construction path: it accepts a
sequence and creates the private backing storage inside `Owned`, rather than
trusting storage supplied by its caller. A cell's own tie uses that path, while
`Best.Pair` uses it only when truncating and otherwise preserves an already
owned bounded witness. `Best.Witness` therefore sees `Kept<T>` throughout the
real resolver and no longer performs the repeated defensive copies reported in
`REAUDIT35`.

The owner-authorized XML warning suppressions remain deferred and are not
findings. Authoritative-document alignment remains separate. The existing
hand-aligned `dotnet format` differences are settled and were not raised again.
Existing uncommitted documentation and handoff files were preserved.

## Finding

### 1. The regression covers only one of the two witness producers

**Severity: low — the production code is correct, but reverting the cell's
owned-tie construction passes the test intended to preserve the optimization**

There are two places where a non-empty witness originates:

- a cell renders its own tied readings through `Owned.Of`
  (`Compiler/Resolution/Resolver.cs:608-611`);
- `Best.Pair` either truncates through `Owned.Of` or preserves ownership through
  `Owned.Copy` (`Resolver.cs:719-736`).

The new regression constructs a list by calling `Best.Pair` directly and then
checks that `Copy`, `Pair`, `Either`, and `Best` preserve its identity
(`Test/Unit/ResolverCost.cs:178-208`). That is a strong, non-allocation-based
guard for the second producer and for every propagation consumer.

It does not execute the first producer. The test comment acknowledges this and
says the cell change cannot be discriminated from outside
(`ResolverCost.cs:196-202`). A focused mutation demonstrates the consequence:
changing only

```csharp
Owned.Of(order.Select(node => node.ToString()))
```

back to

```csharp
[.. order.Select(node => node.ToString())]
```

left `AWitnessIsMadeOnceAndKeptAsItRises` and the 85 resolution-behaviour cases
selected with it green. The mutated branch was exercised, so 100% line and
branch coverage would not distinguish it either. That mutation restores the
one extra defensive copy for every cell whose own readings tie — the exact half
of the fix the test says it does not pin.

This is externally observable as allocation even though the final `Best`
correctly owns its copy. The programmer's own measurement records 112 bytes per
tie. It can also be tested structurally by placing the ownership assertion at
the producer boundary rather than after `Best` has normalized both cases.

**Recommendation:** add a regression for the cell-owned-tie producer. A warmed
real-resolver ambiguity repeated enough times can amplify the 112-byte delta
for a narrow allocation guard; alternatively, factor the producer boundary so
an internal test can assert that its raw result is already owned before
`Best.Witness` gets a chance to repair it. Keep the existing identity test for
`Pair` and propagation — it correctly protects the other half.

## Status of `REAUDIT35`

| prior item | result |
|---|---|
| non-empty resolver witnesses are copied instead of owned at their producers | **Fixed in production.** `Cell.Witness` builds through `Owned.Of`; `Best.Pair` creates or preserves the owned representation; `Best` retains it. The only remaining issue is the missing regression for the cell producer described above. |

## What was rechecked without another finding

- `Owned.Of` copies enumeration results into storage created inside the private
  `Kept<T>` representation; caller-owned arrays and collections cannot become
  its backing storage.
- `Best.Pair` returns at most two readings, preserves identity for an owned
  pair, and safely copies an external pair.
- A witness remains owned through `Either`, cell storage, `TryBest`, and
  `Best.Witness`.
- A cell's own tie still reports all readings in deterministic order; only the
  storage representation changed.
- The stale allocation comments on `Best.Witness` were corrected.
- The structural read-only-result audit includes `Owned.Of` and remains green.
- The unique 149-lexeme allocation ceiling remains below 26 MB.
- Focused real-lexer/resolver measurements reproduced the improvement from the
  audited baseline:

  | source | `bbccc84` | `77a1703` | reduction |
  |---|---:|---:|---:|
  | `sum of list` | 24,272 bytes | 24,016 bytes | 256 bytes |
  | `(sum of list)` | 37,152 bytes | 36,680 bytes | 472 bytes |
  | `((((sum of list))))` | 91,088 bytes | 89,968 bytes | 1,120 bytes |

## Verification

Temporary allocation and mutation probes were removed before the maintained
gates.

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
  passed — 1023 tests, 100% line, 100% branch, 100% method
```
