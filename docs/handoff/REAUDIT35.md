# Re-audit 35 — `REAUDIT34` incorporation

**Audited:** `bbccc84`

**Date:** 2026-08-04

## Result

**The two `REAUDIT34` defects are fixed. There is no remaining correctness
finding from that report, but one low resolver-allocation pessimization remains,
so this is not an unconditional sign-off.**

`Owned.Copy` now retains only its own private representation. A caller-owned
`ReadOnlyCollection`, `ArraySegment`, writable collection, array, and opaque
view are all copied, while a value previously made by `Owned.Copy` is safely
retained. The maintained regression keeps the mutable backing objects and
changes them after constructing both consumers, so it now tests ownership
rather than merely the interface exposed by the view.

The explicit structural probes now assert the raw result before traversing it.
The `Identifier.TryPattern` probe also uses an actual function declaration and
reaches a non-null block sequence. A probe that throws or returns null can no
longer be recorded as successfully opened.

The owner-authorized XML warning suppressions remain deferred and are not
findings in this round. Authoritative-document alignment remains separate, and
the settled hand-aligned formatter output is not a finding. Existing
uncommitted documentation and handoff files were preserved.

## Finding

### 1. Non-empty resolver witnesses never acquire the trusted representation at their producer

**Severity: low — ambiguity propagation performs redundant defensive copies,
and the allocation comments describe a path that does not exist**

`Owned.Copy` is the only code capable of producing `Kept<T>`
(`Compiler/Owned.cs:36-50`). Its documentation says resolver producers make
that value once so later `Best` values retain it (`Owned.cs:29-33`). No resolver
producer does so:

- a cell's own tie constructs a new ordinary collection expression every time
  `TryBest` asks for `Witness` (`Compiler/Resolution/Resolver.cs:587-611`);
- `Best.Pair` constructs another ordinary collection expression
  (`Resolver.cs:717-730`);
- `Best.Witness` is the first resolver location that calls `Owned.Copy`
  (`Resolver.cs:688-702`).

Consequently every non-empty witness handed to `Best` is unmarked and copied
there. A propagated witness is first copied by `Best.Pair`, stored in the next
cell, and copied again when that cell produces a `Best`. This is safe, but it is
the opposite of the stated “make once, keep thereafter” allocation shape. The
older `Best` comment is now also false when it says copying occurs only for a
writable value and never in the resolver (`Resolver.cs:690-700`): the new rule
does not ask writability and necessarily copies every non-empty unmarked value.

The maintained allocation ceiling does not see this branch. Its 149-lexeme
input resolves uniquely (`Test/Unit/ResolverCost.cs:48-65`), so witnesses are
empty and collapse to the shared `Kept<T>.Empty`. Focused probes through the
real lexer and resolver measured warmed resolution of a genuine ambiguity:

| source | current allocation | with `Best.Pair` returning `Owned.Copy(...)` |
|---|---:|---:|
| `sum of list` | 24,272 bytes | 24,128 bytes |
| `(sum of list)` | 37,152 bytes | 36,792 bytes |
| `((((sum of list))))` | 91,088 bytes | 90,080 bytes |

That minimal producer change alone removes 144, 360, and 1,008 bytes
respectively; the increasing difference demonstrates the repeated propagation
cost rather than measurement of a hand-built collection. It does not address
the cell's separately rebuilt own-tie sequence, so it is a lower bound on the
avoidable work.

**Recommendation:** give resolver witness producers a construction path that
creates the private owned representation directly, and preserve that
representation through `Pair`/`Either` and cell storage. Avoid implementing
this as “build an ordinary collection, then copy it” where the producer already
owns the elements; an internal factory can take ownership of freshly created
storage. Add a small ambiguous-propagation allocation regression beside the
existing unique-resolution ceiling, then update the allocation comments to
describe the implemented path.

## Status of `REAUDIT34`

| prior item | result |
|---|---|
| 1. read-only views retain caller-owned mutable backing storage | **Fixed.** Only the private `Kept<T>` representation is retained; all external views are copied, and the maintained test mutates separately retained backing storage. |
| 2. a null opener is counted as successfully exercised | **Fixed.** The raw probe result is asserted non-null, and the formerly null `Identifier.TryPattern` opener now reaches a real declaration. |

## What was rechecked without another finding

- `Best.Witness` and both `Declared.Words` paths use the same ownership rule.
- Direct lists, `Collection<T>`, arrays, `ReadOnlyCollection<T>`,
  `ArraySegment<T>`, and opaque read-only views cannot mutate either consumer
  after construction.
- A value already made by `Owned.Copy` retains identity without exposing its
  private array.
- Positional deconstruction continues to expose protected properties rather
  than raw constructor inputs.
- Explicit probes fail on exceptions and nulls; reflected parameterless
  failures are still collected and asserted empty.
- The repaired function source reaches a grammar `Function`, and
  `Identifier.TryPattern` returns a non-null block sequence.
- The resolver allocation ceiling still passes. Its blind spot is limited to
  non-empty ambiguity witnesses described in finding 1.

## Verification

Temporary probes and the experimental producer change were removed before the
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
  passed — 1022 tests, 100% line, 100% branch, 100% method
```
