# The return-inference pass — how inference precedes checking, for validation

> **Ledger** — `[R]` A plan for the step-4 inference pass: the three-phase gather/infer/check shape that lets an omitted return sort be inferred before a call to it is checked, and the two design-adjacent hinges it leans on. Asks the designer to validate faithfulness to `MIDSESSIONDESIGN` and the recursion rulings.
> supersedes: none
> superseded by: none

**From:** the successor, at `2c6608a`, having built the concrete checker and the first
`4c` slice (`NeverAnswers`), and now planning the engine's ordering before cutting into
it.

The concrete checker is done: initializers, returns (written, and inferred far enough to
refuse a never-answering self-recursion), and calls read on both sides against
fully-typed signatures. What is left in step 4 all hinges on one thing the current
structure does not allow — inferring a function's return sort **before** a call to it is
checked. This plans that, and asks for validation of the two decisions in it that touch
`MIDSESSIONDESIGN` rather than only the code.

## §0 — the ordering problem

`Declare` builds the tree by `Scope`, which for one scope does everything in order:
builds its `Declarations`, reads its statements into `Reading`s, runs the checks
(`Annotations`, `Initializers`, `Returns`, `Arguments`, and call-as-value inside them),
publishes its signatures' sorts, then recurses into the scopes it contains.

A written return sort is resolved in `Declarations.Resolved` before any of this, so a
call to a written-return function is checkable whenever it appears. An **omitted** return
sort is not resolved — it is inferred from the body's return sites, which are only read
when that body's `Scope` runs. A call to such a function is checked during its **own**
scope's pass, which for a module-level call or a sibling body runs **before** the callee's
body. So today the callee's inferred sort is never available in time, and the call is left
uncompared (`var y => text = id 5`, `id` inferring `number`, is silent).

The fix is not a tweak to when a body is visited. It is that **inference is a distinct
phase between reading and checking**, over the whole tree, not interleaved with either.

## §1 — the three-phase shape

> **Gather → Infer → Check.**

- **Gather.** `Scope` keeps doing declarations, readings, signature publication, and
  recursion — but instead of running the checks inline it **records a context** per scope:
  `(declared, read, sorts, function, statements)`. Nothing else moves; the traversal and
  its order are unchanged.
- **Infer.** A new phase over the recorded function contexts computes every omitted return
  sort and stores it, before any checking reads one. §3.
- **Check.** A phase over the recorded contexts runs `Annotations`, `Initializers`,
  `Returns` (written), `Arguments`, and call-as-value — unchanged methods, now reading the
  fully-populated return sorts.

The checks are already pure functions of `(declared, read, sorts, function)` and add to
one finding collection, so extracting them is mechanical. The one invariant to hold is
**finding order**: contexts are recorded outer-first, exactly the order the checks run in
now, so the golden files do not move.

## §2 — what moves, and what does not

The **omitted**-return work now in `Returns` — unify the sites, report `DivergentReturns`
when they disagree, report `NeverAnswers` when they are all self-calls — **is** inference.
It moves into the Infer phase, where "the legality check is the inference pass"
(`RETURNANDLITERALS` §1c) becomes literal: the same walk stores the sort or reports why it
cannot. `Returns` in the Check phase keeps only the **written** case, unchanged. Every
other check is untouched.

## §3 — inferring: base-case-first as a bounded fixpoint

Each omitted-return function's sort is what its return sites agree on. A site's sort is
read by the existing `Inferred`, which resolves a call to another function's **stored**
return sort — so a function whose return is a call to a written-return, or to an
already-inferred, function resolves at once. The rest is ordering:

> Iterate. Each round, infer every not-yet-inferred function whose sites now all resolve,
> and store it. Stop when a round stores nothing.

A **non-recursive** chain drains in dependency order without the order being computed — a
function resolves the round after its callees do. A **recursive** group is what remains
when a round stalls: its members' sites include calls to each other that never resolved.
For those, **base-case-first**: a member with a site that grounds independent of the group
(a literal, a call outside it) takes that sort, which unstalls the next round; a group with
no such site is **unground** — `NeverAnswers` generalised from the direct self-call the
first slice already refuses to the whole group. The answer stored is always ground
(`MIDSESSIONDESIGN` §6): a variable never enters it.

The fixpoint is **bounded** — a fixed maximum number of rounds, exceeding it a finding, per
`MIDSESSIONDESIGN` §5's "a check mints a bounded number … never a hang." A stalled round
that is not a clean recursive group (a site deferred for an unimplemented reason — an
operation) leaves that function uninferred, not refused, exactly as the local check does
today.

## §4 — where the inferred sort lives *(for validation)*

The inferred sort is stored so calls read it. The natural place is the signature's
`ReturnSort`, beside where the written one already lands — the smallest change, and calls
already read it there.

But `MIDSESSIONDESIGN` §3 rules that invalidation is **one design, not two**, riding the
existing dependency graph's cutoff, and §4 that eviction counts demands. A sort stored on
the signature is a second store with no invalidation of its own — fine for a batch check,
wrong for the always-running premise, where an edit to a callee must re-infer its callers.

**The decision I am asking you to make:** for this pass, does the inferred sort live on the
signature (and gain graph-backed invalidation when the incremental story is built), or is
it from the start a value in the dependency graph the design already runs? §3 called the
coupling the programmer's, but tied it to one invalidation design — so I would rather you
confirm the store now than pick one that the invalidation work has to undo.

## §5 — fixpoint or the SCC you say already exists *(for validation)*

`RECURSIVERETURN` §3 says to solve over the recursive group (the SCC), and that "the
compiler already computes [it] to order everything else." I have planned a **fixpoint**,
which finds the same answer without naming the SCC — a stalled round *is* the recursive
groups, and base-case-first falls out of the round order. It is simpler and needs no SCC
construction.

**The decision I am asking you to confirm:** is the fixpoint an acceptable realization of
"base-case-first over the SCC," or is there an existing SCC/ordering computation (the
initialisation-ring or cascade machinery, or the runtime graph's) you intend this to reuse
rather than re-derive by iteration? If the former, I proceed as planned; if the latter,
point me at it and I build on it instead of the fixpoint.

## §6 — staying at 100% through a restructure

The restructure lands behaviour-preserving first, then behaviour is added on top, so the
gate is green at every commit:

1. **Split, as a pure refactor.** `Scope` records contexts; a new Check phase runs the
   same checks over them, in the same order. No finding changes. Gate green.
2. **Insert an empty Infer phase**, and move the omitted-return work from `Returns` into
   it — still local, still per-function, still the same findings. Gate green.
3. **Store** the inferred sort and let the fixpoint drain chains. Calls to omitted-return
   functions now check — new findings, new tests. Gate green.
4. **Recursion**: the group case, base-case-first, unground generalised, the bound.
5. Then monomorphisation, the `(function, instantiation)` key, eviction — the generic
   engine, which the `MIDSESSIONMONOMORPH`/`MIDSESSIONDESIGN` pair governs and which the
   `HOTRELOAD` gap sits beside.

## §7 — what I need validated

1. **§4** — where the inferred sort is stored, given §3's "one invalidation design."
2. **§5** — fixpoint versus an SCC computation you mean this to reuse.
3. That the gather/infer/check split (§1–§2) faithfully realizes the lifecycle, and that
   moving the omitted-return findings into the Infer phase (§2) is the right seam.

Everything else I take as settled and will build to as written.
