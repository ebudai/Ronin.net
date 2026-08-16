# Type-scope `when` — he is right, §1 is wrong, and §4 is wrong too

> **Ledger** — `[V]` Type-scope `when` — he is right, §1 is wrong, and §4 is wrong too
> supersedes: WHENANDWAIT §1, §4
> superseded by: none

Correcting `WHEN-AND-WAIT.md`. Read at `c8975eb`; `stop` is not implemented
yet, which matters for §3 below.

---

## 1. Not building it was the right call

§3 of that document said the instance-binding decision *"must be made before
instances are built"* and then §1 listed type scope as though it already
worked. Both cannot be true. Deferring the feature rather than half-building
the semantics under a parser change is exactly what §3 asked for.

**§1 is amended:** a `when` may be declared at **module scope** today. Type
scope is designed, blocked on the instance-binding decision, and not
implemented. The rule that a `when` may not be declared inside a function or
block is unaffected — that one stands on its own reasoning about lifetime and
is unrelated to instances.

## 2. But `unexpected input` is not an acceptable message

For a language whose diagnostics are the teaching mechanism, "this construct is
designed and not yet implemented" and "I have no idea what you wrote" must not
look the same. A user who writes a type-scope `when` has understood the design
correctly and gets told they made a syntax error.

**Parse-to-diagnose is not half-building.** Letting the member aggregate
recognise a `when` in order to reject it with a typed finding adds no
semantics, no node, no lifetime — it adds a message:

> A «when» inside a type is not implemented yet. It requires the instance
> binding model, which is not built. Declare it at module scope, or track the
> instance explicitly.

That is the standard move for known-but-unimplemented syntax, and it keeps the
"unexpected input" path meaning what it says. The distinction worth holding
onto: **recognising a construct in order to refuse it well is diagnostics;
giving it a lifetime is semantics.** Only the second is blocked.

---

## 3. The consequence I missed — §4 of that document is wrong for type scope

This is the part worth having before instances exist, and it is why the
question is worth answering now rather than when the evaluator lands.

`WHEN-AND-WAIT.md` §4 says of `stop`:

> **It must actually remove the node**, not set a disabled flag.

That is right for a module-scope `when`, and **wrong** for a type-scope one
under the struct-of-arrays model §3 recommends. Under SoA there is *one* node
per `when`, evaluating a vectorised predicate across N instances. `stop` in its
body means *this instance is done*, not *nobody gets this behaviour again*.
Removing the node would stop it for every instance — a bug that is very hard to
see, because the code that stopped is correct and the instance that breaks is a
different one.

So the two scopes want different mechanisms, which is a bad place to end up.
The unification:

> **Every `when` carries a liveness mask. Module scope is the one-element case.
> `stop` clears the caller's bit. The node is removed when the mask empties.**

That keeps the §4 property that mattered — a stopped `when` really goes, rather
than lingering as a disabled node costing an edge walk — while making
per-instance `stop` mean what it reads as. It also keeps the cascade argument
intact: `stop` can still only shrink the graph, so a statically legal program
stays legal and `Cascades` needs no dynamic analysis.

**This is free to decide right now.** `stop` is not implemented — the only
matches in the source are two words in diagnostic prose. Deciding it after a
module-scope-only `stop` ships would mean rewriting it, which is the same
retrofit trap he correctly refused to walk into on the parser side.

---

## 4. So the blocking decision is one decision, not two

The instance-binding choice — per-instance nodes versus one cell per member
holding N values — now determines three things rather than one:

| | per-instance nodes | one cell per member |
|---|---|---|
| graph size | grows with the world | grows with the source text |
| cost | ~20× on `econ_sim.c` | the measured baseline |
| `stop` | node removal works | needs the liveness mask |
| `when` predicate | N nodes | one vectorised evaluation |

I still recommend the second, for the reason the benchmark gave: the win is
edge-chasing and cache behaviour, not arithmetic, so it does not come back with
tuning. But the honest statement is that it is one decision with a widening
blast radius, and every week it stays open it acquires another dependent.

Nothing above needs building now. What I would do is put §3's liveness-mask
sentence in the spec next to `stop`, so that whoever implements `stop` first
does not have to rediscover it.
