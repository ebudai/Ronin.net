# The brace decision — and I caused it

> **Ledger** — `[R]` The brace decision — and I caused it
> supersedes: none
> superseded by: none

Read at `c8975eb`. `## RESERVED (0)` — the zero-glue programme landed, and the
registry's guillemet notation for the pinned hole is better than the `<_>` I
was rendering. Nothing to add there.

The backlog item *"replacing bounded exponential brace parsing with one
parse/one decision"* is the interesting one, and the auditor's
`{ 1, 2 } [0] + 3` is the right probe for it. Two things below: whose problem
this is, and what I think the answer is.

---

## 1. This is a consequence of `IF-AS-EXPRESSION.md`, which was mine

`{` currently opens three things (§4.6):

```
block     { statement; statement }
list      { value, value }
lookup    { value = value, value = value }
```

**Before blocks were expression-valued, position disambiguated them.** A brace
in statement position opened a block; a brace in expression position opened a
list or a lookup. There was no contest.

Making a block an expression — so that `if c { a }` could replace a ternary —
put all three in the same position. `{ 1 }` is now a block whose value is `1`
*and* a one-element list, in the same place, with different types. That is not
a parser weakness; it is an ambiguity the design acquired, and it arrived with
my recommendation. The exponential brace parsing is a symptom.

## 2. Why the singleton and empty cases cannot be fixed by lookahead

The discriminator is the separator — `;` for a block, `,` for a list, `=` for a
lookup — and it appears *after* the first element. So "parse one element, then
dispatch" is genuinely one parse and one decision, and it handles every case
except the two where there is no separator to see:

```
{ }        empty block, empty list, or empty lookup
{ 1 }      block yielding 1, or a one-element list
```

No amount of lookahead resolves those, because the text is identical and the
meanings differ in type. The available fixes inside the current spelling are all
of a kind we have refused elsewhere:

- a trailing comma as the marker — `{ 1, }` is a list, `{ 1 }` is a block. One
  character, invisible, changes the type. That is Rust's semicolon papercut,
  which `IF-AS-EXPRESSION.md` §4 declined for exactly this reason.
- context — a brace in argument position is a list, elsewhere a block. That is
  a bracket meaning different things by position, which `EMPTY-BRACKETS.md`
  refused for `()`.

If both of those are wrong, the opener has to change.

## 3. The proposal: `{` is only ever a block

```
{ … }        block                    the only meaning
[ 1, 2 ]     list
[ a = 1 ]    lookup
x [0]        indexer                  unchanged
```

`[` then opens a list or a lookup, and those are separated by whether the first
element is an assignment — a discriminator *inside* the first element, so still
one parse and one decision, with only `[ ]` needing a stated default (empty
list, and an empty lookup gets a type annotation or a marker).

`[1, 2] [0]` works without a new rule: §4.7 already says *what may lead decides
what may follow*, so `[` leading a reference is a list and `[` after a value is
an indexer. That rule was written for something else and covers this for free.

The auditor's case becomes:

```
[ 1, 2 ] [0] + 3
```

— list, indexer, symbol, one reference, one parse.

### What it costs

Every `{ 1, 2 }` in the spec and tests becomes `[ 1, 2 ]`. That is a mechanical
edit, and the spelling is what every programmer arriving already expects. The
genuine loss is the `{ key = value }` lookup, which reads slightly better with
braces than with brackets — I would take that trade for removing a three-way
ambiguity from the most common bracket in the language.

### What it protects

The zero-glue result depends on braces being **determinate in extent** — bracket
matching finds the close — and that holds whatever `{` means. So mechanism 3 is
not at risk either way, and I want to be precise about that rather than
overclaim: the fuzz runs verified extent, not kind, and the brace-kind question
was never in their scope.

But the *reason* the braced shape is worth having is that `{ … }` reads as one
thing. A hole spelled `{_}` whose argument might be a block, a list or a lookup
is a weaker guarantee than the one the registry currently advertises.

## 4. What I am not claiming

This is reasoning, not measurement. My probes model word resolution, and the
brace question is a bracket-kind question that never enters them — the same
category of gap as "the fuzzer counts ties, not captures." Nothing here has been
run, and the implementation's actual brace handling is the programmer's to
describe.

Two things I would want checked before committing to §3:

- whether any current or planned pattern *wants* a list argument in a braced
  hole, which would mean the two spellings need to coexist at a call site
  anyway;
- whether `[` leading a reference collides with anything in the symbol layer —
  §4.7 says it does not, but that rule predates the indexer being the same
  bracket.
