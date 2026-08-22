# For re-audit — REAUDIT69's finding closed

> **Ledger** — `[R]` Requests re-audit of `350bb56..46c02b8`. REAUDIT69's one adjacent low finding — the `Unresolved` message quoted the raw source slice, so a multiline reference carried a newline into the one-line diagnostic — is cut: the finding quotes the reference's canonical words, rendered from its lexemes, span unchanged. Ruling (A) itself was signed off; this is the last presentation detail.
> supersedes: none
> superseded by: none

**From:** the successor, at `46c02b8`. `REAUDIT69` confirmed `UNRESOLVEDRETURNRULING`
(A) closes the whole REAUDIT65–68 series and accepted all four verification items,
leaving one low-severity presentation finding. It is reproduced and cut; this asks
for signoff on the range since.

## For audit

- **Range:** `350bb56..46c02b8` (2 commits).
- **Against:** `UNRESOLVEDRETURNRULING`.

## The finding, cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | low | the `Unresolved` message sliced raw source (`Source.Text.Substring`), so a reference crossing a line — `nope` newline `more` — carried the newline and indentation into the one-line diagnostic, and quoted whitespace instead of words | `7d11000` | the `Reading` carries the reference's canonical rendering (`lexemes.Render()`, from the lexemes `Read` already resolved, trivia gone); the finding quotes that, as `UnknownType` / `Unanswered` / parse errors already do. The primary span is unchanged |

## Maintained regression (the auditor's coverage)

`AnUnresolvedReferenceRendersCanonically` (`TypeAnnotations`), on a reference split across two
lines:

- `Words` is canonical — `nope more` — with no trivia or line break;
- `Diagnostics.Report` contains no source-derived newline (one physical line); and
- `Primary` still spans the complete original reference (all 13 characters, both lines).

A single-word `nope` still renders `nope` (the golden-file case is unchanged), and a composite
`send (customer return policy) nope` renders with its brackets intact.

## Gate at `46c02b8`

The project gate — CI `.github/workflows/build.yml`, local battery `TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1334` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Reproduced by execution before repair and sabotage-verified after: reverting to the raw slice
makes the multiline `Words` carry the newline and the test fails; restored. No item is left open —
the return-classification series is closed, and `§8` (statement-initial `return`) is recorded as a
separate deferred ruling, not implemented.
