# Monomorphisation closes the residue — and one correction that is live right now

> **Ledger** — `[V]` Monomorphisation closes the recursion residue — ruled. The checker rule it implies is a recommendation.
> supersedes: not yet checked
> superseded by: not yet checked

He is right, and the residue is even smaller than he says. But the first section
is time-sensitive: **one sentence in his own restatement is the version that
loses information**, and if he started before `RECURSIVE-RETURN.md` landed it may
be what is being written.

---

## 1. Read this first — `check` is the wrong verb

> *"Infer from the returns that don't go through the function, then **check** the
> recursive ones against that."*

Measured, that is the version that fails:

```
  function      BASE + CHECK           BASE + UNIFY
  factorial     number                 number                  same
  collect       list of ?              list of number          ** differs **
  find first    optional of ?          optional of number      ** differs **
```

`collect` starts `if n <= 0 { return empty list }`. The base case's own type is
**under-determined** — `list of ?` — and the site that pins the element type is
the *recursive* one. Check the recursive sites against the base and the function
publishes `list of ?`. Unify them in and it publishes `list of number`.

An empty accumulator is how a large share of recursive functions begin, so this
is the common case, not a corner. **The recursive sites must contribute
information, not merely be validated.** Everything else in his restatement is
right; this one verb is not.

Two smaller amendments from the same document, in case they arrived after the
code did:

- **the answer must be ground when solving finishes** — a plain solve *succeeds*
  on `function loop (x) { return loop (x) }` with the answer variable still
  unbound, and an unbound answer is not an answer;
- **it is the recursive *group*, not the function** — with `f` returning `g (n)`
  and `g` holding the base case, a per-function rule refuses a well-typed program,
  and refuses it depending on which function the compiler reaches first.

His own reframing — *the condition is "no type-independent return", not
"recursive"* — is the same finding as the third one and is better phrased than
mine was.

## 2. The monomorphisation argument holds, and cuts deeper than stated

Tested rather than agreed with, because the claim has a seam: *"calls itself at a
different type"* and *"generates infinitely many instantiations"* are not the
same set, and only the second is caught by the monomorphiser.

```
  monomorphic recursion           1 instantiation    closed
  polymorphic, but finite         2 instantiations   closed
      f (T) calls f (number) -- a DIFFERENT type, and it bottoms out
  polymorphic, nested datatype   16 and growing      STOPPED at the depth limit
      f (T) calls f (list of T)
  mutual, nested                 16 and growing      STOPPED at the depth limit
```

Row two is the correction to *my* framing. **"Polymorphic recursion" was never
the residue** — a function that calls itself at a different type but bottoms out
instantiates in two steps and the monomorphiser does not even notice. So
`RETURN-AND-LITERALS.md` §4 named a set larger than the problem, which is the
same error as the rule it was trying to shrink.

Row three is the only real case, and what happens to it is **not a type error**.
It is an instantiation chain that never closes, stopped by the depth limit the
monomorphiser must have anyway, with the message *"this instantiates forever"* —
which tells the author what they actually did.

**So the type rule's residue is empty.** The only rule left is his: *a function
needs a written answer type when no return site is independent of the recursive
group.* Withdraw §4 of `RETURN-AND-LITERALS.md`.

Same arc as the previous five, and he is right to name it as such. What I would
take from the sixth is narrower than "be less careful": **the blanket rule and
the narrow rule usually differ by a quantifier, and finding it is a probe, not a
judgement call.** Both times here the correct set was a strict subset of the one
I named, and both times a fifteen-line model found it.

## 3. Two operational consequences that arrive with this

Neither is a correctness issue and both are cheaper now than later.

**The depth limit is load-bearing, and it is needed before the first generic
recursive function is written.** Without it, row three is not an error — it is a
hang. In a batch compiler that is an annoyance; in a language whose premise is
that the IDE is always running and debug == development, a compiler that hangs on
a keystroke is a far worse failure than one that reports something. Rust hits
exactly this and answers it the same way, with a named recursion limit. The
number matters much less than its existing.

**Inference now runs per instantiation rather than per function.** That is what
"within an instantiation the types are fixed" buys, and it is also what it costs.
For a heavily generic standard library, re-inferring on every keystroke is the
first thing that will get slow. The mitigation is cheap and worth building in
from the start: **cache the inference result keyed by (function, instantiation)**
— the same key the monomorphiser already maintains to avoid emitting duplicates,
so it is a second value in an existing table rather than a new one.

## 4. `return` for the valueless exit — taken, and here is the sentence that closes it

Budai's call, and I have no argument against it: one word, one reservation
already paid, and every reader already parses `return;` correctly. `done` is
withdrawn.

What I flagged was that `stop` and `return` then sit next to each other with
similar shapes and different permanence. That does not need a rule, it needs each
reference entry to name the other. So it does not stay a TODO:

> **`return`** — ends the current body. In a function that answers, write
> `return (the answer)`. In an action or a `when` body there is nothing to
> answer, so write `return` on its own. To stop a `when` from firing *again*,
> see `stop`.

> **`stop`** — disarms this `when`, so it will not fire again. To end only the
> current run and leave the `when` armed, see `return`.

Each points at the other, which is the whole of what prevents the confusion.

## 5. Summary

| | |
|---|---|
| "then **check** the recursive ones" | **unify, not check** — otherwise `return empty list` publishes `list of ?`. Live now |
| ground answer, recursive group | the other two amendments, in case they arrived after the code |
| "no type-independent return", not "recursive" | **his phrasing is better than mine** and is the same finding |
| monomorphisation dissolves polymorphic recursion | **yes** — and the residue is smaller still: finite polymorphic recursion just works |
| the real case | nested datatypes, and it surfaces as *"this instantiates forever"*, not a type error |
| `RETURN-AND-LITERALS.md` §4 | **withdrawn** |
| depth limit | needed **before** the first generic recursive function — otherwise a hang, in an always-running IDE |
| inference cost | per instantiation now. Cache on (function, instantiation) — an existing key |
| `return` vs `done` | **`return`.** `done` withdrawn; the two reference sentences above close the flag |

Still waiting on: the `Scope.Invoke` answer — the version of `NEEDFROMDESIGN.md`
I have stops at §3, so its §4 has not reached me.

Probes: `monomorph_recursion.py`, `recursive_infer.py`.
