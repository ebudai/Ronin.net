# Named lookups — decided: `match` is for inline arms, `@` for named tables

Budai's call, and it is the right one for a better reason than "it avoids a
reserved word". The division lines up exactly with where the compiler can check
exhaustiveness.

```ronin
var x      = match type of y [number = 3, text = 7] otherwise 0;
var garage = car garages @ the datsun otherwise nothing;
```

---

## 1. Why the alternative was expensive — for the record

The proposal was `match (_) to (_)`, so a named table could be matched
discriminant-first. It needed *something* between the operands, because
`match (_) (_)` has two adjacent free holes and nothing marks the boundary:

```
match the datsun car garages
      ^-------^ ^----------^        or   ^--^  ^---------------^
```

That split would be decided by whichever names happen to exist, which is the
silent-capture shape rather than a tie.

But `to` is among the most expensive words available. `RESERVED (0)` becomes
`RESERVED (1)` and it buys:

```
time to live      due to date      response to query      path to file
words to search   reply to id      distance to target     miles to go
```

— all illegal, everywhere, forever. `in` is no better: it is free today only
because the loop's declaring hole is pinned, and a free hole before it would
make it glue again. If a word were ever spent here, `against` was the cheapest
candidate I could find. None was needed.

## 2. Why the split is principled, not a compromise

`MATCH.md` §3 made exhaustiveness fall out of the type: arms covering every case
give `T`, arms missing one give `optional T`. That holds **only where the
compiler can see the arms.**

| form | arms visible? | exhaustiveness |
|---|---|---|
| `match x [ … ]` — inline literal | **always** | checkable. Missing a case is a type error; a redundant `otherwise` is unreachable |
| `table @ x` — named or computed | not necessarily | always `optional T`; `otherwise` always required |

So restricting `match` to inline literals means **every `match` in the language
is statically exhaustive-checkable**, with no runtime-built-table caveat to
document and no diagnostic that has to explain why this match can be checked and
that one cannot.

And `@` is honest in the other direction: you are indexing a table, the key may
not be there, `otherwise` is required, and nobody is surprised.

Each construct ends up with exactly the diagnostic it can support. That is worth
more than the reading order the `to` form would have bought.

## 3. What this fixes on the other side

`MATCH.md` §5 flagged that a dynamically-built lookup could not be
exhaustiveness-checked, and that the difference should be visible rather than
inferred. Under this decision the problem does not arise: the case that cannot
be checked is spelled with a different operator, so the distinction is in the
source rather than in a compiler note.

That paragraph of `MATCH.md` can be struck.

## 4. What stays open

Nothing new. The two items from `MATCH.md` §6 are unaffected:

- whether `type of y` — an open universe, never exhaustive — is distinguished
  from an ADT case set, which is closed. Under this decision it matters *more*,
  not less, because `match` now promises checkability and only one of the two
  can deliver it;
- duplicate keys in a lookup literal are a finding.

And §5's payload rule stands: an arm is a delegate applied to the case's
payload, with a constant arm being the zero-payload case, which works because
reading a zero-argument delegate invokes it.
