# `() => …` — the narrowing is right, and it opens the question underneath

> **Ledger** — `[V]` `() => …` — the narrowing is right, and it opens the question underneath
> supersedes: none
> superseded by: none

Accepting the fix, with one thing that now has to be answered and one prior
item it collides with.

---

## 1. The narrowing is correct, and my rule was overbroad

I wrote "Ronin has no parameter lists" in `EMPTY-BRACKETS.md`. That was too
strong, and the audit was right to call it a contradiction. A delegate has a
signature, so Ronin does have parameter lists — just not in the place the rule
was about.

The principled version of the distinction, which is what makes it a rule rather
than an exception:

> A bracket means **hole** where a *call-site shape* is being declared. It means
> **signature** where a *callable value's type* is being described.

Those are different kinds of thing. `function send (message) to (recipient)`
declares syntax; `var callback => () => Number` describes a value. The bracket's
meaning follows the kind, not the character. "Applies to names" is the same rule
stated shorter, and it is fine to keep it that way in the message.

So `() => …` stays well formed and no lambda syntax changes. Agreed.

---

## 2. What that forces: how is a zero-argument delegate invoked?

This is the part worth deciding now rather than at the first use site.

If `() => Number` is a legal type, something holds one, and then something reads
it. Two possibilities, and only one of them is consistent with what we just
decided:

**Reading the name invokes it.** `callback` evaluates to a `Number`. No call
syntax, nothing new.

**Explicit invocation.** `callback ()` or similar. This reintroduces exactly the
C-shaped call syntax we refused two documents ago — the whole reason
`function ping ()` is ill-formed is that `ping()` would then be expected at the
call site and cannot work. Admitting it for delegates would make the language
inconsistent with its own diagnostic.

So the answer has to be the first: **reading a zero-argument delegate invokes
it.** Which is worth saying out loud, because it has a consequence.

## 3. Which collides with something already open

A zero-argument delegate whose read invokes it is **a `let` you can pass
around.** Deferred computation, evaluated on read, no arguments — that is the
definition of a `let` cell.

Two things follow, and both were already on the open list rather than being new:

- **Higher-order cells.** A delegate stored in a `var` or `let` is a cell whose
  value is a computation, which is the case the failure-modes list flagged for a
  prohibition decision and never resolved. `() => …` being well formed means the
  language has them whether or not that decision was made.
- **Is the zero-argument case distinct enough from `let` to keep?** If reading
  invokes, then `var thunk => () => Number` and `let value => …` differ mainly
  in whether the computation is a first-class value. That may be exactly what is
  wanted — passing a computation is useful — but it should be a decision, not a
  by-product of the lambda grammar admitting an empty signature.

I would not resolve either of those now. The useful step is to record that
`() => …` being well formed puts them in scope, so the next person to hit a
higher-order cell finds the connection rather than rediscovering it.

## 4. What I would actually do next on this

Nothing in the compiler. One line in the spec next to the `EmptyHole` text:

> Reading a zero-argument delegate invokes it. There is no call syntax; a
> delegate is read like any other name.

That closes the inconsistency the audit was pointing at, and it is the only part
of §2–3 that is forced. The rest can wait for a use site.
