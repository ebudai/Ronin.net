# Named lookups — §2 is the reason; §1's measurement says the opposite of §1

> **Ledger** — `[R]` Named lookups — §2 is the reason; §1's measurement says the opposite of §1
> measured at: e70c067

Answering `MATCHNAMED.md`. The decision is not in question here: §2 stands on
its own and is the strongest argument in either match document. §1 is the
supporting reason, and the compiler disagrees with it.

## 1. Adjacent free holes tie. They do not capture

§1 says the `match (_) to (_)` form was needed because `match (_) (_)` splits by
"whichever names happen to exist, which is the silent-capture shape rather than
a tie". Measured, with the pattern actually declared:

```
Pattern.Parse("match _ _")                 ->  match (_) (_)

names {the datsun, car garages}
  match the datsun car garages   ->  Resolved   match «the datsun» «car garages»

names {the datsun, car garages, the, datsun car garages}
  match the datsun car garages   ->  ambiguous at 3 lookup(s) — bracket an
                                     argument to choose:
                                         match «the» «datsun car garages»
                                         match «the datsun» «car garages»
```

It is a tie, and the compiler already answers it with the sentence the language
answers every ambiguity with. The first case is not capture either — it is one
reading because there is only one.

Nor can it become capture. The pattern has two holes, so every split binds two
names and costs two lookups: the readings are cost-invariant, exactly as
`POSTFIXPATTERNS.md` §4 argues for prefix∘postfix. A later declaration can turn
a working statement into a **tie error**, which is a compile failure appearing,
not a wrong program running. Silent capture needs a strictly cheaper wrong
reading, and there is none available.

So `match (_) (_)` is safe by the same argument the postfix document makes. It
would be *unpleasant* — a bracket on most uses — and that is a real objection.
It is not the objection §1 makes.

**Why this is worth correcting rather than letting stand.** The conclusion is
right and the reason is not, which is the shape that has cost this project three
documents already: the left-recursion comment, `WHYNOPOSTFIX.md`, and
`WHYSYMBOLINFIX.md` were each a sound conclusion resting on a measurement that
said something else. Each survived because nobody re-ran it. §2 does not need
§1, so nothing is lost by striking it.

## 2. `@` does not exist

```
a @ b     ->  no parse
```

Not a lexeme, not in the operator table. Half the decision — "`@` for named
tables" — is a syntax the compiler has never seen. Adding it is small: it is a
symbol, so it needs a `Symbol.Special` if it is more than one character (it is
not) and an entry in `Builtin.Operators`, which is now the one table that gives
an operator both halves at once.

Its binding power has to sit **above** `otherwise` (6) so that `car garages @
the datsun otherwise nothing` groups as `(car garages @ the datsun) otherwise
nothing`, and above the pattern level (7) is where indexing belongs — 20, beside
`*`, borrows a level that already exists and costs no new column in the table.

## 3. `nothing` does not exist either

```
nothing              ->  no parse
a otherwise nothing  ->  no parse
```

There is a `Nothing` value in the runtime — it is what a `let` seeds with and
what `otherwise` catches — and no way to write it. `Literal.cs` lexes dates,
numbers and text; `nothing` is an ordinary word, so in `otherwise nothing` it is
an undeclared name.

That matters more under this decision than before, because §2 makes `otherwise`
**required** on every `@`, and the document's own example spells the fallback
`nothing`. The most common fallback in the language is currently unwritable.

## 4. What that leaves

| item | state |
|---|---|
| `match` restricted to inline literals | a decision, unblocked, and it needs §1 of `MATCH-RESULT.md` first — the arms are a bracketed hole kind that cannot be declared |
| `@` | unbuilt, small, and specified above |
| `nothing` as source | unbuilt, smaller, and now on the critical path |
| exhaustiveness as typing | needs a type checker, which does not exist |
| duplicate keys are a finding | unbuilt, and independent of all of the above |

Of these, `nothing` and duplicate keys are the two that could be built today
without another design decision. `@` needs only its binding power agreed, and
§2 above proposes one.
