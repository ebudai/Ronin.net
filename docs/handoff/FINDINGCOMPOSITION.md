# `Unreachable` steps aside — but not for the reason the precedent gives

> **Ledger** — `[V]` verdict. In a `when`, a value-return in a strict argument
> position reports `AnsweringReaction` only; `Unreachable` is suppressed. States
> the general composition rule — **admissibility precedes behaviour** — and adds
> a precedent clause to `STANDINGAUTHORITY`'s ask-or-decide test.
> answers: the REAUDIT73 reaction-composition question
> supersedes: none
> superseded by: none

**Suppress `Unreachable`.** You reached the right answer; the reasoning you offered
for it does not hold, and the reasoning that does is worth having because it
settles the next case too.

---

## §1 — why the precedent does not transfer

`Unanswered` steps aside for `Unresolved` for an **epistemic** reason, stated in
the amendment §2:

> `Unanswered` is a claim about the *whole* body, and a body with an unresolved
> reading has not been fully read — so no claim about its totality is available to
> make.

That check is suppressed because it **cannot know its answer**. `Unreachable` is
not in that position at all: it can see perfectly well that evaluating `send`'s
argument exits the body first. Both findings are **true**, and they are about
different things — one says *this exit form may not appear here*, the other says
*this call never runs*.

So if the precedent were the only argument, both should fire.

## §2 — the reason that does hold: the second finding is strictly derivative

Apply the repair test. **If the user fixes the reported finding, does the other
survive?**

```
  Unresolved -> Unanswered      fix «nope», and Unanswered MAY still fire
                                («{ nope; return; }» is still unanswered)
                                -> suppression DEFERS a finding

  AnsweringReaction -> Unreachable   a «when» body cannot legally contain a
                                     value-return at all, so EVERY repair removes
                                     the return — and with it the unreachability
                                     -> suppression removes nothing
```

This case is **cleaner** than the precedent, not weaker. There is no repair of
`AnsweringReaction` that leaves `Unreachable` standing, so reporting both is
reporting a cause and its own consequence as peers.

## §3 — the general rule, so the next case does not come back here

> **A finding about what a construct *does* is suppressed by a finding that the
> construct *may not be there*. Admissibility precedes behaviour.**

That covers both suppressions with one sentence, and it predicts others — a type
error on an expression should suppress a dead-code finding about the same
expression, for the same reason.

And it keeps `Unreachable` firing where it earns its keep:

```ronin
  function f => number { send return 5; }   -- the return is LEGAL here
```

Nothing is inadmissible, so nothing suppresses; the unreachability of `send` is
the entire content of the finding, and it must fire. **The `when` case is the
carve-out; the function case is the rule.**

## §4 — one thing to check while you are there: the span

You report both findings *on the same span*. That is worth a look, because
`Unreachable`'s subject is **the call that never runs**, not the return that
prevents it:

```
  send return 5
  ^^^^              <- Unreachable: «send» is never called
       ^^^^^^^^     <- AnsweringReaction: this exit form may not appear here
```

Those overlap but are not the same span. If the implementation anchors
`Unreachable` at the return, its subject and its span disagree — the same defect
class as `DivergentReturns` blaming the wrong participant, where the role was
chosen before the finding existed. Suppressing in the `when` case hides it rather
than fixing it, and the function case above will show it plainly.

## §5 — and a clause your instinct earned for the standing rule

`STANDINGAUTHORITY`'s test was *"ask when a wrong answer cannot be recovered by
the next reader."* By that test this was yours: a diagnostic composition is
reversible and pinned by tests, and nobody is permanently misled.

But you were right to bring it, and the rule should say why:

> **Also ask when the decision sets a precedent — even where the instance is
> reversible.** The instance here is one pair of findings; the answer is a rule
> about how every future pair composes, and a rule established silently is one
> nobody can cite.

That is a genuine addition to the three-item list, not an exception to it.

## Summary

| | |
|---|---|
| the ruling | **suppress `Unreachable`** in a `when`; report `AnsweringReaction` alone |
| not for the precedent's reason | `Unanswered` steps aside because it **cannot know its answer**. `Unreachable` knows perfectly well — both findings are true |
| the reason that holds | the second is **strictly derivative**: a `when` body cannot legally contain a value-return, so **every repair removes both**. Cleaner than the precedent, where suppression only defers |
| **the general rule** | **admissibility precedes behaviour** — a finding about what a construct *does* is suppressed by a finding that it *may not be there*. Predicts the next case (a type error should suppress dead-code on the same expression) |
| what still fires | `function f => number { send return 5; }` — the return is **legal**, nothing suppresses, and the unreachability is the whole finding. **The `when` case is the carve-out** |
| **check the span** | `Unreachable`'s subject is **the call that never runs**, not the return. Same span for both suggests the anchor is wrong — and suppressing here **hides** that rather than fixing it |
| **standing rule** | add a clause: **also ask when the decision sets a precedent**, even where the instance is reversible. The instance was small; the rule it establishes is not |
