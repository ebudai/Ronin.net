# The arrow in a pattern — take option (a), and §6 is not at risk

> **Ledger** — `[V]` The arrow in a pattern — take option (a), and §6 is not at risk
> supersedes: none
> superseded by: none

Good catch, and the right one to raise rather than route around. Two answers, and
the first begins with a correction to your concern rather than to the finding —
**the finding is right.**

---

## 1. Admitting a symbol segment does not make `=>` capturable

You wrote that option (a) *"would make `=>` capturable in a way §6's 'zero
reserved words — a symbol cannot be captured by a name' is relying on it not
being."*

**It would not**, and the reason is that capturability is a property of the
**lexer**, not of the matcher. From `Lexemes.cs`'s own summary:

> *"One lexeme per word: `Word.Lex` stops at whitespace, symbols and punctuation,
> so a multi-word name arrives pre-split."*

A name is a run of **word** lexemes. It cannot span a symbol because the lexer
never produces one inside it — not because patterns happen to contain only words.
Widening what a *pattern* may contain does not widen what a *name* may contain,
so §6 holds unchanged under option (a).

**But there is a real thing inside your worry, and it is a different one.** Option
(a) makes it possible for a **user-declared** pattern to contain a symbol. That
is not capture; it is *symbol claiming*, a new axis, and it should be decided
rather than acquired as a side effect. §3 below.

## 2. Take option (a), and the rule the glue machinery needs is "none"

Of the three:

- **(b) `=>` lexing as a word in type position** — no. A lexeme's kind would
  depend on where it appears, which is the same defect as a scoped reservation
  and the same defect as `LexemeKind.Symbol` being asked to carry `=`. We fixed
  that one by *adding* a kind, not by making a kind contextual.
- **(c) resolving the lookup type outside the pattern matcher** — no. That is a
  second grammar with a second ambiguity policy, which is the failure named in
  `TYPEVOCABULARY` §2 and the one the whole type-patterns ruling exists to avoid.
  It would also cost §1's result: the two-arrow declaration resolves uniquely
  *because* the arrow participates in ordinary resolution under the kind filter.
- **(a) a literal segment may be a symbol lexeme** — yes. Smallest change, and it
  is the only one that keeps one resolver.

You asked what opinion `Rules`' infix and glue machinery needs. **The opinion is
none, and it is worth writing down as a rule rather than leaving as an absence:**

> **A symbol segment in a pattern reserves nothing.** Glue exists because a name
> can swallow a word that sits between two holes. A name cannot swallow a symbol,
> so a symbol segment contributes no glue, and R5′, R6b and R7b are unaffected —
> they are about words, and a symbol segment is invisible to them.

That is the same sentence as §6's, applied one layer down, and it is why the
change is small: the rules do not need extending, they need *not* to fire.

The matcher change is the one line you quoted, admitting a symbol lexeme whose
text equals the segment. I would add one restriction, because it costs nothing
and closes the accidental version of §3: **a symbol segment must be a symbol the
lexer already produces as a token class** — `Arrow`, not arbitrary punctuation
assembled by a pattern author.

## 3. The capability this opens, which should be decided on purpose

With (a), a user may declare `something (_) ~> (_)`. Worth ruling now rather than
discovering:

**I would allow it.** Symbols cost **no names** — that is §6's whole point, and it
applies to user patterns as much as to prelude ones. Two modules claiming one
symbol differently is no worse than two modules claiming one word differently,
which the registry already scopes by import. And refusing it would need a
two-class pattern system — prelude patterns may contain symbols, user ones may
not — which is exactly the fork this project keeps paying to remove.

One consequence to record: **the glue registry should list symbol segments as
claimed-but-free.** Not because they reserve anything, but because
*"which module claims `~>`"* is a question someone will ask, and the registry is
where claims are answered. A line that says *reserves nothing* is more useful than
no line.

## 4. Your sequencing is right

Land the six that have no arrow in them — `number`, `text`, `truth`, `error`,
`list of (_)`, `optional (_)` — with the Descriptor kind, the derivation filters
and the regenerated goldens. `lookup (_) => (_)` follows.

And your `Truths` note is the good catch inside the good catch: *it takes every
nullary supply, so the four type names would silently become truth literals.*
That is the kind filter's absence showing up as a wrong answer rather than a
missing one — precisely the shape of the annotation-prune bug. Worth a test named
after it.

## 5. `var number => number;` — **`Supplied`**, not `Shadowed`

Both would be refusals and both would be correct. They should be different
findings because **the remedies differ**:

```
  Shadowed    you declared this name twice.
              -> rename EITHER. Both are yours.

  Supplied    «number» is the language's.
              -> rename YOURS. The other one is not available to move.
```

A message that tells someone to rename one of two things when only one of them
is theirs is a message that wastes a minute of reading. So: **`Supplied`**, with
the name of the supplying module in the text once modules are scoped, because
"the language's" and "the collections module's" are different facts and the
second one has a second remedy — stop importing it.

## 6. Summary

| | |
|---|---|
| the finding | **right**, and right to ask rather than route around |
| your §6 worry | **unfounded** — capturability is a **lexer** property (`Word.Lex` stops at symbols), not a matcher one |
| ruling | **option (a)**: a literal segment may be a symbol lexeme |
| (b) contextual lexing | no — a kind that depends on position is the defect we fixed by adding `Associates` |
| (c) a separate resolver | no — second grammar, second ambiguity policy, and it costs §1's unique two-arrow reading |
| what `Rules` needs | **nothing, stated as a rule**: a symbol segment reserves nothing, because a name cannot span one |
| one restriction | the symbol must be a token class the lexer already produces |
| the capability it opens | user patterns with symbol segments. **Allow it** — no names cost, and refusing needs a two-class pattern system |
| registry | list symbol segments as **claimed, reserves nothing** — the claim is still a fact someone will look up |
| sequencing | **yours** — the six land now, the arrow follows |
| `var number => number;` | **`Supplied`**. Different from `Shadowed` because only one of the two names is the user's to move |
