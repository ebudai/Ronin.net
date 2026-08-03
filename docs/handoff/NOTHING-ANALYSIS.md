# `nothing` — what is already true, and seven things to settle

Analysis of `docs/spec/NOTHINGANDINDEXING.md` §1. Everything below is measured
against the compiler unless marked otherwise. Nothing implemented.

## 1. Most of §1.1 is already true, including the important one

| §1.1 | state |
|---|---|
| `nothing` is a value, not a null pointer | **true** — `Nothing.Instance`, a value like any other |
| `otherwise` catches `nothing` and `Error` | **true and tested** — one predicate, `Replaces` |
| `nothing` does not propagate silently through arithmetic | **true**, and more broadly than stated — see below |
| `optional T` is the type | no type checker exists |
| an optional parameter defaults to `nothing` | no type checker exists |

Every operator in the language, asked with `nothing` on either side:

```
nothing +  1        error(«+» needs two numbers)
nothing -  1        error(«-» needs two numbers)
nothing *  1        error(«*» needs two numbers)
nothing /  1        error(«/» needs two numbers)
nothing @  1        error(«@» indexes a list)
list    @  nothing  error(«@» takes a number for a position)
nothing otherwise 1 1
```

So the rule holds **everywhere, not only in arithmetic**, and it holds by
construction rather than by a check: `Lift` short-circuits `Error` and passes
everything else to an operation that wants numbers. A missing value cannot
quietly become a wrong answer three computations later, which §1.1 calls the
important one and is right to.

## 2. And `nothing` already occurs in programs

It is not hypothetical waiting on a literal. Two cells produce it today:

```
a shadow before any step  ->  nothing      «old x» on the first step
a let before it is read   ->  nothing      the seed a cache starts at
```

`old x otherwise 0` is already the idiom in the test suite. So the value is in
the language and only the *spelling* is missing — which makes this smaller than
it looks, and also means the decision is already load-bearing rather than
prospective.

---

# The concerns

## A. WITHDRAWN — there is no takeover, and I measured the wrong pair

Budai's answer is right and the measurement is cleaner than the reason given for
it. Declaring `nothing found` does not change any statement that was already
working:

```
                              before declaring «nothing found»   after
x otherwise nothing           («x» otherwise «nothing»)          unchanged
x otherwise nothing found     NoParse                            («x» otherwise «nothing found»)
```

The text that "changes" was **not valid before**. Invalid source became valid
with the obvious meaning; nothing was silently re-read. And `x otherwise
nothing` is untouched, because it does not contain the longer name.

I had carried the shape over from the `otherwise` finding without checking that
it applied, which is the error this project keeps naming in other people's work.

### And it narrows that earlier finding, usefully

The `otherwise` hazard is real and this is not, and the difference is exactly
where the declared name sits:

| declared name | effect |
|---|---|
| `x otherwise y` — **spans the operator** | `x otherwise y` was the operation and becomes the name. A working statement changes meaning |
| `nothing found` — **on one side of it** | no statement changes; a longer name is simply available |

So the rule is not "a built-in word can be taken over". It is **a declared name
that spans an operator takes that operator**, which is precisely R5's shape —
glue inside a name — and it is why R5 exists for patterns. Only the first case
needs a decision, and it is the narrower one I should have reported.

Which leaves the spelling of `nothing` an ordinary choice with no hazard
attached: a built-in name costs nothing lexically and is available.

## B. `@` is being asked to do two different jobs

I built `@` as `NOTHINGANDINDEXING.md` §2 specifies it: positional, one-based,
closed, over a list. `MATCHNAMED.md` writes

```ronin
car garages @ the datsun
```

which is a **keyed lookup of a named table**, not a position. They are different
operations — one takes an ordinal, the other a key — and only the first is
built. Whether one symbol serves both is a decision nobody has made, and it
matters because §1.2's "a lookup miss yields `nothing`" is about the second.

Related: a lookup literal does not resolve at all today —

```
{ a = 1 }        NoParse
{ a = 1, b = 2 } NoParse
```

— so that consequence is two steps away, not one. A list literal does evaluate,
to an object array, which is what made `@` implementable.

## C. The escape hatch in §1.1 does not exist

> `otherwise` catches both `nothing` and `Error`, with `if (x is error)` kept for
> the rare case that must distinguish them.

There is no `is`, and no comparison operator of any kind — the table is `+ - * /
otherwise @`. So the settled decision names a mechanism that has no design. It
matters more than it looks: `otherwise` deliberately erases the distinction
between a missing value and a failed one, and the only way back is the thing
that does not exist.

## D. `optional` is a modifier, not a type constructor

The spec lists it beside `compiled`, `shared`, `persistent`, `export`,
`extends` (§4.2), and it is parsed and stored as one. §1.1 treats `optional T`
as a type. Those are different things, and a type checker would read them
differently — a modifier annotates a declaration, a constructor makes a new type
from an old one. Worth stating which, because §1.2's exhaustiveness argument
needs the second.

## E. The rule is broader than §1.1 states — say so

§1.1 says arithmetic. Measured, it is every operator, and the reason is
structural rather than per-operator. The rule reads better and is easier to keep
as:

> `nothing` is inert everywhere except `otherwise`. Any other operation on it is
> an error.

That is what the code does. Stating it that way also settles in advance what a
future operator should do, rather than leaving each one to decide.

## F. The message is true and does not say what happened

`nothing + 1` reports *«+» needs two numbers*. Accurate, and it describes the
symptom rather than the cause — the point of the rule is to catch a **missing
value** at the moment it is used, and the reader is told about types instead.

This is the one item here I can act on without a decision, and I would suggest:

> «+» has nothing on the left. A missing value is not zero — supply one with
> «otherwise», or check before using it.

Say the word if you want it; it is a small change to `Arithmetic` and `Lift`.

## G. §1.2's consequences are all downstream of a type checker

- a lookup miss yields `nothing` — needs lookups to exist at runtime (B);
- `if c { a }` is `optional T` — needs `if` as an expression **and** a type;
- exhaustiveness is the absence of `nothing` — needs a type checker.

None is blocked on `nothing` itself. Worth recording so that "specify `nothing`"
is not mistaken for unblocking them.

---

## What I would do first

`nothing` as a source spelling is small once **A** is answered, and it is the
only item on which four documents rest. **F** is available now. **B** is the one
I would want settled before `@` grows a second meaning, because that is easier
to decide now than to unpick after `match` is built on it.
