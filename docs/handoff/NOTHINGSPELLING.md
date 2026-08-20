# The `nothing` spelling — the case its sort ruling assumes, and the grammar never cut

> **Ledger** — `[R]` The inference-variable slice reached `nothing`, and it has no source spelling: `Optional(Variable)` has nothing to attach to. The sort is ruled (`CHECKERSCOPINGRULINGS` Addition 1) and the case `var x => optional number = nothing` is written as spellable, but the spelling was raised in `NOTHING-ANALYSIS` §A and never cut. Asks whether to cut it, how, and whether the checking path should unify.
> supersedes: none
> superseded by: none

**From:** the successor, at `8ce052c`, having built the inference-variable
foundation for `[]` and reached the parallel `nothing` case you named next.

Empty-list inference is in. `[]` is a `List(Variable(fresh))`, the elements of a
list unify rather than compare, and a determined sibling pins an empty one —
`[[], [5]]` is a `list of list of number`, either order (`8ce052c`). The
unify-with-binding primitive it turns on — an unbound `Variable` binds to what it
meets, `Ground` reads it out, nothing escapes the run — is built and unit-tested
whole (`68d79fb`). `nothing` is meant to be the same mechanism on `Optional`
rather than `List`. **It is not buildable, and the reason is one you have half-
ruled already.**

## §0 — what is already settled

- **The sort.** `CHECKERSCOPINGRULINGS` Addition 1 rules **`nothing :
  Optional(Variable(fresh))`** — "the empty optional at an unknown inner type,
  resolved by unification exactly as `[] : List(Variable(fresh))` is." It names
  the exact case step 2 needs: *"step 2 needs it the first time anyone writes
  `var x => optional number = nothing`."*
- **The value.** `nothing` is a value, `Nothing.Instance`, and occurs in programs
  today — `old x` on the first step, a `let` before it is read (`NOTHING-ANALYSIS`
  §2). `optional (_)` is a type constructor and is built (`FIVERULINGS` §5,
  `Resolver.Optional`).
- So the type side is decided and the runtime value exists. What Addition 1
  assumes without stating is that **`nothing` can be written**.

## §1 — the finding: `nothing` has no spelling

Measured at `8ce052c`. `nothing` in source is an ordinary undeclared name — it
resolves exactly as any other does, and produces no value the checker can sort:

| source | findings | reading |
|---|---|---|
| `var y => number = nothing;` | none | same as the next row |
| `var y => number = zilch;` | none | an arbitrary undeclared name — identical |
| `var x => optional number = nothing;` | none | `nothing` read as null, not as `optional`; nothing checked |

There is no `nothing` lexeme, keyword, or literal. `Inferred(nothing)` is null,
the same as for any name not in scope. The empty list could be built on because
its literal *already existed* in the grammar — `Grammar.Collection`, zero parts.
`nothing` has no counterpart, so `Optional(Variable(fresh))` has nothing to be
minted from, and nothing to be pinned by.

The other route to an `Optional` **value** — a lookup miss, `m @ k : optional V`
(`REAUDIT47RULING` §5) — is an **operation**, and operations are untyped: `xs @ 1`
assigned to both `number` and `optional number` yields no finding. So there is no
optional-typed value in the language at all today, from any source.

## §2 — the gap: raised, leaned, never cut

This is not new ground. `NOTHING-ANALYSIS` §A raised the spelling and concluded
it is **"an ordinary choice with no hazard attached: a built-in name costs
nothing lexically and is available"** — and §"What I would do first" records
*"`nothing` as a source spelling is small once A is answered, and it is the only
item on which four documents rest."* §A was never answered by a ruling; the doc's
`superseded by` edges strike only §C and §D. So the spelling sits raised,
leaned-toward, and uncut — and Addition 1 has since written a case that needs it.

The precedent for how a value's spelling is decided is `RETURNANDLITERALS` §3,
which ruled the truth literals as **"a nullary entry — a name, not a pattern —
so it reserves its own spelling and nothing else."** `nothing` reads as exactly
that shape: a nullary literal that denotes a value, reserving the one word.

## §3 — what building it touches, and why I am asking rather than cutting

Unlike every inference-variable slice so far, this one is **not** a change in
`Checking`. To give `nothing` a sort the checker mints, `nothing` must first
*parse to a node of its own* rather than to a `Name` — a lexer/resolver change,
a new value literal and the `Node` for it, across subsystems I have not been
cutting into. Reserving a language word, and choosing where in the grammar the
literal sits, is a surface decision and a naming one — `NOTHING-ANALYSIS` §A's
"which built-in name" — that I should not make on my own read of a ruling that
assumes the answer.

## §4 — what I need

**Q1 — the spelling.** Is it the bare word **`nothing`**, reserved as a nullary
value literal on the `RETURNANDLITERALS` §3 truth-literal precedent (a name, not
a pattern; reserves its own spelling and nothing else)? `NOTHING-ANALYSIS` §A
leaned this way ("available, no hazard"); this asks you to close it.

**Q2 — whose cut, and how.** The path is lexer → resolver → a literal `Node` →
the checker minting `Optional(Variable(fresh))` for it. Is that whole path mine
to cut, following the truth-literal precedent — or do you want to rule the
grammar mechanics (keyword vs prelude nullary name, and where it parses) before I
touch the resolver?

**Q3 — the pinning surface, and whether checking should unify.** For `[]` I
pinned only *siblings* inside a list, localized, leaving the argument and
initializer checks untouched — they compare by equality. But `nothing`'s primary
case, the one Addition 1 names — `var x => optional number = nothing` — is pinned
by the **annotation**, outward-in. That needs the initializer check to **unify**
the expected type against the value (`optional number` binds the variable in
`Optional(Variable)`), not compare it, or it reports a spurious mismatch. So:
should the checking path unify expected-against-value generally — the outward-in
model, which would also let a top-level `[]` argument pin against its parameter,
consolidating the empty handling — or should annotation-driven pinning stay
special-cased like `[]`'s empty-collection branch, and `[nothing, …]` sibling
pinning be the only new surface? The first is the wider, cleaner change; the
second is the narrower one.

**Q4 — the miss boundary.** `NOTHING-ANALYSIS` §B flagged `@` doing two jobs, and
"a lookup miss yields `nothing`" resting on that. Operation typing is deferred
(the far side of the plan). Confirm the *literal* `nothing` is independent of it —
I build the source literal and its sort now, and the miss-result path (`m @ k :
optional V`) waits on `@`-operation typing — so this slice does not touch `@`.

## §5 — what I do with each answer

- **Q1/Q2 = "cut it, it is `nothing`, follow the precedent":** I add the literal
  (lexer/resolver + `Node`), mint `Optional(Variable(fresh))` for it, and the
  slice proceeds.
- **Q1/Q2 = "I will spell it / rule the mechanics first":** `nothing` waits on
  that; I take the **outward-in empty lookup** (`lookup … = []`, still stubbed at
  `Compilation.cs:677`) meanwhile — buildable now, no variables, finishing
  milestone 3's brackets.
- **Q3 = "checking unifies":** I wire `Unify` into the initializer/argument
  checks (behaviour-preserving for concrete sorts — `Unify` is `Equals` there),
  so `var x => optional number = nothing` checks and top-level `[]` pins too.
- **Q3 = "keep it special-cased":** annotation-driven pinning stays in the
  empty-collection branch; only `[nothing, …]` sibling pinning is new.
- **Q4 = "independent":** I touch only the literal; `@` and the miss are left for
  the operations slice.

The empty-literal pair is one answer from closing. `[]` is done; `nothing` needs
its word.
