# Classifying an unresolved value-return — three heuristics deep, and the authority gives nothing

> **Ledger** — `[R]` The `Unanswered` check must suppress on a body whose value-return did not resolve, and fire otherwise. Detecting an unresolved value-return means telling a `return (_)` anchor from the word «return» in a name — which only the resolver knows, but it yields no structure on failure. Three token heuristics have each failed a new case (REAUDIT65→66→67→68). Asks the designer to choose the direction: give unresolved references their own finding (and suppress broadly), a resolver structural-match query, or accept the token limitation.
> answered by: none
> supersedes: none
> superseded by: none

**From:** the successor, at `acb0aea`, actioning `REAUDIT68`. The direct finding of
each of the last three re-audits is a repair; each repair uncovered the next case of
the SAME underlying problem. I have made three good-faith attempts and a fourth is in
reach, but it too has a case I can already name, so I am bringing the design question
up rather than shipping a fifth token guess.

## §1 — what the check needs

`Unanswered` fires when a function with a **written** value-return type — `=> number` —
has a body that produces no value (`RETURNANDLITERALS` §1b). For a RESOLVED body this is
exact: `Called` walks the resolved tree for a `Node.Call` of `SymbolTable.Answer`
(`return (_)`), and a multi-word name resolves to a `Node.Name`, never an answer call —
so the resolved path already respects every boundary.

The whole difficulty is the ONE case where the body **almost** answers: a value-return
whose VALUE does not resolve — `return nope`, `nope` undeclared. It leaves no resolved
site, so it looks identical to a body that never answers. Policy, ruled across the last
rounds: such a body is **clean** (suppressed), because the unresolved name is its own
concern and «add a `return (_)`» would be a false instruction — there IS a return.

So the check needs a flag: does the body **attempt** a value-return that failed to
resolve? Call it `Answering`.

## §2 — the treadmill: three heuristics, three follow-ons

Every attempt reconstructs `Answering` from the flattened lexemes of the (unresolved)
reference, and every one has failed a structural case a flat scan cannot see:

```
  attempt                              passed              REAUDIT that broke it
  first lexeme is «return»             direct return nope  66: missed «send (return nope)»
  «return» anywhere + a value          + nested            67: «customer return policy»
                                                             — «return» inside a legal name
  «return» STARTS a word-run           + names             68: missed «send return nope»
  (front, or after a non-word)                               — «send (_)» hole, unparenthesized
```

The last is the sharpest: `send return nope` resolves as `send (return nope)` — a
pattern's hole consumes an unparenthesized return call — so `return` follows the word
`send` yet is a real anchor. `send return 5` compiles cleanly (via the resolved tree);
replacing `5` with `nope` deletes the tree, and the token fallback then rejects the same
structure. `send (customer return policy) nope` (REAUDIT67) has `return` following a word
too, but there it is a name. **Two identical token shapes, opposite meanings** — the
difference is purely which the resolver read them as.

A fourth attempt — resolve each maximal word-run and treat a `return` as an anchor unless
its run resolves to a `Node.Name` — passes every control the audits have named. But I can
already state its break: `send customer return policy nope` (the REAUDIT67 name, now
**un**parenthesized, in an unresolved outer). Its run does not resolve to a name (the
trailing `nope`), so the fourth heuristic would call the name's `return` an anchor and
suppress — REAUDIT67's false positive, back once more. The token shape simply does not
carry the distinction.

## §3 — the root

The resolver is the only thing that tells a `return (_)` anchor from the word «return» in
a name (`ReadsAs`, `Atoms`' `Node.Name`-vs-`Node.Call(Answer)` split). But on the case
that matters — an unresolved value — it produces **nothing**: `Resolution` is `NoParse`,
with no `Tree` and no partial structure (`Resolver.cs`, `Resolution` has a private `Tree`
set only by `Resolved(...)`). And `Match` yields no `return (_)` call when the hole does
not resolve (`Resolver.cs:837-839`). So there is no authority to read, and the token
stream cannot stand in for it.

Underneath both is a gap the audits keep naming: **an unresolved reference has no finding
of its own.** `send return nope`, `nope`, `customer return summary` (undeclared) — all
compile to silence today. That silence is why `Unanswered` has to walk on eggshells around
them.

## §4 — the directions, and what each costs

**(A) Give an unresolved reference its own finding, and suppress `Unanswered` on any
unresolved reading.** The `Answering` flag and all its heuristics are DELETED. A body with
any unresolved reading gets that reading's finding instead of `Unanswered`; a fully
resolved body with no value site gets `Unanswered` as today. No silent contradiction,
because the unresolved reading is now loud. **Cost:** it is a policy change —
`{ nope; return; }` would report the undeclared `nope` and NOT `Unanswered` (REAUDIT65
wanted `Unanswered` there), on the reasoning that the unresolved thing is fixed first and
the body re-checked. And it is a new finding with its own scope (what exactly is
«unresolved», how noisy, one per reference or per name).

**(B) A resolver structural-match query** — "does this reference contain a `return (_)`
anchor at any depth, matching the pattern structurally without requiring the hole to
resolve." `Answering` calls it; it respects names by construction (it IS the resolver's
matcher, minus the leaf requirement). **Cost:** a new resolver capability. Contained if it
is a separate read-only pass, but it is resolver work and must not perturb ordinary
resolution.

**(C) Make the resolver produce a partial tree on an unresolved leaf** (a placeholder
node), so `Called` finds the `return (_)` uniformly and `Answering` is deleted.
**Cost:** the broadest — every currently-`NoParse` reference would begin to «resolve», and
every consumer of resolution inherits that. Almost certainly too much for this.

**(D) Accept the token limitation.** Ship the word-run guard (REAUDIT67, current `HEAD`)
or the fourth heuristic, and document that unresolved unparenthesized nested returns —
already-broken source — may draw one extra `Unanswered`. **Cost:** a known, if narrow,
inaccuracy stands; the audit does not sign off on this axis.

## §5 — what I recommend, and what I need

I lean **(A)**. It is the direction every re-audit has gestured at — "unresolved references
do not yet have their own general finding" — and it does not merely patch `Answering`, it
removes the reason it exists: once an unresolved reference is itself a finding, `Unanswered`
need not distinguish an unresolved return from unrelated unresolved text, because neither is
silent. It is a policy change and a new finding, which is why it is yours and not mine.

If you prefer to keep `{ nope; return; }` reporting `Unanswered` exactly as ruled, **(B)**
is the surgical alternative — a structural `return (_)` query the flag consults — and I
would want your read on whether a resolver structural-match pass is welcome.

**Q1 — which direction:** an unresolved-reference finding with broad suppression (A), a
resolver structural-match query (B), a partial-tree resolver change (C), or accept the
token limitation for now (D)?

**Q2 — if (A):** is it acceptable that `{ nope; return; }` reports the undeclared `nope`
rather than `Unanswered`, the missing answer surfacing once the reference resolves? And
what is «an unresolved reference» for this — any `NoParse` reading, one finding per
statement?

Until you rule, `HEAD` keeps the REAUDIT67 word-run guard: correct for names and direct,
grouped, and parenthesized nested returns; the one open inaccuracy is the unparenthesized
`send return nope` on already-unresolved source (REAUDIT68, medium).
