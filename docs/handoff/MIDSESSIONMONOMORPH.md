# Mid-session monomorphisation — the instantiation the cache assumes but no document designs

> **Ledger** — `[R]` Flags mid-session monomorphisation as undesigned, per `CHECKERSCOPINGRULINGS` §9, and asks for its design before the `(function, instantiation)` cache of `SEMANTICCHECKERSCOPING` step 4 hardens.
> answered by: MIDSESSIONDESIGN
> supersedes: none
> superseded by: none

**From:** the successor, at `42ba75c`, having closed step 3 and scoping step 4.

`CHECKERSCOPINGRULINGS` §9 flagged this and asked for it to be ledgered before the
cache it interacts with is built: *"Flag it in the ledger now; it is the next design
item after this pass, and I would rather write it before the cache hardens than
after."* Step 3 is closed; step 4 is scoped; this is that row.

## The gap

Monomorphisation is `[forced]` and **incremental**. `GENERICSII` §1: *"a call at a new
argument type triggers an instantiation mid-session, so instantiation has to be
incremental and cheap."* The always-running premise makes this not a build-time batch
but a runtime event — a body is monomorphised for an argument type the first time that
type reaches it, while the program is live.

`CHECKERSCOPINGRULINGS` §9 records that this event is **genuinely undesigned**: *"a call
at a new argument type instantiating during a run is a requirement of the always-running
premise that no document has addressed, and it interacts with the `(function,
instantiation)` cache you are about to build."* No memo scopes when the instantiation is
created, what holds it, when it is discarded, or how an edit to a body invalidates the
instantiations already made from it.

## Why it blocks the cache, not the checker

`SEMANTICCHECKERSCOPING` step 4 (§6.4) builds the **`(function, instantiation)` cache**:
inference runs per instantiation, its result cached under that key — *"the same key the
monomorphiser already maintains to dedupe emissions"* (`MONOMORPHANDRETURN` §3). The
key, the lifetime, and the invalidation of that cache are the monomorphiser's; a cache
built against an undesigned instantiation lifecycle would fix those by accident, which
is the outcome §9 asks to avoid.

The **near side** of step 4 does not touch this. Checking calls and operations against a
callee's resolved parameter and return sorts (capture), and inferring a concrete omitted
return by unifying its return sites, are equality over fully-typed signatures — no
instantiation, no cache, no inference variable. That work proceeds. This gap gates only
the generic engine step 4 opens after it: the inference variables, the requirement-set a
`Variable` accumulates, base-case-first recursion over the SCC, and the cache.

## What the design must settle

- **Trigger and identity.** What is an *instantiation* as a value — the tuple of
  argument sorts a body is monomorphised at? What mints one, and when, during a live run?
- **Lifetime and invalidation.** An edit to a generic body changes what its instantiations
  infer and emit. What discards or refreshes the instantiations — and the cached inferences
  keyed on them — already made?
- **The cache contract.** Given the above, the `(function, instantiation)` key, its
  eviction, and its relation to the requirement-set a `Variable` accumulates (`GENERICSII`
  §5; the slot `CHECKERSCOPINGRULINGS` Addition 2 shapes but leaves empty).

The successor builds capture and concrete return inference next, and stops at the engine
boundary for this.
