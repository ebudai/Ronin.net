# A bare nullary name is a **call** — and `TryPattern` is the wrong gate

> **Ledger** — `[V]` verdict. Rules a bare nullary reference a call, files a
> nullary function's `Signature` in `Overloads` by routing on member kind rather
> than on hole count, confirms finding 4 follows, refuses nullary overload sets,
> and names the systemic defect the last three findings share.
> answers: `NULLARYSIGNATURE`
> supersedes: none
> superseded by: none

**Q1: a call. Q2: `Overloads` — but the routing bug is the real finding. Q3:
confirmed.**

And the thing worth more than any of them: **the last three findings are one
defect wearing three costumes.** §4.

---

## §1 — Q1: a bare `f` is a call, and the dilemma is not real

Three independent arguments, and the first is the language's whole premise.

**The prose reading.** A reader meeting

```ronin
  var now = current time;
```

reads *call `current time`, keep the answer*. Nobody reads it as *keep the
function*. Likewise `refresh;` as a statement means the refresh happens. A
language whose stated trade is readability at the cost of writability does not get
to take the other reading.

**The "loss" is not a loss, because it is already true at every arity.** You
frame it as: *if `f` is the call, there is no way to name the function value.* But
there is no way to name a **shaped** function as a value either — `use` alone is a
partial pattern, not a name, and nothing in the language spells *the function
`use`*. A function is not a value in Ronin today; a **delegate** is, and a delegate
is written as a literal. So reading a bare `f` as a call keeps nullary functions
**consistent** with shaped ones. Reading it as a value would create the exception —
the only arity whose declared function can be named as a value.

**And the reservation machinery presupposes it.** The nullary reservation exists
because *"«var return» was declarable, and then every bare «return» in scope read
two ways with no bracket able to separate them."* If a bare `f` could be either a
call or a value, **that is two readings again**, with no bracket to separate them —
the exact condition the reservation was built to remove. The mechanism only makes
sense under "call."

### The residue, ledgered rather than solved

Passing a named function as a callback is ordinary in a RAD language, so a
function-as-value spelling may be wanted eventually. Two things about it:

- it is **not blocked by this ruling**, because it does not exist for any arity
  today; and
- **a bare name cannot be that spelling**, since it has to work for shaped
  functions too, and their names are not complete.

```
  approximation                     successor                     trigger
  a declared function cannot be     an explicit function-value     the first stdlib or
  named as a value; delegates       spelling that works at every   user need to pass a
  are the value form                arity                          named function
```

## §2 — Q2: `Overloads` — and the fix is to stop routing on hole count

`Overloads`, keyed by the nullary pattern `[f]`, **and** the name reservation it
already has. Those were never in tension: the reservation stops anyone declaring a
competing `f`, the signature carries the type. A nullary function is both.

But filing it is the smaller half. The actual defect is the gate:

```csharp
  if (member.Identifier.TryPattern(out var pattern, out var blocks) is false)
  {
      Cell(member);   // the value-declaration path
      return;
  }
```

**`TryPattern` asks whether the identifier has holes, and uses the answer to
decide whether the member is a function.** But the member already *is* a
`Grammar.Function` — it says so. Hole count is a **proxy**, exact only while every
function happens to have at least one parameter, and `EMPTYBRACKETS` guaranteed
that some would not.

> **Route by what the member is, not by the shape of its name.** A
> `Grammar.Function` files a `Signature` whatever its arity; `[f]` is a perfectly
> good zero-hole pattern and `Overloads` is keyed by pattern.

That is why "the two are not reconciled" — nothing reconciled them because the
routing never asked the question whose answer would have.

### One thing to refuse while you are in there

`Overloads` keyed by `[f]` admits a set, and the guide says the return type
participates in overload resolution. So two nullary `f`s differing only in return
type would be a legal overload set — and **nothing at the use site distinguishes
them.** A reader seeing `f` has no argument to read and no cue at all; which one
runs depends on an annotation somewhere else.

Refuse it: **a nullary overload set larger than one is an error.** Same family as
`01-02-03` and two same-named types in one function — one spelling, several
readings, no cue at the place a person is reading.

## §3 — Q3: confirmed, and you are not solving it twice

