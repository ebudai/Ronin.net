# Re-audit 37 — `REAUDIT36` incorporation

**Audited:** `b6e2c69`

**Date:** 2026-08-04

## Result

**The production implementation remains correct, but the sole `REAUDIT36`
finding is not closed. It has been moved behind `Best.Readings`; the maintained
identity test still cannot detect either return of the original allocation at
the real cell or construction of an ordinary collection inside the new helper.**

`Best.Readings` is a sensible name for the second producer, and the real cell
currently calls it. The helper builds directly through `Owned.Of`, so the
audited code retains the allocation improvement. The new rendering assertion
is also correct. There is no present runtime correctness or performance defect
in this change.

The issue is solely that the claimed safeguard remains unable to fail on the
regression it is intended to prevent. The test itself now explicitly
acknowledges one of the surviving mutations.

The owner-authorized XML warning suppressions and authoritative-document work
remain deferred and were not raised here. The settled hand-aligned formatter
output is not a finding. Existing uncommitted documentation and handoff files
were preserved.

## Finding

### 1. Testing the extracted helper does not protect the real producer or its allocation shape

**Severity: low — production is correct, but both forms of the original
allocation regression leave the new safeguard green**

The cell now delegates its own tied readings to `Best.Readings`
(`Compiler/Resolution/Resolver.cs:608-609`), and the helper builds an owned list
directly (`Resolver.cs:720-734`). The regression calls that helper with two
hand-built nodes, then proves only that the object returned is already owned
and contains the expected renderings (`Test/Unit/ResolverCost.cs:209-221`).

That identity assertion protects this outcome:

```text
Best.Readings(...) returns Kept<string>
```

It does not protect either fact responsible for the allocation saving:

```text
the real Cell.Witness path calls Best.Readings
Best.Readings creates Kept directly, without an ordinary collection first
```

Both gaps were reproduced with focused mutations.

First, changing only the real cell back to the original expression:

```csharp
private IReadOnlyList<string> Witness
    => order.Count > 1
     ? [.. order.Select(node => node.ToString())]
     : witnesses[order[0].ToString()];
```

left `AWitnessIsMadeOnceAndKeptAsItRises` and the 85 selected
resolution-behaviour cases green. `Best.Readings` remained perfect in isolation
while the resolver stopped using it. The maintained test says this itself at
lines 213-217: a cell that inlines the old collection again still passes.

Second, leaving the cell call intact but changing the helper to:

```csharp
public static IReadOnlyList<string> Readings(IEnumerable<Node> order)
    => Owned.Copy<string>([.. order.Select(node => node.ToString())]);
```

also left the new identity and rendering assertions green. The helper returns a
`Kept<string>`, so `Assert.Same(tied, Owned.Copy(tied))` succeeds, but it first
builds the ordinary collection and then copies it — the same ordinary-then-owned
construction shape whose 112-byte cost motivated this safeguard.

The extraction therefore makes the helper testable but does not make the
optimization invariant structural, and the hand-built call is not a substitute
for the real resolver path. The `Admission` opener for `Best.Readings`
(`Test/Unit/Admission.cs:721-726`) likewise proves only that its published
result is non-writable; it cannot distinguish how it was produced or whether
the cell calls it.

**Recommendation:** protect the externally observable property through the
real resolver. The narrow option remains a warmed ambiguous-resolution
allocation regression, repeated enough to make the per-tie delta visible. If
an allocation assertion is considered too environment-sensitive, make the
owned-at-source invariant structural with a nominal type that the cell's tie
branch must return, or add a test-visible production trace/counter that records
ordinary-to-owned copies during an actual resolution. Testing `Best.Readings`
in isolation should remain as its unit test, but it cannot close the cell-path
finding by itself.

## Status of `REAUDIT36`

| prior item | result |
|---|---|
| regression covers only one of two witness producers | **Not fixed.** The second producer was extracted and tested in isolation, but the real cell can bypass it and the helper can recreate the old allocation internally without failing the test. |

## What was rechecked without another finding

- The audited cell currently calls `Best.Readings` for an own-reading tie.
- `Best.Readings` currently uses `Owned.Of` directly and returns deterministic
  renderings in cell order.
- `Best.Pair`, `Best.Either`, and `Best.Witness` continue to preserve an owned
  bounded witness without another copy.
- The new expected readings, `«one»` and `«two»`, match the actual node renderer.
- `Best.Readings` is included in the structural read-only-result census.
- Real ambiguous resolution retains the allocation improvement measured in
  `REAUDIT36`; the finding concerns regression detection, not current behavior.
- The 149-lexeme allocation ceiling remains green.

## Verification

Both mutation probes were removed before the maintained gates.

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
