# Finding 3 — upheld, with the recommendation amended and a second bug attached

> **Ledger** — `[V]` Finding 3 — upheld, with the recommendation amended and a second bug attached
> supersedes: none
> superseded by: none

**Verdict: the finding is correct and the severity is right.** "Confidently wrong
forever" is the accurate description and it is the worst failure class this
language admits.

**The recommendation is not.** Not because it is wrong — dirtying those nodes is
necessary — but because it is a *smaller* fix than the defect, and the auditor's
own closing sentence predicts what happens next: *"otherwise the same omission
will recur on that path."* A fix that has to be remembered will be forgotten.

And the probe, taken as the specification for a test, would bake in a **wrong
timing**. That is the most actionable thing here, so it is first.

---

## 1. The probe's expected result is wrong, and a test written from it would lock the wrong behaviour in

```text
Remove(box)
Read(observed)     -> 0       // reported as the bug
```

`Read(observed)` returning `0` immediately after `Remove` is **correct**, and it
must stay correct. Removal is a write. Writes are buffered to the round end so
that no body's write is visible to another body in the same step. If removal
takes effect the instant it is called, a step becomes order-dependent
(`instance_removal.py` §1):

```
  arm order ['remove', 'read']   -> remove box: removed,  read box.cash: <Error: stale handle>
  arm order ['read', 'remove']   -> read box.cash: 0,     remove box: removed

  buffered, both orders:
  arm order ['remove', 'read']   -> remove box: removed,  read box.cash: 0
  arm order ['read', 'remove']   -> read box.cash: 0,     remove box: removed
```

Same program, two answers, decided by arm order — the exact defect the
buffered-write rule exists to remove.

So the bug is not that `observed` reads `0` after `Remove`. **The bug is that it
still reads `0` in the next round.** The test wants a round boundary in it:

```text
Remove(box)
Read(observed)         -> 0        // correct: the write is buffered
<round boundary>
Read(observed)         -> Error    // the actual defect
Read(cash, box)        -> Error
```

Without that boundary the fix will be built to make `Remove` take effect
immediately, which trades a high-severity caching bug for a high-severity
determinism bug.

## 2. Amend the fix: removal is a write, not a mutation that also dirties

Two fixes are available and they are not equivalent:

| | |
|---|---|
| **A** — the recommendation | `Remove` additionally advances/dirties the grouped member nodes |
| **B** | `Remove` stops bypassing the write path. It *is* a write; buffering and dirtying fall out of the path everything else already uses |

`Graph.cs:192-209` mutating every member array directly is the defect; the
missing dirty is the symptom. Under A, `Remove` remains a second way to change
state that must separately remember to do what writes do — and the next path
that changes state structurally (creation, population enumeration, bulk delete,
undo in the live environment) starts from the same blank slate. The auditor
already sees this coming and names it; the way to act on that foresight is to
remove the category, not to patch this member of it.

Under B the finding cannot recur, because there is nothing left to forget.

I would also expect B to *shrink* the code: buffering, clock advance, dirty
marking and round-end application all exist already.

## 3. What a stale read should yield — Error, and the reason is narrow

Three candidates, simulated (`instance_removal.py` §3):

```
  freeze   -> observed = 0                        «otherwise» never fires
  nothing  -> observed = 'nothing'                caught by «otherwise»
  error    -> observed = '<Error: stale handle>'  caught by «otherwise»
```

`freeze` is today's behaviour and is the only one of the three **the program
cannot detect**. That alone disqualifies it.

Between the other two the ergonomic difference is exactly zero, because
`NOTHING-AND-INDEXING.md` §1.1 already settled that `otherwise` catches both.
So the choice is purely about which signal is true:

- a **lookup miss** is a question — you probed for something that might not be
  there, and `nothing` is the honest answer;
- a **stale handle** is a bug — you kept a reference to something you deleted.

`Error` is the honest answer to the second, and `box.cash otherwise 0` already
handles it with no new machinery. This also resolves an inconsistency nobody has
flagged: the direct path already returns `Error` while lookup misses return
`nothing`, and the reason those differ should be written down rather than left
looking accidental.

## 4. Granularity — one question I cannot answer from here

The recommendation says "each grouped member cell", which at **column**
granularity is right and is consistent with writes: a member write already
dirties the whole column, so removal is no coarser than what exists.

But if the sparse-update dirty sets are in place, removal should mark **the
removed row** in each member's dirty set rather than the column. Otherwise
deleting one box out of ten thousand wakes every reader of every box's every
member — and unlike a write, a removal is a plausible bulk operation, so the
coarse version is O(deletions × readers).

Which of the two is currently implemented decides the fix, and I do not know
from the finding. If it is still column-granular, do the column version now and
let it ride the sparse work later; it is correct, just coarse.

**Underneath that is a question worth answering explicitly:** does removal
compact the arrays (swap-remove) or tombstone the slot? The `Error` on the
direct path implies generational handles and tombstones. If it is compaction,
other rows change position, handles must indirect through a slot map, and
row-granular dirtying is not available at all. That is a fact about the storage
that the reactive rule depends on, and it should be stated where both can see
it.

## 5. Creation — they are right to ask now, and the answer is two observables

Measured (`instance_removal.py` §4):

```
  before:      population = 1
  in-step:     handle 1 usable immediately (cash=5), but population still reads 1
  after round: population = 2
```

Creation has **two** observables with **different** timings, and writing them
down as one is how the same omission recurs:

| observable | timing | why |
|---|---|---|
| the **handle** | immediate, to its creator | `var b = new box; b.cash = 5` has to work in one step; the handle is a local value, not a shared read |
| the **population** — enumeration, `count of`, `for each` | round boundary | it is a write like any other, and §1 applies to it identically |

So the symmetric rule, which is what should go in the spec:

> A structural change to an instance set is a write. **Creation** advances the
> population node. **Removal** advances the population node and every member
> column (or, with sparse sets, the removed row in each). Both land at the round
> boundary.

## 6. One interaction to get right if cutoff lands first

`FAILUREMODES.md` §2 recommends cutoff — do not propagate when a recompute
yields an unchanged value. Once removal dirties properly, a stale cell
recomputes to `Error` every round forever. Cutoff handles that **only if two
Errors compare equal.**

If `Error` carries a payload — message, site, timestamp — and equality includes
it, every stale cell re-propagates on every round and the graph never goes
quiet. That is precisely the failure §2 was written to prevent, arriving through
a different door.

So: **cutoff equality on `Error` must be by kind, not by payload.** Small,
cheap, and easy to miss until a session gets mysteriously slow.

## 7. Verdict

| | |
|---|---|
| the finding | **upheld**, severity high, "confidently wrong forever" is accurate |
| the probe's expected result | **amended** — `0` immediately after `Remove` is correct; the defect is at the *next round*. A test without a round boundary locks in a determinism bug |
| the recommended fix | **amended** — route removal through the write path rather than adding a dirty step to it. The auditor's own "this will recur" is the argument for the larger fix |
| what a stale read yields | **Error**, not `nothing` and not the frozen value. `otherwise` already catches it |
| granularity | column is correct-but-coarse; row is right if sparse sets exist. Needs an answer about compaction vs tombstones |
| creation | two observables — handle immediate, population at the round boundary. Decide it now, as they ask |
| cutoff | `Error` equality by kind, or stale cells never go quiet |

Probe: `instance_removal.py` — order-dependence, the round-boundary case, the
three stale-read policies, and creation's two timings.
