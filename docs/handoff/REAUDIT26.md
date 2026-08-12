# Re-audit 26 — `REAUDIT25` incorporation

**Audited:** `42b76b1` through `e578678`

**Date:** 2026-08-03

## Result

**No sign-off.  Three high-severity correctness findings, one medium diagnostic
finding, and one low-severity hot-path pessimization remain.**

Several repairs are complete and real.  Pure cells can no longer create or
remove instances; removals made by a failed body are discarded; late qualified
member collisions are preflighted; membership is no longer inferred from the
global node table; trailing separators resolve without admitting leading or
doubled holes; malformed collection nesting now grows linearly; and the stale-
error regression finally forces a second recompute and observes the downstream
cutoff.

The creation half of the structural transaction does not actually roll back,
however: its helper is an iterator and the catch path calls it without
enumerating it.  The new list equality also reached cutoff but not the existing
`changes`/`old` implementations of equality, so an equal list fires an effectful
changes reaction.  Finally, the representation contradicts the design decision
that lists are immutable values: `object[]` escapes directly, can be mutated
behind the graph's back, and can be made cyclic enough to take structural
equality down with an uncatchable stack overflow.

As requested previously, the separate authoritative-document alignment is not
audited here.  Its remaining uncommitted files were preserved.

## Findings

### 1. Failed-body creation rollback is a lazy iterator that is never run

**Severity: high — a failed body commits every instance it created while its
fault says none of its effects applied**

`Release` performs the actual population release and member-column compaction,
but returns `IEnumerable<string>` and contains `yield return`
(`Compiler/Runtime/Graph.cs:344-367`).  Nothing in an iterator body runs until
the result is enumerated.

Round-boundary removal enumerates it correctly with `foreach`.  The failed-body
undo does not:

```csharp
for (var at = made.Count - 1; at >= 0; --at) Release(made[at]);
```

(`Graph.cs:1264-1269`).  Each call merely constructs and discards an iterator.
No slot is released, no generation advances, and no member value is removed.

A focused probe created two instances, wrote both, staged removal of one and of
a pre-existing instance, then threw.  The scalar/member/removal transaction
behaved correctly, but both created handles remained live:

```text
faults:                  1
pre-existing box.cash:  0       (removal correctly discarded)
created slot 1 cash:    0       live; expected stale-handle Error
created slot 2 cash:    0       live; expected stale-handle Error
```

The writes are zero because their firing-local staging was discarded; the
instances themselves are ghosts committed by the failed body.

The maintained test misses this.  It ignores the handle returned by the failed
body's `Create`, creates another instance afterward, and reads only that new
handle and the original (`Test/Unit/Instances.cs:435-466`).  The ghost may remain
between them without changing either assertion, so the test named "neither
removes nor creates" passes while creation persists.

**Recommendation:** make release eager.  A method that mutates state should not
hide that mutation behind deferred enumeration; return the already-known member
collection after performing release, or split eager `Release` from the caller's
dirtying walk.  The failed-body regression must retain the created handle and
assert it is stale afterward.  Keep the multi-create reverse-order case: it is
what establishes compaction and generation correctness rather than only the
single-last-slot accident.

### 2. `changes` still compares list references, so equal lists fire reactions

**Severity: high — an effect runs when the language says the value did not
change**

`Builtin.Same` is now the language's structural list equality and is correctly
used by pending writes, member writes, and recompute cutoff.  `TriggerMode.Changes`
still uses ordinary `Equals` (`Compiler/Runtime/Graph.cs:1161-1169`).  Arrays use
reference equality there.

The trigger's own documentation says changes means "whenever the value differs
from the last settled one", and the `Trigger.Previous` remarks explicitly define
it as `y is not old y`.  Under the settled list rule, two lists with equal
elements are one value.

The probe was a changes trigger whose condition read `tick` but always rebuilt
`[1]`:

```text
baseline: [1]
write tick = 1
recomputed condition: [1]

expected firings: 0
actual firings:   1
```

This is not only excess recomputation: the body is an effect and can write,
create, remove, return, or stop based on an edge that did not occur.

The shadow copy at the start of `Step` is the other surviving value comparison
and also uses ordinary `Equals` (`Graph.cs:926-936`).  An equal list rebuilt by a
derived source therefore advances and dirties `old list` by reference, despite
the comment requiring `Trigger.Previous` and `old` to agree about the boundary.

**Recommendation:** use the one language equality for the `Changes` arm and for
shadow advancement.  The boolean trigger arms may remain boolean comparisons.
Add a list-valued changes regression that counts body executions, and an
equal-list `old` regression that counts downstream evaluations.  Testing only
`Builtin.Same` or cutoff cannot prove either consumer joined it.

### 3. Lists are called immutable values but are exposed as mutable arrays

**Severity: high at the runtime/API boundary — mutation creates permanently
inconsistent cached reads, and cycles can terminate the process**

The designer's finding-6 decision depends on lists being immutable values.
The runtime representation is a mutable `object[]`: evaluation returns a fresh
array (`Compiler/Runtime/Evaluator.cs:97-112`), `Graph.Var` stores an arbitrary
object without freezing it, `Graph.Read` returns the same object, and runtime
declaration bodies receive arguments directly.

A caller can therefore mutate a list without a graph write:

```text
list = [1]
let first = list @ 1
Read(first) -> 1

mutate the returned/stored object[] element to 2

direct list element -> 2
Read(first)         -> 1   (clean cached value)
```

The probe reproduced that split exactly.  Nothing advances the source node or
dirties `first`, so the two reads disagree indefinitely.  This is the same
"confidently wrong cache" failure class as instance removal, now admitted by the
value representation itself.

