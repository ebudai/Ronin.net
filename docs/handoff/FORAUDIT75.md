# For re-audit — the finding-composition ruling, executed

> **Ledger** — `[R]` Requests re-audit of `954b49d..9b33d52`, against `FINDINGCOMPOSITION`. The reaction-composition question that `FORAUDIT74` flagged is ruled and executed: in a `when`, a value-return in an argument reports `AnsweringReaction` only — `Unreachable` steps aside, because a value-return is inadmissible there and every repair removes it and its unreachability together (admissibility precedes behaviour). And the finding is re-anchored: its subject is the CALL that never runs, not the `return` that prevents it. This revises the `Unreachable` finding `FORAUDIT74` introduced, so the two should be read in sequence.
> supersedes: none
> superseded by: none

**From:** the successor, at `9b33d52`. `FORAUDIT74` shipped §3's `Unreachable` and
flagged one open question for a ruling — does the dead-call finding compose with
`AnsweringReaction` in a reaction, or step aside? `FINDINGCOMPOSITION` answers it and
adds a span correction. Both are executed here.

## For audit

- **Range:** `954b49d..9b33d52` (2 commits; `9b33d52` is doc/ledger recording).
- **Against:** `FINDINGCOMPOSITION` (which builds on `UNRESOLVEDRETURNAMENDMENT` §3).
- **Note:** this modifies the `Unreachable` walk from `FORAUDIT74`. If auditing them
  in order, `FORAUDIT74`'s §3 is the introduction and this is its revision.

## The two changes

| § | change | how |
|---|---|---|
| §2–§3 | in a `when`, `Unreachable` is **suppressed**; `AnsweringReaction` alone reports | `Checking` now carries whether its scope is reacting; `Unreachables` is not run for a reacting scope. A value-return is inadmissible in a reaction, so every repair of the `AnsweringReaction` removes the return and its unreachability with it — the dead-call finding is strictly derivative, not a peer |
| §4 | the finding names the **call that never runs**, not the `return` | `Reach` carries the ENCLOSING call and raises the finding on it. `send return 5` now reports at `send` (col 24), not at the `return` (col 29); the message follows — "This call is never reached: one of its arguments is a «return»…" |

## Why not the precedent's reason (the ruling's own point)

`Unanswered` steps aside for `Unresolved` for an **epistemic** reason — an unread body
means it cannot know its answer. `Unreachable` is not in that position: it sees the exit
perfectly well, and both findings are **true**. The reason that holds is that the second
is **strictly derivative** — a `when` cannot legally contain a value-return, so there is
no repair of the `AnsweringReaction` that leaves the `Unreachable` standing. Cleaner than
the precedent, where suppression only *defers*.

## The general rule, now on record

> **Admissibility precedes behaviour.** A finding about what a construct *does* is
> suppressed by a finding that the construct *may not be there*.

Implemented for the `when` case only (its stated scope). It predicts the next case — a
type error on an expression suppressing a dead-code finding about the same expression —
which is **not** built here; it is named in the ruling as a prediction, not a request.

## Where it still fires — the carve-out is the `when`, not the rule

```ronin
  function f => number { send return 5; }   -- the return is LEGAL here
```

Nothing is inadmissible, so nothing suppresses, and the call's unreachability is the whole
finding. Re-anchoring makes its subject and its span agree — the span defect the ruling's
§4 warned about now cannot hide, because the function case shows it plainly.

## Tests

- `TypeAnnotations.AReturnEvaluatedAsAnArgumentMakesTheEnclosingCallUnreachable` — now at
  `send` (col 24), with a note that the subject is the call, not the return (col 29);
- `TypeAnnotations.InAWhenTheDeadCallFindingStepsAsideForTheInadmissibleReturn` — new: a
  value-return in a `when` reports `AnsweringReaction` and **no** `Unreachable`;
- `AReturnNestedThroughCallsIsOneFinding` — at the inner `send` (col 29), the call the
  return is a direct argument to;
- `TheReturnIsReachedThroughAListArgumentAndThroughBothOperands` — list case at `send`;
- the two `Ambiguities` reaction controls — `AnsweringReaction` per site, `Unreachable`
  now suppressed (`DoesNotContain`);
- the every-kind corpus/golden — the `Unreachable` example re-spanned and re-worded.

## Gate at `9b33d52`

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1352` (Release).
- Coverage **100 %** line and **100 %** branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Sabotage-verified: anchoring at the return instead of the call fails the span control; running
`Unreachables` in a reacting scope fails the carve-out and reaction controls. Each was caught, then
restored. `FINDINGCOMPOSITION` §5's standing-rule clause — *also ask when a decision sets a
precedent* — is recorded in `STANDINGAUTHORITY` §2.
