# The unresolved-return ruling holds — three additions and one scope correction

> **Ledger** — `[V]` verdict. Confirms `UNRESOLVEDRETURNRULING`'s (A). Corrects the
> scope of "any unresolved reading" to the callable's own body; adds a principled
> reason for the mutual exclusion; raises a defect visible in the audit's own
> control (`send return 5` is dead code); and qualifies §8's strictness argument
> with `otherwise`.
> answers: Budai's check of `UNRESOLVEDRETURNRULING`
> supersedes: `UNRESOLVEDRETURNRULING` §1.2, the scope of the suppression
> superseded by: none

**The ruling is right and I would have reached the same place.** (A) is correct,
the §2 impossibility argument is a proof rather than a shrug, and the tail-sugar
case in §3 is a genuinely good catch — `function f => number { nope }` attempts a
value-return with no `return` lexeme anywhere, which no token heuristic can ever
see. Nothing needs unwinding.

Four things to send after it. One is a correction; three are additions.

---

## §1 — the correction: "any unresolved reading" must mean **this callable's** body

`UNRESOLVEDRETURNRULING` §1.2 says `Unanswered` suppresses on *"a body containing
any unresolved reading."* As written that includes readings inside a **nested
delegate**, and `REAUDIT63` finding 2 has just moved exactly this boundary:

```ronin
  function f => number {
      var callback = () => { nope };      -- unresolved, inside the DELEGATE
      return;                             -- «f» genuinely does not answer
  }
```

Under the sentence as written, the delegate's unresolved reading suppresses **`f`'s**
`Unanswered`, and a true finding is lost. That is `REAUDIT63` finding 2's
contamination arriving on a second axis — return ownership was separated for
inference; suppression has to respect the same separation.

> **Suppression is per callable. A body's unresolved readings are its own, not
> those of the delegates nested inside it.**

Same rule as return-site gathering: a delegate is a callable and owns its readings;
an `if` or loop body is transparent and its readings belong to the enclosing
callable. Add a control for it — it is not in §7's table.

## §2 — a better reason for the mutual exclusion

§5 requires a body to emit the unresolved finding **or** `Unanswered`, never both,
and grounds it in noise. There is a stronger reason worth putting on record,
because it tells a future reader when the rule applies rather than just that it
does:

> **`Unanswered` is a claim about the *whole* body. A body with an unresolved
> reading has not been fully read, so no claim about its totality is available to
> make.**

That is why the exclusion is principled rather than a tuning choice, and it
generalises: every check whose subject is *the absence of something anywhere in a
body* has the same precondition.

## §3 — a defect sitting in the audit's own control: `send return 5` is dead code

`REAUDIT68` presents `function f => number { send return 5; }` as a **resolved
control that compiles cleanly**, and `UNRESOLVEDRETURNRULING` §7 keeps it as one.
Both are right that it resolves. But look at what it does:

```
  send return 5   ->   send (return 5)
                  ->   evaluating «send»'s argument EXITS f with 5
                  ->   «send» is never called
```

**The enclosing call is unreachable, statically and always.** That is not a
type error and no current check looks for it, so it compiles silent — and a reader
writing `send return 5` has almost certainly not written what they meant.

This is independent of everything the package asked about, and of §8's deferred
item. The rule is small and exact:

> **A `return (_)` in a strictly-evaluated argument position makes its enclosing
> call unreachable. That is a finding.**

Worth raising as its own item rather than folding in here — but it should not stay
buried in a control case labelled "compiles cleanly," because that label is
currently doing double duty for *resolves* and *is fine*, and only the first is
true.

## §4 — the strictness argument in §8 needs one qualification: `otherwise`

§8 argues, in favour of the deferred statement-initial restriction, that *"nested
`return` is additionally dead under strict evaluation … so the expressiveness given
up is arguably negative."* That is right for `send return 5` and it may be wrong
for the one operator that plausibly is not strict:

```ronin
  var total = sum otherwise return 0;
```

If `otherwise` short-circuits — evaluating its right side only when the left is
nothing or error — this is a **live guard idiom**, not dead code, and it is very
much this language's shape: the fallback that exits. A statement-initial
restriction would kill it.

So two things follow, and both point the same way:

- **it strengthens the case for leaving §8 deferred** — there is at least one real
  idiom at stake, which the "expressiveness given up is negative" argument did not
  account for; and
- **§3's finding must be scoped to strict positions**, or it fires on the guard.

**Whether `otherwise` short-circuits is a question about the tree**, and it should
be answered before either item moves. If it does not short-circuit today, that is
itself worth knowing — a fallback that evaluates both sides is a different operator
from the one the guard idiom needs.

## Summary

| | |
|---|---|
| the ruling | **holds.** (A) is right, §2's impossibility argument is a proof, and the tail-sugar case is a real catch. Nothing to unwind |
| **correction** | **suppression is per callable.** "Any unresolved reading" must exclude nested **delegates**, or a delegate's unresolved reading suppresses the enclosing function's true `Unanswered` — `REAUDIT63` finding 2 on a second axis. Add the control |
| **addition** | the mutual exclusion has a **principled** reason, not just a noise one: **`Unanswered` claims something about the whole body, and an unread body supports no such claim** |
| **addition** | **`send return 5` is dead code.** The audit's own resolved control: the `return` exits `f`, so `send` is never called. **A `return (_)` in a strictly-evaluated argument position makes its enclosing call unreachable — that is a finding.** Raise separately |
| **addition** | §8's *"nested return is dead"* needs qualifying: **`sum otherwise return 0`** is a live guard idiom **if `otherwise` short-circuits**. It strengthens the case for leaving §8 deferred, and it scopes the finding above |
| needs the tree | **does `otherwise` short-circuit?** Both items above wait on it, and if it does not, that is its own finding |
