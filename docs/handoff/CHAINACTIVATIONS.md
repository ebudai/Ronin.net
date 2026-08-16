# Chains have duration. `when`s don't. That's the whole confusion.

> **Ledger** — `[R]` Chains have duration. `when`s don't. That's the whole confusion.
> supersedes: not yet checked
> superseded by: not yet checked

Budai's instinct is right — the trouble is the one-activation rule — and his §5
is the right question. This is the explanation first, because it took me a
while to see it clearly, and then the resolution.

---

## 1. Why this is confusing

**A `when` is instantaneous.** It fires on a rising edge and its body runs to
completion inside one step. It cannot "double fire" because a condition can
only rise once per step. That is not a rule anyone wrote down; it falls out of
edge semantics.

**A chain has duration.** `when A { x; wait until B; y }` occupies time between
`x` and `y`. Anything with duration can be re-entered.

The one-activation rule is an attempt to make the chain behave like the `when`
it is spelled as — at most one in flight, like a body that runs to completion.
**Everything ugly follows from that pretence.** Restart-versus-ignore,
`waiting of X`, "clear *every* flag rather than just the first", `stop`'s
ambiguity — four faces of one attempt to hide duration behind a construct that
does not have any.

The author writing `when A and not waiting of A` is not expressing a policy.
They are apologising for the mechanism.

## 2. Why the fix is affordable — his §3, which is the real find

**No value crosses a `wait until`.** The segments are separate bodies, so a
`var` declared before a wait is out of scope after it. True today, by
construction, written down nowhere.

That is the load-bearing fact, and it is worth stating positively rather than
as a limitation: **an activation carries no data, so it has no frame.** The
live-edit objection in `WHEN-AND-WAIT.md` §5 — a suspended frame is state
produced by old code — was aimed at continuations. There are no continuations
here. A second activation costs a *number*, not a frame.

So the objection that killed coroutines does not apply to the thing we actually
built, and has been silently constraining it ever since.

## 3. Resolution: count activations, and delete the policy

- a chain flag becomes a **small count**, not a boolean
- **restart and ignore both disappear** — there is no policy to pick
- `waiting of X` disappears, and with it the only generated name an author had
  to type
- `stop` zeroes the count: the chain is over
- the first segment no longer clears anything — the subtle "clear all flags"
  case goes with the rule that needed it

An author who wants suppression writes it in **their own vocabulary**, using
state they almost certainly already have:

```ronin
when key pressed and not charging {
    charging = true
    charge
    wait until released
    charging = false
    fire
}
```

That is the point. Under one activation, the *uncommon* policy needed a
compiler-invented name. Under counting, both policies are expressible and
neither needs one.

### Why counting is not merely simpler — it is right more often

```ronin
when order placed { reserve stock; wait until payment cleared; ship }
```

Three orders placed, then payment clears:

| | reserve | ship |
|---|---|---|
| restart | 3× | **1×** — two reservations orphaned, silently |
| ignore | **1×** — two orders dropped, silently | 1× |
| count | 3× | 3× |

Only counting is right, and the other two fail silently. This is the example
that decided it for me.

## 4. The two costs, and why both close

**Accumulation.** A head firing faster than its tail completes grows the count
without bound, and he is right that the runaway detector counts rounds within a
step and would not see it.

Fix: **drain one activation per round.** Then `k` pending activations take `k`
rounds inside the step, and the existing round counter sees it — the detector
that already exists starts catching this for free, with the diagnostic it
already has. That also settles a question nobody had asked: `k` activations
running the tail in the *same* round would be `k` writes to the same cells in
one round, which the write model has no answer for. One per round makes each
activation a settled round, which is the model working as designed.

**Data smuggled across a wait via a module-scope `var`.** Under one activation
that works; under counting, activations clobber each other. This is the only
genuinely new hazard, and it is closed by the same §3 restriction — which now
needs to be a **diagnostic**, not just a sentence:

> A value cannot cross «wait until»: the wait ends this segment. If the value
> must persist, it belongs to the thing the chain is about — declare the `when`
> on a type, and the value as a member.

That message is doing real work. **Carrying data across a wait means the
activation has an identity, and an identity is an instance.** Module-scope
chains are fungible and countable; per-identity chains are type-scope `when`s,
where the position lives in the liveness mask and the data lives in member
arrays — which is exactly the machinery `INSTANCE-BINDING.md` already commits
to. The `order placed` example above *should* eventually be a type-scope `when`
on an Order; counting is what makes it behave sensibly until it is.

## 5. Two things to check that I could not

**Chain segments must count as one writer.** The suppression idiom above sets
`charging` in segment 1 and clears it in segment 2. Those are two `when`s after
splitting, so single-writer analysis will reject a program that is one `when`
in source. Whether it already treats a chain as one writer, I do not know —
if not, that is a prerequisite for §3's advice being followable.

**Naming gets cheaper, not harder.** With nothing generated ever typed, the
three names become parenthetical reports in `Fired` and diagnostics. They no
longer need to be prose, and the protected-word cost of the scheme goes away —
which is what his §5 predicted. A nameable `when` is still worth having, but now
only so the accumulation diagnostic can say *which* chain, which is a much
weaker requirement than "the author types this".

## 6. Confidence

This is design reasoning. Nothing here was run — the shape of the argument is
the same as the `wait` level-versus-edge call, and that one held up, but the
part I would most want tested is §4's claim that one-activation-per-round makes
the existing runaway detector sufficient. That is a claim about a detector I
have not read.

It is also a change to shipped behaviour, unlike everything else in this thread,
so it deserves the counterexample treatment before it goes in: try to find a
program where counting is worse than restart, and where the difference is not
the author having smuggled data through a module `var`.
