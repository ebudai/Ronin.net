# For re-audit — REAUDIT74 and REAUDIT75, both findings closed in one commit

> **Ledger** — `[R]` Requests re-audit of `f1a9931..9fc8b20`, against `UNRESOLVEDRETURNAMENDMENT` (§3) and `FINDINGCOMPOSITION` (§2–§3). Both open high findings are cut, and they were the same shape: a fact the dead-call walk consults was not threaded the way its parallel fact already is. REAUDIT74 — `Reach` visited lookup VALUES only, so a `return` in a lookup KEY was silent; it now visits keys too, in evaluation order, reading the declared kind. REAUDIT75 — reaction ownership stopped at the `when` body's own `Reacts` bit, so a nested `if`/loop/block lost the governing `AnsweringReaction` and kept the derivative `Unreachable`; ownership now follows transparent scopes and resets at a delegate, the exact mirror of the `within` partition beside it.
> supersedes: none
> superseded by: none

**From:** the successor, at `9fc8b20`. `REAUDIT74` and `REAUDIT75` each signed off
most of their range and left one high finding open. Both are in the `Unreachable`
machinery, both are a context not carried through the tree, and both are fixed here in
one commit. Reproduced by execution before repair, in both directions, and
sabotage-verified after.

## For audit

- **Range:** `f1a9931..9fc8b20` (1 commit).
- **Against:** `UNRESOLVEDRETURNAMENDMENT` §3 (REAUDIT74) and `FINDINGCOMPOSITION` §2–§3 (REAUDIT75).

## The two findings, cut

| audit | sev | finding | fix |
|---|---|---|---|
| REAUDIT74 | high | `Reach`'s group arm recursed through `part.Value` only. A lookup `[k = v]` evaluates its **key** too, so `send [return 5 = 1]` exits `f` before `send` runs — yet was silent, while the value form `send [1 = return 5]` reported it | the arm now visits **key before value**, in evaluation order, and only where the **KIND** has keys (`Lookup`/`Keyed`) — the same fact `Node.Group.Flattened` reads, not `part.Key is null`, whose own comment warns against re-deriving keyed-ness from the field |
| REAUDIT75 | high | `Checking.Reacting` came from the scope's own `Reacts` bit, which a transparent nested block resets. `when ready { if ready { send return 1; } }` lost the `AnsweringReaction` **and** gained the derivative `Unreachable` — inverting the ruling's priority | reaction ownership now follows the **same partition return ownership does**: a transparent block inherits `reacting`; a delegate resets it. Computed `body.Reacts \|\| (body.Delegate is null && reacting)` — the exact mirror of `within: body.Delegate ?? home` on the line beside it |

## Why they were one bug

Both checks — `AnsweringReaction` (from `Exits`) and the `Unreachable` suppression (from
`Checking.Reacting`) — already read the **same** local `reacting`. So the fix is at the
source: thread `reacting` correctly through the scope tree, and both checks are correct
together and cannot disagree again — which is what REAUDIT75 asked for in as many words.
The lookup-key miss is the same failure on the other axis: the strict walk not visiting
every child the evaluator does.

## Boundaries preserved (the controls that must not move)

- **The delegate is its own callable.** `when ready { var c = () => { send return 1; }; }`
  keeps its `Unreachable` and raises no `AnsweringReaction` — the return answers the
  delegate, which is legal. REAUDIT75 flagged this as the boundary not to blanket-suppress.
- **The lookup key is traversed, not special-cased.** `send [send return 5 = 1]` reports
  the **inner** `send` as the dead call, reached through the key — a nested call, not a
  bare return, so the key is genuinely walked.
- **Ordinary lists and groups still visit values only** — no key exists to walk.

## Tests

- `TypeAnnotations.ALookupEvaluatesItsKeySoAReturnInOneIsAStrictArgumentToo` — key, value,
  nested-call-in-key, and a clean lookup;
- `TypeAnnotations.TheReactionCarriesThroughATransparentBlockAndADelegateResetsIt` — `if`,
  `while`, and a plain block each report `AnsweringReaction` and no `Unreachable`; a nested
  delegate reports `Unreachable` and no `AnsweringReaction`.

## Gate at `9fc8b20`

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1354` (Release).
- Coverage **100 %** line and **100 %** branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Sabotage-verified in all three directions: dropping key traversal fails the lookup-key control;
reverting the reaction to `body.Reacts` fails the transparent-block control (the inversion returns);
dropping the delegate reset fails the delegate control. Each was caught, then restored.
