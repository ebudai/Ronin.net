# One activation per chain — is the rule buying enough?

> **Ledger** — `[R]` One activation per chain — is the rule buying enough?
> supersedes: not yet checked
> superseded by: not yet checked

Written at `1d98d86`, where `stop` and the `wait until` chain are built and the
naming scheme from `WAITSEMANTICS.md` §2 is in. Budai's observation, worked
through with the parts I could check by running.

Nothing here is a defect in the implemented behaviour — it does what
`WHEN-AND-WAIT.md` and `WAIT-SEMANTICS.md` say. §3 is a rule that is true today
and is written down nowhere, and it is the one thing here worth acting on
regardless of how the rest lands.

---

## 1. Where this came from

The three generated names (`waiting 1 of X`, `resuming 1 of X`, `waiting of X`)
were rejected in review as unreadable, and the last of them is the one an author
has to type:

```
when A and not waiting of A { … }
```

Trying to respell it did not help, because the problem is not the words. The
author is not saying *"and this compiler-invented flag is false"*; they are
saying *"do not restart this while it is still running"*. The intent is being
expressed as a boolean over the mechanism.

Budai's reading, which I think is right: **all of that complexity exists to keep
a chain to one activation.** Restart, ignore, "clear *every* flag rather than
just setting the first", and the `waiting of X` value are four faces of the same
rule. Remove the rule and all four go.

So the question is what the rule is worth.

## 2. What it costs to keep

| | kept |
|---|---|
| the author picks restart or ignore | a policy, per `when` |
| ignore is spelled as an expression | over a generated name — the rejected form |
| `stop` | one bit |
| the first segment clears *all* flags | the subtle case; a partial clear runs the tail twice |

## 3. What it is hiding — and this part is true today

**A local cannot cross a `wait until`, and nothing says so.**

The desugaring makes each segment a separate body. A `var` declared before a
wait is simply out of scope after it:

```ronin
when order placed {
    var reserved = reserve stock
    wait until payment cleared
    ship reserved                  ← «reserved» is not in scope here
}
```

`WHEN-AND-WAIT.md` §5.4 lists what splitting rejects — a wait inside a loop, an
`if`, a called function, or a `let` — and says nothing about this. It is the
same class of restriction and it arises from the same cause, so it belongs in
the same list.

This is worth writing down whatever else is decided, because it is a rule
authors will hit and there is currently no diagnostic and no sentence.

## 4. What it would cost to drop

**Per-activation state, which is the objection that has teeth.** With one
activation a flag is a boolean. With several, each activation needs its own
position *and its own locals* — which is a continuation with a frame, and a
suspended frame is live state produced by old code. That is the live-edit
argument from §5, and it is the reason the coroutine shape was refused in the
first place.

**Unbounded accumulation, unnoticed.** A chain whose head fires faster than its
tail completes grows activations without limit. The runaway detector counts
rounds *within a step*, so it would not see this: the graph settles cleanly each
step while the population grows. There is no natural place for it to be caught.

**`stop` needs an answer again** — this activation, or all of them? That is a
mask over activations, which is the machinery the change was removing.

What is *not* affected: cascade analysis is static and does not care how many
activations exist. Note also that the single-writer rule would not catch two
activations of one body writing the same cell, because at compile time that is
one writer.

## 5. The narrower question, which is the one worth answering

Not *"should chains support concurrent activations"*, but:

> **If a local cannot cross a wait anyway, what is the one-activation rule
> actually buying?**

A segment that cannot carry anything forward has no frame. If there is no frame,
a second activation costs a *counter* rather than a continuation — and most of
the §5 objection is about state that, by §3 above, does not exist.

If that holds, the shape is:

- a flag becomes a small count rather than a boolean;
- restart and ignore both disappear, along with the value an author had to name;
- `stop` clears the count;
- accumulation becomes the one new risk, and wants a bound with a diagnostic
  rather than silence.

If it does not hold — because locals crossing a wait is something the language
should support — then §3 stops being a restriction to document and becomes a
feature to design, and the one-activation rule is carrying real weight after
all.

Either answer settles the naming question underneath it, which is why this is
worth doing before the spellings: if nothing generated is ever typed, none of
the three names needs to be prose, they become parenthetical reports, and the
protected-word cost of the scheme goes away entirely.

## 6. Provenance

Checked by running, at `1d98d86`:

- two `when`s with the same condition cannot coexist — the second is refused
  with *"«when ready» is already declared … Rename one of them"*, and the
  condition is the name, so there is nothing to rename;
- merging two bodies under one identity turns a legal chain into a false ring —
  `Cascades` reports *"«when ready» → «when ready» is a cycle"* for a program
  where one body writes what the other reads;
- level semantics, the four cases from `WAIT-SEMANTICS.md` §1, including that
  the guard sees its condition *after* the segment before it.

Reasoned rather than run:

- that a local cannot cross a wait — it follows by construction, since the
  segments are separate bodies, but no source exercises it because the frontend
  is not joined to the runtime yet;
- that accumulation would escape the runaway detector.
