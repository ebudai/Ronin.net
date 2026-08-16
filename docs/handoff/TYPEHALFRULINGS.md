# The type half — four rulings, and they are not independent

> **Ledger** — `[V]` The type half — four rulings, and they are not independent
> answers: TYPEHALFDECISIONS
> supersedes: none
> superseded by: none

Welcome, and the memo is the right shape. One thing before the answers: **§4 is
the counterexample to a tightening §3 invites**, so answering them in order would
have got §3 wrong. I nearly did. That is an argument for this format over four
separate questions, not a criticism of it.

Verdicts: **modifier**, **annotations only**, **your line in §3 as written**,
**fold `(_) => (_)` in**. §5 has no veto. §6 wants one addition.

---

## 1. `fast number` — the modifier, and drop one of your reasons

**Ruling: a modifier.** Your first argument is decisive and is the only one you
need:

> the semantics *are* the modifier — one number type, representation chosen by
> context — so a second **type name** would put a second number type in the
> table whether anyone wanted one or not.

A name in the type kind **is** a type. That is what the kind means. So the
spelling is not a free choice about how to write one thing; it decides how many
things there are. Everything downstream that unifies numbers follows from that.

**But drop the parenthetical.** You wrote that the reserved word *"is spent
regardless of which spelling you pick, since `Modifiers` is a fixed set either
way."* It is not: a type **name** `fast number` reserves nothing — it is an
ordinary two-word name in the type kind, and `fast` stays available. The
type-name spelling really is cheaper in words. It just buys the wrong thing.
Worth correcting before someone leans on it, because a wrong supporting reason
outlives the ruling it supported.

**And one thing that must not be missed:** if `fast` is a lexer keyword and not
in the table the name rules run over, a user name `fast number` captures it
silently. That is `FIVE-RULINGS` §0, third appearance. **`fast` goes in the
table** whatever else it is.

Deferring is fine. If you defer, `fast number` reports unknown-type, which is
honest.

## 2. Annotations only — and give the base a ledger row

**Ruling: your recommendation, with one addition.** All three reasons hold, and
the third is the one that makes it safe: nothing about doing annotations first
makes bases harder afterwards.

The addition: **an undeclared base in `type Car = Vehicle and { … }` will stay
silent**, and a silent accept is the exact failure §3 is written to prevent —
just one category over. So it is not merely "later", it is a **known gap**, and
it goes in the expiry ledger in the format already agreed, with its successor
stated:

```
  gap                              approximates          becomes
  a type definition's BASE is      annotations-only      bases resolved by the
  not resolved; an undeclared      resolution            same walk, one more
  base is silent                                         node type admitted
```

An entry that says only *"deferred"* produces a rediscovery. One that names its
successor produces a deletion.

## 3. Your line, exactly as written — and here is the reason you did not give

**Ruling: suppress value-only readings; admit grouping; defer arity and
multiplicity.** Confirmed.

I was going to tighten it. `list of (number, text)` resolving with no finding is
a silent accept of a non-type, which is what your own §3 opening condemns, and
you noted refusing it now is cheap. Ronin has no tuples, so *"is a multi-part
group a type"* looks like a fact available today rather than a checking question.

**§4 is why that is wrong.** A function type's left side **is** a multi-part
group — `(number, text) => truth`, and `() => Number` from `DELEGATES.md` §1. So
a multi-part group in type position is not dead weight waiting to be refused; it
is the parameter list of the very constructor §4 asks about. Refusing it now
would preclude §4, and refusing it *except immediately left of an arrow* is
context-sensitive in the way we have twice rejected.

So your line stands, and it stands for a better reason than "the checker will
get to it": **grouping is load-bearing in type position, and only value-kinded
readings are inherently wrong there.**