It also invalidates an assumption in recursive `Builtin.Same`
(`Compiler/Runtime/Values.cs:266-279`).  Two distinct arrays can each contain
themselves.  Comparing those two values recursively calls `Same` on the same
pair forever and ends in `StackOverflowException`, which .NET cannot recover
into a `Fault`.  Source literals cannot spell a cycle, but the currently exposed
mutable representation and runtime declaration API can construct one; the
immutability invariant is stated, not enforced.

**Recommendation:** represent lists with an actually immutable runtime type
whose storage cannot be recovered by casting.  Construction should freeze/copy
its elements, indexing and parameter destructuring should consume a read-only
interface, and equality should compare that type.  If raw arrays remain an
accepted host boundary, normalize them into immutable values on entry.  A cycle
guard in `Same` prevents the fatal symptom, but enforcing immutable acyclic
construction closes both the stack-overflow and silent-mutation defects rather
than treating them separately.

### 4. Nested element errors are still replaced by a parent's mixed diagnostic

**Severity: medium — the direct witness is fixed, but one level of nesting hides
the actual syntax error again**

`Collection.Parse` now skips classification when an immediate `Element`
implements `IError` (`Compiler/Grammar/Collection.cs:46-65`).  That fixes
`[a =, b = 2]`.

An error can also live below a normal element's `Destination` or `Origin`.  The
compiler's reflective diagnostic walk is designed to find errors at arbitrary
depth, but the parent collection classifies first and replaces its entire tree
with a new `Mismatched` node.  The nested error is then unreachable.

The full compilation probe was:

```ronin
var lookup = [ [a =, b = 2], c = 3];
```

Expected:

```text
expected value
```

Actual:

```text
entry 1 is a value and entry 2 is an association, so this is neither a list nor a lookup
```

The first outer entry is not a valid list value; it contains the very missing-
value error the immediate guard was meant to preserve.

**Recommendation:** make "contains a syntax error" a compositional property,
or run collection classification only after recursively establishing that its
element trees are sound.  Do not duplicate a shallower `is IError` test for each
new wrapper: the reflective completeness work already established why errors
must be found through every grammar-declared member.  Regress direct, nested-
destination, and nested-origin errors beside both list and association entries.

### 5. Member ownership turned every instance access into a linear field scan

**Severity: low — correct, but the hot path is now O(members) per read/write**

The membership repair correctly stopped treating a node-table hit as proof that
a member belongs to a type.  `Cell` now asks
`population.Members.Contains(Qualified(...))`
(`Compiler/Runtime/Graph.cs:369-377`).  `Members` is a `List<string>` retained in
declaration order (`Compiler/Runtime/Instances.cs:46-52`), so every instance
read and write linearly scans the type's declared fields and constructs the
qualified string twice on a hit.

Grouped storage is intended to make runtime cost scale with the world without
turning every instance into graph structure.  A loop over many instances
reading a common member now additionally scales with the amount of source in
the type, even though ownership is static after declaration.

**Recommendation:** retain the ordered list for column iteration and add an
O(1) ownership index, preferably source member name to qualified cell.  Then
`Cell` is one lookup and returns the stored identity without constructing it
twice.  This is an optimization only; the present result is correct.

## Status of `REAUDIT25`

| prior item | result |
|---|---|
| 1. structural effects bypass the transaction | **Partial.** Pure create/remove and failed removals are fixed. Failed creations do not roll back because the eager-looking call discards a lazy iterator; finding 1. |
| 2. malformed collection parsing is exponential | **Fixed.** The second parse is removed, measured work is linear, and the regression asserts the requested ratio. Basic literal, collection, input, delegate, operation, and indexed-value paths still reach `Value.Parse`. |
| 3. missing lookup value becomes mixed collection | **Partial.** The direct element is fixed; a nested error is still discarded by parent classification; finding 4. |
| 4. trailing separators fail resolution | **Fixed.** Singleton/multiple trailing lists and parenthesized groups resolve; leading and doubled holes remain refused. |
| 5. type failure remains partial | **Fixed.** Full node/constant uniqueness is preflighted before mutation and member ownership comes from the population. The new ownership lookup is correct but linear; finding 5. |
| 6. equal lists defeat cutoff | **Fixed at cutoff.** Structural, recursive list equality prevents downstream wake-up. The same language equality is not used by changes/old, and mutable arrays violate the value premise; findings 2 and 3. |
| stale-error regression gap | **Fixed.** The stale node is dirtied and recomputed, while a downstream evaluation count proves equal errors cut off. |

## What was rechecked without another finding

- Member writes still stage stable handles and remain inside the firing
  transaction.
- Failed-body removals are genuinely discarded; successful removals retain the
  next-round timing.
- Successful create/write/remove combinations preserve handle identity and
  compaction order on the maintained paths.
- Qualified collision preflight covers existing nodes and constants before any
  member is declared.
- Empty, singleton, multiple, nested, and trailing-comma lists retain list
  identity through resolver and evaluator.
- Structural list equality is order-sensitive, recursive, length-sensitive, and
  exits on the first difference for acyclic inputs.
- The `Reference.Parse` rewrite preserves the maintained temporary forms while
  eliminating the second malformed-nest parse.
- `Error` kind/message equality and its newly meaningful downstream test remain
  correct.

## Verification

- Temporary focused probes: ten temporary-value forms passed; failed creation
  rollback, list-valued changes, nested collection-error preservation, and
  list-mutation coherence failed as described.  The probe file was removed
  before repository gates.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **964 passed**, zero failed, zero skipped,
  with **100% line, branch, and method coverage**.
- `git diff --check 42b76b1..e578678`: clean.

The worktree also contains the prior `REAUDIT25` report, the designer's
`LISTEQUALITY.md`/probe, and the separate documentation-alignment edits.  They
were preserved; only this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.
