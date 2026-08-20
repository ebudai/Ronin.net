# `nothing` — the word, and it is a smaller cut than you think

> **Ledger** — `[V]` verdict. Rules `nothing` as a nullary supplied spelling on the
> truth-literal precedent (no lexer change), rules the checking path to unify
> generally, confirms the miss boundary, and flags a latent defect in `Truths`
> that adding a third nullary literal would trip.
> answers: `NOTHINGSPELLING`
> supersedes: `NOTHING-ANALYSIS` §A (the spelling, now cut)
> superseded by: none

**Q1: `nothing`. Q2: yours, and it does not touch the lexer. Q3: unify, generally.
Q4: confirmed.**

And one thing to fix *before* you add it: `Resolver.Truths` is derived from
*"has no shape"*, so a third nullary literal silently becomes a truth. §2.

---

## §1 — Q1: the word is `nothing`, and it was already stated

Take it. Two reasons beyond `NOTHING-ANALYSIS` §A's lean:

**The owner already chose it.** `nothing` rather than `null` is on record as the
built-in constant for the absent value, alongside `optional` before the type. §A
was the analysis; the choice was made outside it. So this is a cut, not a
decision.

**And the shape is the truth-literal one, exactly.** `RETURNANDLITERALS` §3 —
*"a nullary entry — a name, not a pattern — so it reserves its own spelling and
nothing else."*

The tree already says why that is safe, better than I would have:

> *A NULLARY entry reserves its own spelling and nothing else — that is what makes
> «true positive» and «stop word» legal beside «true» and «stop». … Nullary is
> exactly where that reasoning stops holding: with no hole there is no argument to
> sit anywhere, so the name and the call cover the same span and neither is
> reachable past the other.*

So `nothing found` stays a perfectly legal user name, by the same argument that
keeps `truth table` legal. The reservation costs exactly one spelling.

## §2 — Q2: yours, and the path is one line — but fix `Truths` first

**There is no lexer change.** `true` and `false` are not lexemes; they are entries
in `Resolver.Supplies`:

```csharp
  Descriptor.Spelled("Truth.",   "true")  with { SeeAlso = ["false"] },
  Descriptor.Spelled("Untruth.", "false") with { SeeAlso = ["true"] },
```

`nothing` is one more of those — say
`Descriptor.Spelled("The absent value.", "nothing") with { SeeAlso = ["optional (_)"] }`
— and it joins `Whole` automatically, because `Whole` is derived. Your §3 worry
about *"a lexer/resolver change … across subsystems I have not been cutting into"*
was aimed at a cut that is not needed. **This is your slice, on the stated
precedent.**

### The defect to close first

`Whole` derives correctly (*has a null segment* → not whole). **`Truths` does
not:**

```csharp
  Truths = [.. Supplies.Where(supplied => supplied.Shape is null) …]
```

*Shapeless* is not *boolean* — it is a **proxy** that happens to be exact while the
only two nullary supplies are the truths. Add a third and `nothing` silently
becomes a truth literal, wherever `Truths` is consumed.

> **A derived list whose predicate is a proxy for the property breaks the moment a
> second thing satisfies the proxy.** Derive `Truths` from something that actually
> says *these are the truth literals* — the type the entry carries — before adding
> the third entry, not after.

This is the same defect as refusing a denormal to catch an overflow: the test
catches a stand-in for the thing it means.

## §3 — Q3: unify, generally. The narrow option is a stopping condition per construct

Take the wider change, and the argument is not that it is cleaner.

**Special-casing means one pinning branch per empty-ish value.** `[]` has one.
`nothing` would get a second. The outward-in empty lookup you have stubbed at
`Compilation.cs:677` would want a third. That is the same rule written three
times, and three copies of a rule later disagree — the third time that hazard has
come up in as many documents (two window predicates, two Tarjans, two invalidation
designs).

**It is a behaviour-preserving refactor, so it is testable as one.** `Unify` is
`Equals` on ground sorts, which is every sort in the tree today. Land the wiring
green, then the new behaviour on top — the same staging you used for the phase
split.

**And the direction question does not arise.** An annotation resolves to a
**ground** sort, so only the value side carries variables. Unification is
symmetric in form and one-directional in fact; the annotation cannot acquire a
binding because it has nothing to bind.

**It also closes a gap you would otherwise leave open.** A top-level `[]` passed as
an argument cannot pin against its parameter today. That is the same defect as
`var x => optional number = nothing`, one construct over, and the wide change
fixes both at once.

### The policy the wide change forces you to state — and it is already ruled

```
  var x = nothing;      no annotation, no sibling, nothing to pin
  var x = [];           same shape
```

Both leave a `Variable` unbound. **That is the not-ground error**, per the
recursion ruling's *"the answer must be ground"* — reported, never stored, and
**the same finding for both.** Do not invent one policy for the empty list and
another for the absent value; they are one case.

## §4 — Q4: confirmed independent, and there are **two** deferred sources, not one

The literal and the miss are independent. Build the literal and its sort now;
`m @ k : optional V` waits on `@`-operation typing. This slice does not touch `@`.

Two things worth having while you cut it.

**`nothing` is what makes `optional` testable at all.** You observed there is no
optional-typed value in the language today from any source — so
`Descriptor.Shaped("A type whose value may be absent.", Optional)` currently has a
type constructor with nothing that inhabits it. The literal is the first inhabitant,
which is a reason to land it *before* the operations slice rather than beside it.

**And the operations slice inherits two sources, not one.** `old` is
`Descriptor.Shaped(…, Previous)` — an operation — and `old x` on the first step
yields `nothing` at runtime. So its static sort is the same question `@`'s is: if
`old x` can be absent, its sort is `optional T`, not `T`. Flag it now so the
operations slice scopes both, rather than finding the second one after the first is
built.

## Summary

| | |
|---|---|
| **Q1** | **`nothing`.** The owner already chose the word over `null`; §A was the analysis, not the decision. Nullary entry on the truth-literal precedent |
| the cost | **exactly one spelling.** `nothing found` stays legal by the same argument the tree gives for `true positive` — with no hole there is no argument to sit beside it |
| **Q2** | **yours, and no lexer change.** `true`/`false` are `Descriptor.Spelled` entries in `Resolver.Supplies`; `nothing` is one more, and `Whole` picks it up by derivation |
| **fix first** | **`Truths` is derived from *has no shape***, which is a **proxy** for *is a boolean*. A third nullary literal silently becomes a truth. Re-derive it from the type the entry carries |
| the rule | a derived list whose predicate is a **proxy** for the property breaks when a second thing satisfies the proxy — the denormal argument again |
| **Q3** | **unify generally.** The narrow option writes the pinning rule **once per empty-ish construct** — `[]`, `nothing`, the stubbed empty lookup — and three copies later disagree |
| and | it is **behaviour-preserving** (`Unify` ≡ `Equals` on ground sorts), so land it green then build on it |
| and | **the direction question does not arise** — annotations are ground, so only the value side has variables |
| and | it closes the **top-level `[]` against a parameter** gap for free |
| the forced policy | an unpinned `nothing` and an unpinned `[]` are **the same not-ground error**. One policy, not two |
| **Q4** | **confirmed** — the literal is independent of `@` |
| worth having | `nothing` is the **first inhabitant** of `optional`, so it is what makes that constructor testable — land it before the operations slice |
| and | the operations slice inherits **two** sources: `m @ k` **and `old x`**, which yields `nothing` on the first step. Scope both |
