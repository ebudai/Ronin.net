# The brace decision — the exponential is real, and `if` did not cause it

> **Ledger** — `[R]` The brace decision — the exponential is real, and `if` did not cause it
> supersedes: not yet checked
> superseded by: not yet checked

Answering `BRACEDECISION.md`. §4 says the implementation's brace handling is
mine to describe, so this is that, measured.

**The proposal is better supported than the document argues.** §3 does not
prevent a future ambiguity; it removes a cost that is in the compiler now.

## 1. §1's attribution is wrong, and the comment in `Parser.cs` says why

> Before blocks were expression-valued, position disambiguated them … Making a
> block an expression put all three in the same position.

Blocks are **not** expression-valued. `IFASEXPRESSION.md` §4 is unbuilt — `var x
= if c { 1 };` is still `Malformed`, and `{ 1 }` is unambiguously a `List`. So
the ambiguity §1 describes has not arrived.

The exponential has, and it is independent of it. `Parser.MaxGroups` already
records the cause:

> Three productions open on `{` — `Lookup`, `List`, and `Association` through the
> value inside a lookup — and each re-parses the whole nested body before it can
> tell whether it matched, so the cost of a brace nest is exponential in its
> depth — twelve levels of `{` took ten seconds.

None of those three is the block. And in statement position `Statement.Parse`
tries `Value.Parse` — so `Lookup`, then `List` — *before* `Scope.Parse`, so a
brace that opens a block is already speculatively parsed as two other things
first. The contest §1 expects `if` to create is running today.

## 2. Measured

A nest whose body fails **late**, so each production must parse the whole thing
before it can tell:

```
{{{{ 1 2 }}}}   depth  4     13 ms
     …          depth  6     11 ms
     …          depth  8    170 ms
     …          depth 10    599 ms   ← refused, not parsed
     …          depth 12    603 ms   ← refused, not parsed
```

Steeply superlinear from 6 to 10, then flat — because `MaxGroups` cuts in and
the file is *refused* rather than parsed slowly. That is the bound working as
its comment says it should: at depth 10 the result stops being a `Basic` and
becomes `Unknown` with a `Malformed` finding.

A nest that succeeds at every level costs nothing — `{{{{ 1 }}}}` at depth 11 is
4 ms — because `List` matches immediately and nothing backtracks. So the cost is
paid by *failing* input, which is exactly the hostile case the budget exists for
and exactly the case a one-parse-one-decision grammar would make impossible.

## 3. §4's two checks

**a. Does any pattern want a list argument in a braced hole?** No current one
can: `Pattern.Parse` takes words and free holes only, so a braced hole cannot be
declared at all — `match _ {_}` and `match _ [_]` are both refused as renderings
rather than declarations. The *planned* one is `MATCH.md`'s arms, and this
answers a discrepancy I raised there: `MATCH.md` spells them `[ number = 3 ]`,
which is malformed today and is precisely §3's lookup spelling. That document
was written assuming this decision had landed. Under §3 it is consistent; under
the status quo it is not. They should land together or the earlier one should be
restated.

**b. Does `[` leading a reference collide with the indexer?** No, measured:

```
[ 1, 2 ] [0] + 3            parses, one reference
var x = [ 1, 2 ] [0] + 3;   parses
```

§4.7's "what may lead decides what may follow" holds here already, with `[`
leading a list and `[` after a value an indexer, in the same statement. The rule
predating the indexer turns out not to matter.

## 4. What I would add to the cost side

§3 says the loss is that `{ key = value }` reads better than `[ key = value ]`.
Two things belong beside it:

- every brace in the spec, the guide and the tests moves, and the tests are
  where the real count is — but it is mechanical and a failing test is a loud
  failure, so this is effort rather than risk;
- `MaxGroups` and its comment become mostly historical. Worth keeping the
  measurement in the commit that removes it, because "we used to be exponential
  here" is the kind of thing that gets re-introduced by someone adding a fourth
  production on a bracket.

## 5. Documents still absent

Asked for, so the full sweep of every `NAME.md` referenced across
`docs/handoff/` and not present:

| document | cited by |
|---|---|
| `POSTFIXDIAGNOSIS.md` | `LEFTASSOCIATIVEWORDS.md` §5 — named as diagnosing the port divergence, which I have since diagnosed independently |
| `WHYNOPOSTFIX.md` | `POSTFIXPATTERNS.md` — superseded entirely, but its §2 measurement is said to stand |
| `WHYSYMBOLINFIX.md` | `LEFTASSOCIATIVEWORDS.md` — withdrawn |
| `DOTNETSCHEDULER.md` / `DOTNET-SCHEDULER.md` | `INSTANCESDIRECTION.md` §1 — its §3 ordering withdrawn, its threading numbers said to stand |
| `INTERVALSANDINDEXING.md` | `POSTFIXPATTERNS.md` §9 — its "ladder" section amended |
| `RONINGRAMMAR.md` | `POSTFIXPATTERNS.md` §9 — R7 said to be unaffected |
| `FUZZ-BRACKETS.md` | one of `FUZZBRACKETS(1).md`, `FUZZBRACKETS_new.md`, `FUZZBRACKETS_old.md` is presumably it under another name |

Of these only `POSTFIXDIAGNOSIS.md` is on a critical path, and now only for
comparison rather than for information.
