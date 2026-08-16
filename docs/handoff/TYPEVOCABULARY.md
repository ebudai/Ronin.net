# The type vocabulary — four answers, and one correction before you build

> **Ledger** — `[V]` The type vocabulary — four answers, and one correction before you build
> supersedes: none
> superseded by: none

**Lowercase, case-sensitive. Yes to type patterns.** But §2 contains a phrase
that would build the wrong thing, and it is the most important line here:
**one table, not two.**

---

## 0. First, the measurement I ran and what it was worth

I tried to settle §1 on the corpus, case-sensitively. It does not discriminate:
`number` and `Number`, `text` and `Text`, every pair — both spellings are in use
in a 460,030-identifier corpus. The only mildly relevant figure is that 33% of
identifiers are single-word and 67% multi-word.

So this is a taste question with a structural constraint, not a measurable one,
and I would rather say that than manufacture significance from a number that has
none.

## 1. Lowercase, and case-sensitive

**Your weak preference is right, and there is a stronger reason for it than the
spec.**

`FIVE-RULINGS` §4 ruled **one symbol table, entries carrying a kind.** So a type
name *is* a name, in the same table as every other name, subject to the same
rules. And Ronin names are lowercase multi-word runs. Capitalising types would
put **two capitalisation conventions inside one table** — which is the same shape
as two tables, arriving through spelling instead of structure.

The counter-argument is real and worth naming: capitalising the built-ins would
let `Number` the type and `number` the variable coexist, buying back the
collisions that one-table costs. I do not think it is worth it, because **Ronin
names are phrases by design.** A user wanting a money variable writes
`cash on hand`, `account balance`, `total due` — not bare `money`. The pressure
on single bare words is much lower here than in a language where `money` is the
natural variable name, so the thing capitalisation buys is mostly not needed.

And `var cash on hand => money` is of a piece with the line it sits in.
`=> Money` is not. In a language whose premise is readability, that is not a tie-
breaker, it is the argument.

**Case-sensitive: yes, keep it.** Making types the only case-insensitive thing
would be a rule with two answers depending on kind, which is the failure this
project keeps paying for.

**And notice the decision is robust to the other one.** If Ronin ever wants
case-insensitivity, that is a *whole-language* question — VB6 is case-insensitive
and the stated model is VB6, so it is a fair thing to want later. Lowercase
built-ins are the canonical form under either answer, so choosing lowercase now
does not prejudge it and does not have to wait for it.

**The 529 fixtures lose.** Larger edit, smaller inconsistency, and the reasoning
is one line: fixtures are cheap to change and a language's spelling is not. One
request — **make it a pure rename commit with nothing else in it.** 529 mechanical
edits are exactly where a test quietly stops asserting what it asserted, and a
diff containing only renames is the only way that stays visible.

## 2. Yes — and this was already ruled, which is a good sign

Your reasoning reconstructs `GENERICS.md` §1 independently:

> *"Type constructors should be patterns — measured. The type language does not
> need a new syntax. It needs the one that already exists… One resolver, two
> languages."*

Two people arriving at the same answer from different directions is the best
evidence available that it is the right one. Your list of what it buys — ambiguity
as an error, bracket repairs for free, R6 and the glue rules applying to type
patterns — is exactly right and is what §1 measured.

### The correction: one table, not two

You wrote *"a type-side `SymbolTable`"* and *"one resolver, two tables."*

**`FIVE-RULINGS` §4 ruled the opposite, and I reversed my own earlier lean to get
there.** `GENERICS-II` §8a said *"I would separate them"*; that was withdrawn for
three reasons, and the third has since been confirmed by your own bug:

- **`type of x` puts a type into a value position.** Position-selected lookup is
  what a second table depends on, and that is the case it cannot answer.
- **Every name rule would run twice** — R5′, R6b, R7b, self-ambiguity — and fail
  *silently* when they drift.
- **You found the evidence yourself.** *"Type annotations were being read against
  the value table… the prune I added is a stop-gap that stops the wrong answer,
  not a route to the right one."* The route to the right one is the kind field.

So: **a type position does not select a different table. It supplies an expected
`kind`, and the candidate set narrows on kind exactly as it narrows on type** —
`RETURN-AND-LITERALS` §4b. One pass, two predicates, the same code that does
overload narrowing and reading elimination. Your prune becomes the kind
predicate, in the filter rather than beside it.

If you build two tables the prune never becomes principled and §4 has to be
re-litigated after the checker exists.

### What the prelude declares

```
  number          text          truth
  error                      the named bottom type -- assignable to everything,
                             nothing assignable to it. Needed so «x is an error»
                             is writable  (ERROR-AS-VALUE §2)
  list of (_)
  optional (_)
  lookup of {_} {_}          BRACKETED holes -- «lookup of (text) (number)».
                             GENERICS §1: two adjacent free holes have the
                             adjacent-holes problem; bracketed costs nothing
```

**`money` is not a primitive.** The spec example uses it and someone will assume
it is one. It is a **strong alias over `number`**, and `UNITS-RESEARCH` §6 ruled
strong aliases a separate and earlier feature than units — so `money` belongs in
the library once aliases exist, not in the prelude now. Worth a line in the spec
example saying so, or it becomes a primitive by accident.

**No spelling for the action type**, per `FIVE-RULINGS` §2b and `TAIL-SUGAR` §1 —
it is what inference produces for a body with no return, and it is admissible in
no value position, so there is nothing for a user to write.

## 3. The two smaller ones

**`type X;` alone is enough to name `X` in an annotation. Yes.**

A *declaration* is what puts a name in the table; a *definition* is what gives it
structure. An opaque type you can name and pass but not construct is a useful
thing — it is what a library handle is — and refusing it would need the table to
carry a second state (declared-but-not-yet-usable) whose only job is to be an
error. Keep the table one-state.

**An unknown type name reports at the annotation, not deferred.**

The `OVERLOADS` deferral exists because that ambiguity *genuinely cannot be
resolved* until types are known — a real dependency. An unknown name has no such
dependency: at the moment the annotation is written, the table either has the
name or it does not.

And deferring costs the thing `Collection.Repeated` already gets right — *"a
literal with one key repeated four times is one mistake, and four findings saying
so are four copies of it."* One misspelled type used in twelve places is one
mistake. Report it where it was written.

## 4. Summary

| | |
|---|---|
| spelling | **lowercase** — a type name is a name, and Ronin names are lowercase phrases. Capitalising puts two conventions in one table |
| the corpus | **did not discriminate.** Both spellings are everywhere; this is taste plus structure, not measurement |
| case | **sensitive.** Types being the only case-insensitive thing is a rule with two answers |
| and it is robust | whole-language case-insensitivity stays available later; lowercase is canonical either way, so it does not gate this |
| the 529 fixtures | they lose. **Pure rename commit, nothing else in it** |
| parameterised types as patterns | **yes** — and `GENERICS.md` §1 already ruled it, which your reasoning independently reconstructs |
| **"two tables"** | **no — one table with a kind** (`FIVE-RULINGS` §4). A type position supplies an expected **kind**, filtered by the same pass as type. Your annotation prune *is* that predicate |
| prelude | `number`, `text`, `truth`, `error`, `list of (_)`, `optional (_)`, `lookup of {_} {_}` |
| `money` | **not a primitive** — a strong alias, and aliases are a separate earlier feature. Fix the spec example or it becomes one by accident |
| the action type | still no spelling |
| `type X;` alone | **usable.** Declaration puts it in the table; a second state would exist only to be an error |
| unknown type name | **at the annotation.** No dependency justifies deferring, and deferring turns one mistake into twelve findings |
