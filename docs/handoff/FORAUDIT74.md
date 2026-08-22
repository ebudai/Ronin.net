# For re-audit — the unresolved-return amendment, executed

> **Ledger** — `[R]` Requests re-audit of `eccead6..232fcb8`, against `UNRESOLVEDRETURNAMENDMENT`. The amendment's three additions and one scope correction are executed: §1 (per-callable `Unanswered` suppression) was already correct and gains the missing control; §2's principled reason is on record; §3 is a new `Unreachable` finding — a `return (_)` in a strict argument position makes the enclosing call dead code; §4 (`otherwise` short-circuits) is confirmed and, it turns out, needs no explicit scoping because an operation is never itself an argument. One adjacent composition is flagged for a ruling, not settled here: in a `when` reaction `Unreachable` co-fires with `AnsweringReaction`.
> supersedes: none
> superseded by: none

**From:** the successor, at `232fcb8`. `REAUDIT73` signed off the diagnostic-rendering
tail (`ed49e6c..c928691`) and noted the amendment had appeared untracked and outside
its range, with §3/§4 left as separate future items. This is that separate work,
executed and offered for signoff.

## For audit

- **Range:** `eccead6..232fcb8` (3 commits; `232fcb8` is doc/ledger recording only).
- **Against:** `UNRESOLVEDRETURNAMENDMENT` (and `UNRESOLVEDRETURNRULING`, which it amends).

## The amendment, item by item

| § | kind | what the amendment asked | what changed | commit |
|---|---|---|---|---|
| 1 | correction | `Unanswered` suppression is per **callable**: a nested delegate's unresolved reading must not swallow the enclosing function's true `Unanswered`. Add the control missing from the ruling's §7 | **No behaviour change** — the guard already filters `Owner == function`, and REAUDIT63 finding 2 separated delegate ownership. Added the control test and stated the per-callable scope in the guard comment | `1285f72` |
| 2 | addition | record the *principled* reason for the `Unanswered`/unresolved mutual exclusion | the comment now says it: `Unanswered` is a claim about the **whole** body, and an unread body supports no such claim | `1285f72` |
| 3 | addition | a `return (_)` in a strictly-evaluated argument position makes its enclosing call **unreachable** — a finding | new `Unreachable` finding, raised at each such return by a walk (`Reach`) over resolved references | `fc8614c` |
| 4 | qualification | scope §3 away from `otherwise` — its right side is a live guard **if** `otherwise` short-circuits | confirmed it does (`Evaluator.Apply`), and the scoping falls out of the grammar (below) — no `Catches` test is written | `fc8614c` |

## §3 in detail — the walk and its scope

`send return 5` reads as `send (return 5)`; evaluating `send`'s argument exits the
body with `5`, so `send` is never called. `Reach` marks the **strict argument
positions** — a call's arguments, and the parts of a list standing as one — and
reports a `return (_)` sitting in one. One finding per return.

- **Nesting** recurses: `send send return 5` is one finding, on the inner return.
- **A dead call buried in an operand** is still found: `(send return 5) is x`. The walk
  descends operation operands to reach the call, and the call resets the strict context
  for its own arguments.
- **A bare `return` statement** is untouched — it is not in argument position.

## §4 — why no `otherwise` scoping is written

The amendment asked that §3 be scoped to strict positions "or it fires on the guard,"
and asked whether `otherwise` short-circuits. It does — `Apply` evaluates the right
operand only when the left is caught. But the scoping the amendment feared turns out to
be **unnecessary**, because it is unreachable:

> An operation is never itself an argument. `send (a otherwise b)`, `send [a otherwise b]`,
> and every operation-in-argument form **do not parse** (`Malformed`/`Unresolved`).

So an operation's operands are never reached in a strict position, and a `return`
standing directly in one — `total otherwise return 0`, the guard — is never in
argument position and never flagged. A `Catches is null && strict` branch would have a
`strict`-true arm no input can reach; under the 100 %-branch discipline it is not
written. The `Reach` remarks record that if operations ever become arguments, the
`otherwise` scoping must return there. **This is the one place the implementation departs
from the amendment's literal shape — please scrutinise it.**

## Adjacent composition, flagged for a ruling (not settled here)

In a `when` reaction, `return` is already illegal (`AnsweringReaction`). `Unreachable`
now **co-fires** on the same span, so `when ready { send (return 1) to (return 2); }`
raises both kinds, per site. I implemented the amendment's **stated** scope — a return in
a strict argument position is a finding, no reaction carve-out — and did **not** add
suppression I was not asked for. But the established philosophy (ruling §5, amendment §2:
"one finding, not a contradiction stacked on top") could argue `Unreachable` should step
aside where the return is already an illegal exit, exactly as `Unanswered` steps aside for
`Unresolved`. **Whether it should is a composition question for the ruling.** The two
`Ambiguities` controls now assert both kinds explicitly, so the behaviour is pinned either
way.

## Tests

New, in `TypeAnnotations`:

- `AReturnEvaluatedAsAnArgumentMakesTheEnclosingCallUnreachable` — the witness, at its span;
- `AReturnInAStatementOfItsOwnAnswersTheBodyAndIsNotDead` — a bare return is not flagged;
- `OtherwiseReturnIsALiveGuardNotAnUnreachableCall` — §4, the guard is live;
- `AReturnNestedThroughCallsIsOneFinding` — one finding, on the inner return;
- `TheReturnIsReachedThroughAListArgumentAndThroughBothOperands` — list argument and both operands.

New, in `TypeAnnotations`: `ANestedDelegatesUnresolvedReadingDoesNotSuppressTheFunctionsUnanswered`
(§1's control — one `Unanswered` on `f`, one `Unresolved` on the delegate).

Updated: the two `AFunctionThatDeclaresAValueAnswerMustProduceOne` controls that once asserted
`send return 5` "compiles cleanly" now assert their real subject — no `Unanswered`, because a value
return at depth keeps the promise; the call is separately `Unreachable`. The every-kind corpus and
golden gain an `Unreachable` example. The two `Ambiguities` exit-site controls select by kind.

## Gate at `232fcb8`

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1351` (Release).
- Coverage **100 %** line and **100 %** branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Sabotage-verified: firing on every return (dropping the `strict` guard) fails the statement-return
and guard controls; making an operand strict fails the guard; stopping the list recursion fails the
list-argument control. Each was caught, then restored. `§8` (statement-initial `return`) remains a
separate deferred ruling and is untouched.
