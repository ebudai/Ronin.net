# What landed, what the code says, and two questions

> **Ledger** — `[R]` What landed, what the code says, and two questions
> supersedes: not yet checked
> superseded by: not yet checked

Everything ruled in `FIVE-RULINGS.md`, `RETURN-AND-LITERALS.md` and
`DESCRIPTOR-SLICE.md` that does not need the type checker is built. Below is what
that turned up, which is more interesting than the fact it is done.

Two questions at the end, neither blocking — I am starting on the ambiguity
diagnostic's repairs, which needs nothing from either.

---

## 1. Built

| ruling | what it is now |
|---|---|
| `FIVE-RULINGS` §1 | `return (_)` is a builtin pattern. `var return value` is refused; a body's `return x` resolves, where it had never resolved at all |
| `RETURN-AND-LITERALS` §1 | bare `return` too. One exit flavour per body, mixing refused, `return (_)` refused in a `when` |
| `FIVE-RULINGS` §5 | `optional (_)` is a pattern; the modifier keyword, its lexer class and its token factory are gone |
| `RETURN-AND-LITERALS` §3 | `true` / `false` are supplied names |
| `REFERENCE-STRUCTURE` §5 | the overload check is an assertion addressed to whoever maintains it |
| `DESCRIPTOR-SLICE` | one descriptor list; `Builtins` and `Truths` derived from it; `docs/reference.md` generated |

Also, before those: ambiguity is a finding on real source with a span, and there
is a language server with diagnostics and hover.

**One correction of mine that has already been useful.** `stop` "disarms" the
`when` was in the sentence I shipped verbatim, and `Graph.Stop` says *"it REMOVES
the node rather than disabling it"*. Disarm reads as reversible; nothing can
re-arm a `when`. The diagnostic and the reference entries say *remove*.

## 2. `stop` is in the state `return` was in on Tuesday

Checked rather than assumed, because it decides whether it gets a reference entry:

```
  var stop => Number;              accepted -- nothing reserves it
  when ready { stop; }             no reading -- nothing resolves it
```

So there is no source form. `Graph.Stop` exists, `Graph.Return`'s own comment
refers to `stop` as though a reader would know it, and nothing in the frontend
has heard of it. That is exactly where `return` was three days ago: a runtime
operation the language cannot express.

Per `DESCRIPTOR-SLICE` §1 it therefore gets no entry and nothing points at it,
which is the checked cross-reference working rather than an omission. When it
gets a source form, §0's law applies to it as it did to `return`, at the 5
collisions §1 priced.

**Question 1: what does `stop` become?** A nullary builtin pattern, like bare
`return`? It reads like one. I have not built it because "the runtime has an
operation" is not the same as "the language has a word", and guessing which is a
worse mistake here than waiting.

## 3. Leading free holes — two things the document does not have

**The refusal is not R6.** A leading hole is refused by its own check in
`Declarations.Refuse` — `identifier.BeginsWithHole` — with its own `LeadingHole`
finding and its own message about respelling or declaring a symbolic operator.
`Anchors` (R6) is separately about two *anchored* patterns' runs being prefix
free and does not participate. So deleting the clause is one `if` and one finding
kind, and R6 keeps doing its own job untouched. Smaller than the document
assumes.

**And the binding power has a cost the document does not price.** The DP table
carries **one column per distinct binding power the recurrences can ask for** —
today `[0, PatternBindingPower]` plus every operator's level. The resolver's own
allocation guard measured what that costs, before any of this came up:

> 26.2 MB as this is written, and «is» is what moved it — 15 with two operator
> precedences, 21.7 with three, 26.2 with four. The table carries a column per
> level the recurrences can ask for, so **a level costs about 4.5 MB**.

So per-pattern binding powers do not only risk two authors colliding on `47`.
**Every distinct number anyone writes adds a column to the table.** Integers let
a library author widen the DP table by inventing a number; a named ladder bounds
its width by construction.

That turns §5's recommendation from a coordination argument into a resource one,
which I think is the stronger of the two — and it makes the ladder's *size* a
decision with a measurable price rather than a matter of taste.

**Question 2: how many levels?** At roughly 4.5 MB per level on a 149-lexeme
statement, a ladder of eight is not free. I would want the number chosen against
that rather than discovered by it.

## 4. One thing to know about the property test if leading holes land

`REPAIR-COMPLETENESS`'s two vocabularies are anchor-first, so a leading-hole
configuration is a third row with its own exhaustive count. Worth saying because
`LEADING-FREE-HOLES` §3's "0 unreachable readings" rests on exactly the property
that test exists to hold — so it should be generated there rather than carried
across from the probe. Cheap, and it is the difference between the claim being
measured twice and being measured once and quoted.

## 5. What `truth` still needs

The literals landed; the type did not. `truth` as a *type* needs
`FIVE-RULINGS` §4's kind field, and an annotation is still text nothing
validates. What is built is every part of it that does not depend on that — which
includes the thing it was wanted for: `SHRINK-TAGGING` §1's surviving group had
no member that could be written down until `truth` could be spelled, and `var is
valid => truth` is that member. Six fixtures survive the shrink, ten expire.

Worth naming what that was: a rule scheduled to shrink to a residue nobody had
ever expressed. Same shape as the check that reported PASS over zero cases.
