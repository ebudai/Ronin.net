# Error as a value the type ignores — you are right, and the reason is stronger than "no case comes to mind"

> **Ledger** — `[V]` Error is a value the type ignores — ruled. The checker rule it implies (`Error` assignable to every type, one-directional) is a recommendation.
> supersedes: NOTHING-ANALYSIS §C
> superseded by: none

Take the easy way. But it is not free, and the price is not where either of us
was looking: it is not a missing diagnostic, it is **four value-semantics
questions that stop being the type system's problem and become yours to answer
explicitly.** Three of them are one sentence each. One is load-bearing and
invisible until a user hits it.

---

## 1. Why unions buy nothing — the argument is structural, not anecdotal

You could not construct a case. That is not a gap in imagination; it is a
property of where `Error` comes from.

```
  form                                  can be Error
  literal            3, "a", true                 no
  name reference     x                           YES
  a + b   a - b                                  YES   overflow; an Error operand
  a / b                                          YES   division by zero
  a is b                                         YES   an Error operand
  xs [ i ]                                       YES   out of range
  m [ k ]                                        YES   missing key
  f (x)                                          YES   the body may fail
  [ e1, e2 ]     [ k = v ]                       YES   an element may be one
  old x                                          YES   the previous value may have been
  x otherwise y                                  YES   y may itself fail
  - a           a and b                          YES   an Error operand

  13 of 14. The exception is a literal.
```

So under a union, essentially every type in a real program is `T | Error`, and
the annotation **partitions nothing.** A distinction that holds of almost
everything is not a distinction — unless it carries an obligation, and that is
the actual fork:

| | |
|---|---|
| **obligation ON** | `a / b : number \| Error`, and `a / b + 1` does not type-check until it is handled or propagated. Rust's `?`, Java's checked exceptions. **Every arithmetic expression in the language grows a marker** |
| **obligation OFF** | `number \| Error` is usable wherever `number` is, so the union records a possibility that is true of everything and forces nothing — **the easy way with extra syntax** |

**There is no middle.** The union either changes every expression or none, which
is exactly why no example presented itself. Obligation-ON is a different language
and it is not the RAD one.

And on your own example — `if x is an Error { }` where `x` never can be. Under
the table above, "never can be" is true only of a literal or a name bound
transitively to literals. Detecting it is a small **effect** analysis on the
expression's provenance, not a type analysis; a union would not have given it to
you for free either, because `x`'s type would be `number | Error` like everything
else. So the one case unions were supposed to win is a case unions also lose.

## 2. `Error` in the checker: a bottom type, and the direction matters

Your "Error matches anything" is the right formalisation, with one precision that
is cheap now and expensive later:

> **`Error` is assignable to every type. No type is assignable to `Error`.**

One-directional. If it matches in both directions it is a `dynamic`/`any` hole,
and two unrelated types unify *through* it — `text` meets `number` via `Error`
and the checker stops refusing anything. That is the standard way a bottom type
turns into a bug.

It still needs to exist as a *named* type, because `x is an Error` has to be
writable. So: a bottom type with a name, present for the type-test form and for
nothing else.

## 3. The obligation the easy way DOES create — Error equality, through cutoff

This is the one worth the ten minutes, and it exists **only** because `Error` is
a value. A value gets compared by `is`; `is` is what the reactive graph uses for
cutoff; so a cell that keeps failing has its behaviour decided by Error equality.

```
  scenario                           policy    fires   downstream ends holding
  the same failure every round       always        1   Error«divide by zero»
  the same failure every round       never         6   Error«divide by zero»
  the same failure every round       reason        1   Error«divide by zero»

  a failure that CHANGES reason      always        1   Error«divide by zero»
  a failure that CHANGES reason      never         6   Error«missing key «b»»
  a failure that CHANGES reason      reason        2   Error«missing key «b»»
```

- **all Errors equal** — a failure whose reason changes never re-fires, so
  downstream keeps reporting *divide by zero* while the real fault is a missing
  key. A wrong message that cannot correct itself is worse than none.
- **all Errors distinct** — a cell failing the same way fires **every round,
  forever.** In a graph with rounds that is a livelock: one broken cell keeps the
  whole graph awake and every dependent recomputes on every tick.
- **equal when their reasons are equal** — quiet on a stable failure, propagates
  a changed one, propagates recovery. The only one that is both stable and
  truthful.

> **Two Errors are equal when their reasons are equal.**

One sentence, and invisible until someone has a reactive cell that keeps
failing — which is to say, until someone has a user.

## 4. Three more that move from the type system to the value semantics

Each is short, and each is a thing a union would have forced you to write down.

**a. An `Error` may not be a lookup key.** Refused at construction, the same
shape as `TooDeep`. A key must have an equality that means something, and "these
two failures are the same key" is a claim nobody wants to have made by accident.
`E-AGGREGATES` §4 already puts key canonicalisation at construction, so this is
one more refusal in a place that already has them.

**b. An `Error` entering a `match` propagates; it does not fall to the
catch-all.** Otherwise the default arm silently swallows every failure, and a
`match` over a `truth` with `true` and `false` arms looks exhaustive while being
the place errors go to die.

**c. Comparison is total; computation propagates.** `a + b` with an Error operand
is an Error. `a is b` with an Error operand must return a `truth`, not an Error —
because §3 needs `is` to be total for cutoff to be answerable at all. That split
is defensible and it should be stated, because the natural instinct is to make
everything propagate uniformly and that instinct breaks the graph.

## 5. The one real cost that is not a semantics question

**Storage.** Struct-of-arrays instance binding means a `number` column has to be
able to hold an Error, so it cannot be an unboxed `double[]`. With errors outside
the value space you would know statically which columns can fail.

Nameable, solvable — a validity bitmap plus a reason side-array is what columnar
formats do — and for a language whose premise is rapid development over peak
throughput it is the right trade. But it is the one consequence that is expensive
to revisit later, so it belongs in the record now rather than being discovered by
a profiler.

## 6. Summary

| | |
|---|---|
| the easy way | **take it** |
| why unions buy nothing | 13 of 14 expression forms can be an `Error`, so `T \| Error` partitions nothing |
| the real fork | obligation ON = checked exceptions in every arithmetic expression; OFF = the easy way with extra syntax. **No middle** |
| your `if x is an Error` case | unions lose it too — `x`'s type would be `number \| Error` like everything else. It is an **effect** question, and a cheap syntactic one |
| `Error` in the checker | a **named bottom type**: assignable to everything, nothing assignable to it. **One-directional**, or it is an `any` hole |
| **the price** | **two Errors are equal when their reasons are equal** — measured through cutoff: all-equal gives a permanently wrong message, all-distinct gives a livelock |
| lookup keys | an `Error` may not be one — refused at construction |
| `match` | an `Error` propagates rather than hitting the catch-all |
| the split | **comparison is total, computation propagates** — `is` must be total or cutoff has no answer |
| the cost that is not semantics | columns cannot be unboxed. Right trade, worth recording before a profiler finds it |

Probe: `error_as_value.py`.
