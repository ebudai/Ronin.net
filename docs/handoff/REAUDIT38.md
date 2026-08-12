# Re-audit 38 — `REAUDIT37` incorporation

**Audited:** `9f3af71`

**Date:** 2026-08-04

## Result

**The nominal `Owned.Kept<T>` type correctly closes the real-cell bypass from
`REAUDIT37`. The second half remains unguarded: `Best.Readings` can still build
an ordinary collection and pass it through `Owned.Copy` while every maintained
assertion stays green. Unlike the commit's analysis, that rewrite is now easy
to distinguish with a focused producer-level allocation test.**

The audited production code is correct. `Cell.Witness`, the witness dictionary,
and all three `Best` producers return or store `Owned.Kept<string>`. Its private
constructor cannot be called by `Owned` or by resolver code, and the only
factory copies enumeration results into storage created inside the nested type.
The original mutation from `REAUDIT36` is therefore rejected by the C# compiler
rather than left to a unit assertion.

One low safeguard finding remains, so this is not an unconditional sign-off.
It concerns regression detection only; no present runtime correctness or
allocation defect was found.

The owner-authorized warning suppressions and authoritative-document work
remain deferred and were not raised here. The settled hand-aligned formatter
output is not a finding. Existing uncommitted documentation and handoff files
were preserved.

## Finding

### 1. The helper-local ordinary-then-owned regression is measurable but deliberately unguarded

**Severity: low — production is correct, but a 2.2× producer-allocation
regression satisfies the nominal type and every maintained assertion**

The type change is effective at the call site. `Cell.Witness` is declared as
`Owned.Kept<string>` (`Compiler/Resolution/Resolver.cs:608-618`), and replacing
its tied branch with the old collection expression produces `CS1729` and
`CS1061`: the collection expression cannot construct or add to `Kept<string>`.
Direct `new Kept<T>(...)` from the enclosing `Owned` type also produces
`CS0122`; the constructor boundary described in `Compiler/Owned.cs:74-97` is
real.

The maintained test then acknowledges a remaining rewrite but concludes that
no allocation guard can see it because a full resolver run is much larger
(`Test/Unit/ResolverCost.cs:218-225`):

```csharp
public static Owned.Kept<string> Readings(IEnumerable<Node> order)
    => Owned.Copy<string>([.. order.Select(node => node.ToString())]);
```

That conclusion no longer follows after extracting `Best.Readings`. The
producer can be measured directly, without any resolver table or parsing work.
A warmed focused probe over the same two preconstructed `Node.Name` values
measured 1,000 calls:

| implementation | allocated | per call |
|---|---:|---:|
| current direct `Owned.Of(...)` | 120,000 bytes | 120 bytes |
| ordinary collection followed by `Owned.Copy(...)` | 264,000 bytes | 264 bytes |

The admitted mutation adds 144 bytes per call and makes this isolated producer
allocate 2.2 times as much. It compiled successfully and still returns
`Owned.Kept<string>`, so both maintained assertions remain true:

```csharp
Assert.Same(tied, Owned.Copy(tied));
Assert.Equal(["«one»", "«two»"], tied);
```

The nominal type proves the final value owns its backing storage; it cannot
prove that no throwaway collection was built first. That is an allocation
property and should be guarded as one. The existing project already uses
`GC.GetAllocatedBytesForCurrentThread` for focused allocation invariants, so no
production counter or state is required.

The test comment's recorded 112-byte cost is also no longer the value of this
exact helper mutation after the nominal-type refactor; the focused delta is 144
bytes per call in the audited runtime. The broader real-resolver figure may
measure a different path, but it is not a reason to leave the now-isolated
producer unguarded.

**Recommendation:** add a warmed allocation assertion around repeated
`Best.Readings` calls with the nodes allocated before measurement. A relative
same-process comparison against direct `Owned.Of(order.Select(...))` isolates
the extra ordinary-then-owned stage and avoids dependence on the resolver's
total allocation; alternatively, use a threshold with ample space between 120
and 264 bytes per call. Keep the nominal return type—it correctly protects the
cell and store boundaries that an allocation test should not have to police.

## Status of `REAUDIT37`

| prior item | result |
|---|---|
| helper test does not protect the real producer or its allocation shape | **Partially fixed.** `Owned.Kept<T>` makes the real cell/store bypass fail compilation. The helper-internal ordinary-then-owned rewrite still passes all maintained assertions and remains unguarded; finding 1. |

## What was rechecked without another finding

- The original cell mutation fails to compile against the nominal return type.
- The private `Kept<T>` constructor is inaccessible even from its enclosing
  `Owned` type; instances must pass through the nested factory.
- `Kept<T>.Of` copies its input sequence into private array storage.
- `Cell.Offer` admits every incoming witness through `Owned.Copy` or the shared
  owned empty value, and the dictionary can store only `Kept<string>`.
- `Cell.Witness`, `Best.Readings`, `Best.Pair`, and `Best.Either` all expose the
  nominal owned type internally.
- `Best.Witness` and `Declared.Words` continue to publish only read-only
  interfaces and retain the prior alias protections.
- The expected rendered readings remain `«one»` and `«two»`.
- The real resolver's ambiguity results and allocation ceiling remain green.

## Verification

Constructor, call-site, and helper-allocation probes were removed before the
maintained gates.

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
