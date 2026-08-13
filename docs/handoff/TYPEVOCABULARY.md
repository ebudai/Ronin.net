# The type table has no vocabulary — two questions that gate slice 1

`TAIL-SUGAR` closed the action-marker gap, so the type checker is next. Its first
slice is the one `Compilation` has been describing to itself for a while:

> *"Types resolve against a table that does not exist yet, and reading them
> against the wrong one is worse than not reading them at all."*

Two things must be ruled before that table can be built. Neither is something the
code can answer, and picking either silently would bless a spelling as the
language's or commit the checker to a shape.

---

## 0. What the tree says, probed rather than assumed

**Type declarations already work.** `type Money;` twice is `Shadowed`, and
`type Money;` beside `var Money => Number;` is `Shadowed` — so a type takes part
in the declaration table and its refusals today. I had assumed the opposite and
was wrong; this is from a probe.

**Type annotations are never checked.** This compiles with **zero findings**:

```ronin
var x => Nonexistent Type Name;
```

**No type name is known to the compiler at all.** Not one. `Number`, `text` and
`money` are, today, runs of words nothing looks at — which is why the above is
clean and why nothing has forced the questions below.

---

## 1. Which spelling, and is a type name case-sensitive?

The repository disagrees with itself. Annotations in the test sources:

```
  => Number     518
  => number      16
  => Text        11
  => money       11
```

And `docs/spec/introduction.md` is lowercase throughout:

```ronin
var name           => text;
var interest rate  => number;
var cash on hand   => money;
```

`SymbolTable.Names` is an ordinal `HashSet<string>`, so names are
**case-sensitive** as things stand and `Number` is not `number`. The moment an
unknown type is a finding, whichever case is omitted starts failing — 529
annotations one way, 27 plus the spec the other. There is no reading where both
survive without deciding that a type name is case-insensitive, which would make
types the only case-insensitive thing in a language whose names are otherwise
exact.

> **Needed: the built-in type names, their spelling, and whether type names are
> case-sensitive.** Whichever loses gets corrected in the same change — the spec
> examples or the fixtures.

My weak preference, offered only because a preference was asked for: lowercase,
matching the spec and reading like the rest of the language — `var cash on hand
=> money` is of a piece with multi-word lowercase names, where `=> Money` is not.
It is the larger edit and the smaller inconsistency.

## 2. Is a parameterised type a type PATTERN, resolved by the same resolver?

This one decides the shape of the checker's front half, so it wants ruling before
the code rather than after.

`list of number`, `lookup of text number`, `optional number` are runs of words
with arguments in them. That is exactly what a pattern is, and `FIVE-RULINGS` §5
already made `optional` **the pattern `optional (_)`** rather than a modifier
keyword. If types are patterns, then a type annotation is a statement resolved
against a type-side `SymbolTable`, and the whole existing machine applies without
a line of new theory:

- ambiguity in a type annotation is an **error**, on the same terms as anywhere
  else, rather than a silent pick;
- it arrives with **bracket repairs** already — `list of lookup of a b` has more
  than one reading and the repair search answers it for free;
- `R6`, the glue rules and the name rules apply to type patterns as declared,
  which is what stops a user type colliding with `list of (_)`;
- one resolver, one ambiguity story, one repair story, two tables.

The alternative — a bespoke type-expression parser — is a second grammar with a
second ambiguity policy, which is the failure this project has paid for
repeatedly.

> **Needed: are parameterised types type patterns resolved against a type-side
> symbol table by the existing resolver?** If yes, what does the prelude declare —
> `list of (_)`, `lookup of (_) (_)`, `optional (_)`, and what else?

I have deliberately not built the pattern half while this is open, because it
presupposes the answer.

## 3. Smaller, and answerable with the above

- Is `type X;` alone enough to make `X` usable in an annotation, or does a type
  need a definition before it may be named? (Today the declaration is recorded
  either way.)
- Does an unknown type name report at the annotation, or is it deferred to the
  use site the way `OVERLOADS` defers the overload ambiguity?

## 4. What is waiting on this

| | |
|---|---|
| slice 1 | the type table, annotations resolved against it, unknown/ambiguous type findings |
| then | declared parameter and return types captured — `Function.Returns` is parsed and **dropped** today, with no reader anywhere |
| then | `TAIL-SUGAR`'s determination, which needs to know `print x` is an action call, which needs declared return types |
| then | `E` §5 and §7, and the three expiry-ledger rows that narrow when the checker lands |
