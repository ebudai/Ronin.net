# Mid-session monomorphisation — the design

> **Ledger** — `[V]` Designs the instantiation lifecycle the `(function, instantiation)` cache assumes: what an instantiation is, what mints one, what invalidates it, what discards it, and the cache's contract.
> answers: MIDSESSIONMONOMORPH
> supersedes: none
> superseded by: none

**The gap is real and the flag was right.** But the first thing to fix is a
conflation in how it has been stated, because it decides how hard the rest is.

---

## §0 — "mid-session" is the **edit** session, not program runtime

`GENERICSII` §1 says *"a call at a new argument type triggers an instantiation
mid-session"*, and the memo reads that as *"a body is monomorphised for an argument
type the first time that type reaches it, while the program is live."* Those are
two different claims and only the first is true.

Instantiation is keyed on **ground sorts**. There is no subtyping, unification is
equality, and the recursion ruling requires the answer to be ground. So **a call
site's argument tuple is fully determined statically.** No value *flowing* at
runtime can produce an instantiation that static checking did not already know
about.

> **New instantiations arise because the source changed, not because the program
> ran.** Minting is an event in incremental compilation, not a JIT event.

That collapses the hard version of the problem. What remains genuinely live is
*installing* a newly minted body into a program that is already running — which is
**hot reload**, machinery that is already this project's concern rather than a new
one.

**The one thing that would break this** is a construct that defers a call's
argument sorts to runtime. `type of x` branching is on the deferred list; if it
lands, this design needs an amendment, and that should be a known consequence of
taking it rather than a discovery afterwards.

## §1 — what an instantiation is, and why the key is already stable

> **An instantiation is the tuple of ground sorts a body is monomorphised at.**
> The cache key is `(function identity, that tuple)`.

Nothing new is needed to make this stable, because **the stability was already
bought**. A named sort's identity is `(declaring scope, name)`; the container is
the shape words (`B`, not the resolved parameter sorts); spans and ordinals were
refused precisely so identity survives an edit. So the tuple is a structural value
that does not move when someone types.

That yields the single most useful property in this design:

> **An edit to a generic body does not invalidate any key. It invalidates values.**

The cache is therefore never *rebuilt*; entries are *refreshed*. Keys churn only
when a type is renamed or a container is renamed — which is exactly when they
should.

## §2 — minting: demand-driven from call sites, at check time

> **A call site demands the instantiation its argument tuple names. The
> instantiation is minted the first time a call site demands one that does not
> exist, during the check.**

No eager enumeration: under the always-running premise there is no build-time
moment at which the reachable set is known, and the memo's *"incremental and
cheap"* requirement is exactly this.

And by the same discipline as `Variable`: **mint through a supply, not a
constructor.** Two passes must not be able to build the same instantiation
independently — that is the identity bug the `Sort.Variable` factory ruling closed,
and it arrives here for the same reason.

## §3 — invalidation: by dependency, and use the shape that already exists

An edit to a generic body makes every `(f, *)` **value** stale. But that is the
easy case. The hard one:

```
  type T { … }                    edit T's body
  -> a named sort's IDENTITY is unchanged — (declaring scope, name)
  -> so no key changes
  -> but an inference that read T's members may now be wrong
```

So invalidation cannot be *match the key*; it must be **track what the inference
read**. That is a dependency record per cache entry, which is the standard
incremental-compilation answer.

**Do not invent a second one.** This project already has a dependency graph with
change propagation and a **cutoff** on `is` — and cutoff is the property that
matters most here: editing a comment in a generic body, or an edit that leaves the
inferred result identical, must stop propagating rather than reinstantiate
everything downstream. A second invalidation scheme with different semantics is
two copies of a stopping condition that later disagree.

Whether the compiler's cache should literally be cells in the runtime graph, or
the same *design* re-instantiated for the compiler, is a question about coupling
and therefore the programmer's — sharing the instance means a defect in one takes
out the other; sharing the design costs a little duplication and no blast radius.
**The requirement is that the invalidation semantics are one design, not two.**

## §4 — eviction: count the demands, act at zero