With Q1 = call and Q2 filing the signature, a nullary function with no
value-carrying return has the **action sort** as its answer, and
`var x => number = f` is the `ActionInValue` finding you already built. No change
to the action machinery.

Worth being explicit that finding 4 was **two** defects stacked:

```
  (i)  Infer never CONSTRUCTS Action            -> you cut this; it fires for «f 5»
  (ii) a nullary function has no signature      -> this ruling; the bare witness
```

Fixing (i) could never have fired the bare witness, and that is why the finding
looked unresponsive to a correct repair.

## §4 — the thing that matters more: three findings, one defect

Look at what the last three findings actually are.

```
  SymbolTable.Truths      derived from  «has no shape»       proxy for «is a boolean»
  Compilation.Inferred    ignores       Descriptor.Denotes   re-derives, gets null
  Declarations.Declare    routes on     TryPattern           proxy for «is a function»
```

Three sites, one shape. In every case **the fact is declared somewhere and a
consumer re-derives it from a structural stand-in** instead of reading it.

> **Where a declaration states a fact, every consumer reads that fact. A consumer
> that re-derives it from shape has created a second source of truth, and the two
> drift the moment a third case appears.**

That is the same argument as refusing a denormal to catch an overflow, and the
same as `[V/R]` being a union encoded in a token. It is worth a sweep rather than
three repairs: **grep for every place a property is inferred from structure when
the declaration already carries it.** The audit's own recommendation on finding 1
— *"the registry must remain the authority"* — is this rule stated for one case.
It generalises, and there is likely a fourth.

## §5 — two notes on the audit, since both touch my rulings

**Finding 2 is right and does not contradict H-wide.** You may hesitate here
because delegates are transparent under the scope-identity ruling. They are —
**for named-type container identity**. Return ownership is a different axis. A
delegate is a *callable*, so its returns are its own; a delegate is not a *named
container*, so a type declared inside it still belongs to the enclosing function.
Both hold at once, and the audit's phrasing — *"they are not transparent
control-flow blocks"* — is the correct reading. Give delegates their own return
owner without worrying that it reopens anything.

**Finding 5 is the residue of an under-specified fix of mine.**
`INFERENCEPASSVALIDATION` §4 said to order findings by source position, and that
fixed **presentation**. It could not fix **roles**: which return is *established*
and which is *blamed* is chosen during inference, before a finding exists. Sorting
a collection cannot reorder a decision already taken. So sort a callable's `Site`s
by offset **before** inference, as the audit says — and the general form is worth
keeping: *ordering the output does not order the decisions that produced it.*

## Summary

| | |
|---|---|
| **Q1** | **a call.** `var now = current time` reads as *call it*, and a language trading writability for readability does not get the other reading |
| the dilemma | **not real.** No arity can name a declared function as a value today — `use` alone is a partial pattern. "Call" keeps nullary **consistent**; "value" would make it the exception |
| and | the nullary **reservation presupposes it** — it exists to stop a bare name reading two ways, which is exactly what call-or-value would restore |
| ledgered | a function-as-value spelling may be wanted later. **A bare name cannot be it**, since it must work for shaped functions too |
| **Q2** | **`Overloads`, keyed by `[f]`, and the name reservation — both.** They were never in tension |
| the real finding | **`TryPattern` is the wrong gate.** Hole count is a **proxy** for *is a function*, and `EMPTYBRACKETS` guaranteed it would go stale. **Route by member kind** |
| refuse while there | **a nullary overload set larger than one.** Return-type-only overloads have **no cue at the use site** — `01-02-03` in another costume |
| **Q3** | **confirmed** — no change to the action machinery. Finding 4 was **two** defects stacked; you cut one, and it could never have fired the bare witness |
| **§4 — the sweep** | `Truths`, `Inferred`, and `Declare` are **one defect three times**: a consumer re-deriving a declared fact from a structural proxy. **Sweep for the fourth** |
| the rule | **where a declaration states a fact, every consumer reads that fact** — re-deriving it from shape creates a second source of truth that drifts |
| audit finding 2 | **right, and it does not contradict H-wide.** Delegates are transparent for **named-type container identity**; **return ownership is a different axis** |
| audit finding 5 | **my fix was under-specified.** Ordering findings fixed presentation, not **roles** — those are chosen during inference. Sort `Site`s by offset before inferring. *Ordering the output does not order the decisions that produced it* |
