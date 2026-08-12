# Re-audit 39 — `REAUDIT38` incorporation

**Audited:** `2efdbef`

**Date:** 2026-08-04

## Result

**The `REAUDIT38` safeguard finding is fixed. The exact
ordinary-collection-then-`Owned.Copy` mutation now fails the focused allocation
test, while the nominal type continues to reject a cell bypass at compile time.**

The new same-process ratio is well isolated: nodes and delegates are created
outside the measured interval, both paths are warmed, and only the producer is
measured. It does not inherit the resolver table's allocation noise. The
adversarial mutation was applied and the maintained test failed as intended.

The incorporation also changed `Best.Pair` after comparing its collection-array
and `Take(2)` forms. That investigation stopped one construction shape early.
The new private nominal type permits element-wise and mapped factories that
create the final backing array inside `Kept<T>` without either intermediate
shape. Focused probes halve allocation for both witness producers, leaving one
new low missing-optimization finding.

No correctness defect was found. The owner-authorized warning suppressions,
authoritative-document work, and settled hand-aligned formatter output remain
outside this round. Existing uncommitted documentation and handoff files were
preserved.

## Finding

### 1. Both witness producers still allocate an avoidable intermediate shape

**Severity: low — element-wise and mapped owned factories halve isolated
allocation in the ambiguity-reporting producers**

`Owned.Kept<T>` can only retain an array created inside its private factory
(`Compiler/Owned.cs:74-108`). That is the right ownership boundary, but the
only current producer factory accepts `IEnumerable<T>` (`Owned.cs:44-64`). The
caller must therefore manufacture an enumerable even when it already knows
the exact output elements or has an indexable input to transform.

#### `Best.Pair`

For a witness longer than two, `Best.Pair` currently calls:

```csharp
Owned.Of<string>([witness[0], witness[1]])
```

(`Compiler/Resolution/Resolver.cs:748-767`). The collection expression creates
an input array to satisfy `IEnumerable<string>`; `Kept<T>.Of` then enumerates it
into the separate final private array. This is faster than the former
`witness.Take(2)` iterator, as the new comment reports, but both alternatives
pay for an intermediate object.

A structurally safe two-element overload can take the elements rather than
caller storage:

```csharp
Owned.Of(witness[0], witness[1])
```

with the nested factory constructing `new([first, second])`. The array is born
inside `Kept<T>`, so no caller can retain it and the ownership argument remains
unchanged. A warmed 1,000-call probe over a preconstructed three-reading
witness measured:

| `Best.Pair` construction | allocated | per call |
|---|---:|---:|
| current collection-expression input | 128,000 bytes | 128 bytes |
| element-wise private factory | 64,000 bytes | 64 bytes |

The current code is twice the allocation necessary for the same two-string
owned result. The new comment compares 128 against 152 bytes and concludes the
128-byte form is preferred; it does not consider the 64-byte form.

#### `Best.Readings`

The new regression uses this as its desired baseline:

```csharp
Owned.Of(order.Select(node => node.ToString()))
```

(`Test/Unit/ResolverCost.cs:232-265`), which is also the production
implementation (`Compiler/Resolution/Resolver.cs:732-746`). It correctly
rejects an ordinary collection followed by `Copy`, but the `Select` iterator is
itself avoidable. `Cell` supplies a `List<Node>`, and the maintained test
supplies a `Node[]`; both are indexable.

A mapped factory accepting `IReadOnlyList<TSource>` plus a noncapturing selector
can allocate the final string array, fill it in place, and construct `Kept`
without a LINQ iterator. Using that shape with the selector
`static node => node.ToString()` measured:

| `Best.Readings` construction | allocated | per call |
|---|---:|---:|
| current `Select` enumerable | 112,000 bytes | 112 bytes |
| mapped private factory | 64,000 bytes | 64 bytes |

The maintained ratio assertion permits a faster `Best.Readings`, so this
change need not weaken the safeguard; its baseline and explanatory numbers
should be updated to the best owned-construction shape rather than treating the
iterator form as the oracle.

**Recommendation:** add safe producer overloads that receive values, not
caller-created storage: a two-element overload for `Pair`, and a mapped
`IReadOnlyList` overload for `Readings`. Build the final arrays inside the
nested `Kept<T>` factory, where the private constructor is accessible. Update
the producer comments and compare the maintained allocation guard against the
mapped baseline. A small direct guard for the `Pair` branch would preserve the
64-byte form; the existing test currently records measurements but makes no
assertion for it.

## Status of `REAUDIT38`

| prior item | result |
|---|---|
| helper-local ordinary-then-owned regression is unguarded | **Fixed.** The new producer-local ratio fails the exact `Owned.Copy([..])` mutation, independently of resolver allocation. |

## What was rechecked without another finding

- The allocation helper warms both paths and measures allocations on the same
  thread after delegate/closure construction.
- The ratio has wide separation: the intended `Readings` path is roughly half
  the admitted mutation, and the mutation fails the maintained assertion.
- The nominal `Kept<T>` return type still makes the old cell collection
  expression fail compilation.
- `Best.Pair` remains semantically bounded to the first two readings and returns
  owned storage in both branches.
- The change from `Take(2)` to explicit elements improves the audited branch
  from 152 to 128 bytes per call; finding 1 is the remaining 128-to-64 step.
- Witness ordering, rendering, identity preservation, and ambiguity diagnostics
  remain unchanged.
- The real resolver allocation ceiling remains green.

## Verification

The mutation and alternative-factory probes were removed before the maintained
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
  passed — 1024 tests, 100% line, 100% branch, 100% method
```
