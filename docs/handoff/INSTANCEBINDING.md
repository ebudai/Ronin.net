# Instance binding — decided

> **Ledger** — `[V]` Instance binding — decided
> supersedes: not yet checked
> superseded by: not yet checked

He is right that a recommendation which has been consistent, is the sole blocker
on a construct that is now user-visible in a diagnostic, and cannot be
retrofitted, should stop being a recommendation.

**Decided: one cell per declared member, holding N values. Not one node per
instance.** Budai overrules if he disagrees; absent that, build on it.

---

## The load-bearing reason, which is not the benchmark

I have been quoting `econ_sim.c` — per-instance scalar nodes ran ~20× slower
than the grouped equivalent, and the cost was edge-chasing and cache behaviour
rather than arithmetic, so it does not come back with tuning. That number is
real, but it is **corroboration, not the argument**, and it is worth being
precise about that before anyone builds a runtime on it.

The argument is:

> **Under grouped storage the dependency graph is the size of the source text.
> Under per-instance nodes it is the size of the world.**

Everything downstream inherits that. Edge counts, dirty propagation, cascade
analysis, the SCC check, the cutoff comparison, diagnostics that name a node —
all of them scale with how much code was written rather than with how much data
exists. That holds at twelve instances and at a million, so it does not depend
on the benchmark generalising, and it is a *correctness and comprehensibility*
property before it is a performance one.

It matters because both stated targets are in play: a VB6-style form has twelve
controls and a simulation has a hundred thousand entities. At twelve, grouped
storage wins nothing on speed and costs a little indirection. It still wins,
because the graph a person debugs is the graph they wrote.

## What follows immediately

| | |
|---|---|
| type-scope `when` | one node, a vectorised predicate across the member array, firing the body per instance whose entry changed |
| `stop` | clears the caller's bit in the liveness mask; the node goes when the mask empties |
| instance identity | an index into the member arrays, not a pointer |
| adding / removing an instance | an array operation; removal wants a free list or swap-with-last plus a stable handle table |
| cutoff | becomes array-valued and therefore O(N) per cell — use a dirty range or a digest, never a full compare |
| live edit | adding or removing a member is adding or dropping one array, rather than walking N objects. This gets *easier*, which is worth knowing given hot reload is a goal |

## What is not decided, and is a refinement rather than an alternative

Three things sit inside the decision. None of them reopens it, and none needs
answering now:

- **Subtypes.** Members that only some instances have make the arrays ragged.
  The standard answer is one array set per concrete type — archetypes — and it
  composes with everything above.
- **Sparse firing.** A predicate over N entries is wasteful when three of ten
  thousand are armed. A dirty list or a per-block summary fixes it; the mask
  alone does not, since you still scan to read it.
- **References between instances.** A member holding a reference stores an
  index, so a polymorphic reference needs a type as well. Worth settling when
  references exist, not before.

## Pin it with a test, not a comment

The decision is checkable, and a comment will not survive an optimisation
pass:

> Create N instances of a type with M members and one type-scope `when`.
> Assert the graph's node count is a function of M only, and is unchanged
> between N = 1 and N = 1000.

That test fails the moment someone reintroduces per-instance nodes for a
plausible local reason, which is how this kind of decision is normally lost.

## The residual risk, stated

If it turns out wrong, the cost is high — that is exactly why he pushed for it
to be decided rather than left recommended. The way it could be wrong is if
instances turn out to be mostly heterogeneous and few, so that archetype
proliferation costs more than per-instance nodes would have. I think that is
unlikely for both target workloads, but it is the failure mode to watch, and the
signal would be archetype count growing with instance count rather than with
type count.