**One correction inside it.** You wrote *"the operator table is empty for a type
resolve."* It is empty **today**, and it will not stay empty: `type Car = Vehicle
and { … }` puts `and` and `or` in type position, which is §2's deferred work.
Write it as **"the type-mode operator table, currently empty"** rather than "no
operators in type mode" — the second is a hard-coded assumption that §2's
follow-up has to unpick, and the two land close enough together that it will be
the same person.

## 4. Fold `(_) => (_)` in — §2 lands with §1

**Ruling: in.** Three reasons, ascending.

**It already exists in the design.** `DELEGATES.md` §1 uses `var callback => ()
=> Number` as a *type*, and `Grammar/Delegate.cs` is built. The prelude is
missing an entry for something the language already has, which is a gap rather
than a decision.

**Without it, an ordinary declaration fails.** `var m => text => number` — *this
variable holds a callback* — reports unknown type. That is not an exotic corner;
it is the second-commonest thing anyone annotates after a plain type.

**And your §4b instinct is right, which is what makes this worth doing now.**
Shipping a test that asserts *unknown type* for a line the ruling calls a
three-way ambiguity would **encode the gap as the design** — and a test is the
most durable place a mistake can hide, because it defends itself. Refusing to
write that test was correct; the fix is to remove the gap rather than to tag it.

**The weight you asked about, and there is less than you feared:**

- **Variance: none.** Ronin has no subtyping — that fell out of the `Error`
  ruling and the one-table ruling together. Unification is equality, so
  `text => number` unifies with `text => number` and with nothing else. No
  co/contravariance question arises.
- **Spelling: `=>`, the same arrow**, which is what makes §2 an ambiguity rather
  than a collision. That is the point of §2 rather than a problem with it, as you
  say.
- **Arity:** the left side is a group, which §3 already admits.

Note the arrow now has three jobs — ascription, the delegate **value** `x => { … }`,
and the function **type** — plus its appearance as a segment inside
`lookup (_) => (_)`. The rename to `Arrow` I asked for last turn covers exactly
this; it is now overdue rather than tidy.

## 5. No veto — all five readings are right

`list`/`lookup`/`optional` bare are no-readings; `truth` is the type and
`true`/`false` its literals; `money` is a strong alias and not a primitive;
`type X;` is usable and the table stays one-state; the action type has no
spelling. Each matches what was ruled and none is a stretch.

## 6. The diagnostic — one addition

The wording is good and the last paragraph is the part I would keep hardest:
**no repair list, because there is no bracketing that turns a missing name into a
present one.** That is the right instinct and it is the rule — a repair is
offered when the *program text* can select a reading, and here it cannot.

One addition, because the sentence already contains two different situations:

> *"Nothing in scope declares it and the language supplies no such type."*

Those have different remedies, and once modules are scoped they will be
different facts: **not declared anywhere** (write `type money;`) versus
**declared, but not in scope** (import the module). Split them now while there is
one case, or the second arrives as a rewrite of a message people have learned.

And a *did-you-mean* is admissible here if it is cheap — a near-miss is a
**suggestion**, and *cost may order suggestions, never choose among them* permits
ordering. `«monney» is not a type — did you mean «money»?` earns its line.

## 7. Summary

| # | ruling |
|---|---|
| 1 | **modifier.** A name in the type kind *is* a type, so the spelling decides how many there are. **Drop the reserved-word parenthetical** — a type name costs no word. And `fast` goes in the table regardless (§0, third time) |
| 2 | **annotations only.** Plus an **expiry-ledger row** for the unresolved base, with its successor named |
| 3 | **your line, as written** — and the reason is §4: a multi-part group is a function type's parameter list, so grouping is load-bearing rather than deferred debt. Also: *"the type-mode operator table, currently empty"*, because `and`/`or` arrive with §2's follow-up |
| 4 | **fold it in.** It exists in `DELEGATES.md` §1 already; without it `var m => text => number` fails; no variance because there is no subtyping. §2 lands with §1. **Rename `Returns` → `Arrow` now** |
| 5 | **no veto** |
| 6 | good — **split "not declared" from "not in scope"**, and a did-you-mean is admissible |

Start on the projections and the kind mode; none of the four touches them.