Instantiations accumulate over a session that runs for days, including ones
demanded by call sites the user has since deleted. Left alone that is unbounded
growth in a program that never restarts — **the third appearance of the shape the
watchdog exists for.**

> **An instantiation is live exactly while some call site demands it. Keep a count
> of demanding sites; an edit decrements the old and increments the new; discard at
> zero.**

Exact, O(1) per edit, and no scan — the same answer as demoting a promoted column
at zero over-wide elements. Reachability by mark-and-sweep would also be correct
and would cost a walk per edit, which the always-running premise cannot afford.

## §5 — the always-running bound: instantiation must be bounded in **time**, not only in depth

I said a depth limit was load-bearing before the first generic recursive function.
The always-running premise adds a second bound that a batch compiler never needs.

A half-typed generic recursion is re-checked **on every keystroke**. So:

> **A single check must mint at most a bounded number of instantiations. Exceeding
> the budget is a finding naming the chain — never a hang, and never a partial
> cache.**

The depth limit stops an infinite chain. The per-check budget stops a finite but
enormous one from freezing the editor while someone is still typing the base case.
And "never a partial cache" matters: an aborted check must leave no entry behind,
or the next keystroke reads a value that was never completed.

## §6 — the cache contract: values are ground, variables never escape

This answers the memo's third bullet and it is a one-line rule with real teeth.

> **A cache value is ground. Inference variables do not enter it and do not
> outlive the inference run that created them.**

The recursion ruling already requires the answer to be ground, so this costs
nothing and it forecloses a bad class of bug: a cached entry holding a live
`Variable` whose requirement set some later pass mutates — equal values with
divergent state, which is exactly the defect `REAUDIT56` finding 4 raised and the
factory ruling closed. If a variable would escape into a cache value, that is
already the *not ground* error, reported rather than stored.

So the requirement-set a `Variable` accumulates is **per-run scaffolding**, not
cache content. It informs the diagnostic and the solution; it is not what gets
keyed.

## §7 — two things I need confirmed, because they are about the tree

1. **Can a call's argument sorts ever be unknown until runtime today?** §0 depends
   on no. If some construct already defers them, say so and this design gets an
   amendment now rather than later.
2. **Is there a usable hot-reload path for installing a newly minted body into a
   running program?** §0 hands the live half of the problem to it. If that does not
   exist yet, the gap is real but it is a *hot-reload* gap, not a monomorphisation
   one — and it should be ledgered under that name.

## Summary

| | |
|---|---|
| **the reframe** | **"mid-session" is the *edit* session, not program runtime.** Instantiation is keyed on **ground** sorts, so a call site's tuple is determined statically — new instantiations come from **source changes**, not from values flowing |
| what stays live | *installing* a new body into a running program — that is **hot reload**, an existing concern, not a new one |
| the caveat | **`type of x` branching would break §0.** Take this design knowing that, rather than discovering it |
| **what an instantiation is** | the **tuple of ground sorts**; the key is `(function identity, tuple)` |
| why it is stable | the stability was **already bought** — `(declaring scope, name)`, container `B`, no spans. **So an edit to a body invalidates values, never keys.** The cache is refreshed, never rebuilt |
| **minting** | **demand-driven from call sites at check time**, and **through a supply, not a constructor** — the same identity bug the `Variable` factory closed |
| **invalidation** | **by dependency, not by key match** — editing a type's body changes no identity but can change an inference. Use the **existing** graph's semantics, including **cutoff**; two invalidation schemes is a stopping condition stated twice |
| **eviction** | **count the demanding call sites; discard at zero.** Exact, O(1) per edit, no scan — third instance of the same answer |
| **the new bound** | a check must mint a **bounded number** of instantiations. A half-typed recursion is re-checked every keystroke. Exceed it → a **finding naming the chain**, never a hang, and **never a partial cache** |
| **the cache contract** | **values are ground; inference variables never enter and never outlive their run.** Costs nothing, and forecloses a cached `Variable` with mutating state |
| **needs the tree (1)** | can a call's argument sorts be unknown until runtime today? |
| **needs the tree (2)** | is there a usable hot-reload path for installing a minted body live? If not, that gap is **hot reload's**, and belongs in the ledger under that name |
