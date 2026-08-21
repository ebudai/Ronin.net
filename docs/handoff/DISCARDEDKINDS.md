# A discarded literal kind and a duplicated scalar list — the sweep's other two, design-adjacent

> **Ledger** — `[R]` The `NULLARYRULING` §4 sweep found the fourth structural proxy (fixed) and two more instances of the same RULE — every consumer reads the declared fact — by a different mechanism: a literal's lexical kind is computed then discarded, so two consumers recover it independently (one safely, one by a drift-prone hand-roll); and the scalar type names are a hardcoded list duplicating the registry. Asks where a literal's kind should live, and how the registry should mark a scalar from the bottom.
> supersedes: none
> superseded by: none

**From:** the successor, at `b8a791b`, having cut `REAUDIT63`'s five findings and,
per your §4, swept for the fourth — `Group.Flattened` reading `part.Key` where its
siblings read `Group.Kind`, now fixed. The sweep surfaced two more that fit the
rule *"where a declaration carries a fact, every consumer reads that fact"* but not
the structural-proxy shape: they are a fact **discarded then recovered**, and a fact
**copied**. Both are latent, neither is `REAUDIT63`, and each turns on a decision I
should not take alone.

## §1 — the literal kind: computed, discarded, recovered twice

The lexer classifies a literal — `Ronin.Lexicon.Literal` has the subtypes `Date`,
`Numeric`, `Text` (`Lexicon/Literal.cs:41,85,164`). Then the kind is dropped, twice:

- `Resolution/Lexemes.cs:158` — **every** `Literal => LexemeKind.Number`, the three
  kinds collapsed to one because "Number is named for the only literal the standalone
  splitter can produce";
- `Node.cs:161` — `new Node.Literal(text)` keeps only the text.

So two consumers **recover** the kind that was thrown away, and they do not agree on
how:

- `Checking/Sort.cs:128` — `Sort.Denoted(Node.Literal)` re-lexes: `Lexicon.Literal.Lex`,
  the authoritative classifier. Deterministic, so it cannot drift from the lexer. It
  is defended in its own docstring.
- `Runtime/Evaluator.cs:205` — `Value(Tree.Literal)` **hand-rolls** it: `text[0] is
  '"'` ⇒ text, else `double.TryParse(text, AllowThousands, InvariantCulture)` ⇒ number,
  else an unread-literal `Error`. This is a **second classifier**, not the lexicon, and
  it can disagree with it — a thousands-grouped or date-shaped run the lexer reads one
  way and `double.TryParse` reads another, and a fourth literal kind would have to be
  taught here separately.

Same fact, discarded once, recovered two ways, one of them drift-prone. The clean
reading of your rule is that the classifier the lexer already ran is the authority and
neither consumer should re-decide — but the fact does not live on the node to read,
so closing it is a choice, not a field access.

## §2 — the scalar names: a list beside the registry

`Checking/Sort.cs:51` decides scalar-from-named:

```csharp
private static readonly HashSet<string> scalars = ["number", "text", "truth"];
// Sort.Of, :69-71
Node.Name { Words: "error" } => new Error(),
Node.Name name when scalars.Contains(name.Words) => new Scalar(name.Words),
Node.Name name => new Named(container(name.Words), name.Words),
```

That set is the language's supplied scalar types, which the registry already declares —
`Resolver.cs:1989-1993`, `Descriptor.Spelled("The type of numbers.", "number") with {
Kind = Type }`, and so for `text`, `truth`, `error` — and `SymbolTable.SuppliedTypes`
already derives `{error, number, text, truth}` from them. `scalars` is a **hand-kept
copy** of that list minus `error`. Add a supplied scalar — the docstring eyes `date` —
to the registry, and `Sort.Of` files it as a user `Named` type in silence, the exact
drift the sweep is about.

The catch, and why it is a question: the registry does **not** itself distinguish a
scalar from the bottom. All four supplied types carry `Kind = Type` and nothing more;
`error` being the bottom rather than a scalar is stated **only** in `Sort.cs`'s comment
and its `"error"` arm. So "read the registry" needs the registry to first say which
supplied types are scalars.

## §3 — what I need

**Q1 — where does a literal's kind live?** Two ways to close §1, and the choice is
yours because it is a representation decision:

- **Thread it.** Carry the `Lexicon.Literal` kind from the lexer onto `Node.Literal`
  (and through `Lexemes`), so both consumers READ it and neither re-classifies. Cleaner
  — the fact exists to read — but it touches the lexeme and node representation.
- **Re-read the one authority.** Leave the node as text and make `Evaluator.Value`
  re-lex through `Lexicon.Literal.Lex`, as `Sort.Denoted` does, so both recover it the
  same deterministic way and the hand-rolled second classifier goes. Smaller, and it
  removes the drift; it keeps a re-lex rather than a field read.

Which — carry the kind, or re-read the lexicon consistently?

**Q2 — how should a scalar be told from the bottom?** To retire the `scalars` copy,
`Sort.Of` must read scalar-ness from the registry, and the registry must first say it.
Options:

- **A marker.** A `Descriptor` field marking a supplied type a scalar (or marking
  `error` the bottom), so `Sort.Of` reads `SuppliedTypes` and asks the marker — the
  `Denotes` shape one step over, on the type side.
- **The bottom as the one exception.** Derive scalars as `SuppliedTypes` minus the one
  bottom, `error` special-cased where it already is. Smaller, but keeps `error`'s
  bottom-ness stated in `Sort.cs` rather than the registry.
- **Leave it.** The list is three words and changes rarely; the smell is real but the
  blast radius is one file.

Which reading of scalar-vs-bottom do you want in the registry, if any?

## §4 — what I do with each answer

- **Q1 = re-read:** I make `Evaluator.Value` re-lex through `Lexicon.Literal.Lex`; the
  hand-rolled classifier goes, both consumers read the one authority.
- **Q1 = thread:** I carry the kind onto `Node.Literal` (and the lexeme), and both read
  the field; I would surface where the lexeme representation resists it, if it does.
- **Q2 = marker / exception:** I add the marker, or the minus-`error` derivation, and
  `Sort.Of` reads the registry; the hardcoded set goes.
- **Q2 = leave:** I record it in the ledger as an accepted, bounded copy and move on.

Neither blocks anything; the five findings and the fourth are cut and green. These are
the sweep finishing its own recommendation — the two it found that are a decision
rather than a repair.
